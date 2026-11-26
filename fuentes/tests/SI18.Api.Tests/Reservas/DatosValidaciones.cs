using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Modulos.Reservas;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Reservas;
using EstadoReserva = SI18.Dominio.Comun.EstadoReserva;
using Rol = SI18.Dominio.Comun.Rol;
using TipoTarifa = SI18.Dominio.Comun.TipoTarifa;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Ayudante de las pruebas de spec-006 (tasks.md T006, «Datos de las pruebas de integración»). Usa titular,
/// personal, pista y fechas que no usa ninguna otra prueba, y se apoya en <see cref="DatosReservas"/> sin cambiarlo.
/// Ninguna prueba de HU-06 adelanta <c>fabrica.Reloj</c> ni cuenta filas globales de <c>validaciones_acceso</c>.
/// </summary>
public sealed class DatosValidaciones(FabricaApi fabrica)
{
    public const string Carla = "ciudadana2.empadronada@si18.test";
    public const string PersonalEste = "personal.este@si18.test";
    public const string PersonalOeste = "personal.oeste@si18.test";
    public const string Este = "Polideportivo Este";
    public const string Pista = "Pista de pádel 2";

    /// <summary>Una fecha por reserva (tasks.md, «Asignación de fechas»).</summary>
    public static readonly DateOnly Dia18 = new(2027, 1, 18);
    public static readonly DateOnly Dia20 = new(2027, 1, 20);
    public static readonly DateOnly Dia21 = new(2027, 1, 21);
    public static readonly DateOnly Dia22 = new(2027, 1, 22);
    public static readonly DateOnly Dia25 = new(2027, 1, 25);
    public static readonly DateOnly FinHorarioVerano = new(2026, 10, 25);
    public static readonly DateOnly InicioHorarioVerano = new(2027, 3, 28);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DatosReservas Reservas { get; } = new(fabrica);

    /// <summary>La hora de la API, leída de <c>fabrica.Reloj</c>.</summary>
    public DateTime Ahora => Reservas.Ahora;

    // ── Peticiones HTTP ──────────────────────────────────────────────────────────

    public static Task<HttpResponseMessage> Validar(HttpClient cliente, string? codigo) =>
        cliente.PostAsJsonAsync("/api/validaciones-acceso", new PeticionValidacionAcceso(codigo), OpcionesJson.Predeterminadas, Ct);

    // ── Servicio con reloj propio ────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta <paramref name="accion"/> con un <see cref="ServicioValidarAcceso"/> cuyo reloj está fijado en
    /// <paramref name="ahoraUtc"/>, con la sesión de <paramref name="correoPersonal"/> y sobre la misma base de
    /// datos, en su propio ámbito y su propio <see cref="Si18DbContext"/> (como una petición).
    /// </summary>
    public async Task<T> ConServicio<T>(DateTime ahoraUtc, string correoPersonal, Func<ServicioValidarAcceso, Task<T>> accion) =>
        await ConServicio(ahoraUtc, await LeerPersonal(correoPersonal), accion);

    /// <summary>Como la anterior, con el <see cref="IUsuarioActual"/> que se pase (por ejemplo, uno que no existe).</summary>
    public async Task<T> ConServicio<T>(DateTime ahoraUtc, IUsuarioActual usuario, Func<ServicioValidarAcceso, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var reloj = new FakeTimeProvider(new DateTimeOffset(ahoraUtc, TimeSpan.Zero));
        return await accion(new ServicioValidarAcceso(db, reloj, usuario));
    }

    // ── Lecturas ─────────────────────────────────────────────────────────────────

