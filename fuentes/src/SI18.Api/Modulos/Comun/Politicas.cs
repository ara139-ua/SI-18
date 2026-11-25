using Microsoft.AspNetCore.Authorization;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Políticas de autorización por rol (research.md R10; RF-009). Un endpoint exclusivo de un rol usa
/// <c>.RequireAuthorization(Politicas.EsTecnico)</c>; con otro rol responde 403 <c>ROL_NO_PERMITIDO</c>.
/// La comprobación de centro (D7) no es una política: la hace el caso de uso con <c>PermisosCentro</c>.
/// </summary>
public static class Politicas
{
    public const string EsCiudadanoORepresentante = nameof(EsCiudadanoORepresentante);
    public const string EsRepresentante = nameof(EsRepresentante);
    public const string EsPersonal = nameof(EsPersonal);
    public const string EsTecnico = nameof(EsTecnico);

    public static void Registrar(AuthorizationOptions opciones)
    {
        opciones.AddPolicy(EsCiudadanoORepresentante, p => p.RequireRole(
            ClaimsSesion.Texto(Rol.Ciudadano), ClaimsSesion.Texto(Rol.RepresentanteClub)));
        opciones.AddPolicy(EsRepresentante, p => p.RequireRole(ClaimsSesion.Texto(Rol.RepresentanteClub)));
        opciones.AddPolicy(EsPersonal, p => p.RequireRole(ClaimsSesion.Texto(Rol.PersonalCentro)));
        opciones.AddPolicy(EsTecnico, p => p.RequireRole(ClaimsSesion.Texto(Rol.TecnicoConcejalia)));
    }
}
