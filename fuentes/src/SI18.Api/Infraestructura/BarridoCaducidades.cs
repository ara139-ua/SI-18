using Microsoft.EntityFrameworkCore;
using SI18.Dominio.Comun;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Barrido periódico de las reservas pendientes de pago que ya no están vigentes (research.md R8.4; RF-016).
/// Persiste el estado <c>caducada</c> y el pago <c>fallido</c> para que «Mis reservas» y el informe vean el estado
/// real. La corrección no depende de él: la vigencia la decide <c>Reserva.EstaVigente</c> (R8.1–R8.3).
/// Dueño: Saul (tasks.md T068).
/// </summary>
public sealed class BarridoCaducidades(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<BarridoCaducidades> logger) : BackgroundService
{
    /// <summary>Cada cuánto se ejecuta el barrido.</summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo, timeProvider);
        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await EjecutarUnaVez(stoppingToken);
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    // Un ciclo fallido no debe detener la API: el siguiente lo vuelve a intentar.
                    logger.LogError(error, "Ha fallado el barrido de caducidades");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Parada normal de la API.
        }
    }

    /// <summary>
    /// Caduca, con <c>Reserva.Caducar</c>, las reservas pendientes de pago creadas hace
    /// <see cref="ValoresNegocio.TiempoBloqueoPago"/> o más. Público para las pruebas (T070).
    /// </summary>
    /// <returns>Cuántas reservas se han caducado.</returns>
    public async Task<int> EjecutarUnaVez(CancellationToken ct)
    {
        await using var ambito = scopeFactory.CreateAsyncScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var limite = ahora - ValoresNegocio.TiempoBloqueoPago;

        var caducadas = await db.Reservas
            .Include(r => r.Pago)
            .Where(r => r.Estado == EstadoReserva.PendientePago && r.CreadaEnUtc <= limite)
            .ToListAsync(ct);

        foreach (var reserva in caducadas)
        {
            reserva.Caducar(ahora);
        }

        if (caducadas.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Barrido de caducidades: {Cantidad} reservas caducadas", caducadas.Count);
        }

        return caducadas.Count;
    }
}
