using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SI18.Api.Tests.Infraestructura;
using SI18.Api.Tests.Disponibilidad;
using SI18.Api.Tests.Reservas;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;
using EstadoSolicitud = SI18.Contratos.Disponibilidad.EstadoSolicitud;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// Verificación integrada del enunciado (5.4) como pruebas automáticas: dos recorridos que cruzan historias, por
/// HTTP, con la misma API y la misma base de datos que el resto de pruebas pero con un reloj propio
/// (<see cref="FakeTimeProvider"/>) en una fábrica derivada de <see cref="FabricaApi"/>. Documentados en
/// <c>docs/verificacion-integrada.md</c>.
/// <list type="bullet">
/// <item>R1 (satisfactorio): HU-01 → HU-02 → HU-06. Polideportivo Este, «Pista de tenis 1», jueves 11/03/2027 de
/// 10:00 a 11:00, con <c>ciudadana2.empadronada</c> y <c>personal.este</c>.</item>
/// <item>R2 (error, permiso y estado): HU-05 → HU-01 → HU-02 → HU-06. Polideportivo Norte, «Pista de pádel 2»,
/// lunes de 08:00 a 09:00 (la del 15/03/2027), con <c>representante.norte</c>, <c>tecnico</c>, <c>ciudadana.empadronada</c>,
/// <c>personal.oeste</c> y el código de R1.</item>
/// </list>
/// Fecha y pistas elegidas con <c>git grep</c>: ninguna otra prueba reserva ese jueves en Este «Pista de tenis 1»
/// ni usa los lunes de 08:00 a 09:00 en Norte «Pista de pádel 2» (las franjas fijas aprobadas valen toda la temporada).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RecorridosIntegradosTests(FabricaApi fabrica) : IAsyncLifetime
{
    private const string Este = "Polideportivo Este";
    private const string Norte = "Polideportivo Norte";
    private const string PistaR1 = "Pista de tenis 1";
    private const string PistaR2 = "Pista de pádel 2";
    private const string Carla = "ciudadana2.empadronada@si18.test";
    private const string Ana = "ciudadana.empadronada@si18.test";
    private const string RepresentanteNorte = "representante.norte@si18.test";
    private const string Tecnico = "tecnico@si18.test";
    private const string PersonalEste = "personal.este@si18.test";
    private const string PersonalOeste = "personal.oeste@si18.test";

    private static readonly DateOnly JuevesR1 = new(2027, 3, 11);
    private static readonly DateOnly LunesR2 = new(2027, 3, 15);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);
    private readonly DatosValidaciones _validaciones = new(fabrica);
    private readonly FakeTimeProvider _reloj = new(DateTimeOffset.UtcNow);
    private WebApplicationFactory<Program> _api = null!;

    public ValueTask InitializeAsync()
    {
        // Misma API y misma base de datos que fabrica, con su propio reloj.
        _api = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(_reloj);
        }));
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    private async Task<HttpClient> Sesion(string correo)
    {
        var cliente = _api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new PeticionInicioSesion { Correo = correo, Contrasena = FabricaApi.ClaveDemo },
            OpcionesJson.Predeterminadas, Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return cliente;
    }

    private void Ahora(DateOnly dia, int hora, int minuto) =>
        _reloj.SetUtcNow(new DateTimeOffset(DatosDisponibilidad.Utc(dia, hora, minuto), TimeSpan.Zero));

    private async Task<ResultadoBusquedaDto> Buscar(HttpClient cliente, string centro, string deporte, DateOnly fecha)
    {
        var ruta = $"/api/disponibilidad/franjas?deporteId={await _datos.Deporte(deporte)}&centroId={await _datos.Centro(centro)}&fecha={fecha:yyyy-MM-dd}";
        var respuesta = await cliente.GetAsync(ruta, Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<ResultadoBusquedaDto>(OpcionesJson.Predeterminadas, Ct))!;
    }

    // ── R1 · HU-01 → HU-02 → HU-06 ───────────────────────────────────────────────

    [Fact]
    public async Task R1_buscar_en_Este_reservar_y_pagar_ver_el_codigo_en_Mis_reservas_y_validarlo_dos_veces()
    {
        // Precondición: son las 09:50 de Madrid; la franja de las 10:00 empieza en 10 minutos y el QR vale desde las 09:45.
        Ahora(JuevesR1, 9, 50);
        var inicioUtc = DatosDisponibilidad.Utc(JuevesR1, 10);
        var pista = await _datos.Pista(Este, PistaR1);

        // HU-01: la franja de las 10:00 de «Pista de tenis 1» sale en la búsqueda de Este.
        using var carla = await Sesion(Carla);
        var busqueda = await Buscar(carla, Este, "Tenis", JuevesR1);
        var franja = busqueda.Pistas.Single(p => p.InstalacionId == pista).Franjas.Single(f => f.InicioUtc == inicioUtc);
        Assert.Equal("10:00", franja.HoraInicio);

        // HU-02: reservar con el importe que mostró la búsqueda y pagar.
        var resumen = await Json<ResumenReservaDto>(await PedirResumen(carla, pista, inicioUtc));
        Assert.Equal(franja.Precio, resumen.Importe);
        var creada = await PedirReserva(carla, pista, inicioUtc, resumen.Importe);
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var pendiente = await Json<ReservaDto>(creada);
        Assert.Equal(EstadoReserva.PendientePago, pendiente.Estado);
        Assert.Null(pendiente.CodigoAcceso);

        var pagada = await PedirPago(carla, pendiente.Id, TarjetaAceptada);
        Assert.Equal(HttpStatusCode.OK, pagada.StatusCode);
        var confirmada = await Json<ReservaDto>(pagada);
        Assert.Equal(EstadoReserva.Confirmada, confirmada.Estado);
        Assert.False(string.IsNullOrEmpty(confirmada.CodigoAcceso));

        // «Mis reservas»: la confirmada trae su código.
        var mias = await carla.GetFromJsonAsync<List<ReservaDto>>("/api/reservas", OpcionesJson.Predeterminadas, Ct);
        var enLista = mias!.Single(r => r.Id == pendiente.Id);
        Assert.Equal(EstadoReserva.Confirmada, enLista.Estado);
        Assert.Equal(confirmada.CodigoAcceso, enLista.CodigoAcceso);

        // HU-06: personal.este valida dentro de la ventana (09:52) y otra vez a las 09:55.
        using var personal = await Sesion(PersonalEste);
        Ahora(JuevesR1, 9, 52);
        var primera = await DatosValidaciones.Validar(personal, confirmada.CodigoAcceso);
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        var aceptada1 = await Json<ValidacionAccesoDto>(primera);
        Assert.Equal("Carla Vecina", aceptada1.Titular);
        Assert.Equal(PistaR1, aceptada1.Instalacion);
        Assert.Equal(Este, aceptada1.Centro);
        Assert.Equal("10:00", aceptada1.HoraInicio);
        Assert.Null(aceptada1.ValidacionAnteriorUtc);
        Assert.Null(aceptada1.ValidacionAnteriorHora);

        Ahora(JuevesR1, 9, 55);
        var segunda = await DatosValidaciones.Validar(personal, confirmada.CodigoAcceso);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        var aceptada2 = await Json<ValidacionAccesoDto>(segunda);
        Assert.Equal(DatosDisponibilidad.Utc(JuevesR1, 9, 52), aceptada2.ValidacionAnteriorUtc);
        Assert.Equal("09:52", aceptada2.ValidacionAnteriorHora);

        // Las dos quedan registradas y la reserva no cambia (RF-008, RF-010).
        var filas = await _validaciones.DeLaReserva(pendiente.Id);
        Assert.Equal(2, filas.Count);
        Assert.All(filas, v => Assert.True(v.Aceptada));
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.Confirmada, (await new DatosReservas(fabrica).Leer(pendiente.Id)).Estado);
    }

    // ── R2 · HU-05 → HU-01 → HU-02 → HU-06 ───────────────────────────────────────

    [Fact]
    public async Task R2_franja_fija_aprobada_bloquea_la_reserva_el_personal_de_otro_centro_y_una_ciudadana_no_validan()
    {
        // El código de R1: una reserva confirmada de Este, creada con la API y con su propio reloj.
        var codigoR1 = await ConfirmadaDeEste(new DateOnly(2027, 3, 12));

        Ahora(LunesR2, 7, 0);
        var inicioUtc = DatosDisponibilidad.Utc(LunesR2, 8);
        var norte = await _datos.Pista(Norte, PistaR2);

        // HU-01 (antes): la franja de las 08:00 de Norte «Pista de pádel 2» está libre.
        using var ana = await Sesion(Ana);
        var antes = await Buscar(ana, Norte, "Pádel", LunesR2);
        var franja = antes.Pistas.Single(p => p.InstalacionId == norte).Franjas.Single(f => f.InicioUtc == inicioUtc);

        // HU-05: el representante del club solicita todos los lunes de 08:00 a 09:00 y el técnico la aprueba.
        using var representante = await Sesion(RepresentanteNorte);
        var temporada = await _datos.Temporada("2026-2027");
        var solicitada = await representante.PostAsJsonAsync(
            "/api/solicitudes-franjas-fijas",
            new PeticionSolicitudFranjasFijas(norte, temporada, [1], "08:00", "09:00"), OpcionesJson.Predeterminadas, Ct);
        Assert.Equal(HttpStatusCode.Created, solicitada.StatusCode);
        var solicitud = (await solicitada.Content.ReadFromJsonAsync<SolicitudFranjasFijasDto>(OpcionesJson.Predeterminadas, Ct))!;
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);

        using var tecnico = await Sesion(Tecnico);
        var aprobada = await tecnico.PostAsync($"/api/solicitudes-franjas-fijas/{solicitud.Id}/aprobacion", content: null, Ct);
        Assert.Equal(HttpStatusCode.OK, aprobada.StatusCode);
        Assert.Equal(EstadoSolicitud.Aprobada, (await aprobada.Content.ReadFromJsonAsync<SolicitudFranjasFijasDto>(OpcionesJson.Predeterminadas, Ct))!.Estado);

        // HU-01 (después): la franja ya no sale; las demás de esa pista sí.
        var despues = await Buscar(ana, Norte, "Pádel", LunesR2);
        var horas = despues.Pistas.Single(p => p.InstalacionId == norte).Franjas.Select(f => f.HoraInicio).ToList();
        Assert.DoesNotContain("08:00", horas);
        Assert.Contains("09:00", horas);

        // HU-02: reservarla da FRANJA_BLOQUEADA_CLUB y no se crea nada.
        var reservada = await PedirReserva(ana, norte, inicioUtc, franja.Precio);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reservada.StatusCode);
        Assert.Equal(CodigosError.FranjaBloqueadaClub, (await Problema(reservada)).Codigo);
        Assert.Empty(await new DatosReservas(fabrica).DeLaFranja(norte, inicioUtc));

        // HU-06: personal.oeste con el código de R1 (una reserva de Este) → 403 QR_OTRO_CENTRO, sin datos de la reserva.
        var reservaR1 = await ReservaDelCodigo(codigoR1);
        var antesIntentos = (await _validaciones.DeLaReserva(reservaR1)).Count;
        using var oeste = await Sesion(PersonalOeste);
        var otroCentro = await DatosValidaciones.Validar(oeste, codigoR1);
        Assert.Equal(HttpStatusCode.Forbidden, otroCentro.StatusCode);
        var cuerpo = (await JsonDocument.ParseAsync(await otroCentro.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct)).RootElement;
        Assert.Equal(CodigosError.QrOtroCentro, cuerpo.GetProperty("codigo").GetString());
        Assert.False(cuerpo.TryGetProperty("reserva", out _));
        var validacionId = cuerpo.GetProperty("validacionId").GetInt64();

        // …y el intento queda registrado con la reserva, rechazado y con ese motivo.
        var intentos = await _validaciones.DeLaReserva(reservaR1);
        Assert.Equal(antesIntentos + 1, intentos.Count);
        var fila = await _validaciones.Leer(validacionId);
        Assert.False(fila.Aceptada);
        Assert.Equal(CodigosError.QrOtroCentro, fila.MotivoRechazo);
        Assert.Equal(reservaR1, fila.ReservaId);
        Assert.Equal(await _datos.Usuario(PersonalOeste), fila.PersonalId);

        // HU-06: una ciudadana en POST /api/validaciones-acceso → 403 ROL_NO_PERMITIDO y no se registra nada.
        var ultimo = await _validaciones.UltimoId(PersonalEste);
        var porCiudadana = await DatosValidaciones.Validar(ana, codigoR1);
        Assert.Equal(HttpStatusCode.Forbidden, porCiudadana.StatusCode);
        Assert.Equal(CodigosError.RolNoPermitido, (await Problema(porCiudadana)).Codigo);
        Assert.Equal(antesIntentos + 1, (await _validaciones.DeLaReserva(reservaR1)).Count);
        Assert.Equal(ultimo, await _validaciones.UltimoId(PersonalEste));
    }

    /// <summary>Una confirmada de Este creada por HTTP (viernes 12/03/2027 de 10:00 a 11:00 en «Pista de tenis 2»): su código y nada más.</summary>
    private async Task<string> ConfirmadaDeEste(DateOnly dia)
    {
        Ahora(dia, 9, 50);
        var inicioUtc = DatosDisponibilidad.Utc(dia, 10);
        var pista = await _datos.Pista(Este, "Pista de tenis 2");
        using var carla = await Sesion(Carla);
        var resumen = await Json<ResumenReservaDto>(await PedirResumen(carla, pista, inicioUtc));
        var creada = await Json<ReservaDto>(await PedirReserva(carla, pista, inicioUtc, resumen.Importe));
        var pagada = await PedirPago(carla, creada.Id, TarjetaAceptada);
        Assert.Equal(HttpStatusCode.OK, pagada.StatusCode);
        return (await Json<ReservaDto>(pagada)).CodigoAcceso!;
    }

    private async Task<long> ReservaDelCodigo(string codigo)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<SI18.Api.Infraestructura.Si18DbContext>();
        return db.Reservas.Where(r => r.CodigoAcceso == codigo).Select(r => r.Id).Single();
    }
}
