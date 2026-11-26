namespace SI18.Contratos.Comun;

/// <summary>Usuario con sesión iniciada (OpenAPI, <c>UsuarioActual</c>).</summary>
public sealed record UsuarioActualDto
{
    public required int Id { get; init; }
    public required string Nombre { get; init; }
    public required string Correo { get; init; }
    public required Rol Rol { get; init; }

    /// <summary>Solo para ciudadano y representante de club (RF-007).</summary>
    public TipoTarifa? TipoTarifa { get; init; }

    /// <summary>Solo para personal del centro.</summary>
    public int? CentroId { get; init; }
    public string? CentroNombre { get; init; }

    /// <summary>Solo para representante de club.</summary>
    public int? ClubId { get; init; }
    public string? ClubNombre { get; init; }
}
