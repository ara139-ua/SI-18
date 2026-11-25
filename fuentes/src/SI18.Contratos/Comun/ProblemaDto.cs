namespace SI18.Contratos.Comun;

/// <summary>
/// Respuesta de error <c>application/problem+json</c> (contracts/errores.md; OpenAPI, <c>Problema</c>).
/// El cliente decide solo por <see cref="Codigo"/> y muestra <see cref="Detail"/> tal cual.
/// </summary>
public sealed record ProblemaDto
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int Status { get; init; }
    public string? Detail { get; init; }
    public string? Codigo { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
