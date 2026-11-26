using System.Net;
using System.Text.Json;

namespace SI18.Cliente.Comun;

/// <summary>
/// Respuesta de error de la API (contracts/errores.md). El cliente decide solo por <see cref="Codigo"/> y
/// muestra <see cref="Detalle"/> tal cual, porque ya viene en español para el usuario.
/// </summary>
public sealed class ErrorApi(string codigo, string detalle, HttpStatusCode estado) : Exception(detalle)
{
    public string Codigo { get; } = codigo;
    public string Detalle { get; } = detalle;
    public HttpStatusCode Estado { get; } = estado;

    /// <summary>Errores por campo de un 400 <c>PETICION_INVALIDA</c>.</summary>
    public IReadOnlyDictionary<string, string[]> Errores { get; init; } = new Dictionary<string, string[]>();

    /// <summary>
    /// Propiedades del Problem Details que el contrato no recoge en los campos de arriba (por ejemplo, <c>reserva</c>
    /// y <c>validacionId</c> de spec-006). Vacía si no hay ninguna.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> Extensiones { get; init; } = new Dictionary<string, JsonElement>();
}