    /// <summary>Todos los intentos registrados de una reserva, del más antiguo al más reciente.</summary>
    public async Task<List<ValidacionAcceso>> DeLaReserva(long reservaId)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.ValidacionesAcceso.AsNoTracking().Where(v => v.ReservaId == reservaId).OrderBy(v => v.Id).ToListAsync(Ct);
    }

    public async Task<ValidacionAcceso> Leer(long validacionId)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.ValidacionesAcceso.AsNoTracking().SingleAsync(v => v.Id == validacionId, Ct);
    }

    /// <summary>El mayor <c>id</c> de <c>validaciones_acceso</c> de esa persona (0 si no tiene ninguno).</summary>
    public async Task<long> UltimoId(string correoPersonal)
    {
        var personal = await Reservas.Usuario(correoPersonal);
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.ValidacionesAcceso.AsNoTracking()
            .Where(v => v.PersonalId == personal)
            .Select(v => (long?)v.Id)
            .MaxAsync(Ct) ?? 0;
    }

    /// <summary>Si hay algún intento registrado con ese <c>codigo_leido</c> (único de la prueba, un <c>Guid</c>).</summary>
    public async Task<bool> HayValidacionConCodigo(string codigoLeido)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.ValidacionesAcceso.AsNoTracking().AnyAsync(v => v.CodigoLeido == codigoLeido, Ct);
    }

    // ── Reservas idempotentes ────────────────────────────────────────────────────

    /// <summary>
    /// La reserva confirmada de Carla en Este «Pista de pádel 2» a la hora en punto actual de
    /// <c>fabrica.Reloj</c> (21/12/2026 a las 18:00 de Madrid). Idempotente: si ya existe (por ejemplo, porque la
    /// base de pruebas se reutiliza con <c>SI18_TEST_CONNECTION</c>) devuelve su <c>id</c>; si no, la inserta.
    /// </summary>
    public Task<long> ConfirmadaCompartida()
    {
        var ahora = Ahora;
        var horaEnPunto = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, 0, 0, DateTimeKind.Utc);
        return ConfirmadaEn(horaEnPunto);
    }

    /// <summary>Confirmada de Carla en la pista de HU-06 en <paramref name="inicioUtc"/>, creada de forma idempotente.</summary>
    public async Task<long> ConfirmadaEn(DateTime inicioUtc)
    {
        var pista = await Reservas.Pista(Este, Pista);
        var carla = await Reservas.Usuario(Carla);

        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            var existente = await db.Reservas.AsNoTracking()
                .Where(r => r.UsuarioId == carla && r.InstalacionId == pista && r.InicioUtc == inicioUtc
                    && r.Estado == EstadoReserva.Confirmada)
                .Select(r => (long?)r.Id)
                .FirstOrDefaultAsync(Ct);
            if (existente is not null)
            {
                return existente.Value;
            }
        }

        return await Reservas.Confirmada(Carla, pista, inicioUtc, inicioUtc - TimeSpan.FromDays(1));
    }

    /// <summary>
    /// Cancelada de Carla en la pista de HU-06 en <paramref name="inicioUtc"/>, creada de forma idempotente. Una
    /// cancelada no es activa: no choca con <c>ux_reservas_activa</c> ni con la confirmada de la misma franja.
    /// </summary>
    public async Task<long> CanceladaEn(DateTime inicioUtc)
    {
        var pista = await Reservas.Pista(Este, Pista);
        var carla = await Reservas.Usuario(Carla);

        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            var existente = await db.Reservas.AsNoTracking()
                .Where(r => r.UsuarioId == carla && r.InstalacionId == pista && r.InicioUtc == inicioUtc
                    && r.Estado == EstadoReserva.Cancelada)
                .Select(r => (long?)r.Id)
                .FirstOrDefaultAsync(Ct);
            if (existente is not null)
            {
                return existente.Value;
            }
        }

        return await Reservas.Cancelada(Carla, pista, inicioUtc, inicioUtc - TimeSpan.FromDays(1));
    }

    /// <summary>Confirmada de Carla en la pista de HU-06 el <paramref name="fecha"/> a las 10:00 de Madrid.</summary>
    public Task<long> ConfirmadaALasDiez(DateOnly fecha) => ConfirmadaEn(DatosReservas.Utc(fecha, 10));

    // ── Internos ─────────────────────────────────────────────────────────────────

    private async Task<UsuarioPersonalPrueba> LeerPersonal(string correo)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var u = await db.Usuarios.AsNoTracking().SingleAsync(x => x.Correo == correo, Ct);
        return new UsuarioPersonalPrueba(u.Id, u.CentroId!.Value);
    }
}

/// <summary>Usuario de la sesión de una persona del personal de centro, para un servicio construido a mano.</summary>
public sealed record UsuarioPersonalPrueba(int Id, int CentroId) : IUsuarioActual
{
    Rol IUsuarioActual.Rol => Rol.PersonalCentro;

    TipoTarifa? IUsuarioActual.TipoTarifa => null;

    int? IUsuarioActual.CentroId => CentroId;

    int? IUsuarioActual.ClubId => null;
}
