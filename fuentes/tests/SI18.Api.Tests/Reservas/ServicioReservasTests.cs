using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Reservas;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <see cref="IServicioReservas"/> para HU-03 y HU-07 (specs/002-reservar-pagar tasks.md T046; research.md E8;
/// RF-019a). Polideportivo Norte, «Pista de tenis 1» y «Pista de tenis 2», enero de 2027 (sin el 15/01, que usa
/// 000-core). <see cref="EnTransaccion{T}"/> hace de llamador, como lo harán HU-03 y HU-07: abre la transacción,
/// llama, guarda y confirma. Sirve de ejemplo de uso para Cancelaciones.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ServicioReservasTests(FabricaApi fabrica)
{
    private const string Norte = "Polideportivo Norte";
    private static readonly DateOnly Dia18 = new(2027, 1, 18);
    private static readonly DateOnly Dia19 = new(2027, 1, 19);
    private static readonly DateOnly Dia20 = new(2027, 1, 20);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Tenis1() => _datos.Pista(Norte, "Pista de tenis 1");

    private Task<int> Tenis2() => _datos.Pista(Norte, "Pista de tenis 2");

    private DateTime Hace(int minutos) => _datos.Ahora - TimeSpan.FromMinutes(minutos);

    /// <summary>
    /// Llamador de prueba: abre la transacción en el <see cref="Si18DbContext"/> del ámbito, llama a
    /// <see cref="IServicioReservas"/>, hace <c>SaveChanges</c> y confirma (o deshace, con <paramref name="confirmar"/>
    /// a <c>false</c>).
    /// </summary>
    private async Task<T> EnTransaccion<T>(Func<IServicioReservas, Task<T>> accion, bool confirmar = true)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var servicio = ambito.ServiceProvider.GetRequiredService<IServicioReservas>();

        await using var transaccion = await db.Database.BeginTransactionAsync(Ct);
        var resultado = await accion(servicio);
        await db.SaveChangesAsync(Ct);
        if (confirmar)
        {
            await transaccion.CommitAsync(Ct);
        }
        else
        {
            await transaccion.RollbackAsync(Ct);
        }

        return resultado;
    }

    private async Task<string> CodigoDeError(Func<IServicioReservas, Task> accion)
    {
        var error = await Assert.ThrowsAsync<ErrorNegocio>(() => EnTransaccion(async s => { await accion(s); return 0; }));
        return error.Codigo;
    }

    // ── CancelarPorUsuario (HU-03) ──────────────────────────────────────────────

    [Fact]
    public async Task Cancelar_una_reserva_de_otra_persona_da_NO_ENCONTRADO()
    {
        var id = await _datos.Confirmada(Ana, await Tenis1(), Utc(Dia18, 9), Hace(5));
        var bruno = await _datos.Usuario(Bruno);

        Assert.Equal(CodigosError.NoEncontrado, await CodigoDeError(s => s.CancelarPorUsuario(id, bruno, Ct)));
        Assert.Equal(EstadoReserva.Confirmada, (await _datos.Leer(id)).Estado);
    }

    [Fact]
    public async Task Cancelar_una_pendiente_da_TRANSICION_NO_PERMITIDA()
    {
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia18, 10), Hace(1));
        var ana = await _datos.Usuario(Ana);

        Assert.Equal(CodigosError.TransicionNoPermitida, await CodigoDeError(s => s.CancelarPorUsuario(id, ana, Ct)));
        Assert.Equal(EstadoReserva.PendientePago, (await _datos.Leer(id)).Estado);
    }

    [Fact]
    public async Task Cancelar_una_reserva_ya_empezada_da_RESERVA_YA_EMPEZADA()
    {
        var inicio = Utc(new DateOnly(2026, 12, 20), 10);
        var id = await _datos.Confirmada(Ana, await Tenis1(), inicio, inicio - TimeSpan.FromDays(1));
        var ana = await _datos.Usuario(Ana);

        Assert.Equal(CodigosError.ReservaYaEmpezada, await CodigoDeError(s => s.CancelarPorUsuario(id, ana, Ct)));
        Assert.Equal(EstadoReserva.Confirmada, (await _datos.Leer(id)).Estado);
    }

    [Fact]
    public async Task Cancelar_una_confirmada_propia_la_cancela_y_devuelve_los_datos_para_la_devolucion()
    {
        var inicio = Utc(Dia18, 11);
        var id = await _datos.Confirmada(Ana, await Tenis1(), inicio, Hace(5));
        var ana = await _datos.Usuario(Ana);
        var ahora = _datos.Ahora;

        var afectada = await EnTransaccion(s => s.CancelarPorUsuario(id, ana, Ct));

        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReserva.Cancelada, guardada.Estado);
        Assert.Equal(MotivoCancelacion.Usuario, guardada.MotivoCancelacion);
        Assert.Equal(ahora, guardada.CanceladaEnUtc!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(EstadoPago.Completado, guardada.Pago!.Estado);
        Assert.Equal(new ReservaAfectada(id, ana, inicio, guardada.Pago.Id, 6.00m), afectada);
    }

    // ── AplicarCierre (HU-07) ───────────────────────────────────────────────────

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Un_cierre_de_10_a_12_solo_afecta_a_las_franjas_de_10_y_11()
    {
        var pista = await Tenis2();
        var nueve = await _datos.Confirmada(Ana, pista, Utc(Dia19, 9), Hace(5));
        var diez = await _datos.Confirmada(Bruno, pista, Utc(Dia19, 10), Hace(5));
        var once = await _datos.Pendiente(Ana, pista, Utc(Dia19, 11), Hace(1));
        var doce = await _datos.Confirmada(Ana, pista, Utc(Dia19, 12), Hace(5));

        var resultado = await EnTransaccion(s => s.AplicarCierre(pista, Utc(Dia19, 10), Utc(Dia19, 12), Ct));

        Assert.Equal([diez], resultado.Canceladas.Select(r => r.ReservaId));
        Assert.Equal([once], resultado.Caducadas.Select(r => r.ReservaId));
        Assert.Equal(EstadoReserva.Confirmada, (await _datos.Leer(nueve)).Estado);
        Assert.Equal(EstadoReserva.Confirmada, (await _datos.Leer(doce)).Estado);

        var cancelada = await _datos.Leer(diez);
        Assert.Equal(EstadoReserva.Cancelada, cancelada.Estado);
        Assert.Equal(MotivoCancelacion.CierreMantenimiento, cancelada.MotivoCancelacion);
        Assert.Equal(EstadoPago.Completado, cancelada.Pago!.Estado);

        var caducada = await _datos.Leer(once);
        Assert.Equal(EstadoReserva.Caducada, caducada.Estado);
        Assert.Equal(EstadoPago.Fallido, caducada.Pago!.Estado);
    }

    [Fact]
    public async Task Un_cierre_caduca_las_pendientes_vigentes_y_no_vigentes_y_ordena_las_listas_por_inicio()
    {
        var pista = await Tenis2();
        var trece = await _datos.Confirmada(Ana, pista, Utc(Dia20, 13), Hace(5));
        var doce = await _datos.Pendiente(Bruno, pista, Utc(Dia20, 12), Hace(1));
        var once = await _datos.Confirmada(Bruno, pista, Utc(Dia20, 11), Hace(5));
        var diez = await _datos.Pendiente(Ana, pista, Utc(Dia20, 10), Hace(30));

        var resultado = await EnTransaccion(s => s.AplicarCierre(pista, Utc(Dia20, 10), Utc(Dia20, 14), Ct));

        Assert.Equal([once, trece], resultado.Canceladas.Select(r => r.ReservaId));
        Assert.Equal([diez, doce], resultado.Caducadas.Select(r => r.ReservaId));
        foreach (var id in new[] { diez, doce })
        {
            var caducada = await _datos.Leer(id);
            Assert.Equal(EstadoReserva.Caducada, caducada.Estado);
            Assert.Equal(EstadoPago.Fallido, caducada.Pago!.Estado);
        }

        foreach (var id in new[] { once, trece })
        {
            Assert.Equal(EstadoReserva.Cancelada, (await _datos.Leer(id)).Estado);
        }
    }

    // ── Transacción del llamador ────────────────────────────────────────────────

    [Fact]
    public async Task Sin_transaccion_abierta_lanza_InvalidOperationException()
    {
        var id = await _datos.Confirmada(Ana, await Tenis1(), Utc(Dia18, 13), Hace(5));
        var ana = await _datos.Usuario(Ana);
        var pista = await Tenis1();
        using var ambito = fabrica.Services.CreateScope();
        var servicio = ambito.ServiceProvider.GetRequiredService<IServicioReservas>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CancelarPorUsuario(id, ana, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AplicarCierre(pista, Utc(Dia18, 13), Utc(Dia18, 14), Ct));
        Assert.Equal(EstadoReserva.Confirmada, (await _datos.Leer(id)).Estado);
    }

    [Fact]
    public async Task Si_el_llamador_deshace_la_transaccion_no_cambia_nada()
    {
        var id = await _datos.Confirmada(Ana, await Tenis1(), Utc(Dia18, 12), Hace(5));
        var ana = await _datos.Usuario(Ana);

        await EnTransaccion(s => s.CancelarPorUsuario(id, ana, Ct), confirmar: false);

        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReserva.Confirmada, guardada.Estado);
        Assert.Null(guardada.CanceladaEnUtc);
    }
}
