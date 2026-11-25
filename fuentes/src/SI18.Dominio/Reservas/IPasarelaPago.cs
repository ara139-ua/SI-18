namespace SI18.Dominio.Reservas;

/// <summary>
/// Pasarela de pago (research.md R9; contracts/modulos.md). En SI-18 solo existe la simulada
/// (<c>SI18.Api.Infraestructura.PasarelaSimulada</c>). Ninguna implementación guarda datos de tarjeta.
/// </summary>
public interface IPasarelaPago
{
    /// <summary>Intenta cobrar <paramref name="importe"/>. Devuelve <c>true</c> si el cobro se acepta.</summary>
    /// <exception cref="SI18.Dominio.Comun.ErrorNegocio"><c>PETICION_INVALIDA</c> si la tarjeta no tiene 16 dígitos.</exception>
    Task<bool> Cobrar(decimal importe, string numeroTarjetaFicticia, CancellationToken ct);

    /// <summary>Devuelve <paramref name="importe"/> del pago <paramref name="pagoId"/>. Siempre termina correctamente.</summary>
    Task Devolver(long pagoId, decimal importe, CancellationToken ct);
}
