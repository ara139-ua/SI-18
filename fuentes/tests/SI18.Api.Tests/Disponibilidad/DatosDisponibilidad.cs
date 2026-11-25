using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Modulos.Disponibilidad.Provisional;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;
using SI18.Dominio.Reservas;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>Estado de una reserva de prueba, llevado hasta él con la máquina de estados de 000-core.</summary>
public enum EstadoPrueba
{
    PendienteVigente,
    Confirmada,
    Caducada,
    Cancelada,
}

/// <summary>
/// Ayudante de las pruebas de spec-001 (tasks.md, «Datos de las pruebas de integración»): busca los
/// identificadores de la siembra de 000-core e inserta reservas, cierres y franjas fijas ficticias directamente
/// en la base de pruebas.
/// </summary>
public sealed class DatosDisponibilidad(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DateTime Ahora => fabrica.Reloj.GetUtcNow().UtcDateTime;

    /// <summary>Instante UTC de una fecha y hora de pared de Madrid.</summary>
    public static DateTime Utc(DateOnly fecha, int hora, int minuto = 0) => HorarioMadrid.AUtc(fecha, new TimeOnly(hora, minuto));

    public async Task<int> Centro(string nombre) =>
        await Consultar(db => db.Centros.Where(c => c.Nombre == nombre).Select(c => c.Id).SingleAsync(Ct));

    public async Task<int> Deporte(string nombre) =>
        await Consultar(db => db.Deportes.Where(d => d.Nombre == nombre).Select(d => d.Id).SingleAsync(Ct));

    public async Task<int> Pista(string centro, string pista) =>
        await Consultar(db => (
            from i in db.Instalaciones
            join c in db.Centros on i.CentroId equals c.Id
            where c.Nombre == centro && i.Nombre == pista
            select i.Id).SingleAsync(Ct));

    public async Task<int> Usuario(string correo) =>
        await Consultar(db => db.Usuarios.Where(u => u.Correo == correo).Select(u => u.Id).SingleAsync(Ct));

    /// <summary>Inserta una reserva de la ciudadana empadronada en el estado pedido.</summary>
    public async Task Reserva(int pistaId, DateTime inicioUtc, EstadoPrueba estado, DateTime? creadaEnUtc = null)
    {
        var usuarioId = await Usuario("ciudadana.empadronada@si18.test");
        var creada = creadaEnUtc ?? Ahora - TimeSpan.FromMinutes(1);
        var reserva = new Reserva(usuarioId, pistaId, inicioUtc, TipoTarifa.Empadronado, 6.00m, creada);
        var poco = creada + TimeSpan.FromSeconds(30);

        switch (estado)
        {
            case EstadoPrueba.Confirmada:
                reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), poco);
                break;
            case EstadoPrueba.Caducada:
                reserva.Caducar(creada + ValoresNegocio.TiempoBloqueoPago);
                break;
            case EstadoPrueba.Cancelada:
                reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), poco);
                reserva.Cancelar(MotivoCancelacion.Usuario, poco);
                break;
        }

        await Guardar(db => db.Reservas.Add(reserva));
    }

    /// <summary>Inserta un cierre por mantenimiento registrado por el personal del centro Norte.</summary>
    public async Task Cierre(int pistaId, DateTime inicioUtc, DateTime finUtc)
    {
        var personalId = await Usuario("personal.norte@si18.test");
        await Guardar(db => db.CierresMantenimiento.Add(
            new CierreMantenimiento(pistaId, inicioUtc, finUtc, "Mantenimiento de prueba", personalId, Ahora)));
    }

    /// <summary>
    /// Inserta una solicitud de franjas fijas de temporada en el estado pedido (aprobada por defecto) y devuelve su
    /// identificador. Sin <paramref name="club"/>, la pide el primer club de la siembra.
    /// </summary>
    public async Task<int> FranjaFija(
        int pistaId, short[] diasSemana, int horaInicio, int horaFin,
        EstadoSolicitud estado = EstadoSolicitud.Aprobada, string? club = null)
    {
        var (clubId, temporadaId) = await Consultar(async db => (
            club is null
                ? await db.Clubes.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync(Ct)
                : await db.Clubes.Where(c => c.Nombre == club).Select(c => c.Id).SingleAsync(Ct),
            await db.Temporadas.Where(t => t.Nombre == "2026-2027").Select(t => t.Id).SingleAsync(Ct)));

        var solicitud = new SolicitudFranjasFijas(clubId, pistaId, temporadaId, diasSemana,
            new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0), Ahora);

        if (estado == EstadoSolicitud.Aprobada)
        {
            solicitud.Aprobar(Ahora);
        }
        else if (estado == EstadoSolicitud.Rechazada)
        {
            solicitud.Rechazar(Ahora);
        }

        await Guardar(db => db.SolicitudesFranjasFijas.Add(solicitud));
        return solicitud.Id;
    }

    /// <summary>Identificador de una temporada de la siembra.</summary>
    public async Task<int> Temporada(string nombre) =>
        await Consultar(db => db.Temporadas.Where(t => t.Nombre == nombre).Select(t => t.Id).SingleAsync(Ct));

    /// <summary>
    /// Como <see cref="ConServicio{T}"/>, pero con <see cref="ServicioSolicitudesFranjasFijas"/> y su propio reloj. Sirve
    /// para probar fechas sin tocar el reloj compartido y sin insertar temporadas ni usuarios:
    /// <c>InicializadorDatosTests</c> exige <c>Temporadas: 1</c> y <c>Usuarios: 10</c> en la base compartida.
    /// </summary>
    public async Task<T> ConServicioSolicitudes<T>(DateTime ahoraUtc, Func<ServicioSolicitudesFranjasFijas, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var reloj = new FakeTimeProvider(new DateTimeOffset(ahoraUtc, TimeSpan.Zero));
        var servicio = new ServicioSolicitudesFranjasFijas(
            db,
            reloj,
            new ConsultaReservasProvisional(db, reloj),
            ambito.ServiceProvider.GetRequiredService<SI18.Api.Modulos.Comun.IServicioAvisos>());

        return await accion(servicio);
    }

    /// <summary>
    /// Ejecuta <paramref name="accion"/> con un <see cref="ServicioDisponibilidad"/> que usa su propio reloj fijado
    /// en <paramref name="ahoraUtc"/>, sobre la misma base de datos. Es para las pruebas de límites de tiempo:
    /// <c>FakeTimeProvider</c> no retrocede, y el reloj de <see cref="FabricaApi"/> lo comparten todas las pruebas.
    /// </summary>
    public async Task<T> ConServicio<T>(DateTime ahoraUtc, Func<ServicioDisponibilidad, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var reloj = new FakeTimeProvider(new DateTimeOffset(ahoraUtc, TimeSpan.Zero));
        var servicio = new ServicioDisponibilidad(
            db,
            reloj,
            new ConsultaReservasProvisional(db, reloj),
            new ConsultaCierresProvisional(db),
            new ConsultaFranjasFijasProvisional(db));

        return await accion(servicio);
    }

    /// <summary>Día ISO (lunes = 1 … domingo = 7) de una fecha.</summary>
    public static short DiaIso(DateOnly fecha) => (short)(fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek);

    private async Task<T> Consultar<T>(Func<Si18DbContext, Task<T>> consulta)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await consulta(ambito.ServiceProvider.GetRequiredService<Si18DbContext>());
    }

    private async Task Guardar(Action<Si18DbContext> anadir)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        anadir(db);
        await db.SaveChangesAsync(Ct);
    }
}
