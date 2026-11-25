using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Usuario de la petición en curso, leído de los claims de la cookie de sesión (contracts/modulos.md,
/// «Comun»; research.md R10). Solo se usa en endpoints protegidos: sin sesión, cualquier propiedad lanza
/// <c>NO_AUTENTICADO</c>.
/// </summary>
public interface IUsuarioActual
{
    int Id { get; }

    Rol Rol { get; }

    /// <summary>Solo ciudadano y representante de club (RF-007).</summary>
    TipoTarifa? TipoTarifa { get; }

    /// <summary>Solo personal del centro.</summary>
    int? CentroId { get; }

    /// <summary>Solo representante de club.</summary>
    int? ClubId { get; }
}
