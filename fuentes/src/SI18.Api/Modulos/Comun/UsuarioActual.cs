using System.Globalization;
using System.Security.Claims;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>Lee el usuario de la petición en curso de los claims de su cookie de sesión (research.md R10).</summary>
public sealed class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public int Id => int.Parse(Obligatorio(ClaimsSesion.Sub), CultureInfo.InvariantCulture);

    public Rol Rol => ClaimsSesion.Leer<Rol>(Obligatorio(ClaimsSesion.Rol));

    public TipoTarifa? TipoTarifa =>
        Opcional(ClaimsSesion.TipoTarifa) is { } texto ? ClaimsSesion.Leer<TipoTarifa>(texto) : null;

    public int? CentroId => Entero(ClaimsSesion.CentroId);

    public int? ClubId => Entero(ClaimsSesion.ClubId);

    private ClaimsPrincipal Principal
    {
        get
        {
            var principal = accesor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true
                ? principal
                : throw CodigosError.Error(CodigosError.NoAutenticado);
        }
    }

    private string? Opcional(string tipo) => Principal.FindFirst(tipo)?.Value;

    private string Obligatorio(string tipo) =>
        Opcional(tipo) ?? throw CodigosError.Error(CodigosError.NoAutenticado);

    private int? Entero(string tipo) =>
        Opcional(tipo) is { } texto ? int.Parse(texto, CultureInfo.InvariantCulture) : null;
}
