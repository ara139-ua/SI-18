using System.Globalization;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Modulos.Disponibilidad.Provisional;
using SI18.Api.Modulos.Reservas;
using SI18.Api.Tests.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using Rol = SI18.Dominio.Comun.Rol;
using TipoTarifa = SI18.Dominio.Comun.TipoTarifa;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Ayudante de las pruebas de spec-002 (tasks.md T011, «Datos de las pruebas de integración»). Se apoya en
/// <see cref="DatosDisponibilidad"/> sin cambiarlo: inserta reservas en cada estado directamente en la base de
/// pruebas, con el usuario y el instante de creación pedidos, y construye un <see cref="ServicioReservar"/> con su
/// propio reloj para los límites de tiempo.
/// </summary>
public sealed class DatosReservas(FabricaApi fabrica)
{
    public const string Ana = "ciudadana.empadronada@si18.test";
    public const string Bruno = "ciudadano.noempadronado@si18.test";
    public const string Elena = "representante.padel@si18.test";
    public const string Diego = "representante.norte@si18.test";

    public const string TarjetaAceptada = "4000000000000001";
    public const string TarjetaRechazada = "4000000000000002";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DatosDisponibilidad Disponibilidad { get; } = new(fabrica);

    /// <summary>La hora de la API. Otras pruebas la adelantan: nunca se supone que son las 18:00 en punto.</summary>
    public DateTime Ahora => fabrica.Reloj.GetUtcNow().UtcDateTime;

    public Task<int> Pista(string centro, string pista) => Disponibilidad.Pista(centro, pista);

    public Task<int> Usuario(string correo) => Disponibilidad.Usuario(correo);

    /// <summary>Instante UTC de una fecha y hora de pared de Madrid.</summary>
    public static DateTime Utc(DateOnly fecha, int hora, int minuto = 0) => DatosDisponibilidad.Utc(fecha, hora, minuto);

    // ── Reservas insertadas directamente ─────────────────────────────────────────

    /// <summary>Pendiente de pago creada en <paramref name="creadaEnUtc"/>.</summary>
    public Task<long> Pendiente(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc) =>
        Insertar(correo, pistaId, inicioUtc, creadaEnUtc, _ => { });

    /// <summary>Confirmada (pago completado) 30 s después de crearse.</summary>
    public Task<long> Confirmada(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc) =>
        Insertar(correo, pistaId, inicioUtc, creadaEnUtc,
            r => r.Confirmar(GeneradorCodigoAcceso.Nuevo(), creadaEnUtc + TimeSpan.FromSeconds(30)));

    /// <summary>Caducada por tiempo: su pago se resolvió justo en <c>creada + TiempoBloqueoPago</c> (barrido).</summary>
    public Task<long> CaducadaPorTiempo(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc) =>
        Insertar(correo, pistaId, inicioUtc, creadaEnUtc, r => r.Caducar(creadaEnUtc + ValoresNegocio.TiempoBloqueoPago));

    /// <summary>Caducada por un pago rechazado: su pago se resolvió 1 min después de crearse.</summary>
    public Task<long> CaducadaPorRechazo(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc) =>
        Insertar(correo, pistaId, inicioUtc, creadaEnUtc, r => r.Caducar(creadaEnUtc + TimeSpan.FromMinutes(1)));

    /// <summary>Confirmada y cancelada por su titular 1 min después de crearse.</summary>
    public Task<long> Cancelada(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc) =>
        Insertar(correo, pistaId, inicioUtc, creadaEnUtc, r =>
        {
            r.Confirmar(GeneradorCodigoAcceso.Nuevo(), creadaEnUtc + TimeSpan.FromSeconds(30));
            r.Cancelar(MotivoCancelacion.Usuario, creadaEnUtc + TimeSpan.FromMinutes(1));
        });

