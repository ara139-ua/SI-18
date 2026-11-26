namespace SI18.Contratos.Reservas;

/// <summary>
/// Copia para el cliente de <c>SI18.Dominio.Comun.EstadoReserva</c> (specs/002-reservar-pagar/research.md E12).
/// Mismos nombres y orden que el dominio; en JSON viaja en snake_case (<c>pendiente_pago</c>, <c>confirmada</c>,
/// <c>caducada</c>, <c>cancelada</c>). Lo comprueba <c>EstadoReservaEspejoTests</c>.
/// </summary>
public enum EstadoReserva
{
    PendientePago,
    Confirmada,
    Caducada,
    Cancelada,
}
