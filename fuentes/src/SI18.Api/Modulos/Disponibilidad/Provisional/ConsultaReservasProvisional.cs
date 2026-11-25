using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Reservas;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Disponibilidad.Provisional;

/// <summary>
/// PROVISIONAL (spec-001, RF-014a): sustituir por la implementación de spec-002 (Saul) y borrar este fichero.
/// Lectura de solo consulta de las reservas activas de una instalación (specs/001-buscar-pistas/research.md D5).
/// Su comportamiento lo fija <c>ConsultasProvisionalesTests</c>.
/// </summary>
public sealed class ConsultaReservasProvisional(Si18DbContext db, TimeProvider reloj) : IConsultaReservas
{
    public async Task<IReadOnlyList<ReservaActiva>> Activas(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)
    {
        // Misma vigencia que Reserva.EstaVigente: una pendiente de pago bloquea durante TiempoBloqueoPago.
        var limitePendientes = reloj.GetUtcNow().UtcDateTime - ValoresNegocio.TiempoBloqueoPago;

        return await db.Reservas.AsNoTracking()
            .Where(r => r.InstalacionId == instalacionId && r.InicioUtc >= desdeUtc && r.InicioUtc < hastaUtc)
            .Where(r => r.Estado == EstadoReserva.Confirmada
                || (r.Estado == EstadoReserva.PendientePago && r.CreadaEnUtc > limitePendientes))
            .OrderBy(r => r.InicioUtc)
            .Select(r => new ReservaActiva(r.Id, r.UsuarioId, r.InicioUtc, r.Estado))
            .ToListAsync(ct);
    }
}
