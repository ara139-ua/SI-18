namespace SI18.Contratos.Reservas;

/// <summary>
/// Extensión <c>reserva</c> del Problem Details de un rechazo con <c>QR_RESERVA_NO_CONFIRMADA</c> o
/// <c>QR_FUERA_DE_VENTANA</c> (specs/006-validar-qr/data-model.md § 3). Sin campo de titular, para que no se
/// pueda devolver por error (CE-003).
/// </summary>
public sealed record ReservaValidadaDto(string Instalacion, string Centro, DateOnly Fecha, string HoraInicio, string HoraFin);
