namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Cambios de estado de reservas que piden otros módulos (contracts/modulos.md, «Reservas»). Solo Reservas
/// escribe en sus tablas. La implementación es de la spec-002.
/// </summary>
public interface IServicioReservas
{
    /// <summary>
    /// HU-03: cancela la reserva del usuario. Si no es suya → <c>NO_ENCONTRADO</c>; si ya ha empezado →
    /// <c>RESERVA_YA_EMPEZADA</c>.
    /// </summary>
    Task<ReservaAfectada> CancelarPorUsuario(long reservaId, int usuarioId, CancellationToken ct);

    /// <summary>
    /// HU-07: cancela las reservas confirmadas y caduca las pendientes (pago → fallido) que se solapan con el
    /// cierre [<paramref name="inicioUtc"/>, <paramref name="finUtc"/>).
    /// </summary>
    Task<ResultadoCierre> AplicarCierre(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct);
}

/// <summary>
/// Reserva cancelada o caducada. <see cref="PagoId"/> e <see cref="Importe"/> permiten a Cancelaciones crear
/// la devolución sin leer la tabla <c>pagos</c>.
/// </summary>
public sealed record ReservaAfectada(long ReservaId, int UsuarioId, DateTime InicioUtc, long PagoId, decimal Importe);

/// <summary>Reservas afectadas por un cierre de mantenimiento.</summary>
public sealed record ResultadoCierre(IReadOnlyList<ReservaAfectada> Canceladas, IReadOnlyList<ReservaAfectada> Caducadas);
