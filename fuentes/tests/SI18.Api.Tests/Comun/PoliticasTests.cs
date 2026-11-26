using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Modulos.Comun;
using SI18.Api.Tests.Infraestructura;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// Políticas por rol (HU-C3 esc. 6; RF-009). Los 403 reales de cada endpoint los prueba su spec.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PoliticasTests(FabricaApi fabrica)
{
    private static ClaimsPrincipal ConRol(string rol) =>
        new(new ClaimsIdentity([new Claim(ClaimsSesion.Sub, "1"), new Claim(ClaimsSesion.Rol, rol)], "prueba", ClaimsSesion.Sub, ClaimsSesion.Rol));

    private async Task<bool> Cumple(string rol, string politica)
    {
        var autorizacion = fabrica.Services.GetRequiredService<IAuthorizationService>();
        return (await autorizacion.AuthorizeAsync(ConRol(rol), politica)).Succeeded;
    }

    [Theory]
    [InlineData(Politicas.EsTecnico)]
    [InlineData(Politicas.EsPersonal)]
    [InlineData(Politicas.EsRepresentante)]
    public async Task Un_ciudadano_no_cumple_las_politicas_de_otros_roles(string politica)
    {
        Assert.False(await Cumple("ciudadano", politica));
    }

    [Theory]
    [InlineData("tecnico_concejalia", Politicas.EsTecnico)]
    [InlineData("personal_centro", Politicas.EsPersonal)]
    [InlineData("representante_club", Politicas.EsRepresentante)]
    [InlineData("ciudadano", Politicas.EsCiudadanoORepresentante)]
    [InlineData("representante_club", Politicas.EsCiudadanoORepresentante)]
    public async Task Cada_rol_cumple_su_politica(string rol, string politica)
    {
        Assert.True(await Cumple(rol, politica));
    }

    [Fact]
    public async Task El_tecnico_no_puede_reservar()
    {
        Assert.False(await Cumple("tecnico_concejalia", Politicas.EsCiudadanoORepresentante));
    }
}
