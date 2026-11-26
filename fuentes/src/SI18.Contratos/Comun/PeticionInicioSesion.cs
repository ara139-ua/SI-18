using System.ComponentModel.DataAnnotations;

namespace SI18.Contratos.Comun;

/// <summary>Cuerpo de <c>POST /api/sesion</c> (OpenAPI, <c>PeticionInicioSesion</c>).</summary>
public sealed record PeticionInicioSesion
{
    [Required(ErrorMessage = "Escribe tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Escribe un correo electrónico válido.")]
    [MaxLength(254, ErrorMessage = "El correo no puede tener más de 254 caracteres.")]
    public required string Correo { get; init; }

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [MinLength(1, ErrorMessage = "Escribe tu contraseña.")]
    [MaxLength(200, ErrorMessage = "La contraseña no puede tener más de 200 caracteres.")]
    public required string Contrasena { get; init; }
}
