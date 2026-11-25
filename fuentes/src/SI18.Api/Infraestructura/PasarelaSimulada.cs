using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Pasarela de pago simulada y determinista (research.md R9; RF-021). Una tarjeta de 16 dígitos que
/// termina en <c>0002</c> se rechaza y cualquier otra se acepta. Las devoluciones siempre se completan.
/// <para>No guarda ni escribe en los registros ningún dato de tarjeta, ni siquiera los últimos dígitos.</para>
/// Dueño: Artur (tasks.md T063).
/// </summary>
public sealed class PasarelaSimulada : IPasarelaPago
{
    private const int DigitosTarjeta = 16;
    private const string FinalRechazado = "0002";

    public Task<bool> Cobrar(decimal importe, string numeroTarjetaFicticia, CancellationToken ct)
    {
        var numero = (numeroTarjetaFicticia ?? string.Empty).Replace(" ", string.Empty);

        if (numero.Length != DigitosTarjeta || !numero.All(char.IsAsciiDigit))
        {
            throw CodigosError.Error(CodigosError.PeticionInvalida);
        }

        return Task.FromResult(!numero.EndsWith(FinalRechazado, StringComparison.Ordinal));
    }

    public Task Devolver(long pagoId, decimal importe, CancellationToken ct) => Task.CompletedTask;
}