    /// <summary>La reserva tal como está en la base, con su pago.</summary>
    public async Task<Reserva> Leer(long id)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.Reservas.AsNoTracking().Include(r => r.Pago).SingleAsync(r => r.Id == id, Ct);
    }

    /// <summary>Reservas de una franja, en cualquier estado.</summary>
    public async Task<List<Reserva>> DeLaFranja(int pistaId, DateTime inicioUtc)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.Reservas.AsNoTracking().Include(r => r.Pago)
            .Where(r => r.InstalacionId == pistaId && r.InicioUtc == inicioUtc)
            .OrderBy(r => r.Id)
            .ToListAsync(Ct);
    }

    // ── Servicio con reloj propio ────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta <paramref name="accion"/> con un <see cref="ServicioReservar"/> cuyo reloj está fijado en
    /// <paramref name="ahoraUtc"/> (también el de su <see cref="ServicioDisponibilidad"/> y su
    /// <see cref="ConsultaReservas"/>), con la sesión de <paramref name="correo"/> y sobre la misma base de datos.
    /// Cada llamada usa su propio ámbito y su propio <see cref="Si18DbContext"/>, como una petición. Mismo motivo que
    /// <c>DatosDisponibilidad.ConServicio</c>: <c>FakeTimeProvider</c> no retrocede y el reloj de la fábrica es compartido.
    /// </summary>
    public async Task<T> ConServicio<T>(
        DateTime ahoraUtc, string correo, Func<ServicioReservar, Task<T>> accion, IPasarelaPago? pasarela = null)
    {
        var usuario = await LeerUsuario(correo);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var reloj = new FakeTimeProvider(new DateTimeOffset(ahoraUtc, TimeSpan.Zero));
        var disponibilidad = new ServicioDisponibilidad(
            db,
            reloj,
            new ConsultaReservas(db, reloj),
            new ConsultaCierresProvisional(db),
            new ConsultaFranjasFijas(db, new ConsultaCierresProvisional(db)));
        var servicio = new ServicioReservar(db, reloj, disponibilidad, pasarela ?? new PasarelaSimulada(), usuario);

        return await accion(servicio);
    }

    // ── Peticiones HTTP ──────────────────────────────────────────────────────────

    /// <summary>Instante UTC en ISO 8601, como lo entrega <c>GET /api/disponibilidad/franjas</c>.</summary>
    public static string Iso(DateTime utc) => utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static Task<HttpResponseMessage> PedirResumen(HttpClient cliente, int instalacionId, DateTime inicioUtc) =>
        cliente.GetAsync($"/api/reservas/resumen?instalacionId={instalacionId}&inicioUtc={Iso(inicioUtc)}", Ct);

    public static Task<HttpResponseMessage> PedirReserva(HttpClient cliente, int instalacionId, DateTime inicioUtc, decimal importeMostrado) =>
        cliente.PostAsJsonAsync("/api/reservas", new PeticionReserva(instalacionId, inicioUtc, importeMostrado), OpcionesJson.Predeterminadas, Ct);

    public static Task<HttpResponseMessage> PedirPago(HttpClient cliente, long id, string numeroTarjeta) =>
        cliente.PostAsJsonAsync($"/api/reservas/{id}/pago", new PeticionPago(numeroTarjeta), OpcionesJson.Predeterminadas, Ct);

    public static async Task<T> Json<T>(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson.Predeterminadas, Ct))!;

    public static Task<ProblemaDto> Problema(HttpResponseMessage respuesta) => Json<ProblemaDto>(respuesta);

    /// <summary>Hora de pared de Madrid en «HH:mm» (segundos truncados).</summary>
    public static string HoraMadrid(DateTime utc) =>
        HorarioMadrid.ALocal(utc).Hora.ToString("HH':'mm", CultureInfo.InvariantCulture);

    // ── Internos ─────────────────────────────────────────────────────────────────

    private async Task<long> Insertar(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc, Action<Reserva> llevarAlEstado)
    {
        var usuario = await LeerUsuario(correo);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var reserva = new Reserva(usuario.Id, pistaId, inicioUtc, usuario.TipoTarifa!.Value, 6.00m, creadaEnUtc);
        llevarAlEstado(reserva);
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync(Ct);
        return reserva.Id;
    }

    private async Task<UsuarioPrueba> LeerUsuario(string correo)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var u = await db.Usuarios.AsNoTracking().SingleAsync(x => x.Correo == correo, Ct);
        return new UsuarioPrueba(u.Id, u.Rol, CalculadoraTarifa.Para(u.Rol, u.Empadronado), u.CentroId, u.ClubId);
    }

    /// <summary>Usuario de la sesión para un <see cref="ServicioReservar"/> construido a mano.</summary>
    private sealed record UsuarioPrueba(int Id, Rol Rol, TipoTarifa? TipoTarifa, int? CentroId, int? ClubId) : IUsuarioActual;
}

/// <summary>Pasarela de prueba: delega en <see cref="PasarelaSimulada"/> y cuenta los cobros (tasks.md T011, T031).</summary>
public sealed class PasarelaContadora : IPasarelaPago
{
    private readonly PasarelaSimulada _simulada = new();
    private int _cobros;

    public int Cobros => Volatile.Read(ref _cobros);

    public Task<bool> Cobrar(decimal importe, string numeroTarjetaFicticia, CancellationToken ct)
    {
        Interlocked.Increment(ref _cobros);
        return _simulada.Cobrar(importe, numeroTarjetaFicticia, ct);
    }

    public Task Devolver(long pagoId, decimal importe, CancellationToken ct) => _simulada.Devolver(pagoId, importe, ct);
}
