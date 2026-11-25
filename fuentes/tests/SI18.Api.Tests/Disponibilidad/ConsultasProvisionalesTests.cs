using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Disponibilidad.Provisional;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// Fija el comportamiento de las consultas provisionales de spec-001 (tasks.md T007; research.md D5), para que
/// las definitivas de spec-002, spec-005 y spec-007 lo puedan reutilizar. Polideportivo Oeste, enero de 2027.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ConsultasProvisionalesTests(FabricaApi fabrica)
{
    private const string Centro = "Polideportivo Oeste";
    private static readonly DateOnly Martes = new(2027, 1, 12);
    private static readonly DateOnly Miercoles = new(2027, 1, 13);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<T> ConContexto<T>(Func<Si18DbContext, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await accion(ambito.ServiceProvider.GetRequiredService<Si18DbContext>());
    }

    // ── IConsultaReservas ────────────────────────────────────────────────────────

    [Fact]
    public async Task Una_pendiente_bloquea_hasta_los_10_minutos_y_no_despues()
    {
        var pista = await _datos.Pista(Centro, "Pista de tenis 1");
        var ahora = _datos.Ahora;
        var hace959 = Utc(Martes, 9);
        var hace10 = Utc(Martes, 10);
        await _datos.Reserva(pista, hace959, EstadoPrueba.PendienteVigente, ahora - TimeSpan.FromSeconds(599));
        await _datos.Reserva(pista, hace10, EstadoPrueba.PendienteVigente, ahora - ValoresNegocio.TiempoBloqueoPago);

        var activas = await ConContexto(db =>
            new ConsultaReservasProvisional(db, fabrica.Reloj).Activas(pista, Utc(Martes, 0), Utc(Martes.AddDays(1), 0), Ct));

        Assert.Contains(activas, r => r.InicioUtc == hace959);
        Assert.DoesNotContain(activas, r => r.InicioUtc == hace10);
    }

    [Fact]
    public async Task Solo_cuentan_confirmadas_y_pendientes_vigentes_dentro_del_rango()
    {
        var pista = await _datos.Pista(Centro, "Pista de tenis 2");
        await _datos.Reserva(pista, Utc(Martes, 11), EstadoPrueba.Confirmada);
        await _datos.Reserva(pista, Utc(Martes, 12), EstadoPrueba.Caducada);
        await _datos.Reserva(pista, Utc(Martes, 13), EstadoPrueba.Cancelada);
        await _datos.Reserva(pista, Utc(Miercoles, 11), EstadoPrueba.Confirmada);

        var activas = await ConContexto(db =>
            new ConsultaReservasProvisional(db, fabrica.Reloj).Activas(pista, Utc(Martes, 0), Utc(Miercoles, 0), Ct));

        Assert.Equal([Utc(Martes, 11)], activas.Select(r => r.InicioUtc));
    }

    // ── IConsultaCierres ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Los_cierres_se_solapan_con_intervalos_semiabiertos()
    {
        var pista = await _datos.Pista(Centro, "Pista de pádel 1");
        await _datos.Cierre(pista, Utc(Martes, 10), Utc(Martes, 12));

        var antes = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 9), Utc(Martes, 10), Ct));
        var dentro = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 11), Utc(Martes, 12), Ct));
        var despues = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 12), Utc(Martes, 13), Ct));

        Assert.Empty(antes);
        Assert.Single(dentro);
        Assert.Empty(despues);
    }

    // ── IConsultaFranjasFijas ────────────────────────────────────────────────────

    [Fact]
    public async Task Una_franja_fija_aprobada_bloquea_sus_dias_y_horas_dentro_de_la_temporada()
    {
        var pista = await _datos.Pista(Centro, "Pista de pádel 2");
        await _datos.FranjaFija(pista, [2, 4], 19, 21);   // martes y jueves de 19:00 a 21:00

        var martes = await ConContexto(db => new ConsultaFranjasFijasProvisional(db).FranjasBloqueadas(pista, Martes, Martes, Ct));
        var miercoles = await ConContexto(db => new ConsultaFranjasFijasProvisional(db).FranjasBloqueadas(pista, Miercoles, Miercoles, Ct));
        var julio = new DateOnly(2027, 7, 6);              // martes fuera de la temporada 2026-2027
        var enJulio = await ConContexto(db => new ConsultaFranjasFijasProvisional(db).FranjasBloqueadas(pista, julio, julio, Ct));

        Assert.Equal([new TimeOnly(19, 0), new TimeOnly(20, 0)], martes.Select(f => f.HoraInicio));
        Assert.Empty(miercoles);
        Assert.Empty(enJulio);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Pendiente)]
    [InlineData(EstadoSolicitud.Rechazada)]
    public async Task Una_solicitud_no_aprobada_no_bloquea_nada(EstadoSolicitud estado)
    {
        var pista = await _datos.Pista(Centro, "Pista de tenis 1");
        var hora = estado == EstadoSolicitud.Pendiente ? 15 : 17;
        await _datos.FranjaFija(pista, [3], hora, hora + 1, estado);   // miércoles

        var bloqueadas = await ConContexto(db => new ConsultaFranjasFijasProvisional(db).FranjasBloqueadas(pista, Miercoles, Miercoles, Ct));

        Assert.DoesNotContain(bloqueadas, f => f.HoraInicio == new TimeOnly(hora, 0));
    }
}
