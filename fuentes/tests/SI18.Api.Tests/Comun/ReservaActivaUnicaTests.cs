using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// El índice parcial <c>ux_reservas_activa</c> impide dos reservas activas en la misma franja aunque lleguen
/// a la vez (RF-014; research.md R7; quickstart.md V9). Solo se puede probar contra PostgreSQL real.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ReservaActivaUnicaTests(FabricaApi fabrica)
{
    private const int Intentos = 20;
    private const string ViolacionUnica = "23505";
    private const string Indice = "ux_reservas_activa";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private DateTime Ahora => fabrica.Reloj.GetUtcNow().UtcDateTime;

    /// <summary>Una pista reservable y un ciudadano de la siembra.</summary>
    private async Task<(int InstalacionId, int UsuarioId)> Datos()
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var instalacion = await db.Instalaciones.Where(i => i.ReservablePorFranjas).OrderBy(i => i.Id).Select(i => i.Id).FirstAsync(Ct);
        var usuario = await db.Usuarios.Where(u => u.Rol == Rol.Ciudadano).OrderBy(u => u.Id).Select(u => u.Id).FirstAsync(Ct);
        return (instalacion, usuario);
    }

    private Reserva NuevaReserva(int usuarioId, int instalacionId, DateTime inicioUtc) =>
        new(usuarioId, instalacionId, inicioUtc, TipoTarifa.Empadronado, 6.00m, Ahora);

    private static bool EsViolacionDelIndice(DbUpdateException error) =>
        error.InnerException is PostgresException { SqlState: ViolacionUnica, ConstraintName: Indice };

    [Fact]
    public async Task De_20_reservas_simultaneas_de_la_misma_franja_solo_una_se_guarda()
    {
        var (instalacionId, usuarioId) = await Datos();
        var inicio = HorarioMadrid.AUtc(new DateOnly(2027, 1, 15), new TimeOnly(10, 0));
        var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> Intentar()
        {
            // Cada intento con su propio DbContext, como dos peticiones distintas.
            using var ambito = fabrica.Services.CreateScope();
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            db.Reservas.Add(NuevaReserva(usuarioId, instalacionId, inicio));

            await salida.Task;
            try
            {
                await db.SaveChangesAsync(Ct);
                return true;
            }
            catch (DbUpdateException error) when (EsViolacionDelIndice(error))
            {
                return false;
            }
        }

        var intentos = Enumerable.Range(0, Intentos).Select(_ => Task.Run(Intentar, Ct)).ToArray();
        salida.SetResult();
        var resultados = await Task.WhenAll(intentos);

        Assert.Equal(1, resultados.Count(guardada => guardada));
        Assert.Equal(Intentos - 1, resultados.Count(guardada => !guardada));

        using var comprobacion = fabrica.Services.CreateScope();
        var activas = await comprobacion.ServiceProvider.GetRequiredService<Si18DbContext>().Reservas
            .CountAsync(r => r.InstalacionId == instalacionId && r.InicioUtc == inicio, Ct);
        Assert.Equal(1, activas);
    }

    [Fact]
    public async Task Una_reserva_caducada_no_impide_reservar_la_misma_franja()
    {
        var (instalacionId, usuarioId) = await Datos();
        var inicio = HorarioMadrid.AUtc(new DateOnly(2027, 1, 15), new TimeOnly(11, 0));

        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            var caducada = NuevaReserva(usuarioId, instalacionId, inicio);
            caducada.Caducar(Ahora + ValoresNegocio.TiempoBloqueoPago);
            db.Reservas.Add(caducada);
            await db.SaveChangesAsync(Ct);
        }

        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            db.Reservas.Add(NuevaReserva(usuarioId, instalacionId, inicio));
            await db.SaveChangesAsync(Ct);
        }

        using var comprobacion = fabrica.Services.CreateScope();
        var estados = await comprobacion.ServiceProvider.GetRequiredService<Si18DbContext>().Reservas
            .Where(r => r.InstalacionId == instalacionId && r.InicioUtc == inicio)
            .Select(r => r.Estado)
            .ToListAsync(Ct);
        Assert.Equal([EstadoReserva.PendientePago, EstadoReserva.Caducada], estados.Order());
    }
}
