using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using EstadoSolicitud = SI18.Dominio.Comun.EstadoSolicitud;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// <c>IConsultaFranjasFijas</c> definitiva de spec-005 (tasks.md T023; RF-014–RF-016). Las dos primeras pruebas vienen de
/// las de la provisional de spec-001 y conservan sus pistas de Oeste (no chocan con ConflictosFranjasFijasTests: otras
/// pistas u otros días). Las nuevas usan el Polideportivo Norte: «Pista de tenis 2» y el pabellón.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ConsultaFranjasFijasTests(FabricaApi fabrica)
{
    private const string Oeste = "Polideportivo Oeste";
    private const string Norte = "Polideportivo Norte";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<T> Con<T>(Func<IConsultaFranjasFijas, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await accion(ambito.ServiceProvider.GetRequiredService<IConsultaFranjasFijas>());
    }

    // ── Movidas desde ConsultasProvisionalesTests (spec-001) ─────────────────────

    [Fact]
    public async Task Una_franja_fija_aprobada_bloquea_sus_dias_y_horas_dentro_de_la_temporada()
    {
        var martes = new DateOnly(2027, 1, 12);
        var miercoles = new DateOnly(2027, 1, 13);
        var pista = await _datos.Pista(Oeste, "Pista de pádel 2");
        await _datos.FranjaFija(pista, [2, 4], 19, 21);   // martes y jueves de 19:00 a 21:00

        var enMartes = await Con(c => c.FranjasBloqueadas(pista, martes, martes, Ct));
        var enMiercoles = await Con(c => c.FranjasBloqueadas(pista, miercoles, miercoles, Ct));
        var julio = new DateOnly(2027, 7, 6);              // martes fuera de la temporada 2026-2027
        var enJulio = await Con(c => c.FranjasBloqueadas(pista, julio, julio, Ct));

        Assert.Equal([new TimeOnly(19, 0), new TimeOnly(20, 0)], enMartes.Select(f => f.HoraInicio));
        Assert.Empty(enMiercoles);
        Assert.Empty(enJulio);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Pendiente)]
    [InlineData(EstadoSolicitud.Rechazada)]
    public async Task Una_solicitud_no_aprobada_no_bloquea_nada(EstadoSolicitud estado)
    {
        var miercoles = new DateOnly(2027, 1, 13);
        var pista = await _datos.Pista(Oeste, "Pista de tenis 1");
        var hora = estado == EstadoSolicitud.Pendiente ? 15 : 17;
        await _datos.FranjaFija(pista, [3], hora, hora + 1, estado);   // miércoles

        var bloqueadas = await Con(c => c.FranjasBloqueadas(pista, miercoles, miercoles, Ct));

        Assert.DoesNotContain(bloqueadas, f => f.HoraInicio == new TimeOnly(hora, 0));
    }

    // ── Cierres: la franja fija queda suspendida (esc. 1–2 de la historia 5) ─────

    [Fact]
    public async Task Un_cierre_suspende_solo_las_franjas_fijas_que_cubre()
    {
        var pista = await _datos.Pista(Norte, "Pista de tenis 2");
        var martes = new DateOnly(2027, 2, 2);
        var otroMartes = new DateOnly(2027, 2, 9);
        await _datos.FranjaFija(pista, [2], 19, 21);
        await _datos.Cierre(pista, Utc(martes, 19), Utc(martes, 20));
        await _datos.Cierre(pista, Utc(otroMartes, 18), Utc(otroMartes, 22));

        var conCierreParcial = await Con(c => c.FranjasBloqueadas(pista, martes, martes, Ct));
        var conCierreTotal = await Con(c => c.FranjasBloqueadas(pista, otroMartes, otroMartes, Ct));
        var sinCierre = await Con(c => c.FranjasBloqueadas(pista, martes.AddDays(14), martes.AddDays(14), Ct));

        Assert.Equal([new TimeOnly(20, 0)], conCierreParcial.Select(f => f.HoraInicio));
        Assert.Empty(conCierreTotal);
        Assert.Equal([new TimeOnly(19, 0), new TimeOnly(20, 0)], sinCierre.Select(f => f.HoraInicio));
    }

    // ── Representantes afectados por un cierre (esc. 3 de la historia 5, para HU-07) ─

    [Fact]
    public async Task Los_representantes_afectados_son_los_de_los_clubes_cuyas_franjas_fijas_cubre_el_cierre()
    {
        var pabellon = await _datos.Pista(Norte, "Pabellón");
        var miercoles = new DateOnly(2027, 2, 3);
        await _datos.FranjaFija(pabellon, [3], 18, 20, club: ApiFranjasFijas.ClubNorte);
        await _datos.FranjaFija(pabellon, [3], 20, 22, club: "Club de Pádel Ejemplo");
        var norte = await Representantes(ApiFranjasFijas.ClubNorte);
        var padel = await Representantes("Club de Pádel Ejemplo");

        var ambos = await Con(c => c.RepresentantesAfectados(pabellon, Utc(miercoles, 19), Utc(miercoles, 21), Ct));
        var soloPadel = await Con(c => c.RepresentantesAfectados(pabellon, Utc(miercoles, 21), Utc(miercoles, 22), Ct));
        var ninguno = await Con(c => c.RepresentantesAfectados(pabellon, Utc(miercoles, 8), Utc(miercoles, 9), Ct));
        var otroDia = await Con(c => c.RepresentantesAfectados(pabellon, Utc(miercoles.AddDays(1), 18), Utc(miercoles.AddDays(1), 22), Ct));

        Assert.Equal(norte.Concat(padel).Order(), ambos.Order());
        Assert.Equal(ambos.Distinct().Count(), ambos.Count);
        Assert.Equal(padel.Order(), soloPadel.Order());
        Assert.Empty(ninguno);
        Assert.Empty(otroDia);
    }

    // ── CE-005: la búsqueda de HU-01 da lo mismo que antes ───────────────────────

    [Fact]
    public async Task Con_franja_fija_y_cierre_la_busqueda_da_cerrada_y_bloqueada_como_antes()
    {
        var pista = await _datos.Pista(Norte, "Pista de tenis 2");
        var jueves = new DateOnly(2026, 12, 24);
        await _datos.FranjaFija(pista, [4], 19, 21);
        await _datos.Cierre(pista, Utc(jueves, 19), Utc(jueves, 20));

        using var ambito = fabrica.Services.CreateScope();
        var consulta = ambito.ServiceProvider.GetRequiredService<IConsultaDisponibilidad>();
        Assert.Equal(CodigosError.InstalacionCerrada, await consulta.MotivoNoDisponible(pista, Utc(jueves, 19), Ct));
        Assert.Equal(CodigosError.FranjaBloqueadaClub, await consulta.MotivoNoDisponible(pista, Utc(jueves, 20), Ct));

        using var ciudadana = await fabrica.CrearClienteConSesion("ciudadana.empadronada@si18.test");
        var busqueda = await ciudadana.GetFromJsonAsync<ResultadoBusquedaDto>(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Deporte("Tenis")}&centroId={await _datos.Centro(Norte)}&fecha=2026-12-24",
            OpcionesJson.Predeterminadas, Ct);
        var horas = busqueda!.Pistas.Single(p => p.InstalacionId == pista).Franjas.Select(f => f.HoraInicio).ToList();
        Assert.DoesNotContain("19:00", horas);
        Assert.DoesNotContain("20:00", horas);
        Assert.Contains("21:00", horas);
    }

    private async Task<IReadOnlyList<int>> Representantes(string club)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.Usuarios
            .Where(u => u.Rol == SI18.Dominio.Comun.Rol.RepresentanteClub && db.Clubes.Any(c => c.Id == u.ClubId && c.Nombre == club))
            .Select(u => u.Id)
            .ToListAsync(Ct);
    }
}
