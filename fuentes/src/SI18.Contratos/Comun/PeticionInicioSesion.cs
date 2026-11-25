using System.ComponentModel.DataAnnotations;

namespace SI18.Contratos.Comun;

/// <summary>Cuerpo de <c>POST /api/sesion</c> (OpenAPI, <c>PeticionInicioSesion</c>).</summary>
public sealed record PeticionInicioSesion
{
    [Required, EmailAddress, MaxLength(254)]
    public required string Correo { get; init; }

    [Required, MinLength(1), MaxLength(200)]
    public required string Contrasena { get; init; }
}
