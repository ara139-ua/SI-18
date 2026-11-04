namespace SI18.Dominio.Comun;

/// <summary>
/// Usuario del sistema con un único rol (data-model.md, «Usuario»; RF-006). Precargado.
/// El tipo de tarifa no se guarda: lo calcula <c>CalculadoraTarifa</c> (RF-007).
/// </summary>
public class Usuario
{
    private Usuario() { }

    public Usuario(
        string nombre,
        string correo,
        string hashContrasena,
        Rol rol,
        bool empadronado,
        int? clubId = null,
        int? centroId = null)
    {
        Nombre = nombre;
        Correo = NormalizarCorreo(correo);
        HashContrasena = hashContrasena;
        Rol = rol;
        Empadronado = empadronado;
        ClubId = clubId;
        CentroId = centroId;
    }

    public int Id { get; private set; }

    /// <summary>varchar(100), obligatorio.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>varchar(254), único; se guarda en minúsculas.</summary>
    public string Correo { get; private set; } = null!;

    /// <summary>Hash PBKDF2 de <c>PasswordHasher</c> (R10). Nunca se expone en la API.</summary>
    public string HashContrasena { get; private set; } = null!;

    public Rol Rol { get; private set; }

    public bool Empadronado { get; private set; }

    /// <summary>Obligatorio si y solo si el rol es <see cref="Rol.RepresentanteClub"/>.</summary>
    public int? ClubId { get; private set; }

    /// <summary>Obligatorio si y solo si el rol es <see cref="Rol.PersonalCentro"/>.</summary>
    public int? CentroId { get; private set; }

    /// <summary>Forma en la que se guarda y se busca un correo: sin espacios y en minúsculas.</summary>
    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();
}
