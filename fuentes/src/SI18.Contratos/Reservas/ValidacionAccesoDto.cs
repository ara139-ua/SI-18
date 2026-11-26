namespace SI18.Contratos.Reservas;

/// <summary>
/// Respuesta 201 de <c>POST /api/validaciones-acceso</c>: código aceptado (specs/006-validar-qr/data-model.md § 3).
/// Fechas y horas «HH:mm» de Madrid, calculadas por la API. <see cref="ValidacionAnteriorUtc"/> y
/// <see cref="ValidacionAnteriorHora"/> son los de la validación aceptada más reciente de la reserva, si la hay (RF-006a).
/// </summary>
public sealed record ValidacionAccesoDto(
    long ValidacionId,
    string Titular,
    string Instalacion,
    string Centro,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    DateTime? ValidacionAnteriorUtc,
    string? ValidacionAnteriorHora);
