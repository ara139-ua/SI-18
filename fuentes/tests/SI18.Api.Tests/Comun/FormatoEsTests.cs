using SI18.Contratos.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>Formato español de importes y fechas (RF-027, principio IX; sin base de datos).</summary>
public class FormatoEsTests
{
    [Theory]
    [InlineData(6, "6,00 €")]
    [InlineData(10, "10,00 €")]
    [InlineData(4, "4,00 €")]
    [InlineData(1234.5, "1.234,50 €")]
    public void Los_importes_van_con_coma_decimal_y_espacio_duro_antes_del_euro(double euros, string esperado)
    {
        Assert.Equal(esperado, FormatoEs.Importe((decimal)euros));
    }

    [Theory]
    [InlineData(2026, 10, 25, 7, "25/10/2026 08:00")] // ya en horario de invierno (UTC+1)
    [InlineData(2026, 10, 24, 6, "24/10/2026 08:00")] // aún en horario de verano (UTC+2)
    [InlineData(2026, 12, 31, 23, "01/01/2027 00:00")] // cambia el día y el año en Madrid
    public void Las_fechas_se_muestran_en_hora_de_Madrid(int anio, int mes, int dia, int horaUtc, string esperado)
    {
        Assert.Equal(esperado, FormatoEs.FechaHora(new DateTime(anio, mes, dia, horaUtc, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Una_fecha_que_no_es_UTC_falla()
    {
        Assert.Throws<ArgumentException>(() => FormatoEs.FechaHora(new DateTime(2026, 10, 25, 7, 0, 0, DateTimeKind.Local)));
    }
}
