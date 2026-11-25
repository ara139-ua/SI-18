namespace SI18.Contratos.Reservas;

/// <summary>
/// Cuerpo de <c>POST /api/reservas</c> (specs/002-reservar-pagar/data-model.md § 3). Los tres campos son
/// obligatorios. <see cref="InstalacionId"/> e <see cref="InicioUtc"/> son los de la franja de HU-01;
/// <see cref="ImporteMostrado"/> (&gt; 0, dos decimales) es el importe del resumen: si difiere del actual, la API
/// responde <c>PRECIO_CAMBIADO</c>.
/// </summary>
public sealed record PeticionReserva(int InstalacionId, DateTime InicioUtc, decimal ImporteMostrado);
