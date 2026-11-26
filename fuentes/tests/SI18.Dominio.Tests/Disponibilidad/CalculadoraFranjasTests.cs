using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Dominio.Tests.Disponibilidad;

[Trait("Categoria", "Limites")]
public class CalculadoraFranjasTests
{
    public static TheoryData<DateOnly> DiasDeCambioDeHora() => new()
    {
        new DateOnly(2026, 10, 25), // fin del horario de verano (03:00 → 02:00)
        new DateOnly(2027, 3, 28),  // inicio del horario de verano (02:00 → 03:00)
        new DateOnly(2026, 12, 21), // día normal
    };

    [Theory]
    [MemberData(nameof(DiasDeCambioDeHora))]
    public void Hay_14_franjas_de_08_00_a_21_00_hora_de_Madrid(DateOnly fecha)
    {
        var franjas = CalculadoraFranjas.FranjasDelDia(7, fecha);

        Assert.Equal(14, franjas.Count);
        Assert.Equal(new TimeOnly(8, 0), franjas[0].HoraInicio);
        Assert.Equal(new TimeOnly(21, 0), franjas[^1].HoraInicio);
        Assert.All(franjas, f =>
        {
            Assert.Equal(7, f.InstalacionId);
            Assert.Equal(fecha, f.Fecha);
            Assert.Equal((fecha, f.HoraInicio), HorarioMadrid.ALocal(f.InicioUtc));
        });
    }

    [Theory]
    [MemberData(nameof(DiasDeCambioDeHora))]
    public void Cada_franja_dura_una_hora_real_y_son_consecutivas(DateOnly fecha)
    {
        var franjas = CalculadoraFranjas.FranjasDelDia(1, fecha);

        Assert.All(franjas, f =>
        {
            Assert.Equal(DateTimeKind.Utc, f.InicioUtc.Kind);
            Assert.Equal(TimeSpan.FromHours(1), f.FinUtc - f.InicioUtc);
        });
        Assert.All(franjas.Zip(franjas.Skip(1)), par => Assert.Equal(par.First.FinUtc, par.Second.InicioUtc));
    }

    [Theory]
    [InlineData(2026, 10, 24, 6)] // horario de verano (UTC+2)
    [InlineData(2026, 10, 25, 7)] // horario de invierno (UTC+1)
    [InlineData(2027, 3, 27, 7)]  // horario de invierno (UTC+1)
    [InlineData(2027, 3, 28, 6)]  // horario de verano (UTC+2)
    public void La_primera_franja_empieza_a_las_08_00_de_Madrid(int anio, int mes, int dia, int horaUtc)
    {
        var primera = CalculadoraFranjas.FranjasDelDia(1, new DateOnly(anio, mes, dia))[0];

        Assert.Equal(new DateTime(anio, mes, dia, horaUtc, 0, 0, DateTimeKind.Utc), primera.InicioUtc);
    }
}
