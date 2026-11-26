using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Dominio.Tests.Disponibilidad;

[Trait("Categoria", "Limites")]
public class CalculadoraPrecioTests
{
    // Las 6 tarifas de la siembra (spec, D5; data-model.md, «Datos de inicialización»).
    private static readonly IReadOnlyList<Tarifa> TarifasD5 =
    [
        new(TipoInstalacion.PistaTenis, TipoTarifa.Empadronado, 6.00m),
        new(TipoInstalacion.PistaTenis, TipoTarifa.NoEmpadronado, 10.00m),
        new(TipoInstalacion.PistaTenis, TipoTarifa.Club, 4.00m),
        new(TipoInstalacion.PistaPadel, TipoTarifa.Empadronado, 6.00m),
        new(TipoInstalacion.PistaPadel, TipoTarifa.NoEmpadronado, 10.00m),
        new(TipoInstalacion.PistaPadel, TipoTarifa.Club, 4.00m),
    ];

    [Theory]
    [InlineData(TipoInstalacion.PistaTenis, TipoTarifa.Empadronado, 6.00)]
    [InlineData(TipoInstalacion.PistaTenis, TipoTarifa.NoEmpadronado, 10.00)]
    [InlineData(TipoInstalacion.PistaTenis, TipoTarifa.Club, 4.00)]
    [InlineData(TipoInstalacion.PistaPadel, TipoTarifa.Empadronado, 6.00)]
    [InlineData(TipoInstalacion.PistaPadel, TipoTarifa.NoEmpadronado, 10.00)]
    [InlineData(TipoInstalacion.PistaPadel, TipoTarifa.Club, 4.00)]
    public void El_precio_es_el_de_la_tarifa_por_una_hora(TipoInstalacion tipo, TipoTarifa tarifa, double esperado)
    {
        Assert.Equal((decimal)esperado, CalculadoraPrecio.Precio(tipo, tarifa, TarifasD5));
    }

    [Fact]
    public void Empadronado_y_no_empadronado_pagan_distinto_por_la_misma_franja()
    {
        Assert.NotEqual(
            CalculadoraPrecio.Precio(TipoInstalacion.PistaTenis, TipoTarifa.Empadronado, TarifasD5),
            CalculadoraPrecio.Precio(TipoInstalacion.PistaTenis, TipoTarifa.NoEmpadronado, TarifasD5));
    }

    [Theory]
    [InlineData(TipoInstalacion.Piscina)]
    [InlineData(TipoInstalacion.Pabellon)]
    public void Sin_tarifa_para_el_tipo_lanza_instalacion_no_reservable(TipoInstalacion tipo)
    {
        var error = Assert.Throws<ErrorNegocio>(() => CalculadoraPrecio.Precio(tipo, TipoTarifa.Empadronado, TarifasD5));

        Assert.Equal(CodigosError.InstalacionNoReservable, error.Codigo);
    }
}
