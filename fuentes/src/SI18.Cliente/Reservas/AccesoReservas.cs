using SI18.Cliente.Comun;

namespace SI18.Cliente.Reservas;

/// <summary>Resultado de comprobar quién abre una pantalla de Reservas.</summary>
public enum Acceso
{
    SinSesion,
    SinPermiso,
    Permitido,
}

/// <summary>
/// Acceso por URL a las pantallas de Reservas, igual que <c>/buscar-pistas</c> (spec-001 D7; research.md E11):
/// sin sesión, a iniciar sesión; con un rol que no reserva, aviso y nada más. La regla de verdad la aplica la API
/// (<c>EsCiudadanoORepresentante</c>); esto solo evita pedir datos que la API va a negar.
/// </summary>
public static class AccesoReservas
{
    public const string AvisoSinPermiso = "Reservar pistas es para ciudadanos y representantes de club.";
    public const string AvisoSinPermisoValidar = "Validar códigos de acceso es para el personal de centro.";

    public static async Task<Acceso> Comprobar(EstadoSesion sesion)
    {
        var estado = await sesion.GetAuthenticationStateAsync();
        if (estado.User.Identity?.IsAuthenticated != true)
        {
            return Acceso.SinSesion;
        }

        return estado.User.IsInRole("ciudadano") || estado.User.IsInRole("representante_club")
            ? Acceso.Permitido
            : Acceso.SinPermiso;
    }

    /// <summary>
    /// Igual que <see cref="Comprobar"/>, pero para validar códigos de acceso (spec-006, research.md V10): solo el
    /// personal de centro. La regla de verdad la aplica la API (<c>ROL_NO_PERMITIDO</c>).
    /// </summary>
    public static async Task<Acceso> ComprobarPersonal(EstadoSesion sesion)
    {
        var estado = await sesion.GetAuthenticationStateAsync();
        if (estado.User.Identity?.IsAuthenticated != true)
        {
            return Acceso.SinSesion;
        }

        return estado.User.IsInRole("personal_centro") ? Acceso.Permitido : Acceso.SinPermiso;
    }
}
