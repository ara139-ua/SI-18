using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using static SI18.Api.Tests.Disponibilidad.ApiFranjasFijas;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// Historias 2 y 4 de spec-005: aprobar y rechazar (tasks.md T012 y T019). Polideportivo Este, con este reparto para
/// que una aprobación no estropee otra prueba: aprobar → «Pista de pádel 1» los martes; rechazar → «Pista de pádel 2»
/// los miércoles; ya resuelta → «Pista de tenis 1» los viernes; rechazar con conflictos → «Pista de tenis 2» los jueves.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RevisarFranjasFijasTests(FabricaApi fabrica)
{
    private const string Este = "Polideportivo Este";
    private static readonly DateOnly Martes = new(2026, 12, 22);
    private static readonly DateOnly Miercoles = new(2026, 12, 23);
    private static readonly DateOnly Jueves = new(2026, 12, 24);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);
    private readonly ApiFranjasFijas _api = new(fabrica);

    private async Task<IReadOnlyList<string>> HorasLibres(string pista, DateOnly fecha)
    {
        using var ciudadana = await fabrica.CrearClienteConSesion("ciudadana.empadronada@si18.test");
        var resultado = await ciudadana.GetFromJsonAsync<ResultadoBusquedaDto>(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Deporte("Pádel")}&centroId={await _datos.Centro(Este)}&fecha={fecha:yyyy-MM-dd}",
            OpcionesJson.Predeterminadas, Ct);
        return resultado!.Pistas.SingleOrDefault(p => p.Nombre == pista)?.Franjas.Select(f => f.HoraInicio).ToList() ?? [];
    }

    // ── Aprobar (historia 2) ─────────────────────────────────────────────────────

    [Fact]
    public async Task Aprobar_sin_conflictos_bloquea_las_franjas_de_la_temporada_y_avisa_al_club()
    {
        var pista = await _datos.Pista(Este, "Pista de pádel 1");
        var solicitud = await _api.Crear(pista, [2], 19, 21);
        var (representantes, avisosAntes) = await _api.AvisosDelClub(ClubNorte);

        var respuesta = await _api.Aprobar(solicitud.Id);

        // Esc. 1: aprobada con su fecha de resolución.
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var aprobada = (await Leer<SolicitudFranjasFijasDto>(respuesta))!;
        Assert.Equal(EstadoSolicitud.Aprobada, aprobada.Estado);
        Assert.NotNull(aprobada.ResueltaEnUtc);

        // Esc. 2 y 5: el martes desaparecen 19:00 y 20:00 de esa pista; el miércoles no; fuera de la temporada, nada.
        var martes = await HorasLibres("Pista de pádel 1", Martes);
        Assert.DoesNotContain("19:00", martes);
        Assert.DoesNotContain("20:00", martes);
        Assert.Contains("18:00", martes);
        Assert.Contains("19:00", await HorasLibres("Pista de pádel 1", Miercoles));

        using var ambito = fabrica.Services.CreateScope();
        var julio = new DateOnly(2027, 7, 6);
        Assert.Empty(await ambito.ServiceProvider.GetRequiredService<IConsultaFranjasFijas>().FranjasBloqueadas(pista, julio, julio, Ct));

        // Esc. 3: HU-02 recibe el motivo de la franja bloqueada.
        var consulta = ambito.ServiceProvider.GetRequiredService<IConsultaDisponibilidad>();
        Assert.Equal("FRANJA_BLOQUEADA_CLUB", await consulta.MotivoNoDisponible(pista, Utc(Martes, 19), Ct));

        // Esc. 4: un aviso por cada representante del club, con el resultado, los días y el horario.
        var (_, avisosDespues) = await _api.AvisosDelClub(ClubNorte);
        var nuevos = avisosDespues.Except(avisosAntes).ToList();
        Assert.Equal(representantes, avisosDespues.Count - avisosAntes.Count);
        Assert.All(nuevos, texto =>
        {
            Assert.Contains("ha sido aprobada", texto);
            Assert.Contains("martes de 19:00 a 21:00", texto);
            Assert.Contains("Pista de pádel 1 (Polideportivo Este)", texto);
        });
    }

    [Fact]
    public async Task El_tecnico_ve_primero_las_pendientes()
    {
        await _api.Crear(await _datos.Pista(Este, "Pista de tenis 1"), [1], 8, 9);

        var lista = await _api.Listar(Tecnico);

        var ultimaPendiente = lista.Select((s, i) => (s, i)).Where(x => x.s.Estado == EstadoSolicitud.Pendiente).Max(x => x.i);
        var primeraResuelta = lista.Select((s, i) => (s, i)).Where(x => x.s.Estado != EstadoSolicitud.Pendiente).Select(x => x.i).DefaultIfEmpty(int.MaxValue).Min();
        Assert.True(ultimaPendiente < primeraResuelta);
    }

    [Fact]
    public async Task Un_representante_no_puede_aprobar_y_un_id_inexistente_da_404()
    {
        var solicitud = await _api.Crear(await _datos.Pista(Este, "Pista de tenis 1"), [1], 9, 10);

        var comoRepresentante = await _api.Aprobar(solicitud.Id, RepresentanteNorte);
        var inexistente = await _api.Aprobar(999999);

        Assert.Equal(HttpStatusCode.Forbidden, comoRepresentante.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("NO_ENCONTRADO", await Codigo(inexistente));
    }

    // ── Rechazar (historia 4) ────────────────────────────────────────────────────

    [Fact]
    public async Task Rechazar_deja_las_franjas_libres_y_avisa_al_club()
    {
        var solicitud = await _api.Crear(await _datos.Pista(Este, "Pista de pádel 2"), [3], 19, 21);
        var (representantes, avisosAntes) = await _api.AvisosDelClub(ClubNorte);

        var respuesta = await _api.Rechazar(solicitud.Id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var rechazada = (await Leer<SolicitudFranjasFijasDto>(respuesta))!;
        Assert.Equal(EstadoSolicitud.Rechazada, rechazada.Estado);
        Assert.NotNull(rechazada.ResueltaEnUtc);

        var miercoles = await HorasLibres("Pista de pádel 2", Miercoles);
        Assert.Contains("19:00", miercoles);
        Assert.Contains("20:00", miercoles);

        var (_, avisosDespues) = await _api.AvisosDelClub(ClubNorte);
        Assert.Equal(representantes, avisosDespues.Count - avisosAntes.Count);
        Assert.All(avisosDespues.Except(avisosAntes), texto => Assert.Contains("ha sido rechazada", texto));
    }

    [Fact]
    public async Task Se_puede_rechazar_una_solicitud_con_conflictos()
    {
        var pista = await _datos.Pista(Este, "Pista de tenis 2");
        await _datos.Reserva(pista, Utc(Jueves, 19), EstadoPrueba.Confirmada);
        var solicitud = await _api.Crear(pista, [4], 19, 21);
        Assert.False((await _api.Conflictos(solicitud.Id)).PuedeAprobarse);

        var respuesta = await _api.Rechazar(solicitud.Id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Una_solicitud_ya_resuelta_no_se_vuelve_a_resolver_ni_avisa_otra_vez()
    {
        var pista = await _datos.Pista(Este, "Pista de tenis 1");
        var aprobada = await _api.Crear(pista, [5], 19, 20);
        var rechazada = await _api.Crear(pista, [5], 20, 21);
        Assert.Equal(HttpStatusCode.OK, (await _api.Aprobar(aprobada.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _api.Rechazar(rechazada.Id)).StatusCode);
        var (_, avisosAntes) = await _api.AvisosDelClub(ClubNorte);

        HttpResponseMessage[] intentos =
        [
            await _api.Aprobar(aprobada.Id),
            await _api.Rechazar(aprobada.Id),
            await _api.Aprobar(rechazada.Id),
            await _api.Rechazar(rechazada.Id),
        ];

        foreach (var intento in intentos)
        {
            Assert.Equal(HttpStatusCode.Conflict, intento.StatusCode);
            Assert.Equal("SOLICITUD_YA_RESUELTA", await Codigo(intento));
        }

        var (_, avisosDespues) = await _api.AvisosDelClub(ClubNorte);
        Assert.Equal(avisosAntes.Count, avisosDespues.Count);
    }
}
