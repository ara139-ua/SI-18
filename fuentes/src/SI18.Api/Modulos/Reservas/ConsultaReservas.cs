using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Implementación definitiva de <see cref="IConsultaReservas"/> (specs/002-reservar-pagar/research.md E7;
/// contracts/modulos.md). Hace la misma consulta que la provisional de spec-001, a la que sustituye, y la fijan las
/// mismas pruebas (<c>ConsultaReservasTests</c>). Solo lectura. Dueño: Saul.
/// </summary>
public sealed class ConsultaReservas(Si18DbContext db, TimeProvider reloj) : IConsultaReservas
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
