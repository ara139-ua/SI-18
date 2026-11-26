namespace SI18.Contratos.Reservas;

/// <summary>
/// Respuesta de <c>GET /api/reservas/resumen</c> (specs/002-reservar-pagar/data-model.md § 3). <see cref="Fecha"/>,
/// <see cref="HoraInicio"/> y <see cref="HoraFin"/> («HH:mm») son de Madrid, calculadas por la API.
/// <see cref="Importe"/> es el precio de la franja para la tarifa del usuario, en euros con dos decimales.
/// </summary>
public sealed record ResumenReservaDto(
    int InstalacionId,
    string Pista,
    string Centro,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    DateTime InicioUtc,
    decimal Importe);
