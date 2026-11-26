using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>Catálogo precargado contra la API y PostgreSQL reales (tasks.md T058; quickstart.md V7–V8).</summary>
[Collection(ColeccionApi.Nombre)]
public class CatalogoTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task V7_hay_4_centros_con_horario_de_08_00_a_22_00()
    {
        using var cliente = fabrica.CreateClient();

        var centros = await cliente.GetFromJsonAsync<List<CentroDto>>("/api/centros", OpcionesJson.Predeterminadas, Ct);

        Assert.NotNull(centros);
        Assert.Equal(4, centros.Count);
        Assert.All(centros, c =>
        {
            Assert.Equal("08:00", c.HoraApertura);
            Assert.Equal("22:00", c.HoraCierre);
        });
    }

    [Fact]
    public async Task V8_reservables_por_franjas_son_16_y_todas_pistas_de_tenis_o_padel()
    {
        using var cliente = fabrica.CreateClient();

        var instalaciones = await cliente.GetFromJsonAsync<List<InstalacionDto>>(
            "/api/instalaciones?reservablePorFranjas=true", OpcionesJson.Predeterminadas, Ct);

        Assert.NotNull(instalaciones);
        Assert.Equal(16, instalaciones.Count);
        Assert.All(instalaciones, i =>
        {
            Assert.Contains(i.Tipo, new[] { TipoInstalacion.PistaTenis, TipoInstalacion.PistaPadel });
            Assert.True(i.ReservablePorFranjas);
            Assert.NotEmpty(i.DeporteIds);
        });
    }
}
