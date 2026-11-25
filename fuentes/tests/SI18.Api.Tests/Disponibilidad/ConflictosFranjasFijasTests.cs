using System.Net;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Disponibilidad;
using static SI18.Api.Tests.Disponibilidad.ApiFranjasFijas;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;
using EstadoSolicitudDominio = SI18.Dominio.Comun.EstadoSolicitud;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// Historia 3 de spec-005: los conflictos que impiden aprobar (tasks.md T016; RF-008, RF-009; D8 de 000-core).
/// Polideportivo Oeste. Cada caso usa su pista y su día para no depender de los demás.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ConflictosFranjasFijasTests(FabricaApi fabrica)
{
    private const string Oeste = "Polideportivo Oeste";
    private static readonly DateOnly Martes = new(2026, 12, 22);
    private static readonly DateOnly OtroMartes = new(2026, 12, 29);
    private static readonly DateOnly Jueves = new(2026, 12, 24);
    private static readonly DateOnly Sabado = new(2026, 12, 26);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);
    private readonly ApiFranjasFijas _api = new(fabrica);

    [Fact]
    public async Task Esc1_2_una_reserva_confirmada_sale_sin_datos_personales_e_impide_aprobar()
    {
        var pista = await _datos.Pista(Oeste, "Pista de tenis 1");
        await _datos.Reserva(pista, Utc(Martes, 19), EstadoPrueba.Confirmada);
        var solicitud = await _api.Crear(pista, [2], 19, 21);

        using var tecnico = await fabrica.CrearClienteConSesion(Tecnico);
        var crudo = await tecnico.GetStringAsync($"{Ruta}/{solicitud.Id}/conflictos", Ct);
        var conflictos = await _api.Conflictos(solicitud.Id);

        Assert.False(conflictos.PuedeAprobarse);
        Assert.Contains(conflictos.Reservas, r =>
            r.Fecha == Martes && r.HoraInicio == "19:00" && r.HoraFin == "20:00" && r.Estado == EstadoReservaEnConflicto.Confirmada);
        Assert.DoesNotContain("usuario", crudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("si18.test", crudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ana", crudo, StringComparison.Ordinal);

        var respuesta = await _api.Aprobar(solicitud.Id);
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("SOLICITUD_CON_CONFLICTOS", await Codigo(respuesta));
        Assert.Equal(EstadoSolicitud.Pendiente, (await _api.Listar(Tecnico)).Single(s => s.Id == solicitud.Id).Estado);
    }

    [Fact]
    public async Task Esc3_una_pendiente_vigente_cuenta_y_una_caducada_no()
    {
        var pista = await _datos.Pista(Oeste, "Pista de tenis 2");
        await _datos.Reserva(pista, Utc(OtroMartes, 19), EstadoPrueba.PendienteVigente);
        await _datos.Reserva(pista, Utc(OtroMartes, 20), EstadoPrueba.Caducada);
        var solicitud = await _api.Crear(pista, [2], 19, 21);

        var conflictos = await _api.Conflictos(solicitud.Id);

        Assert.Contains(conflictos.Reservas, r => r.Fecha == OtroMartes && r.HoraInicio == "19:00" && r.Estado == EstadoReservaEnConflicto.PendientePago);
        Assert.DoesNotContain(conflictos.Reservas, r => r.Fecha == OtroMartes && r.HoraInicio == "20:00");
    }

    [Fact]
    public async Task Esc4_una_cancelada_o_una_fuera_del_horario_no_cuentan()
    {
        var pista = await _datos.Pista(Oeste, "Pista de pádel 1");
        await _datos.Reserva(pista, Utc(Martes, 19), EstadoPrueba.Cancelada);
        await _datos.Reserva(pista, Utc(Martes, 21), EstadoPrueba.Confirmada);
        var solicitud = await _api.Crear(pista, [2], 19, 21);

        var conflictos = await _api.Conflictos(solicitud.Id);

        Assert.Empty(conflictos.Reservas);
        Assert.True(conflictos.PuedeAprobarse);
    }

    [Fact]
    public async Task Una_reserva_del_propio_club_tambien_es_un_conflicto()
    {
        var pista = await _datos.Pista(Oeste, "Pista de pádel 1");
        await _datos.Reserva(pista, Utc(Jueves, 19), EstadoPrueba.Confirmada, titular: RepresentanteNorte);
        var solicitud = await _api.Crear(pista, [4], 19, 21, RepresentanteNorte);

        var conflictos = await _api.Conflictos(solicitud.Id);

        Assert.Contains(conflictos.Reservas, r => r.Fecha == Jueves && r.HoraInicio == "19:00");
        Assert.False(conflictos.PuedeAprobarse);
    }

    [Fact]
    public async Task Esc5_si_se_solapa_con_otra_aprobada_no_se_puede_aprobar()
    {
        var pabellon = await _datos.Pista(Oeste, "Pabellón");
        var aprobada = await _datos.FranjaFija(pabellon, [2], 19, 21, club: "Club de Pádel Ejemplo");
        var solicitud = await _api.Crear(pabellon, [2], 20, 22);

        var conflictos = await _api.Conflictos(solicitud.Id);
        var respuesta = await _api.Aprobar(solicitud.Id);

        Assert.Empty(conflictos.Reservas);
        Assert.Contains(conflictos.Solapadas, s => s.Id == aprobada && s.ClubNombre == "Club de Pádel Ejemplo" && s.HoraInicio == "19:00");
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("FRANJA_FIJA_SOLAPADA", await Codigo(respuesta));
    }

    [Fact]
    public async Task Esc6_al_aprobar_una_de_dos_pendientes_solapadas_la_otra_ya_no_se_puede_aprobar()
    {
        var pabellon = await _datos.Pista(Oeste, "Pabellón");
        var primera = await _api.Crear(pabellon, [5], 10, 12);
        var segunda = await _api.Crear(pabellon, [5], 11, 13, RepresentantePadel);
        Assert.True((await _api.Conflictos(segunda.Id)).PuedeAprobarse);

        Assert.Equal(HttpStatusCode.OK, (await _api.Aprobar(primera.Id)).StatusCode);

        Assert.False((await _api.Conflictos(segunda.Id)).PuedeAprobarse);
        Assert.Equal("FRANJA_FIJA_SOLAPADA", await Codigo(await _api.Aprobar(segunda.Id)));
    }

    [Fact]
    public async Task Con_reservas_y_solapes_a_la_vez_el_error_es_el_de_las_reservas()
    {
        var pista = await _datos.Pista(Oeste, "Pista de tenis 1");
        await _datos.Reserva(pista, Utc(Sabado, 10), EstadoPrueba.Confirmada);
        await _datos.FranjaFija(pista, [6], 10, 11, EstadoSolicitudDominio.Aprobada);
        var solicitud = await _api.Crear(pista, [6], 9, 12);

        var respuesta = await _api.Aprobar(solicitud.Id);

        Assert.Equal("SOLICITUD_CON_CONFLICTOS", await Codigo(respuesta));
    }

    [Fact]
    public async Task Un_representante_no_puede_ver_los_conflictos()
    {
        var solicitud = await _api.Crear(await _datos.Pista(Oeste, "Pista de tenis 2"), [1], 8, 9);
        using var representante = await fabrica.CrearClienteConSesion(RepresentanteNorte);

        var respuesta = await representante.GetAsync($"{Ruta}/{solicitud.Id}/conflictos", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
