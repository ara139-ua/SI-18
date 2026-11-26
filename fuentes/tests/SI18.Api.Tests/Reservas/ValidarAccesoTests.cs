using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using static SI18.Api.Tests.Reservas.DatosValidaciones;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <c>POST /api/validaciones-acceso</c> por HTTP (specs/006-validar-qr tasks.md T007, T013, T018, T018b, T020).
/// Todas las pruebas que necesitan una confirmada en su ventana usan la misma reserva compartida de Carla en Este
/// «Pista de pádel 2» a la hora en punto actual de <c>fabrica.Reloj</c> (análisis C1), creada de forma idempotente.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ValidarAccesoTests(FabricaApi fabrica) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosValidaciones _datos = new(fabrica);
    private long _compartida;

    public async ValueTask InitializeAsync() => _compartida = await _datos.ConfirmadaCompartida();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── Historia 1: código aceptado ──────────────────────────────────────────────

    [Fact]
    public async Task HU1_esc1_un_codigo_valido_se_acepta_con_el_titular_la_pista_y_la_franja_y_queda_registrado()
    {
        var reserva = await _datos.Reservas.Leer(_compartida);
        var anterior = (await _datos.DeLaReserva(_compartida)).LastOrDefault(v => v.Aceptada);
        var personal = await _datos.Reservas.Usuario(PersonalEste);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, reserva.CodigoAcceso);
        var ahora = _datos.Ahora;

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var dto = await DatosReservas.Json<ValidacionAccesoDto>(respuesta);
        Assert.Equal("Carla Vecina", dto.Titular);
        Assert.Equal(Pista, dto.Instalacion);
        Assert.Equal(Este, dto.Centro);
        Assert.Equal(HorarioMadrid.Hoy(ahora), dto.Fecha);
        Assert.Equal("18:00", dto.HoraInicio);
        Assert.Equal("19:00", dto.HoraFin);

        // La reserva es compartida: otra prueba de la clase puede haberla validado antes (L1 del registro de analyze).
        Assert.Equal(anterior?.FechaUtc, dto.ValidacionAnteriorUtc);
        Assert.Equal(anterior is null ? null : DatosReservas.HoraMadrid(anterior.FechaUtc), dto.ValidacionAnteriorHora);

        var fila = await _datos.Leer(dto.ValidacionId);
        Assert.True(fila.Aceptada);
        Assert.Null(fila.MotivoRechazo);
        Assert.Equal(_compartida, fila.ReservaId);
        Assert.Equal(personal, fila.PersonalId);
        Assert.Equal(reserva.CodigoAcceso, fila.CodigoLeido);
        Assert.Equal(ahora, fila.FechaUtc);
    }

    [Fact]
    public async Task RF004_los_espacios_delante_y_detras_del_codigo_no_cuentan()
    {
        var reserva = await _datos.Reservas.Leer(_compartida);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, $"  {reserva.CodigoAcceso}  ");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var dto = await DatosReservas.Json<ValidacionAccesoDto>(respuesta);
        var fila = await _datos.Leer(dto.ValidacionId);
        Assert.True(fila.Aceptada);
        Assert.Equal(reserva.CodigoAcceso, fila.CodigoLeido);
    }

    // ── Historia 2: rechazos ─────────────────────────────────────────────────────

    [Fact]
    public async Task HU2_esc4_un_codigo_desconocido_se_rechaza_y_se_registra_sin_reserva()
    {
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, "noexiste");

        var problema = await LeerRechazo(respuesta, HttpStatusCode.UnprocessableEntity, CodigosError.QrDesconocido);
        Assert.False(problema.TieneReserva);
        var fila = await _datos.Leer(problema.ValidacionId);
        Assert.False(fila.Aceptada);
        Assert.Null(fila.ReservaId);
        Assert.Equal(CodigosError.QrDesconocido, fila.MotivoRechazo);
        Assert.Equal("noexiste", fila.CodigoLeido);
    }

    [Fact]
    public async Task RF004_el_codigo_con_una_letra_en_otra_caja_es_desconocido_y_se_registra_sin_reserva()
    {
        // T034 (aclaración 2026-11-26): el código se compara distinguiendo mayúsculas y minúsculas.
        var reserva = await _datos.Reservas.Leer(_compartida);
        var codigo = reserva.CodigoAcceso!;
        var posicion = codigo.IndexOf(codigo.First(char.IsLetter));
        var otraCaja = codigo[..posicion] + (char.IsUpper(codigo[posicion]) ? char.ToLowerInvariant(codigo[posicion]) : char.ToUpperInvariant(codigo[posicion])) + codigo[(posicion + 1)..];
        Assert.NotEqual(codigo, otraCaja);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, otraCaja);

        var problema = await LeerRechazo(respuesta, HttpStatusCode.UnprocessableEntity, CodigosError.QrDesconocido);
        Assert.False(problema.TieneReserva);
        var fila = await _datos.Leer(problema.ValidacionId);
        Assert.False(fila.Aceptada);
        Assert.Null(fila.ReservaId);
        Assert.Equal(otraCaja, fila.CodigoLeido);
    }

    [Fact]
    public async Task HU2_esc5_el_codigo_de_otro_centro_se_rechaza_sin_datos_de_la_reserva_pero_se_registra_con_ella()
    {
        var reserva = await _datos.Reservas.Leer(_compartida);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalOeste);

        var respuesta = await Validar(cliente, reserva.CodigoAcceso);

        var problema = await LeerRechazo(respuesta, HttpStatusCode.Forbidden, CodigosError.QrOtroCentro);
        Assert.False(problema.TieneReserva);
        var fila = await _datos.Leer(problema.ValidacionId);
        Assert.False(fila.Aceptada);
        Assert.Equal(_compartida, fila.ReservaId);
        Assert.Equal(CodigosError.QrOtroCentro, fila.MotivoRechazo);
        Assert.Equal(await _datos.Reservas.Usuario(PersonalOeste), fila.PersonalId);
    }

    [Fact]
    public async Task HU2_esc3_una_reserva_cancelada_se_rechaza_con_los_datos_de_la_reserva_y_sin_el_titular()
    {
        var inicio = DatosReservas.Utc(HorarioMadrid.Hoy(_datos.Ahora), HorarioMadrid.ALocal(_datos.Ahora).Hora.Hour);
        var cancelada = await _datos.CanceladaEn(inicio);
        var reserva = await _datos.Reservas.Leer(cancelada);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, reserva.CodigoAcceso);

        var problema = await LeerRechazo(respuesta, HttpStatusCode.UnprocessableEntity, CodigosError.QrReservaNoConfirmada);
        Assert.True(problema.TieneReserva);
        Assert.Equal(Pista, problema.Reserva!.Instalacion);
        Assert.Equal(Este, problema.Reserva.Centro);
        Assert.Equal(HorarioMadrid.Hoy(_datos.Ahora), problema.Reserva.Fecha);
        Assert.Equal("18:00", problema.Reserva.HoraInicio);
        Assert.Equal("19:00", problema.Reserva.HoraFin);
        Assert.DoesNotContain("Carla", problema.Cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("titular", problema.Cuerpo, StringComparison.OrdinalIgnoreCase);
        var fila = await _datos.Leer(problema.ValidacionId);
        Assert.False(fila.Aceptada);
        Assert.Equal(cancelada, fila.ReservaId);
        Assert.Equal(CodigosError.QrReservaNoConfirmada, fila.MotivoRechazo);
    }

    [Fact]
    public async Task Un_codigo_de_100_caracteres_es_desconocido_y_se_registra_recortado_a_64()
    {
        var largo = new string('a', 100);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, largo);

        var problema = await LeerRechazo(respuesta, HttpStatusCode.UnprocessableEntity, CodigosError.QrDesconocido);
        var fila = await _datos.Leer(problema.ValidacionId);
        Assert.Equal(ValidacionAcceso.LongitudMaximaCodigo, fila.CodigoLeido.Length);
        Assert.Equal(largo[..ValidacionAcceso.LongitudMaximaCodigo], fila.CodigoLeido);
    }

    // ── Historia 3: registro de cada intento ─────────────────────────────────────

    [Theory]
    [InlineData("""{ "codigo": "   " }""")]
    [InlineData("""{}""")]
    public async Task HU3_RF004_un_codigo_vacio_es_un_400_con_errors_codigo_y_no_registra_nada(string cuerpo)
    {
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);
        var antes = await _datos.UltimoId(PersonalEste);

        var respuesta = await cliente.PostAsync(
            "/api/validaciones-acceso", new StringContent(cuerpo, Encoding.UTF8, "application/json"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await DatosReservas.Problema(respuesta);
        Assert.Equal(CodigosError.PeticionInvalida, problema.Codigo);
        Assert.Contains("codigo", problema.Errors!.Keys);
        Assert.Equal(antes, await _datos.UltimoId(PersonalEste));
    }

    [Fact]
    public async Task RF010_validar_no_cambia_la_reserva()
    {
        var antes = await _datos.Reservas.Leer(_compartida);
        using var cliente = await fabrica.CrearClienteConSesion(PersonalEste);

        var respuesta = await Validar(cliente, antes.CodigoAcceso);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var despues = await _datos.Reservas.Leer(_compartida);
        Assert.Equal(antes.Estado, despues.Estado);
        Assert.Equal(antes.CodigoAcceso, despues.CodigoAcceso);
        Assert.Equal(antes.CanceladaEnUtc, despues.CanceladaEnUtc);
    }

    /// <summary>
    /// RF-009 (análisis A2): si no se puede registrar el intento, no hay resultado. El fallo se provoca con una persona
    /// del personal cuyo <c>id</c> no existe en <c>usuarios</c> (viola la FK de <c>personal_id</c>). El <c>INSERT</c>
    /// fallido no deja filas: un solo <c>SaveChanges</c>, en su propia transacción.
    /// </summary>
    [Fact]
    public async Task RF009_si_falla_el_registro_la_validacion_lanza_y_no_deja_ninguna_fila()
    {
        var reservaId = await _datos.ConfirmadaALasDiez(Dia25);
        var codigo = (await _datos.Reservas.Leer(reservaId)).CodigoAcceso!;
        var centro = await _datos.Reservas.Disponibilidad.Centro(Este);
        var personalInexistente = new UsuarioPersonalPrueba(int.MaxValue, centro);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _datos.ConServicio(DatosReservas.Utc(Dia25, 9, 50), personalInexistente, s => s.Validar(codigo, Ct)));

        Assert.Empty(await _datos.DeLaReserva(reservaId));
    }

    // ── Historia 4: solo el personal de centro ───────────────────────────────────

    [Fact]
    public async Task HU4_esc1_sin_sesion_es_un_401_y_no_registra_nada()
    {
        var codigo = Guid.NewGuid().ToString("N");
        using var cliente = fabrica.CreateClient();

        var respuesta = await Validar(cliente, codigo);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.NoAutenticado, (await DatosReservas.Problema(respuesta)).Codigo);
        Assert.False(await _datos.HayValidacionConCodigo(codigo));
    }

    [Theory]
    [InlineData("ciudadana2.empadronada@si18.test")]
    [InlineData("representante.padel@si18.test")]
    [InlineData("tecnico@si18.test")]
    public async Task HU4_esc2_los_demas_roles_reciben_un_403_y_no_registran_nada(string correo)
    {
        var codigo = Guid.NewGuid().ToString("N");
        using var cliente = await fabrica.CrearClienteConSesion(correo);

        var respuesta = await Validar(cliente, codigo);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(CodigosError.RolNoPermitido, (await DatosReservas.Problema(respuesta)).Codigo);
        Assert.False(await _datos.HayValidacionConCodigo(codigo));
    }

    // ── Internos ─────────────────────────────────────────────────────────────────

    private sealed record Rechazo(long ValidacionId, ReservaValidadaDto? Reserva, string Cuerpo)
    {
        public bool TieneReserva => Reserva is not null;
    }

    /// <summary>Comprueba estado y código del Problem Details y lee sus extensiones <c>validacionId</c> y <c>reserva</c>.</summary>
    private static async Task<Rechazo> LeerRechazo(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync(Ct);
        Assert.Equal(estado, respuesta.StatusCode);
        using var json = JsonDocument.Parse(cuerpo);
        var raiz = json.RootElement;
        Assert.Equal(codigo, raiz.GetProperty("codigo").GetString());
        var validacionId = raiz.GetProperty("validacionId").GetInt64();

        ReservaValidadaDto? reserva = null;
        if (raiz.TryGetProperty("reserva", out var datos))
        {
            reserva = datos.Deserialize<ReservaValidadaDto>(OpcionesJson.Predeterminadas);
        }

        return new Rechazo(validacionId, reserva, cuerpo);
    }
}
