using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Modulos.Reservas;
using SI18.Contratos.Disponibilidad;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;
using EstadoSolicitudDto = SI18.Contratos.Disponibilidad.EstadoSolicitud;
using EstadoSolicitudDominio = SI18.Dominio.Comun.EstadoSolicitud;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>Resultado de crear una solicitud: la solicitud creada o los errores por campo (400).</summary>
public sealed record ResultadoCreacion(SolicitudFranjasFijasDto? Solicitud, IReadOnlyDictionary<string, string[]> Errores);

/// <summary>
/// Ciclo de vida de las solicitudes de franjas fijas (spec-005 HU-05; research.md F2, F4 y F5): crear, listar, ver
/// los conflictos y aprobar o rechazar con el aviso al club en la misma operación. Dueño: Artur.
/// </summary>
public sealed class ServicioSolicitudesFranjasFijas(
    Si18DbContext db,
    TimeProvider reloj,
    IConsultaReservas reservas,
    IServicioAvisos avisos)
{
    private static readonly string[] NombresDias = ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];
    private static readonly TimeOnly HoraMinima = ValoresNegocio.HoraApertura;
    private static readonly TimeOnly HoraMaxima = ValoresNegocio.HoraCierre;

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    // ── Solicitar (US1) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una solicitud pendiente a nombre de <paramref name="clubId"/> (RF-001–RF-005). Valida antes de construir
    /// la entidad, porque su constructor lanza <see cref="ArgumentException"/> (research.md F2).
    /// </summary>
    public async Task<ResultadoCreacion> Crear(PeticionSolicitudFranjasFijas peticion, int clubId, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();

        var dias = peticion.DiasSemana ?? [];
        if (dias.Count == 0 || dias.Any(d => d is < 1 or > 7) || dias.Distinct().Count() != dias.Count)
        {
            errores["diasSemana"] = ["Elige al menos un día, sin repetir."];
        }

        var inicio = LeerHora(peticion.HoraInicio);
        var fin = LeerHora(peticion.HoraFin);
        if (inicio is null)
        {
            errores["horaInicio"] = ["Elige una hora en punto entre las 08:00 y las 22:00."];
        }

        if (fin is null)
        {
            errores["horaFin"] = ["Elige una hora en punto entre las 08:00 y las 22:00."];
        }
        else if (inicio is not null && fin <= inicio)
        {
            errores["horaFin"] = ["La hora de fin debe ser posterior a la de inicio."];
        }

        var instalacion = await db.Instalaciones.AsNoTracking().SingleOrDefaultAsync(i => i.Id == peticion.InstalacionId, ct);
        if (instalacion is null || instalacion.Tipo is not (TipoInstalacion.PistaTenis or TipoInstalacion.PistaPadel or TipoInstalacion.Pabellon))
        {
            errores["instalacionId"] = ["Elige una pista o un pabellón."];
        }

        var hoy = HorarioMadrid.Hoy(Ahora);
        var temporada = await db.Temporadas.AsNoTracking().SingleOrDefaultAsync(t => t.Id == peticion.TemporadaId, ct);
        if (temporada is null || temporada.FechaFin < hoy)
        {
            errores["temporadaId"] = ["Esa temporada ya ha terminado."];
        }

        if (errores.Count > 0)
        {
            return new ResultadoCreacion(null, errores);
        }

        var solicitud = new SolicitudFranjasFijas(
            clubId, peticion.InstalacionId, peticion.TemporadaId,
            dias.Select(d => (short)d).Order().ToArray(), inicio!.Value, fin!.Value, Ahora);
        db.SolicitudesFranjasFijas.Add(solicitud);
        await db.SaveChangesAsync(ct);

        return new ResultadoCreacion(await ADto(solicitud, ct), new Dictionary<string, string[]>());
    }

    /// <summary>
    /// Solicitudes de un club (<paramref name="clubId"/>) o de todos (<c>null</c>, técnico). Pendientes primero; dentro
    /// de cada estado, la más antigua primero (RF-006, RF-007).
    /// </summary>
    public async Task<IReadOnlyList<SolicitudFranjasFijasDto>> Listar(int? clubId, CancellationToken ct)
    {
        var solicitudes = await db.SolicitudesFranjasFijas.AsNoTracking()
            .Where(s => clubId == null || s.ClubId == clubId)
            .OrderBy(s => s.Estado != EstadoSolicitudDominio.Pendiente)
            .ThenBy(s => s.CreadaEnUtc)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        var nombres = await Nombres(ct);
        return solicitudes.Select(s => ADto(s, nombres)).ToList();
    }

    /// <summary>Temporadas que no han terminado (fecha de fin ≥ hoy en Madrid; RF-003).</summary>
    public async Task<IReadOnlyList<TemporadaDto>> TemporadasAbiertas(CancellationToken ct)
    {
        var hoy = HorarioMadrid.Hoy(Ahora);
        return await db.Temporadas.AsNoTracking()
            .Where(t => t.FechaFin >= hoy)
            .OrderBy(t => t.FechaInicio)
            .Select(t => new TemporadaDto(t.Id, t.Nombre, t.FechaInicio, t.FechaFin))
            .ToListAsync(ct);
    }

    // ── Conflictos (US3) ─────────────────────────────────────────────────────────

    /// <summary>Lo que impide aprobar la solicitud, calculado ahora (RF-008).</summary>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c>.</exception>
    public async Task<ConflictosSolicitudDto> ConflictosDe(int id, CancellationToken ct)
    {
        var solicitud = await db.SolicitudesFranjasFijas.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        var (enConflicto, solapadas) = await Conflictos(solicitud, ct);
        return new ConflictosSolicitudDto(enConflicto, solapadas, enConflicto.Count == 0 && solapadas.Count == 0);
    }

    // ── Aprobar y rechazar (US2, US4) ────────────────────────────────────────────

    /// <summary>
    /// Aprueba si no hay conflictos, en el orden de research.md F4, y avisa a los representantes del club en el mismo
    /// <c>SaveChanges</c> (RF-009, RF-010, RF-013).
    /// </summary>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c>, <c>SOLICITUD_YA_RESUELTA</c>, <c>SOLICITUD_CON_CONFLICTOS</c>
    /// o <c>FRANJA_FIJA_SOLAPADA</c>.</exception>
    public async Task<SolicitudFranjasFijasDto> Aprobar(int id, CancellationToken ct)
    {
        var solicitud = await PendienteParaResolver(id, ct);

        var (enConflicto, solapadas) = await Conflictos(solicitud, ct);
        if (enConflicto.Count > 0)
        {
            throw CodigosError.Error(CodigosError.SolicitudConConflictos);
        }

        if (solapadas.Count > 0)
        {
            throw CodigosError.Error(CodigosError.FranjaFijaSolapada);
        }

        solicitud.Aprobar(Ahora);
        return await AvisarYGuardar(solicitud, "aprobada", ct);
    }

    /// <summary>Rechaza la solicitud, tenga o no conflictos, y avisa al club (RF-011, RF-013). Sin motivo escrito.</summary>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c> o <c>SOLICITUD_YA_RESUELTA</c>.</exception>
    public async Task<SolicitudFranjasFijasDto> Rechazar(int id, CancellationToken ct)
    {
        var solicitud = await PendienteParaResolver(id, ct);
        solicitud.Rechazar(Ahora);
        return await AvisarYGuardar(solicitud, "rechazada", ct);
    }

    private async Task<SolicitudFranjasFijas> PendienteParaResolver(int id, CancellationToken ct)
    {
        var solicitud = await db.SolicitudesFranjasFijas.SingleOrDefaultAsync(s => s.Id == id, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        // Antes de calcular conflictos: una solicitud ya resuelta no se vuelve a resolver (RF-012).
        return solicitud.Estado == EstadoSolicitudDominio.Pendiente
            ? solicitud
            : throw CodigosError.Error(CodigosError.SolicitudYaResuelta);
    }

    private async Task<SolicitudFranjasFijasDto> AvisarYGuardar(SolicitudFranjasFijas solicitud, string resultado, CancellationToken ct)
    {
        var dto = await ADto(solicitud, ct);
        var texto = $"Tu solicitud de franjas fijas para {dto.InstalacionNombre} ({dto.CentroNombre}), " +
                    $"{TextoDias(solicitud.DiasSemana)} de {dto.HoraInicio} a {dto.HoraFin}, " +
                    $"temporada {dto.TemporadaNombre}, ha sido {resultado}.";

        var representantes = await db.Usuarios.AsNoTracking()
            .Where(u => u.ClubId == solicitud.ClubId && u.Rol == Rol.RepresentanteClub)
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var usuarioId in representantes)
        {
            avisos.Avisar(usuarioId, texto);
        }

        // Un único SaveChanges: el cambio de estado y los avisos se guardan juntos o no se guarda nada (R18).
        await db.SaveChangesAsync(ct);
        return dto with { Estado = Enum.Parse<EstadoSolicitudDto>(solicitud.Estado.ToString()), ResueltaEnUtc = solicitud.ResueltaEnUtc };
    }

    // ── Comunes ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Único cálculo de conflictos, para mostrarlos y para decidir al aprobar (research.md F4): reservas activas de
    /// la instalación entre ahora (o el inicio de la temporada, si es posterior) y su fin que la solicitud cubre (de cualquier titular, sin datos
    /// personales), y solicitudes aprobadas que se solapan.
    /// </summary>
    private async Task<(IReadOnlyList<ReservaEnConflictoDto> Reservas, IReadOnlyList<SolicitudSolapadaDto> Solapadas)> Conflictos(
        SolicitudFranjasFijas solicitud, CancellationToken ct)
    {
        var temporada = await db.Temporadas.AsNoTracking().SingleAsync(t => t.Id == solicitud.TemporadaId, ct);
        // Solo reservas de la temporada y de ahora en adelante: si la temporada aún no ha empezado, las de antes no
        // cuentan (RF-008; T030).
        var inicioTemporada = HorarioMadrid.AUtc(temporada.FechaInicio, TimeOnly.MinValue);
        var desde = Ahora > inicioTemporada ? Ahora : inicioTemporada;
        var hasta = HorarioMadrid.AUtc(temporada.FechaFin.AddDays(1), TimeOnly.MinValue);

        var enConflicto = new List<ReservaEnConflictoDto>();
        if (desde < hasta)
        {
            foreach (var reserva in await reservas.Activas(solicitud.InstalacionId, desde, hasta, ct))
            {
                var (fecha, hora) = HorarioMadrid.ALocal(reserva.InicioUtc);
                if (ReglaFranjasFijas.Cubre(solicitud, fecha, hora))
                {
                    enConflicto.Add(new ReservaEnConflictoDto(
                        fecha,
                        Hora(hora),
                        Hora(hora.Add(ValoresNegocio.DuracionFranja)),
                        reserva.Estado == EstadoReserva.Confirmada ? EstadoReservaEnConflicto.Confirmada : EstadoReservaEnConflicto.PendientePago));
                }
            }
        }

        var aprobadas = await db.SolicitudesFranjasFijas.AsNoTracking()
            .Where(s => s.InstalacionId == solicitud.InstalacionId
                        && s.TemporadaId == solicitud.TemporadaId
                        && s.Estado == EstadoSolicitudDominio.Aprobada
                        && s.Id != solicitud.Id)
            .ToListAsync(ct);

        var clubes = await db.Clubes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);
        var solapadas = aprobadas
            .Where(otra => ReglaFranjasFijas.SeSolapan(solicitud, otra))
            .Select(otra => new SolicitudSolapadaDto(otra.Id, clubes[otra.ClubId], Dias(otra.DiasSemana), Hora(otra.HoraInicio), Hora(otra.HoraFin)))
            .ToList();

        return (enConflicto.OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio, StringComparer.Ordinal).ToList(), solapadas);
    }

    private sealed record NombresCatalogo(
        IReadOnlyDictionary<int, string> Clubes,
        IReadOnlyDictionary<int, (string Instalacion, string Centro)> Instalaciones,
        IReadOnlyDictionary<int, string> Temporadas);

    private async Task<NombresCatalogo> Nombres(CancellationToken ct)
    {
        var clubes = await db.Clubes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);
        var instalaciones = await (
                from i in db.Instalaciones.AsNoTracking()
                join c in db.Centros.AsNoTracking() on i.CentroId equals c.Id
                select new { i.Id, Instalacion = i.Nombre, Centro = c.Nombre })
            .ToDictionaryAsync(x => x.Id, x => (x.Instalacion, x.Centro), ct);
        var temporadas = await db.Temporadas.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Nombre, ct);
        return new NombresCatalogo(clubes, instalaciones, temporadas);
    }

    private async Task<SolicitudFranjasFijasDto> ADto(SolicitudFranjasFijas s, CancellationToken ct) => ADto(s, await Nombres(ct));

    private static SolicitudFranjasFijasDto ADto(SolicitudFranjasFijas s, NombresCatalogo nombres)
    {
        var (instalacion, centro) = nombres.Instalaciones[s.InstalacionId];
        return new SolicitudFranjasFijasDto(
            s.Id,
            nombres.Clubes[s.ClubId],
            s.InstalacionId,
            instalacion,
            centro,
            nombres.Temporadas[s.TemporadaId],
            Dias(s.DiasSemana),
            Hora(s.HoraInicio),
            Hora(s.HoraFin),
            Enum.Parse<EstadoSolicitudDto>(s.Estado.ToString()),
            s.CreadaEnUtc,
            s.ResueltaEnUtc);
    }

    /// <summary>«martes y jueves», «lunes, miércoles y viernes», «lunes».</summary>
    public static string TextoDias(IEnumerable<short> dias)
    {
        var nombres = dias.Order().Select(d => NombresDias[d - 1]).ToList();
        return nombres.Count == 1 ? nombres[0] : $"{string.Join(", ", nombres[..^1])} y {nombres[^1]}";
    }

    private static IReadOnlyList<int> Dias(IEnumerable<short> dias) => dias.Order().Select(d => (int)d).ToList();

    private static string Hora(TimeOnly hora) => hora.ToString("HH':'mm", CultureInfo.InvariantCulture);

    /// <summary>Una hora «HH:mm» en punto entre las 08:00 y las 22:00, o <c>null</c> si no lo es.</summary>
    private static TimeOnly? LeerHora(string? texto) =>
        TimeOnly.TryParseExact(texto, "HH':'mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora)
        && hora.Minute == 0 && hora >= HoraMinima && hora <= HoraMaxima
            ? hora
            : null;
}
