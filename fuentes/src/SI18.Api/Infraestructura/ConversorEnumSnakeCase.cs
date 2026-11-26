using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Guarda un enumerado como texto en snake_case (data-model.md, «Convenciones» y «Enumerados»):
/// <c>PendientePago</c> ↔ <c>'pendiente_pago'</c>.
/// </summary>
public sealed class ConversorEnumSnakeCase<TEnum>() : ValueConverter<TEnum, string>(
    valor => ASnakeCase(valor.ToString()),
    texto => Enum.Parse<TEnum>(DesdeSnakeCase(texto)))
    where TEnum : struct, Enum
{
    /// <summary>Valores en base de datos de <typeparamref name="TEnum"/>, en el orden del enumerado.</summary>
    public static IReadOnlyList<string> ValoresPermitidos { get; } =
        Enum.GetNames<TEnum>().Select(ASnakeCase).ToArray();

    internal static string ASnakeCase(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('_');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static string DesdeSnakeCase(string snake) =>
        string.Concat(snake.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
}
