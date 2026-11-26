using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// El barrido caduca las reservas pendientes de pago al llegar a <see cref="ValoresNegocio.TiempoBloqueoPago"/>, y
/// su pago pasa a fallido (tasks.md T070; research.md R8.4; RF-016).
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class BarridoCaducidadesTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>El barrido que registra la API (<c>RegistroInfraestructura</c>), no una instancia aparte.</summary>
    private BarridoCaducidades Barrido =>
        fabrica.Services.GetServices<IHostedService>().OfType<BarridoCaducidades>().Single();

    /// <summary>Inserta una reserva pendiente de pago creada ahora, en una franja que no usa otra prueba.</summary>
    private async Task<(long Id, DateTime CreadaEnUtc)> InsertarPendiente()
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var instalacionId = await db.Instalaciones.Where(i => i.ReservablePorFranjas).OrderBy(i => i.Id).Select(i => i.Id).FirstAsync(Ct);
        var usuarioId = await db.Usuarios.Where(u => u.Rol == Rol.Ciudadano).OrderBy(u => u.Id).Select(u => u.Id).FirstAsync(Ct);

        var ahora = fabrica.Reloj.GetUtcNow().UtcDateTime;
        var inicio = HorarioMadrid.AUtc(new DateOnly(2027, 2, 10), new TimeOnly(12, 0));
        var reserva = new Reserva(usuarioId, instalacionId, inicio, TipoTarifa.Empadronado, 6.00m, ahora);
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync(Ct);

        return (reserva.Id, ahora);
    }

    private async Task<Reserva> Leer(long id)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.Reservas.AsNoTracking().Include(r => r.Pago).SingleAsync(r => r.Id == id, Ct);
    }

    [Fact]
    public async Task Una_reserva_pendiente_caduca_a_los_10_minutos_exactos_y_no_antes()
    {
        var (id, creadaEnUtc) = await InsertarPendiente();

        // T + 9:59 → sigue pendiente.
        fabrica.Reloj.Advance(ValoresNegocio.TiempoBloqueoPago - TimeSpan.FromSeconds(1));
        await Barrido.EjecutarUnaVez(Ct);

        var antes = await Leer(id);
        Assert.Equal(EstadoReserva.PendientePago, antes.Estado);
        Assert.Equal(EstadoPago.Pendiente, antes.Pago!.Estado);
        Assert.Null(antes.Pago.ResueltoEnUtc);

        // T + 10:00 → caducada, con el pago fallido y resuelto_en_utc informado.
        fabrica.Reloj.Advance(TimeSpan.FromSeconds(1));
        var ahora = fabrica.Reloj.GetUtcNow().UtcDateTime;
        Assert.Equal(creadaEnUtc + ValoresNegocio.TiempoBloqueoPago, ahora);
        await Barrido.EjecutarUnaVez(Ct);

        var despues = await Leer(id);
        Assert.Equal(EstadoReserva.Caducada, despues.Estado);
        Assert.Null(despues.CodigoAcceso);
        Assert.Equal(EstadoPago.Fallido, despues.Pago!.Estado);
        Assert.NotNull(despues.Pago.ResueltoEnUtc);
        Assert.Equal(ahora, despues.Pago.ResueltoEnUtc.Value.ToUniversalTime(), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task El_barrido_salta_una_reserva_bloqueada_y_la_caduca_en_la_siguiente_vuelta()
    {
        // Las pendientes no vigentes que hayan dejado otras pruebas no cuentan en este caso.
        await Barrido.EjecutarUnaVez(Ct);

        // Sin adelantar el reloj (spec-002 T006): la pendiente se inserta ya creada hace 10 minutos.
        var ahora = fabrica.Reloj.GetUtcNow().UtcDateTime;
        long id;
        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            var instalacionId = await db.Instalaciones.Where(i => i.ReservablePorFranjas).OrderBy(i => i.Id).Select(i => i.Id).FirstAsync(Ct);
            var usuarioId = await db.Usuarios.Where(u => u.Rol == Rol.Ciudadano).OrderBy(u => u.Id).Select(u => u.Id).FirstAsync(Ct);
            var inicio = HorarioMadrid.AUtc(new DateOnly(2027, 2, 11), new TimeOnly(12, 0));
            var reserva = new Reserva(usuarioId, instalacionId, inicio, TipoTarifa.Empadronado, 6.00m, ahora - ValoresNegocio.TiempoBloqueoPago);
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync(Ct);
            id = reserva.Id;
        }

        using (var bloqueo = fabrica.Services.CreateScope())
        {
            var db = bloqueo.ServiceProvider.GetRequiredService<Si18DbContext>();
            await using var transaccion = await db.Database.BeginTransactionAsync(Ct);
            await db.Reservas.FromSql($"SELECT * FROM reservas WHERE id = {id} FOR UPDATE").ToListAsync(Ct);

            // Con la fila bloqueada, el barrido termina sin esperar y no caduca nada.
            var caducadas = await Barrido.EjecutarUnaVez(Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);

            Assert.Equal(0, caducadas);
            Assert.Equal(EstadoReserva.PendientePago, (await Leer(id)).Estado);

            await transaccion.RollbackAsync(Ct);
        }

        // Liberado el bloqueo, la siguiente vuelta la caduca.
        Assert.True(await Barrido.EjecutarUnaVez(Ct) >= 1);

        var despues = await Leer(id);
        Assert.Equal(EstadoReserva.Caducada, despues.Estado);
        Assert.Equal(EstadoPago.Fallido, despues.Pago!.Estado);
    }
}
