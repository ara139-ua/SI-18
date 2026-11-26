namespace SI18.Contratos.Reservas;

/// <summary>
/// Cuerpo de <c>POST /api/reservas/{id}/pago</c> (specs/002-reservar-pagar/data-model.md § 3).
/// <see cref="NumeroTarjeta"/>: 16 dígitos después de quitar los espacios. No se guarda ni se registra, y
/// <see cref="ToString"/> no lo imprime (R9 de 000-core).
/// </summary>
public sealed record PeticionPago(string NumeroTarjeta)
{
    public override string ToString() => "PeticionPago { NumeroTarjeta = *** }";
}
