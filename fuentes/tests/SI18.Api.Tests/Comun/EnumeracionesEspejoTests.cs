using System.Text.Json;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using RolDominio = SI18.Dominio.Comun.Rol;
using TipoInstalacionDominio = SI18.Dominio.Comun.TipoInstalacion;
using TipoTarifaDominio = SI18.Dominio.Comun.TipoTarifa;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// Las copias de los enumerados en SI18.Contratos.Comun no se separan de las de SI18.Dominio.Comun
/// (tasks.md T059): mismos nombres en el mismo orden, y en JSON el mismo texto que en la base de datos.
/// </summary>
public class EnumeracionesEspejoTests
{
    [Fact]
    public void Rol_tiene_los_mismos_nombres()
    {
        Assert.Equal(Enum.GetNames<RolDominio>(), Enum.GetNames<Rol>());
    }

    [Fact]
    public void TipoTarifa_tiene_los_mismos_nombres()
    {
        Assert.Equal(Enum.GetNames<TipoTarifaDominio>(), Enum.GetNames<TipoTarifa>());
    }

    [Fact]
    public void TipoInstalacion_tiene_los_mismos_nombres()
    {
        Assert.Equal(Enum.GetNames<TipoInstalacionDominio>(), Enum.GetNames<TipoInstalacion>());
    }

    [Fact]
    public void En_JSON_viajan_con_el_mismo_texto_que_en_la_base_de_datos()
    {
        Assert.Equal(ConversorEnumSnakeCase<RolDominio>.ValoresPermitidos, EnJson<Rol>());
        Assert.Equal(ConversorEnumSnakeCase<TipoTarifaDominio>.ValoresPermitidos, EnJson<TipoTarifa>());
        Assert.Equal(ConversorEnumSnakeCase<TipoInstalacionDominio>.ValoresPermitidos, EnJson<TipoInstalacion>());
    }

    private static string[] EnJson<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(valor => JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(valor, OpcionesJson.Predeterminadas))!)
            .ToArray();
}
