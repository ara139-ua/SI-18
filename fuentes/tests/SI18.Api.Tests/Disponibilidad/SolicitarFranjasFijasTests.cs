using System.Net;
using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using static SI18.Api.Tests.Disponibilidad.ApiFranjasFijas;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>Historia 1 de spec-005: solicitar franjas fijas (tasks.md T008). Polideportivo Sur.</summary>
[Collection(ColeccionApi.Nombre)]
public class SolicitarFranjasFijasTests(FabricaApi fabrica)
{
    private const string Sur = "Polideportivo Sur";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);
    private readonly ApiFranjasFijas _api = new(fabrica);

    private async Task<HttpResponseMessage> EnviarComoNorte(int instalacion, int[] dias, string inicio, string fin)
    {
        using var cliente = await fabrica.CrearClienteConSesion(RepresentanteNorte);
        return await Enviar(cliente, instalacion, await _datos.Temporada("2026-2027"), dias, inicio, fin);
    }

    [Fact]
    public async Task Esc1_la_solicitud_queda_pendiente_a_nombre_del_club_de_la_sesion()
    {
        var pabellon = await _datos.Pista(Sur, "Pabellón");

        var respuesta = await EnviarComoNorte(pabellon, [4, 2], "19:00", "21:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var solicitud = (await Leer<SolicitudFranjasFijasDto>(respuesta))!;
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Equal(ClubNorte, solicitud.ClubNombre);
        Assert.Equal([2, 4], solicitud.DiasSemana);
        Assert.Equal(("19:00", "21:00"), (solicitud.HoraInicio, solicitud.HoraFin));
        Assert.Null(solicitud.ResueltaEnUtc);
        Assert.EndsWith($"{Ruta}/{solicitud.Id}", respuesta.Headers.Location!.ToString());
        Assert.Contains(await _api.Listar(RepresentanteNorte), s => s.Id == solicitud.Id);
    }

    [Fact]
    public async Task Esc2_una_solicitud_pendiente_no_bloquea_nada()
    {
        var pista = await _datos.Pista(Sur, "Pista de pádel 1");
        await _api.Crear(pista, [2], 19, 21);

        // Martes 9/3/2027 con un reloj propio (lunes 8/3): spec-002 reserva Sur pádel del 22 al 28/12 en
        // CrearReservaTests, y una reserva suya de las 20:00 del 22/12 hacía fallar esta prueba según el orden.
        var martes = new DateOnly(2027, 3, 9);
        var busqueda = await _datos.ConServicio(DatosDisponibilidad.Utc(martes.AddDays(-1), 10), async servicio =>
            await servicio.Buscar(await _datos.Deporte("Pádel"), await _datos.Centro(Sur), martes, SI18.Dominio.Comun.TipoTarifa.Empadronado, Ct));

        var horas = busqueda.Pistas.Single(p => p.InstalacionId == pista).Franjas.Select(f => f.HoraInicio).ToList();
        Assert.Contains("19:00", horas);
        Assert.Contains("20:00", horas);
    }

    [Theory]
    [InlineData("Sala polivalente")]
    [InlineData("Piscina")]
    public async Task Esc3_una_sala_o_una_piscina_no_admiten_franjas_fijas(string instalacion)
    {
        var respuesta = await EnviarComoNorte(await _datos.Pista(Sur, instalacion), [2], "19:00", "21:00");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await Cuerpo(respuesta)).GetProperty("errors").TryGetProperty("instalacionId", out _));
    }

    [Theory]
    [InlineData(new int[0], "19:00", "21:00", "diasSemana")]
    [InlineData(new[] { 2, 2 }, "19:00", "21:00", "diasSemana")]
    [InlineData(new[] { 0 }, "19:00", "21:00", "diasSemana")]
    [InlineData(new[] { 8 }, "19:00", "21:00", "diasSemana")]
    [InlineData(new[] { 2 }, "19:30", "21:00", "horaInicio")]
    [InlineData(new[] { 2 }, "07:00", "09:00", "horaInicio")]
    [InlineData(new[] { 2 }, "20:00", "23:00", "horaFin")]
    [InlineData(new[] { 2 }, "19:00", "19:00", "horaFin")]
    [InlineData(new[] { 2 }, "20:00", "19:00", "horaFin")]
    public async Task Esc4_un_dato_incorrecto_marca_su_campo(int[] dias, string inicio, string fin, string campo)
    {
        var respuesta = await EnviarComoNorte(await _datos.Pista(Sur, "Pabellón"), dias, inicio, fin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await Cuerpo(respuesta);
        Assert.Equal("PETICION_INVALIDA", cuerpo.GetProperty("codigo").GetString());
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty(campo, out _), $"Falta errors.{campo}");
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Esc5_una_temporada_terminada_no_se_puede_elegir()
    {
        // Sin insertar temporadas (InicializadorDatosTests exige Temporadas: 1): con un reloj propio en julio de 2027,
        // «2026-2027» ya ha terminado.
        var pista = await _datos.Pista(Sur, "Pista de tenis 1");
        var temporada = await _datos.Temporada("2026-2027");
        var peticion = new PeticionSolicitudFranjasFijas(pista, temporada, [6], "08:00", "09:00");
        var julio = new DateTime(2027, 7, 1, 10, 0, 0, DateTimeKind.Utc);

        var terminada = await _datos.ConServicioSolicitudes(julio, servicio => servicio.Crear(peticion, 1, Ct));
        var abiertas = await _datos.ConServicioSolicitudes(julio, servicio => servicio.TemporadasAbiertas(Ct));

        Assert.Null(terminada.Solicitud);
        Assert.True(terminada.Errores.ContainsKey("temporadaId"));
        Assert.Empty(abiertas);

        // El último día de la temporada, a las 23:59 de Madrid (21:59 UTC en verano), todavía se acepta.
        var ultimoMinuto = new DateTime(2027, 6, 30, 21, 59, 0, DateTimeKind.Utc);
        var aceptada = await _datos.ConServicioSolicitudes(ultimoMinuto, servicio => servicio.Crear(peticion, 1, Ct));
        Assert.NotNull(aceptada.Solicitud);
    }

    [Fact]
    public async Task Esc6_un_representante_no_ve_las_solicitudes_de_otro_club()
    {
        var solicitud = await _api.Crear(await _datos.Pista(Sur, "Pista de tenis 2"), [1], 9, 10);

        Assert.DoesNotContain(await _api.Listar(RepresentantePadel), s => s.Id == solicitud.Id);
        Assert.All(await _api.Listar(RepresentantePadel), s => Assert.NotEqual(ClubNorte, s.ClubNombre));
    }

    [Theory]
    [InlineData("ciudadana.empadronada@si18.test")]
    [InlineData(Tecnico)]
    public async Task Solo_un_representante_puede_solicitar(string correo)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);

        var respuesta = await Enviar(cliente, await _datos.Pista(Sur, "Pabellón"), await _datos.Temporada("2026-2027"), [2], "19:00", "21:00");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("ROL_NO_PERMITIDO", await Codigo(respuesta));
    }

    [Fact]
    public async Task Sin_sesion_da_401()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await Enviar(cliente, 1, 1, [2], "19:00", "21:00");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_ciudadano_no_puede_ver_el_listado()
    {
        using var cliente = await fabrica.CrearClienteConSesion("ciudadana.empadronada@si18.test");

        var respuesta = await cliente.GetAsync(Ruta, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
