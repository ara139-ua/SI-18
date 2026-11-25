using System.Globalization;

namespace SI18.Contratos.Comun;

/// <summary>
/// Formato español de importes y fechas para mostrar al usuario (RF-027; constitución, principio IX).
/// Está en Contratos para que lo use el cliente y se pueda probar sin navegador.
/// </summary>
public static class FormatoEs
{
    // Separadores de España fijados aquí, sin depender de los datos de idioma de la máquina o del navegador:
    // con ToString("C2", es-ES) el espacio antes del € sale normal en Windows y duro con el ICU del navegador.
    private static readonly NumberFormatInfo Numeros = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
        NegativeSign = "-",
    };

    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    /// <summary>Importe en euros: <c>6m</c> → «6,00 €», con espacio duro (U+00A0) antes del símbolo.</summary>
    public static string Importe(decimal importe) => importe.ToString("N2", Numeros) + " €";

    /// <summary>Instante UTC en hora de Madrid con formato <c>dd/MM/yyyy HH:mm</c>.</summary>
    /// <exception cref="ArgumentException">Si <paramref name="utc"/> no es <see cref="DateTimeKind.Utc"/>.</exception>
    public static string FechaHora(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Se esperaba un DateTime en UTC.", nameof(utc));
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utc, Madrid).ToString("dd'/'MM'/'yyyy HH':'mm", CultureInfo.InvariantCulture);
    }
}
