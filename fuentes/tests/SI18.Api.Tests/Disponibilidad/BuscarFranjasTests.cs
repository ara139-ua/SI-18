using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// <c>GET /api/disponibilidad/franjas</c>: escenarios de aceptación de la historia 1 de spec-001 (tasks.md T011).
/// Polideportivo Norte, «Pádel», fechas del 22 al 28/12/2026. El reloj de la API empieza el lunes 21/12/2026 a las
/// 18:00 de Madrid.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class BuscarFranjasTests(FabricaApi fabrica)
{
    private const string Norte = "Polideportivo Norte";
    private const string Ana = "ciudadana.empadronada@si18.test";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<HttpResponseMessage> Buscar(HttpClient cliente, DateOnly fecha, string centro = Norte, string deporte = "Pádel") =>
        await cliente.GetAsync(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Deporte(deporte)}&centroId={await _datos.Centro(centro)}&fecha={fecha:yyyy-MM-dd}", Ct);

    private async Task<ResultadoBusquedaDto> BuscarOk(DateOnly fecha, string correo = Ana)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);
        var respuesta = await Buscar(cliente, fecha);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<ResultadoBusquedaDto>(OpcionesJson.Predeterminadas, Ct))!;
    }

    private static IEnumerable<string> Horas(ResultadoBusquedaDto resultado, string pista) =>
        resultado.Pistas.Single(p => p.Nombre == pista).Franjas.Select(f => f.HoraInicio);

    private static readonly string[] CatorceHoras =
        Enumerable.Range(8, 14).Select(h => $"{h:00}:00").ToArray();

    // ── Escenarios de la historia 1 ──────────────────────────────────────────────

    [Fact]
    public async Task Esc1_sin_nada_en_la_fecha_salen_las_dos_pistas_con_sus_14_franjas()
    {
        var resultado = await BuscarOk(new DateOnly(2026, 12, 22));

        Assert.Equal(2, resultado.TotalPistas);
        Assert.Equal(["Pista de pádel 1", "Pista de pádel 2"], resultado.Pistas.Select(p => p.Nombre));
        Assert.All(resultado.Pistas, p =>
        {
            Assert.Equal(CatorceHoras, p.Franjas.Select(f => f.HoraInicio));
            Assert.Equal("22:00", p.Franjas[^1].HoraFin);
            Assert.Equal(TipoInstalacion.PistaPadel, p.Tipo);
        });
    }

    [Fact]
    public async Task Esc2_una_confirmada_y_una_pendiente_vigente_ocupan_su_franja_solo_en_su_pista()
    {
        var fecha = new DateOnly(2026, 12, 23);
        var pista1 = await _datos.Pista(Norte, "Pista de pádel 1");
        await _datos.Reserva(pista1, Utc(fecha, 18), EstadoPrueba.Confirmada);
        await _datos.Reserva(pista1, Utc(fecha, 19), EstadoPrueba.PendienteVigente);

        var resultado = await BuscarOk(fecha);

        Assert.DoesNotContain("18:00", Horas(resultado, "Pista de pádel 1"));
        Assert.DoesNotContain("19:00", Horas(resultado, "Pista de pádel 1"));
        Assert.Contains("18:00", Horas(resultado, "Pista de pádel 2"));
        Assert.Contains("19:00", Horas(resultado, "Pista de pádel 2"));
    }

    [Fact]
    public async Task Esc3_una_pendiente_caducada_deja_la_franja_libre()
    {
        var fecha = new DateOnly(2026, 12, 24);
        var pista1 = await _datos.Pista(Norte, "Pista de pádel 1");
        await _datos.Reserva(pista1, Utc(fecha, 18), EstadoPrueba.Caducada);

        Assert.Contains("18:00", Horas(await BuscarOk(fecha), "Pista de pádel 1"));
    }

    [Fact]
    public async Task Esc4_un_cierre_de_10_a_12_quita_las_franjas_de_10_y_11()
    {
        var fecha = new DateOnly(2026, 12, 25);
        var pista2 = await _datos.Pista(Norte, "Pista de pádel 2");
        await _datos.Cierre(pista2, Utc(fecha, 10), Utc(fecha, 12));

        var horas = Horas(await BuscarOk(fecha), "Pista de pádel 2").ToList();

        Assert.DoesNotContain("10:00", horas);
        Assert.DoesNotContain("11:00", horas);
        Assert.Contains("09:00", horas);
        Assert.Contains("12:00", horas);
    }

    [Fact]
    public async Task Esc5_una_franja_fija_de_club_de_19_a_21_quita_las_franjas_de_19_y_20()
    {
        var fecha = new DateOnly(2026, 12, 26);
        var pista1 = await _datos.Pista(Norte, "Pista de pádel 1");
        await _datos.FranjaFija(pista1, [DiaIso(fecha)], 19, 21);

        var horas = Horas(await BuscarOk(fecha), "Pista de pádel 1").ToList();

        Assert.DoesNotContain("19:00", horas);
        Assert.DoesNotContain("20:00", horas);
        Assert.Contains("18:00", horas);
        Assert.Contains("21:00", horas);
    }

    [Fact]
    public async Task Una_franja_fija_con_un_cierre_encima_tampoco_aparece()
    {
        var fecha = new DateOnly(2026, 12, 27);
        var pista2 = await _datos.Pista(Norte, "Pista de pádel 2");
        await _datos.FranjaFija(pista2, [DiaIso(fecha)], 19, 21);
        await _datos.Cierre(pista2, Utc(fecha, 19), Utc(fecha, 20));

        var horas = Horas(await BuscarOk(fecha), "Pista de pádel 2").ToList();

        Assert.DoesNotContain("19:00", horas);
        Assert.DoesNotContain("20:00", horas);
    }

    // Límites de tiempo: con un reloj propio (DatosDisponibilidad.ConServicio), porque el de FabricaApi no
    // retrocede y lo comparten todas las pruebas. Es el mismo ServicioDisponibilidad que usa el endpoint.

    private async Task<ResultadoBusquedaDto> BuscarA(DateTime ahoraUtc, DateOnly fecha) =>
        await _datos.ConServicio(ahoraUtc, async servicio =>
            await servicio.Buscar(await _datos.Deporte("Pádel"), await _datos.Centro(Norte), fecha, SI18.Dominio.Comun.TipoTarifa.Empadronado, Ct));

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Esc6_hoy_a_las_18_solo_salen_las_franjas_que_aun_no_han_empezado()
    {
        var hoy = new DateOnly(2026, 12, 21);

        var a18 = await BuscarA(Utc(hoy, 18), hoy);
        var a1759 = await BuscarA(Utc(hoy, 17, 59) + TimeSpan.FromSeconds(59), hoy);

        Assert.All(a18.Pistas, p => Assert.Equal(["19:00", "20:00", "21:00"], p.Franjas.Select(f => f.HoraInicio)));
        Assert.All(a1759.Pistas, p => Assert.Equal("18:00", p.Franjas[0].HoraInicio));
    }

    [Fact]
    public async Task Esc6_por_la_API_hoy_no_salen_franjas_ya_empezadas()
    {
        var hoy = HorarioMadridHoy();
        var resultado = await BuscarOk(hoy);

        Assert.All(resultado.Pistas, p => Assert.All(p.Franjas, f => Assert.True(f.InicioUtc.ToUniversalTime() > _datos.Ahora)));
    }

    private DateOnly HorarioMadridHoy() => SI18.Dominio.Comun.HorarioMadrid.Hoy(_datos.Ahora);

    [Fact]
    public async Task Las_pistas_salen_por_nombre_y_las_franjas_por_hora()
    {
        var resultado = await BuscarOk(new DateOnly(2026, 12, 22));

        Assert.Equal(resultado.Pistas.Select(p => p.Nombre).Order(StringComparer.Ordinal), resultado.Pistas.Select(p => p.Nombre));
        Assert.All(resultado.Pistas, p => Assert.Equal(p.Franjas.OrderBy(f => f.InicioUtc), p.Franjas));
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task El_dia_del_cambio_de_hora_salen_14_franjas_de_una_hora_real()
    {
        var cambio = new DateOnly(2026, 10, 25);

        var resultado = await BuscarA(new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc), cambio);

        Assert.Equal(2, resultado.Pistas.Count);
        Assert.All(resultado.Pistas, p =>
        {
            Assert.Equal(14, p.Franjas.Count);
            Assert.Equal(new DateTime(2026, 10, 25, 7, 0, 0, DateTimeKind.Utc), p.Franjas[0].InicioUtc);
            Assert.Equal("08:00", p.Franjas[0].HoraInicio);
            Assert.All(p.Franjas, f => Assert.Equal(TimeSpan.FromHours(1), f.FinUtc - f.InicioUtc));
        });
    }

    // ── Errores y permisos ───────────────────────────────────────────────────────

    private static async Task<string?> Codigo(HttpResponseMessage respuesta) =>
        (await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct))
            .RootElement.GetProperty("codigo").GetString();

    [Fact]
    public async Task Sin_sesion_da_401()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await Buscar(cliente, new DateOnly(2026, 12, 22));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("NO_AUTENTICADO", await Codigo(respuesta));
    }

    [Theory]
    [InlineData("personal.norte@si18.test")]
    [InlineData("tecnico@si18.test")]
    public async Task Personal_y_tecnico_reciben_403(string correo)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);

        var respuesta = await Buscar(cliente, new DateOnly(2026, 12, 22));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("ROL_NO_PERMITIDO", await Codigo(respuesta));
    }

    [Fact]
    public async Task Sin_fecha_da_400_con_el_campo_marcado()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await cliente.GetAsync($"/api/disponibilidad/franjas?deporteId={await _datos.Deporte("Pádel")}&centroId={await _datos.Centro(Norte)}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = (await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct)).RootElement;
        Assert.Equal("PETICION_INVALIDA", cuerpo.GetProperty("codigo").GetString());
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty("fecha", out _));
    }

    [Fact]
    public async Task Un_centro_inexistente_da_404()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await cliente.GetAsync($"/api/disponibilidad/franjas?deporteId={await _datos.Deporte("Pádel")}&centroId=999999&fecha=2026-12-22", Ct);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", await Codigo(respuesta));
    }
}
