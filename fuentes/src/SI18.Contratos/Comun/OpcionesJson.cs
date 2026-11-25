using System.Text.Json;
using System.Text.Json.Serialization;

namespace SI18.Contratos.Comun;

/// <summary>
/// Opciones JSON compartidas por la API y el cliente: propiedades en camelCase y enumerados como texto en
/// snake_case (<c>RepresentanteClub</c> ↔ <c>"representante_club"</c>), como en el OpenAPI.
/// </summary>
public static class OpcionesJson
{
    /// <summary>Instancia de solo lectura para serializar y deserializar.</summary>
    public static JsonSerializerOptions Predeterminadas { get; } = Crear();

    /// <summary>Aplica la configuración común a unas opciones existentes (por ejemplo, las de ASP.NET Core).</summary>
    public static void Aplicar(JsonSerializerOptions opciones)
    {
        opciones.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opciones.PropertyNameCaseInsensitive = true;
        opciones.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    }

    private static JsonSerializerOptions Crear()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Aplicar(opciones);
        opciones.MakeReadOnly(populateMissingResolver: true);
        return opciones;
    }
}
