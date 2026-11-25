using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// Búsquedas sin resultados y fuera de plazo (historia 3 de spec-001; tasks.md T018). Polideportivo Norte. El
/// reloj de la API empieza el lunes 21/12/2026 a las 18:00 de Madrid: hoy + 7 = 28/12 y hoy + 8 = 29/12.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class BuscarSinResultadosTests(FabricaApi fabrica)
{
    private const string Norte = "Polideportivo Norte";
    private const string Ana = "ciudadana.empadronada@si18.test";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<HttpResponseMessage> Buscar(string deporte, DateOnly fecha, string centroId = "")
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);
        var centro = centroId.Length > 0 ? centroId : (await _datos.Centro(Norte)).ToString();
        return await cliente.GetAsync($"/api/disponibilidad/franjas?deporteId={await _datos.Deporte(deporte)}&centroId={centro}&fecha={fecha:yyyy-MM-dd}", Ct);
    }

    private static async Task<JsonElement> Cuerpo(HttpResponseMessage respuesta) =>
        (await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct)).RootElement.Clone();

    private DateOnly Hoy => HorarioMadrid.Hoy(_datos.Ahora);

    [Fact]
    public async Task Un_deporte_sin_pistas_reservables_da_200_con_total_cero()
    {
        var respuesta = await Buscar("Baloncesto", Hoy.AddDays(1));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoBusquedaDto>(OpcionesJson.Predeterminadas, Ct);
        Assert.Equal(0, resultado!.TotalPistas);
        Assert.Empty(resultado.Pistas);
    }

    [Fact]
    public async Task Hoy_mas_7_tiene_franjas_y_hoy_mas_8_esta_fuera_de_antelacion()
    {
        var dentro = await Buscar("Pádel", Hoy.AddDays(ValoresNegocio.AntelacionMaximaDias));
        var fuera = await Buscar("Pádel", Hoy.AddDays(ValoresNegocio.AntelacionMaximaDias + 1));

        Assert.Equal(HttpStatusCode.OK, dentro.StatusCode);
        Assert.NotEmpty((await dentro.Content.ReadFromJsonAsync<ResultadoBusquedaDto>(OpcionesJson.Predeterminadas, Ct))!.Pistas);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fuera.StatusCode);
        var cuerpo = await Cuerpo(fuera);
        Assert.Equal("FUERA_DE_ANTELACION", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal("Solo puedes reservar desde hoy hasta dentro de 7 días.", cuerpo.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Ayer_esta_fuera_de_antelacion()
    {
        var respuesta = await Buscar("Pádel", Hoy.AddDays(-1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("FUERA_DE_ANTELACION", (await Cuerpo(respuesta)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Hoy_a_las_21_ya_no_quedan_franjas_pero_si_pistas()
    {
        // Con reloj propio: el de FabricaApi no retrocede y lo comparten todas las pruebas.
        var hoy = new DateOnly(2026, 12, 21);

        var resultado = await _datos.ConServicio(Utc(hoy, 21), async servicio =>
            await servicio.Buscar(await _datos.Deporte("Pádel"), await _datos.Centro(Norte), hoy, SI18.Dominio.Comun.TipoTarifa.Empadronado, Ct));

        Assert.Equal(2, resultado.TotalPistas);
        Assert.Empty(resultado.Pistas);
    }

    [Fact]
    public async Task La_fecha_se_valida_antes_que_la_existencia_del_centro()
    {
        var respuesta = await Buscar("Pádel", Hoy.AddDays(8), centroId: "999999");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("FUERA_DE_ANTELACION", (await Cuerpo(respuesta)).GetProperty("codigo").GetString());
    }
}
