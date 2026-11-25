namespace SI18.Contratos.Reservas;

/// <summary>
/// Reserva del usuario: respuesta de crear, pagar y ver una, y cada elemento de «Mis reservas»
/// (specs/002-reservar-pagar/data-model.md § 3).
/// <list type="bullet">
/// <item><see cref="Estado"/> es el efectivo: una pendiente de pago no vigente llega como <c>caducada</c>.</item>
/// <item><see cref="LimitePagoUtc"/> y <see cref="HoraLimitePago"/> («HH:mm» de Madrid, truncada al minuto) solo
/// si el estado es <c>pendiente_pago</c>.</item>
/// <item><see cref="CodigoAcceso"/> solo si el estado es <c>confirmada</c>.</item>
/// </list>
/// </summary>
public sealed record ReservaDto(
    long Id,
    int InstalacionId,
    string Pista,
    string Centro,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    DateTime InicioUtc,
    DateTime FinUtc,
    decimal Importe,
    EstadoReserva Estado,
    DateTime? LimitePagoUtc,
    string? HoraLimitePago,
    string? CodigoAcceso);
