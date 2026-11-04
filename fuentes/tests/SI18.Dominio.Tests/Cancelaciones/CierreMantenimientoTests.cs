using SI18.Dominio.Cancelaciones;

namespace SI18.Dominio.Tests.Cancelaciones;

[Trait("Categoria", "Limites")]
public class CierreMantenimientoTests
{
    private static DateTime Utc(int hora) => new(2026, 11, 10, hora, 0, 0, DateTimeKind.Utc);

    private static readonly CierreMantenimiento CierreDe10a12 =
        new(1, Utc(10), Utc(12), "Pintado de líneas", 1, Utc(8));

    [Theory]
    [InlineData(9, false)]  // 09:00–10:00 termina justo cuando empieza el cierre
    [InlineData(10, true)]  // 10:00–11:00
    [InlineData(11, true)]  // 11:00–12:00
    [InlineData(12, false)] // 12:00–13:00 empieza justo cuando acaba el cierre
    public void Afecta_usa_intervalos_semiabiertos(int horaInicioFranja, bool afectada)
    {
        Assert.Equal(afectada, CierreDe10a12.Afecta(Utc(horaInicioFranja), Utc(horaInicioFranja + 1)));
    }

    [Fact]
    public void Afecta_a_una_franja_que_contiene_todo_el_cierre()
    {
        Assert.True(CierreDe10a12.Afecta(Utc(9), Utc(13)));
    }
}
