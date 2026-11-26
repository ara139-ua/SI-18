using System.Net;
using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <c>GET /api/reservas/resumen</c> y <c>POST /api/reservas</c> (specs/002-reservar-pagar tasks.md T018).
/// Polideportivo Sur, «Pista de pádel 1» y «Pista de pádel 2», del 22 al 28/12/2026.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CrearReservaTests(FabricaApi fabrica)
{
    private const string Sur = "Polideportivo Sur";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Padel1() => _datos.Pista(Sur, "Pista de pádel 1");

    // ── Historia 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task HU1_esc1_el_resumen_de_una_franja_libre_trae_pista_centro_fecha_horas_e_importe()
    {
        var pista = await Padel1();
        var inicio = Utc(new DateOnly(2026, 12, 22), 18);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirResumen(cliente, pista, inicio);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resumen = await Json<ResumenReservaDto>(respuesta);
        Assert.Equal(pista, resumen.InstalacionId);
        Assert.Equal("Pista de pádel 1", resumen.Pista);
        Assert.Equal(Sur, resumen.Centro);
        Assert.Equal(new DateOnly(2026, 12, 22), resumen.Fecha);
        Assert.Equal("18:00", resumen.HoraInicio);
        Assert.Equal("19:00", resumen.HoraFin);
        Assert.Equal(inicio, resumen.InicioUtc);
        Assert.Equal(6.00m, resumen.Importe);
        Assert.Empty(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    public async Task HU1_esc2_crear_deja_la_reserva_pendiente_con_su_hora_limite_y_quita_la_franja_de_la_busqueda()
    {
        var pista = await Padel1();
        var fecha = new DateOnly(2026, 12, 22);
        var inicio = Utc(fecha, 19);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);
        var ahora = _datos.Ahora;

        var respuesta = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reserva = await Json<ReservaDto>(respuesta);
        Assert.Equal($"/api/reservas/{reserva.Id}", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(EstadoReserva.PendientePago, reserva.Estado);
        Assert.Equal(ahora + ValoresNegocio.TiempoBloqueoPago, reserva.LimitePagoUtc);
        Assert.Equal(HoraMadrid(ahora + ValoresNegocio.TiempoBloqueoPago), reserva.HoraLimitePago);
        Assert.Null(reserva.CodigoAcceso);
        Assert.Equal("19:00", reserva.HoraInicio);
        Assert.Equal("20:00", reserva.HoraFin);
        Assert.Equal(inicio + ValoresNegocio.DuracionFranja, reserva.FinUtc);

        var guardada = await _datos.Leer(reserva.Id);
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.PendientePago, guardada.Estado);
        Assert.Equal(EstadoPago.Pendiente, guardada.Pago!.Estado);
        Assert.Equal(6.00m, guardada.Pago.Importe);

        var busqueda = await cliente.GetFromJsonAsync<ResultadoBusquedaDto>(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Disponibilidad.Deporte("Pádel")}&centroId={await _datos.Disponibilidad.Centro(Sur)}&fecha={fecha:yyyy-MM-dd}",
            OpcionesJson.Predeterminadas, Ct);
        Assert.DoesNotContain(busqueda!.Pistas.Single(p => p.InstalacionId == pista).Franjas, f => f.InicioUtc == inicio);
    }

    [Fact]
    public async Task HU1_esc4_un_representante_de_club_paga_la_tarifa_de_club()
    {
        var pista = await Padel1();
        var inicio = Utc(new DateOnly(2026, 12, 22), 20);
        using var cliente = await fabrica.CrearClienteConSesion(Elena);

        var resumen = await Json<ResumenReservaDto>(await PedirResumen(cliente, pista, inicio));
        var respuesta = await PedirReserva(cliente, pista, inicio, resumen.Importe);

        Assert.Equal(4.00m, resumen.Importe);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reserva = await Json<ReservaDto>(respuesta);
        Assert.Equal(4.00m, reserva.Importe);
        var guardada = await _datos.Leer(reserva.Id);
        Assert.Equal(SI18.Dominio.Comun.TipoTarifa.Club, guardada.TipoTarifa);
        Assert.Equal(4.00m, guardada.Pago!.Importe);
    }

    // ── Errores ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sin_sesion_da_401()
    {
        using var cliente = fabrica.CreateClient();

        var resumen = await PedirResumen(cliente, await Padel1(), Utc(new DateOnly(2026, 12, 22), 10));
        var crear = await PedirReserva(cliente, await Padel1(), Utc(new DateOnly(2026, 12, 22), 10), 6.00m);

        Assert.Equal(HttpStatusCode.Unauthorized, resumen.StatusCode);
        Assert.Equal(CodigosError.NoAutenticado, (await Problema(resumen)).Codigo);
        Assert.Equal(HttpStatusCode.Unauthorized, crear.StatusCode);
    }

    [Theory]
    [InlineData("personal.sur@si18.test")]
    [InlineData("tecnico@si18.test")]
    public async Task Personal_y_tecnico_reciben_403(string correo)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);
        var inicio = Utc(new DateOnly(2026, 12, 22), 10);

        var resumen = await PedirResumen(cliente, await Padel1(), inicio);
        var crear = await PedirReserva(cliente, await Padel1(), inicio, 6.00m);

        Assert.Equal(HttpStatusCode.Forbidden, resumen.StatusCode);
        Assert.Equal(CodigosError.RolNoPermitido, (await Problema(resumen)).Codigo);
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
        Assert.Equal(CodigosError.RolNoPermitido, (await Problema(crear)).Codigo);
        Assert.Empty(await _datos.DeLaFranja(await Padel1(), inicio));
    }

    [Fact]
    public async Task Sin_inicioUtc_da_400_con_el_campo_marcado()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await cliente.GetAsync($"/api/reservas/resumen?instalacionId={await Padel1()}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(CodigosError.PeticionInvalida, problema.Codigo);
        Assert.Equal(["Indica la hora de inicio."], problema.Errors!["inicioUtc"]);
    }

    [Fact]
    public async Task Crear_sin_inicioUtc_da_400_con_el_campo_marcado()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await cliente.PostAsJsonAsync("/api/reservas", new { instalacionId = await Padel1(), importeMostrado = 6.00m }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(CodigosError.PeticionInvalida, problema.Codigo);
        Assert.True(problema.Errors!.ContainsKey("inicioUtc"));
    }

    [Fact]
    public async Task Importe_mostrado_0_da_400_y_no_crea_nada()
    {
        var pista = await Padel1();
        var inicio = Utc(new DateOnly(2026, 12, 22), 11);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirReserva(cliente, pista, inicio, 0m);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(CodigosError.PeticionInvalida, problema.Codigo);
        Assert.True(problema.Errors!.ContainsKey("importeMostrado"));
        Assert.Empty(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    public async Task Una_instalacion_que_no_existe_da_404()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);
        var inicio = Utc(new DateOnly(2026, 12, 22), 12);

        var resumen = await PedirResumen(cliente, 999999, inicio);
        var crear = await PedirReserva(cliente, 999999, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.NotFound, resumen.StatusCode);
        Assert.Equal(CodigosError.NoEncontrado, (await Problema(resumen)).Codigo);
        Assert.Equal(HttpStatusCode.NotFound, crear.StatusCode);
        Assert.Equal(CodigosError.NoEncontrado, (await Problema(crear)).Codigo);
    }

    [Fact]
    public async Task Las_10_30_no_son_el_inicio_de_una_franja_y_dan_400()
    {
        var pista = await Padel1();
        var inicio = Utc(new DateOnly(2026, 12, 22), 10, 30);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var resumen = await PedirResumen(cliente, pista, inicio);
        var crear = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.BadRequest, resumen.StatusCode);
        Assert.Equal(CodigosError.PeticionInvalida, (await Problema(resumen)).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, crear.StatusCode);
        Assert.Equal(CodigosError.PeticionInvalida, (await Problema(crear)).Codigo);
        Assert.Empty(await _datos.DeLaFranja(pista, inicio));
    }

    // ── Historia 2 ───────────────────────────────────────────────────────────────

    private Task<int> Padel2() => _datos.Pista(Sur, "Pista de pádel 2");

    [Fact]
    public async Task HU2_esc1_con_una_pendiente_vigente_de_otra_persona_la_franja_esta_ocupada()
    {
        var pista = await Padel2();
        var inicio = Utc(new DateOnly(2026, 12, 22), 18);
        await _datos.Pendiente(Ana, pista, inicio, _datos.Ahora - TimeSpan.FromMinutes(1));
        using var cliente = await fabrica.CrearClienteConSesion(Bruno);

        var respuesta = await PedirReserva(cliente, pista, inicio, 10.00m);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(CodigosError.FranjaOcupada, problema.Codigo);
        Assert.Equal(CodigosError.Detalle(CodigosError.FranjaOcupada), problema.Detail);
        Assert.Single(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    public async Task Doble_envio_de_la_misma_reserva_crea_una_sola()
    {
        var pista = await Padel2();
        var inicio = Utc(new DateOnly(2026, 12, 22), 19);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var primera = await PedirReserva(cliente, pista, inicio, 6.00m);
        var segunda = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal(CodigosError.FranjaOcupada, (await Problema(segunda)).Codigo);
        Assert.Single(await _datos.DeLaFranja(pista, inicio));
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU2_esc4_una_pendiente_ajena_de_10_minutos_exactos_se_caduca_al_reservar()
    {
        var pista = await Padel2();
        var inicio = Utc(new DateOnly(2026, 12, 22), 20);
        var ahora = _datos.Ahora;
        var anterior = await _datos.Pendiente(Bruno, pista, inicio, ahora - ValoresNegocio.TiempoBloqueoPago);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var caducada = await _datos.Leer(anterior);
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.Caducada, caducada.Estado);
        Assert.Equal(EstadoPago.Fallido, caducada.Pago!.Estado);
        Assert.Equal(ahora, caducada.Pago.ResueltoEnUtc!.Value, TimeSpan.FromMilliseconds(1));
        var nueva = await _datos.Leer((await Json<ReservaDto>(respuesta)).Id);
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.PendientePago, nueva.Estado);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU2_esc4_una_pendiente_ajena_de_9_minutos_59_segundos_sigue_bloqueando()
    {
        var pista = await Padel2();
        var inicio = Utc(new DateOnly(2026, 12, 22), 21);
        var anterior = await _datos.Pendiente(Bruno, pista, inicio, _datos.Ahora - ValoresNegocio.TiempoBloqueoPago + TimeSpan.FromSeconds(1));
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.FranjaOcupada, (await Problema(respuesta)).Codigo);
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.PendientePago, (await _datos.Leer(anterior)).Estado);
    }
}
