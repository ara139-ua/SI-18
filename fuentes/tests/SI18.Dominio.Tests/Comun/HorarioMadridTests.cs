using SI18.Dominio.Comun;

namespace SI18.Dominio.Tests.Comun;

[Trait("Categoria", "Limites")]
public class HorarioMadridTests
{
    private static DateTime Utc(int a, int m, int d, int h, int min) =>
        new(a, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Hoy_las_23_30_UTC_del_31_12_2026_es_1_1_2027_en_Madrid()
    {
        Assert.Equal(new DateOnly(2027, 1, 1), HorarioMadrid.Hoy(Utc(2026, 12, 31, 23, 30)));
    }

    [Theory]
    // Cambio a horario de invierno: domingo 25/10/2026 (CET, UTC+1); el sábado aún es CEST (UTC+2).
    [InlineData(2026, 10, 25, 7)]
    [InlineData(2026, 10, 24, 6)]
    // Cambio a horario de verano: domingo 28/03/2027 (CEST, UTC+2); el sábado aún es CET (UTC+1).
    [InlineData(2027, 3, 28, 6)]
    [InlineData(2027, 3, 27, 7)]
    public void AUtc_las_08_00_de_Madrid_respeta_el_cambio_de_hora(int a, int m, int d, int horaUtcEsperada)
    {
        var resultado = HorarioMadrid.AUtc(new DateOnly(a, m, d), new TimeOnly(8, 0));

        Assert.Equal(Utc(a, m, d, horaUtcEsperada, 0), resultado);
        Assert.Equal(DateTimeKind.Utc, resultado.Kind);
    }

    [Theory]
    [InlineData(2026, 10, 25, 8)]
    [InlineData(2026, 10, 24, 21)]
    [InlineData(2027, 3, 28, 8)]
    [InlineData(2026, 12, 31, 21)]
    public void ALocal_deshace_AUtc(int a, int m, int d, int h)
    {
        var fecha = new DateOnly(a, m, d);
        var hora = new TimeOnly(h, 0);

        var (fechaLocal, horaLocal) = HorarioMadrid.ALocal(HorarioMadrid.AUtc(fecha, hora));

        Assert.Equal(fecha, fechaLocal);
        Assert.Equal(hora, horaLocal);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Un_DateTime_no_UTC_lanza_excepcion(DateTimeKind tipo)
    {
        var noUtc = new DateTime(2026, 11, 4, 10, 0, 0, tipo);

        Assert.Throws<ArgumentException>(() => HorarioMadrid.ALocal(noUtc));
        Assert.Throws<ArgumentException>(() => HorarioMadrid.Hoy(noUtc));
    }
}
