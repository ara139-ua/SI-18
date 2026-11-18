using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;

namespace SI18.Dominio.Tests.Cancelaciones;

[Trait("Categoria", "Limites")]
public class PoliticaDevolucionTests
{
    private static readonly DateTime Inicio = new(2026, 11, 20, 17, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Usuario_con_24_h_y_1_s_de_antelacion_tiene_devolucion()
    {
        var ahora = Inicio - TimeSpan.FromHours(24) - TimeSpan.FromSeconds(1);

        Assert.True(PoliticaDevolucion.HayDevolucion(MotivoDevolucion.CancelacionUsuario, Inicio, ahora));
    }

    [Fact]
    public void Usuario_con_24_h_exactas_no_tiene_devolucion()
    {
        var ahora = Inicio - TimeSpan.FromHours(24);

        Assert.False(PoliticaDevolucion.HayDevolucion(MotivoDevolucion.CancelacionUsuario, Inicio, ahora));
    }

    [Fact]
    public void Con_el_cambio_de_hora_25_h_de_pared_son_24_h_reales_y_no_hay_devolucion()
    {
        // Sábado 27/03/2027 10:00 Madrid (CET) = 09:00 UTC; domingo 28/03/2027 11:00 Madrid (CEST) = 09:00 UTC.
        var ahora = HorarioMadrid.AUtc(new DateOnly(2027, 3, 27), new TimeOnly(10, 0));
        var inicio = HorarioMadrid.AUtc(new DateOnly(2027, 3, 28), new TimeOnly(11, 0));

        Assert.Equal(new DateTime(2027, 3, 27, 9, 0, 0, DateTimeKind.Utc), ahora);
        Assert.Equal(new DateTime(2027, 3, 28, 9, 0, 0, DateTimeKind.Utc), inicio);
        Assert.False(PoliticaDevolucion.HayDevolucion(MotivoDevolucion.CancelacionUsuario, inicio, ahora));
    }

    [Theory]
    [InlineData(48)]
    [InlineData(24)]
    [InlineData(1)]
    [InlineData(0)]
    public void Cierre_por_mantenimiento_siempre_tiene_devolucion(int horasAntes)
    {
        var ahora = Inicio - TimeSpan.FromHours(horasAntes);

        Assert.True(PoliticaDevolucion.HayDevolucion(MotivoDevolucion.CierreMantenimiento, Inicio, ahora));
    }
}
