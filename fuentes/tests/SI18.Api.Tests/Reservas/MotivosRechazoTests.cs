using System.Net;
using SI18.Api.Tests.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Cada motivo de RF-011 de 000-core y <c>PRECIO_CAMBIADO</c> llegan con su código y su texto, y no crean nada
/// (specs/002-reservar-pagar tasks.md T036; HU4, CE-006). Polideportivo Sur, «Pista de tenis 1» y «Pista de tenis 2»,
/// del 22 al 28/12/2026 (sin el 23/12, que usa <c>PrecioPorTarifaTests</c> de spec-001).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MotivosRechazoTests(FabricaApi fabrica)
{
    private const string Sur = "Polideportivo Sur";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Tenis1() => _datos.Pista(Sur, "Pista de tenis 1");

    private Task<int> Tenis2() => _datos.Pista(Sur, "Pista de tenis 2");

    /// <summary>Pide la reserva como Ana (tarifa de empadronada: 6,00 €).</summary>
    private async Task<HttpResponseMessage> Reservar(int pista, DateTime inicio, decimal importe = 6.00m)
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);
        return await PedirReserva(cliente, pista, inicio, importe);
    }

    /// <summary>El código y el texto del catálogo, y ninguna reserva en la franja (HU4 esc. 6).</summary>
    private async Task AssertRechazo(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo, int pista, DateTime inicio)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(codigo, problema.Codigo);
        Assert.Equal(CodigosError.Detalle(codigo), problema.Detail);
        Assert.Empty(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU4_esc1_a_8_dias_esta_fuera_de_plazo_y_a_7_dias_se_puede_reservar()
    {
        var pista = await Tenis1();
        var dentro = Utc(new DateOnly(2026, 12, 28), 21);
        var fuera = Utc(new DateOnly(2026, 12, 29), 10);

        var rechazo = await Reservar(pista, fuera);
        var creada = await Reservar(pista, dentro);

        await AssertRechazo(rechazo, HttpStatusCode.UnprocessableEntity, CodigosError.FueraDeAntelacion, pista, fuera);
        Assert.Equal("Solo puedes reservar desde hoy hasta dentro de 7 días.", (await Problema(await Reservar(pista, fuera))).Detail);
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU4_esc2_la_franja_de_las_18_se_puede_reservar_hasta_las_17_59_59()
    {
        var pista = await Tenis2();
        var dia = new DateOnly(2026, 12, 24);
        var inicio = Utc(dia, 18);

        async Task<string?> Crear(DateTime ahora)
        {
            try
            {
                await _datos.ConServicio(ahora, Ana, s => s.Crear(new PeticionReserva(pista, inicio, 6.00m), Ct));
                return null;
            }
            catch (ErrorNegocio error)
            {
                return error.Codigo;
            }
        }

        Assert.Equal(CodigosError.FranjaYaEmpezada, await Crear(inicio));
        Assert.Equal(CodigosError.FranjaYaEmpezada, await Crear(inicio + TimeSpan.FromMinutes(1)));
        Assert.Empty(await _datos.DeLaFranja(pista, inicio));

        Assert.Null(await Crear(inicio - TimeSpan.FromSeconds(1)));
        Assert.Single(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    public async Task HU4_esc3_un_cierre_da_INSTALACION_CERRADA()
    {
        var pista = await Tenis1();
        var inicio = Utc(new DateOnly(2026, 12, 26), 10);
        await _datos.Disponibilidad.Cierre(pista, inicio, inicio + ValoresNegocio.DuracionFranja);

        await AssertRechazo(await Reservar(pista, inicio), HttpStatusCode.UnprocessableEntity, CodigosError.InstalacionCerrada, pista, inicio);
    }

    [Fact]
    public async Task HU4_esc3_una_franja_fija_aprobada_da_FRANJA_BLOQUEADA_CLUB()
    {
        var pista = await Tenis2();
        var fecha = new DateOnly(2026, 12, 27);
        var inicio = Utc(fecha, 9);
        await _datos.Disponibilidad.FranjaFija(pista, [DatosDisponibilidad.DiaIso(fecha)], 9, 10);

        await AssertRechazo(await Reservar(pista, inicio), HttpStatusCode.UnprocessableEntity, CodigosError.FranjaBloqueadaClub, pista, inicio);
    }

    [Theory]
    [InlineData("Sala polivalente")]
    [InlineData("Pabellón")]
    public async Task HU4_esc4_una_instalacion_que_no_se_reserva_por_franjas_da_INSTALACION_NO_RESERVABLE(string instalacion)
    {
        var pista = await _datos.Pista(Sur, instalacion);
        var inicio = Utc(new DateOnly(2026, 12, 22), 10);

        await AssertRechazo(await Reservar(pista, inicio), HttpStatusCode.UnprocessableEntity, CodigosError.InstalacionNoReservable, pista, inicio);
    }

    [Fact]
    public async Task HU4_esc5_un_importe_distinto_del_actual_da_PRECIO_CAMBIADO()
    {
        var pista = await Tenis1();
        var inicio = Utc(new DateOnly(2026, 12, 22), 11);

        await AssertRechazo(await Reservar(pista, inicio, 5.00m), HttpStatusCode.Conflict, CodigosError.PrecioCambiado, pista, inicio);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task El_dia_del_cambio_de_hora_hay_14_franjas_y_la_de_las_8_dura_una_hora()
    {
        var pista = await Tenis1();
        var dia = new DateOnly(2027, 3, 28);
        var ahora = Utc(new DateOnly(2027, 3, 25), 12);

        for (var hora = 8; hora <= 21; hora++)
        {
            var inicio = Utc(dia, hora);
            var resumen = await _datos.ConServicio(ahora, Ana, s => s.Resumen(pista, inicio, Ct));
            Assert.Equal($"{hora:00}:00", resumen.HoraInicio);
        }

        var ocho = Utc(dia, 8);
        Assert.Equal(new DateTime(2027, 3, 28, 6, 0, 0, DateTimeKind.Utc), ocho);

        var creada = await _datos.ConServicio(ahora, Ana, s => s.Crear(new PeticionReserva(pista, ocho, 6.00m), Ct));
        var pagada = await _datos.ConServicio(ahora, Ana, s => s.Pagar(creada.Id, TarjetaAceptada, Ct));

        Assert.Equal(EstadoReserva.Confirmada, pagada.Estado);
        Assert.Equal(TimeSpan.FromHours(1), pagada.FinUtc - pagada.InicioUtc);
        Assert.Equal("08:00", pagada.HoraInicio);
        Assert.Equal("09:00", pagada.HoraFin);
    }
}
