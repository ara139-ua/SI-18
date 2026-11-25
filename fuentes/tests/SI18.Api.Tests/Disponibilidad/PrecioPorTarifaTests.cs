using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// El precio de cada franja es el de la tarifa de quien busca (historia 2 de spec-001; tasks.md T014).
/// Polideportivo Sur, «Tenis», 23/12/2026. Tarifas de la siembra (D5 de 000-core): 6,00 / 10,00 / 4,00.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PrecioPorTarifaTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<IReadOnlyList<decimal>> PreciosPara(string correo)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);
        var ruta = $"/api/disponibilidad/franjas?deporteId={await _datos.Deporte("Tenis")}&centroId={await _datos.Centro("Polideportivo Sur")}&fecha=2026-12-23";
        var resultado = await cliente.GetFromJsonAsync<ResultadoBusquedaDto>(ruta, OpcionesJson.Predeterminadas, Ct);

        return resultado!.Pistas.SelectMany(p => p.Franjas).Select(f => f.Precio).ToList();
    }

    [Theory]
    [InlineData("ciudadana.empadronada@si18.test", 6.00)]
    [InlineData("ciudadano.noempadronado@si18.test", 10.00)]
    [InlineData("representante.padel@si18.test", 4.00)]   // representante no empadronado: tarifa club
    public async Task Cada_tarifa_ve_su_precio_en_todas_las_franjas(string correo, double esperado)
    {
        var precios = await PreciosPara(correo);

        Assert.NotEmpty(precios);
        Assert.All(precios, p => Assert.Equal((decimal)esperado, p));
    }

    [Fact]
    public async Task Empadronado_y_no_empadronado_ven_precios_distintos_para_la_misma_franja()
    {
        var empadronado = await PreciosPara("ciudadana.empadronada@si18.test");
        var noEmpadronado = await PreciosPara("ciudadano.noempadronado@si18.test");

        Assert.Equal(empadronado.Count, noEmpadronado.Count);
        Assert.All(empadronado.Zip(noEmpadronado), par => Assert.NotEqual(par.First, par.Second));
    }
}
