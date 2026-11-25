using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Dominio.Tests.Disponibilidad;

/// <summary>Regla de las franjas fijas: solapes, horas cubiertas y expansión por temporada (spec-005 T004).</summary>
[Trait("Categoria", "Limites")]
public class ReglaFranjasFijasTests
{
    private const short Lunes = 1, Martes = 2, Miercoles = 3, Jueves = 4, Domingo = 7;
    private static readonly DateTime Creada = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Temporada Temporada2026 = new("2026-2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30));

    private static SolicitudFranjasFijas Solicitud(short[] dias, int inicio, int fin, int instalacion = 1, int temporada = 1) =>
        new(1, instalacion, temporada, dias, new TimeOnly(inicio, 0), new TimeOnly(fin, 0), Creada);

    // ── SeSolapan ────────────────────────────────────────────────────────────────

    [Fact]
    public void Martes_19_21_y_martes_20_22_se_solapan()
    {
        Assert.True(ReglaFranjasFijas.SeSolapan(Solicitud([Martes], 19, 21), Solicitud([Martes], 20, 22)));
    }

    [Fact]
    public void Martes_19_20_y_martes_20_21_no_se_solapan()
    {
        Assert.False(ReglaFranjasFijas.SeSolapan(Solicitud([Martes], 19, 20), Solicitud([Martes], 20, 21)));
    }

    [Fact]
    public void El_mismo_horario_en_otro_dia_no_se_solapa()
    {
        Assert.False(ReglaFranjasFijas.SeSolapan(Solicitud([Martes], 19, 21), Solicitud([Miercoles], 19, 21)));
    }

    [Fact]
    public void Basta_un_dia_en_comun()
    {
        Assert.True(ReglaFranjasFijas.SeSolapan(Solicitud([Lunes, Jueves], 19, 21), Solicitud([Martes, Jueves], 19, 20)));
    }

    [Theory]
    [InlineData(2, 1)]   // otra instalación
    [InlineData(1, 2)]   // otra temporada
    public void Otra_instalacion_u_otra_temporada_no_se_solapa(int instalacion, int temporada)
    {
        Assert.False(ReglaFranjasFijas.SeSolapan(Solicitud([Martes], 19, 21), Solicitud([Martes], 19, 21, instalacion, temporada)));
    }

    // ── Cubre ────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]   // la hora de fin no está incluida
    [InlineData(18, false)]
    public void Martes_19_21_cubre_las_horas_de_19_y_20(int hora, bool cubre)
    {
        var martes = new DateOnly(2026, 12, 22);

        Assert.Equal(cubre, ReglaFranjasFijas.Cubre(Solicitud([Martes], 19, 21), martes, new TimeOnly(hora, 0)));
    }

    [Fact]
    public void No_cubre_otro_dia_de_la_semana()
    {
        var miercoles = new DateOnly(2026, 12, 23);

        Assert.False(ReglaFranjasFijas.Cubre(Solicitud([Martes], 19, 21), miercoles, new TimeOnly(19, 0)));
    }

    // ── Expandir ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Fuera_de_la_temporada_no_hay_franjas()
    {
        var julio = new DateOnly(2027, 7, 6);

        Assert.Empty(ReglaFranjasFijas.Expandir(Solicitud([Martes], 19, 21), Temporada2026, julio, julio.AddDays(6)));
    }

    [Fact]
    public void Martes_y_jueves_19_21_en_una_semana_son_4_franjas_ordenadas()
    {
        var lunes = new DateOnly(2026, 12, 21);

        var franjas = ReglaFranjasFijas.Expandir(Solicitud([Martes, Jueves], 19, 21), Temporada2026, lunes, lunes.AddDays(6));

        Assert.Equal(4, franjas.Count);
        Assert.Equal(franjas.OrderBy(f => f.InicioUtc), franjas);
        Assert.Equal(
            [(new DateOnly(2026, 12, 22), new TimeOnly(19, 0)), (new DateOnly(2026, 12, 22), new TimeOnly(20, 0)),
             (new DateOnly(2026, 12, 24), new TimeOnly(19, 0)), (new DateOnly(2026, 12, 24), new TimeOnly(20, 0))],
            franjas.Select(f => (f.Fecha, f.HoraInicio)));
    }

    [Fact]
    public void Solo_se_expande_dentro_de_la_temporada_aunque_el_rango_la_desborde()
    {
        var franjas = ReglaFranjasFijas.Expandir(Solicitud([Martes], 19, 20), Temporada2026, new DateOnly(2027, 6, 1), new DateOnly(2027, 7, 31));

        Assert.All(franjas, f => Assert.True(f.Fecha <= new DateOnly(2027, 6, 30)));
        Assert.Equal(new DateOnly(2027, 6, 29), franjas[^1].Fecha);
    }

    [Theory]
    [InlineData(2026, 10, 25, 7)]   // fin del horario de verano: 08:00 de Madrid = 07:00 UTC
    [InlineData(2027, 3, 28, 6)]    // inicio del horario de verano: 08:00 de Madrid = 06:00 UTC
    public void El_dia_del_cambio_de_hora_las_franjas_siguen_en_hora_de_Madrid(int anio, int mes, int dia, int horaUtc)
    {
        var domingo = new DateOnly(anio, mes, dia);

        var franjas = ReglaFranjasFijas.Expandir(Solicitud([Domingo], 8, 10), Temporada2026, domingo, domingo);

        Assert.Equal([new TimeOnly(8, 0), new TimeOnly(9, 0)], franjas.Select(f => f.HoraInicio));
        Assert.Equal(new DateTime(anio, mes, dia, horaUtc, 0, 0, DateTimeKind.Utc), franjas[0].InicioUtc);
        Assert.All(franjas, f => Assert.Equal(TimeSpan.FromHours(1), f.FinUtc - f.InicioUtc));
    }
}
