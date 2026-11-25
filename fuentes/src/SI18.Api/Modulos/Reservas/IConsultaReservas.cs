using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Consulta de reservas activas (contracts/modulos.md, «Reservas»). La consumen Disponibilidad y
/// CursosInformes. La implementación es de la spec-002.
/// </summary>
public interface IConsultaReservas
{
    /// <summary>
    /// Reservas activas (pendiente de pago vigente o confirmada) de una instalación en el rango semiabierto
    /// [<paramref name="desdeUtc"/>, <paramref name="hastaUtc"/>).
    /// </summary>
    Task<IReadOnlyList<ReservaActiva>> Activas(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
}

/// <summary>Reserva activa de una instalación.</summary>
public sealed record ReservaActiva(long Id, int UsuarioId, DateTime InicioUtc, EstadoReserva Estado);
