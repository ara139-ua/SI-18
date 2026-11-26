using System.Net;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
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
}
