using SI18.Cliente.Reservas;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Texto del aviso de validación anterior (RF-006a; converge 2026-11-26, F3 → T032): «hoy a las HH:mm» si la
/// anterior es del mismo día de Madrid que ahora, y la fecha y la hora si no. Sin base de datos.
/// </summary>
public class AvisoValidacionAnteriorTextoTests
{
    private static DateTime Utc(int anio, int mes, int dia, int hora, int minuto) =>
        new(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void Del_mismo_dia_de_Madrid_dice_hoy_y_la_hora()
    {
        // 08:50 UTC = 09:50 en Madrid (invierno); ahora, 09:20 UTC = 10:20.
        var anterior = Utc(2026, 12, 15, 8, 50);
        var ahora = Utc(2026, 12, 15, 9, 20);

        Assert.Equal(HorarioMadrid.Hoy(anterior), HorarioMadrid.Hoy(ahora));
        Assert.Equal("Este código ya se validó hoy a las 09:50.", TextosReservas.AvisoValidacionAnterior(anterior, ahora));
    }

    [Fact]
    public void De_otro_dia_de_Madrid_dice_la_fecha_y_la_hora_aunque_sea_el_mismo_dia_UTC()
    {
        // 22:50 UTC = 23:50 en Madrid el 14/12; ahora, 23:10 UTC = 00:10 del 15/12 en Madrid.
        var anterior = Utc(2026, 12, 14, 22, 50);
        var ahora = Utc(2026, 12, 14, 23, 10);

        Assert.NotEqual(HorarioMadrid.Hoy(anterior), HorarioMadrid.Hoy(ahora));
        Assert.Equal("Este código se validó por última vez el 14/12/2026 a las 23:50.", TextosReservas.AvisoValidacionAnterior(anterior, ahora));
    }

    [Fact]
    public void En_horario_de_verano_cuenta_el_dia_de_Madrid_y_no_el_UTC()
    {
        // 22:30 UTC del 14/07 = 00:30 del 15/07 en Madrid (UTC+2); ahora, 06:00 UTC del 15/07 = 08:00.
        var anterior = Utc(2027, 7, 14, 22, 30);
        var ahora = Utc(2027, 7, 15, 6, 0);

        Assert.Equal(HorarioMadrid.Hoy(anterior), HorarioMadrid.Hoy(ahora));
        Assert.Equal("Este código ya se validó hoy a las 00:30.", TextosReservas.AvisoValidacionAnterior(anterior, ahora));
    }
}
