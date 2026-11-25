using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Dominio.Tests.Disponibilidad;

[Trait("Categoria", "Limites")]
public class ReglaDisponibilidadTests
{
    private const int Pista = 1;
    private const int OtraPista = 2;

    /// <summary>Lunes 21/12/2026 a las 18:00 en Madrid (17:00 UTC).</summary>
    private static readonly DateOnly Hoy = new(2026, 12, 21);
    private static readonly DateTime Ahora = HorarioMadrid.AUtc(Hoy, new TimeOnly(18, 0));

    private static Franja FranjaDe(DateOnly fecha, int hora, int instalacion = Pista) =>
        CalculadoraFranjas.FranjasDelDia(instalacion, fecha).Single(f => f.HoraInicio == new TimeOnly(hora, 0));

    private static string? Motivo(
        Franja franja,
        DateTime? ahora = null,
        bool reservable = true,
        IReadOnlyList<CierreMantenimiento>? cierres = null,
        IReadOnlyList<Franja>? bloqueos = null,
        IReadOnlyList<DateTime>? reservas = null) =>
        ReglaDisponibilidad.MotivoNoDisponible(franja, ahora ?? Ahora, reservable, cierres ?? [], bloqueos ?? [], reservas ?? []);

    private static CierreMantenimiento CierreSobre(Franja franja, int instalacion = Pista) =>
        new(instalacion, franja.InicioUtc, franja.FinUtc, "Mantenimiento", 1, Ahora);

    // ── 1. Antelación (fecha de Madrid) ──────────────────────────────────────────

    [Fact]
    public void El_lunes_a_las_18_la_franja_de_las_21_del_lunes_siguiente_esta_libre()
    {
        Assert.Null(Motivo(FranjaDe(new DateOnly(2026, 12, 28), 21)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(21)]
    public void Cualquier_franja_del_dia_29_esta_fuera_de_antelacion(int hora)
    {
        Assert.Equal(CodigosError.FueraDeAntelacion, Motivo(FranjaDe(new DateOnly(2026, 12, 29), hora)));
    }

    [Fact]
    public void Hoy_mas_7_si_y_hoy_mas_8_no()
    {
        Assert.Null(Motivo(FranjaDe(Hoy.AddDays(ValoresNegocio.AntelacionMaximaDias), 8)));
        Assert.Equal(CodigosError.FueraDeAntelacion, Motivo(FranjaDe(Hoy.AddDays(ValoresNegocio.AntelacionMaximaDias + 1), 8)));
    }

    [Fact]
    public void Una_fecha_pasada_esta_fuera_de_antelacion_antes_que_empezada()
    {
        Assert.Equal(CodigosError.FueraDeAntelacion, Motivo(FranjaDe(Hoy.AddDays(-1), 21)));
    }

    [Fact]
    public void La_antelacion_usa_la_fecha_de_Madrid_y_no_la_UTC()
    {
        // 31/12/2026 a las 23:30 UTC ya es 01/01/2027 en Madrid: hoy + 7 es el 08/01/2027.
        var ahora = new DateTime(2026, 12, 31, 23, 30, 0, DateTimeKind.Utc);

        Assert.Null(Motivo(FranjaDe(new DateOnly(2027, 1, 8), 8), ahora));
        Assert.Equal(CodigosError.FueraDeAntelacion, Motivo(FranjaDe(new DateOnly(2027, 1, 9), 8), ahora));
    }

    // ── 2. Franja ya empezada ────────────────────────────────────────────────────

    [Fact]
    public void Empezada_hace_un_minuto_no_se_puede_reservar()
    {
        Assert.Equal(CodigosError.FranjaYaEmpezada, Motivo(FranjaDe(Hoy, 18), Ahora + TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void En_el_instante_de_inicio_ya_esta_empezada_y_un_segundo_antes_no()
    {
        var franja = FranjaDe(Hoy, 19);

        Assert.Equal(CodigosError.FranjaYaEmpezada, Motivo(franja, franja.InicioUtc));
        Assert.Null(Motivo(franja, franja.InicioUtc - TimeSpan.FromSeconds(1)));
    }

    // ── 3–6. Orden de los motivos ────────────────────────────────────────────────

    [Fact]
    public void Los_motivos_se_comprueban_en_el_orden_de_data_model()
    {
        var franja = FranjaDe(Hoy.AddDays(1), 10);
        IReadOnlyList<CierreMantenimiento> cierres = [CierreSobre(franja)];
        IReadOnlyList<Franja> bloqueos = [franja];
        IReadOnlyList<DateTime> reservas = [franja.InicioUtc];

        Assert.Equal(CodigosError.InstalacionNoReservable, Motivo(franja, reservable: false, cierres: cierres, bloqueos: bloqueos, reservas: reservas));
        Assert.Equal(CodigosError.InstalacionCerrada, Motivo(franja, cierres: cierres, bloqueos: bloqueos, reservas: reservas));
        Assert.Equal(CodigosError.FranjaBloqueadaClub, Motivo(franja, bloqueos: bloqueos, reservas: reservas));
        Assert.Equal(CodigosError.FranjaOcupada, Motivo(franja, reservas: reservas));
        Assert.Null(Motivo(franja));
    }

    [Fact]
    public void La_antelacion_va_antes_que_el_resto_de_motivos()
    {
        var franja = FranjaDe(Hoy.AddDays(8), 10);

        Assert.Equal(CodigosError.FueraDeAntelacion, Motivo(franja, reservable: false, cierres: [CierreSobre(franja)], bloqueos: [franja], reservas: [franja.InicioUtc]));
    }

    [Fact]
    public void Un_cierre_que_acaba_cuando_empieza_la_franja_no_la_cierra()
    {
        var franja = FranjaDe(Hoy.AddDays(1), 10);
        var anterior = FranjaDe(Hoy.AddDays(1), 9);

        Assert.Null(Motivo(franja, cierres: [CierreSobre(anterior)]));
    }

    [Fact]
    public void Los_cierres_y_bloqueos_de_otra_instalacion_no_cuentan()
    {
        var franja = FranjaDe(Hoy.AddDays(1), 10);
        var mismaHoraOtraPista = FranjaDe(Hoy.AddDays(1), 10, OtraPista);

        Assert.Null(Motivo(franja, cierres: [CierreSobre(franja, OtraPista)], bloqueos: [mismaHoraOtraPista]));
    }

    [Fact]
    public void Una_reserva_a_otra_hora_no_ocupa_la_franja()
    {
        var franja = FranjaDe(Hoy.AddDays(1), 10);

        Assert.Null(Motivo(franja, reservas: [FranjaDe(Hoy.AddDays(1), 11).InicioUtc]));
    }
}
