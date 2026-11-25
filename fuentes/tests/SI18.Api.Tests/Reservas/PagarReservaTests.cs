using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Reservas;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
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

    // ── Historia 3: pago rechazado y formato de la tarjeta ───────────────────────

    private Task<long> PendienteDeAna(int pista, int hora) =>
        _datos.Pendiente(Ana, pista, Utc(Dia22, hora), _datos.Ahora - TimeSpan.FromMinutes(1));

    [Fact]
    public async Task HU3_esc1_con_una_tarjeta_rechazada_queda_caducada_sin_codigo()
    {
        var id = await PendienteDeAna(await Tenis2(), 10);

        var respuesta = await Pagar(Ana, id, TarjetaRechazada);

        await AssertProblema(respuesta, HttpStatusCode.UnprocessableEntity, CodigosError.PagoRechazado);
        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, guardada.Estado);
        Assert.Equal(EstadoPago.Fallido, guardada.Pago!.Estado);
        Assert.Null(guardada.CodigoAcceso);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU3_esc2_volver_a_pagar_tras_un_rechazo_es_TRANSICION_NO_PERMITIDA_sin_cobro()
    {
        var id = await PendienteDeAna(await Tenis2(), 11);
        await Pagar(Ana, id, TarjetaRechazada);
        var antes = await _datos.Leer(id);

        var respuesta = await Pagar(Ana, id, TarjetaAceptada);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.TransicionNoPermitida);
        var despues = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, despues.Estado);
        Assert.Null(despues.CodigoAcceso);
        Assert.Equal(antes.Pago!.ResueltoEnUtc, despues.Pago!.ResueltoEnUtc);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Rechazada_a_los_9_59_sigue_siendo_TRANSICION_NO_PERMITIDA_a_los_10_01()
    {
        var creada = _datos.Ahora - TimeSpan.FromMinutes(30);
        var id = await _datos.Pendiente(Ana, await Tenis2(), Utc(Dia22, 12), creada);
        var pasarela = new PasarelaContadora();

        var rechazo = await Assert.ThrowsAsync<ErrorNegocio>(() => _datos.ConServicio(
            creada + ValoresNegocio.TiempoBloqueoPago - TimeSpan.FromSeconds(1), Ana,
            servicio => servicio.Pagar(id, TarjetaRechazada, Ct), pasarela));
        var reintento = await Assert.ThrowsAsync<ErrorNegocio>(() => _datos.ConServicio(
            creada + ValoresNegocio.TiempoBloqueoPago + TimeSpan.FromSeconds(1), Ana,
            servicio => servicio.Pagar(id, TarjetaAceptada, Ct), pasarela));

        Assert.Equal(CodigosError.PagoRechazado, rechazo.Codigo);
        Assert.Equal(CodigosError.TransicionNoPermitida, reintento.Codigo);
        Assert.Equal(1, pasarela.Cobros);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Con_el_pago_resuelto_antes_de_los_10_minutos_no_esta_caducada_por_tiempo()
    {
        var id = await _datos.CaducadaPorRechazo(Ana, await Tenis2(), Utc(Dia22, 13), _datos.Ahora - TimeSpan.FromMinutes(20));

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.TransicionNoPermitida);
    }

    [Fact]
    public async Task Pagar_dos_veces_es_TRANSICION_NO_PERMITIDA_y_conserva_el_codigo()
    {
        var id = await PendienteDeAna(await Tenis2(), 14);
        var codigo = (await Json<ReservaDto>(await Pagar(Ana, id))).CodigoAcceso;

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.TransicionNoPermitida);
        Assert.NotNull(codigo);
        Assert.Equal(codigo, (await _datos.Leer(id)).CodigoAcceso);
    }

    [Theory]
    [InlineData("4000 0000 0000 001")]
    [InlineData("4000-0000-0000-000A")]
    public async Task HU3_esc4_una_tarjeta_con_formato_no_valido_da_400_y_no_cambia_nada(string tarjeta)
    {
        var hora = tarjeta.Contains('-', StringComparison.Ordinal) ? 16 : 15;
        var id = await PendienteDeAna(await Tenis2(), hora);

        var respuesta = await Pagar(Ana, id, tarjeta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema(respuesta);
        Assert.Equal(CodigosError.PeticionInvalida, problema.Codigo);
        Assert.Equal(["Escribe los 16 dígitos de la tarjeta."], problema.Errors!["numeroTarjeta"]);
        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.PendientePago, guardada.Estado);
        Assert.Equal(EstadoPago.Pendiente, guardada.Pago!.Estado);
    }

    [Fact]
    public async Task HU3_esc3_tras_un_rechazo_la_franja_vuelve_a_estar_libre()
    {
        var pista = await Tenis2();
        var inicio = Utc(Dia22, 17);
        var id = await _datos.Pendiente(Ana, pista, inicio, _datos.Ahora - TimeSpan.FromMinutes(1));
        await Pagar(Ana, id, TarjetaRechazada);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var busqueda = await cliente.GetFromJsonAsync<ResultadoBusquedaDto>(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Disponibilidad.Deporte("Tenis")}&centroId={await _datos.Disponibilidad.Centro(Este)}&fecha=2026-12-22",
            OpcionesJson.Predeterminadas, Ct);
        var nueva = await PedirReserva(cliente, pista, inicio, 6.00m);

        Assert.Contains(busqueda!.Pistas.Single(p => p.InstalacionId == pista).Franjas, f => f.InicioUtc == inicio);
        Assert.Equal(HttpStatusCode.Created, nueva.StatusCode);
    }

    [Fact]
    public async Task HU5_esc4_pagar_la_reserva_de_otra_persona_da_404_y_no_la_cambia()
    {
        var id = await _datos.Pendiente(Bruno, await Tenis2(), Utc(Dia22, 18), _datos.Ahora - TimeSpan.FromMinutes(1));

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.NotFound, CodigosError.NoEncontrado);
        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.PendientePago, guardada.Estado);
        Assert.Equal(EstadoPago.Pendiente, guardada.Pago!.Estado);
    }

    // ── Fase 9: cierre posterior ─────────────────────────────────────────────────

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task Una_pendiente_vigente_caducada_por_un_cierre_responde_TRANSICION_NO_PERMITIDA_sin_cobro()
    {
        var pista = await Tenis1();
        var inicio = Utc(Dia22, 17);
        var id = await _datos.Pendiente(Ana, pista, inicio, _datos.Ahora - TimeSpan.FromMinutes(1));
        using (var ambito = fabrica.Services.CreateScope())
        {
            // Como lo hará HU-07: la transacción la abre y la confirma quien llama.
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            await using var transaccion = await db.Database.BeginTransactionAsync(Ct);
            await ambito.ServiceProvider.GetRequiredService<IServicioReservas>()
                .AplicarCierre(pista, inicio, inicio + ValoresNegocio.DuracionFranja, Ct);
            await db.SaveChangesAsync(Ct);
            await transaccion.CommitAsync(Ct);
        }

        var antes = await _datos.Leer(id);

        var respuesta = await Pagar(Ana, id);

        await AssertProblema(respuesta, HttpStatusCode.Conflict, CodigosError.TransicionNoPermitida);
        var despues = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Caducada, despues.Estado);
        Assert.Null(despues.CodigoAcceso);
        Assert.Equal(antes.Pago!.ResueltoEnUtc, despues.Pago!.ResueltoEnUtc);
    }

    // ── Pulido: la tarjeta no se registra (R9) ───────────────────────────────────

    [Fact]
    public async Task El_numero_de_tarjeta_no_aparece_en_ningun_registro()
    {
        var id = await _datos.Pendiente(Ana, await Tenis1(), Utc(Dia22, 18), _datos.Ahora - TimeSpan.FromMinutes(1));
        var registro = new RegistroDePrueba();

        // La misma API y la misma base, con todos los registros (también Debug y Trace) capturados.
        await using var api = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddLogging(l => l.AddProvider(registro).SetMinimumLevel(LogLevel.Trace))));
        using var cliente = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var sesion = await cliente.PostAsJsonAsync("/api/sesion",
            new PeticionInicioSesion { Correo = Ana, Contrasena = FabricaApi.ClaveDemo }, OpcionesJson.Predeterminadas, Ct);
        Assert.True(sesion.IsSuccessStatusCode);

        var respuesta = await PedirPago(cliente, id, TarjetaAceptada);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotEmpty(registro.Mensajes);
        Assert.DoesNotContain(registro.Mensajes, m => m.Contains(TarjetaAceptada, StringComparison.Ordinal));
        Assert.DoesNotContain(registro.Mensajes, m => m.Contains("0001", StringComparison.Ordinal));
    }

    /// <summary>Guarda el texto de cada mensaje registrado, con su excepción si la hay.</summary>
    private sealed class RegistroDePrueba : ILoggerProvider
    {
        public ConcurrentQueue<string> Mensajes { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Registrador(Mensajes);

        public void Dispose()
        {
        }

        private sealed class Registrador(ConcurrentQueue<string> mensajes) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                mensajes.Enqueue($"{formatter(state, exception)} {exception}");
        }
    }
}
