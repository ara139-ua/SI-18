using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;
using EstadoReservaDominio = SI18.Dominio.Comun.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <c>POST /api/reservas/{id}/pago</c> y <c>GET /api/reservas/{id}</c> (specs/002-reservar-pagar tasks.md T019 y
/// siguientes). Polideportivo Este, «Pista de tenis 1» y «Pista de tenis 2», del 22 al 28/12/2026. Las reservas se
/// insertan ya creadas en el instante que pide cada caso, sin adelantar el reloj.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PagarReservaTests(FabricaApi fabrica)
{
    private const string Este = "Polideportivo Este";

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Tenis1() => _datos.Pista(Este, "Pista de tenis 1");

    private Task<int> Tenis2() => _datos.Pista(Este, "Pista de tenis 2");

    private static readonly DateOnly Dia22 = new(2026, 12, 22);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>El barrido que registra la API, como en <c>BarridoCaducidadesTests</c>.</summary>
    private BarridoCaducidades Barrido =>
        fabrica.Services.GetServices<IHostedService>().OfType<BarridoCaducidades>().Single();

    private async Task<HttpResponseMessage> Pagar(string correo, long id, string tarjeta = TarjetaAceptada)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);
        return await PedirPago(cliente, id, tarjeta);
    }

    private static async Task AssertProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(codigo, problema.Codigo);
        Assert.Equal(CodigosError.Detalle(codigo), problema.Detail);
    }

    [Fact]
    public async Task HU1_esc3_con_una_tarjeta_aceptada_queda_confirmada_con_su_codigo_de_acceso()
    {
        var pista = await Tenis1();
        var id = await _datos.Pendiente(Ana, pista, Utc(new DateOnly(2026, 12, 22), 10), _datos.Ahora - TimeSpan.FromMinutes(1));
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirPago(cliente, id, "4000 0000 0000 0001");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var reserva = await Json<ReservaDto>(respuesta);
        Assert.Equal(EstadoReserva.Confirmada, reserva.Estado);
        Assert.Equal(GeneradorCodigoAcceso.Longitud, reserva.CodigoAcceso?.Length);
        Assert.Null(reserva.LimitePagoUtc);
        Assert.Null(reserva.HoraLimitePago);

        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Confirmada, guardada.Estado);
        Assert.Equal(reserva.CodigoAcceso, guardada.CodigoAcceso);
        Assert.Equal(EstadoPago.Completado, guardada.Pago!.Estado);
        Assert.NotNull(guardada.Pago.ResueltoEnUtc);

        var vista = await Json<ReservaDto>(await cliente.GetAsync($"/api/reservas/{id}", TestContext.Current.CancellationToken));
        Assert.Equal(EstadoReserva.Confirmada, vista.Estado);
        Assert.Equal(reserva.CodigoAcceso, vista.CodigoAcceso);
    }

    // ── Historia 2: límites de los 10 minutos ────────────────────────────────────

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task A_los_9_59_se_cobra_y_queda_confirmada()
    {
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia22, 11),
            _datos.Ahora - ValoresNegocio.TiempoBloqueoPago + TimeSpan.FromSeconds(1));

        var respuesta = await Pagar(Ana, id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(EstadoReserva.Confirmada, (await Json<ReservaDto>(respuesta)).Estado);
        Assert.Equal(EstadoPago.Completado, (await _datos.Leer(id)).Pago!.Estado);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU2_esc3_a_los_10_minutos_exactos_caduca_sin_cobrar()
    {
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia22, 12), _datos.Ahora - ValoresNegocio.TiempoBloqueoPago);

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.ReservaCaducada);
        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, guardada.Estado);
        Assert.Equal(EstadoPago.Fallido, guardada.Pago!.Estado);
        Assert.Null(guardada.CodigoAcceso);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Caducada_por_el_barrido_responde_RESERVA_CADUCADA_sin_cambios()
    {
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia22, 13), _datos.Ahora - ValoresNegocio.TiempoBloqueoPago);
        await Barrido.EjecutarUnaVez(Ct);
        var antes = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, antes.Estado);

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.ReservaCaducada);
        var despues = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, despues.Estado);
        Assert.Equal(antes.Pago!.ResueltoEnUtc, despues.Pago!.ResueltoEnUtc);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Caducada_porque_otra_persona_reservo_su_franja_responde_RESERVA_CADUCADA()
    {
        var pista = await Tenis1();
        var inicio = Utc(Dia22, 14);
        var deAna = await _datos.Pendiente(Ana, pista, inicio, _datos.Ahora - ValoresNegocio.TiempoBloqueoPago);
        using (var bruno = await fabrica.CrearClienteConSesion(Bruno))
        {
            Assert.Equal(HttpStatusCode.Created, (await PedirReserva(bruno, pista, inicio, 10.00m)).StatusCode);
        }

        var respuesta = await Pagar(Ana, deAna);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.ReservaCaducada);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Con_el_pago_resuelto_justo_a_los_10_minutos_esta_caducada_por_tiempo()
    {
        var id = await _datos.CaducadaPorTiempo(Ana, await Tenis1(), Utc(Dia22, 15), _datos.Ahora - TimeSpan.FromMinutes(20));

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.ReservaCaducada);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Dos_pagos_simultaneos_de_la_misma_reserva_cobran_una_sola_vez()
    {
        var ahora = _datos.Ahora;
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia22, 16), ahora - TimeSpan.FromMinutes(1));
        var pasarela = new PasarelaContadora();
        var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<string?> Intentar()
        {
            await salida.Task;
            try
            {
                await _datos.ConServicio(ahora, Ana, servicio => servicio.Pagar(id, TarjetaAceptada, Ct), pasarela);
                return null;
            }
            catch (ErrorNegocio error)
            {
                return error.Codigo;
            }
        }

        var intentos = new[] { Task.Run(Intentar, Ct), Task.Run(Intentar, Ct) };
        salida.SetResult();
        var resultados = await Task.WhenAll(intentos);

        Assert.Single(resultados, r => r is null);
        Assert.Single(resultados, r => r == CodigosError.TransicionNoPermitida);
        Assert.Equal(1, pasarela.Cobros);
        Assert.Equal(EstadoReservaDominio.Confirmada, (await _datos.Leer(id)).Estado);
    }
}
