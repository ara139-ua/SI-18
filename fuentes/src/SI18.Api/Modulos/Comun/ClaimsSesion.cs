using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Claims de la cookie de sesión (research.md R10): <c>sub</c>, <c>role</c>, <c>tipo_tarifa</c>,
/// <c>centro_id</c> y <c>club_id</c>. Los valores de los enumerados son los mismos textos en snake_case
/// que en la base de datos y en el JSON (<c>representante_club</c>).
/// </summary>
public static class ClaimsSesion
{
    public const string Esquema = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string NombreCookie = "si18.sesion";

    public const string Sub = "sub";
    public const string Rol = "role";
    public const string TipoTarifa = "tipo_tarifa";
    public const string CentroId = "centro_id";
    public const string ClubId = "club_id";

    /// <summary>Principal de sesión de <paramref name="usuario"/>, con su tarifa calculada una sola vez (RF-007).</summary>
    public static ClaimsPrincipal Crear(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(Rol, Texto(usuario.Rol)),
        };

        if (CalculadoraTarifa.Para(usuario.Rol, usuario.Empadronado) is { } tarifa)
        {
            claims.Add(new Claim(TipoTarifa, Texto(tarifa)));
        }

        if (usuario.CentroId is { } centro)
        {
            claims.Add(new Claim(CentroId, centro.ToString(CultureInfo.InvariantCulture)));
        }

        if (usuario.ClubId is { } club)
        {
            claims.Add(new Claim(ClubId, club.ToString(CultureInfo.InvariantCulture)));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema, nameType: Sub, roleType: Rol));
    }

    /// <summary>Texto del claim de un enumerado: <c>RepresentanteClub</c> → <c>representante_club</c>.</summary>
    public static string Texto<TEnum>(TEnum valor) where TEnum : struct, Enum =>
        ConversorEnumSnakeCase<TEnum>.ASnakeCase(valor.ToString());

    /// <summary>Enumerado a partir del texto de su claim.</summary>
    public static TEnum Leer<TEnum>(string texto) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Single(valor => Texto(valor) == texto);
}
