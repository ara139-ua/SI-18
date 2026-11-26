namespace SI18.Contratos.Reservas;

/// <summary>
/// Cuerpo de <c>POST /api/validaciones-acceso</c> (specs/006-validar-qr/data-model.md § 3).
/// <see cref="Codigo"/>: obligatorio; se quitan los espacios de los extremos; vacío → 400 <c>PETICION_INVALIDA</c>
/// con <c>errors.codigo</c>.
/// </summary>
public sealed record PeticionValidacionAcceso(string? Codigo);
