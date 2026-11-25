using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Cancelaciones;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// <see cref="IConsultaFranjasFijas"/> definitiva (spec-005 RF-015 y RF-016; research.md F6). Sustituye a la
/// provisional de spec-001. Las franjas salen de <see cref="ReglaFranjasFijas.Expandir"/>, la misma regla que usan los
/// solapes y los conflictos. Dueño: Artur.
/// </summary>
public sealed class ConsultaFranjasFijas(Si18DbContext db, IConsultaCierres cierres) : IConsultaFranjasFijas
{
    /// <summary>
    /// Franjas de las solicitudes <b>aprobadas</b> de la instalación en [<paramref name="desde"/>, <paramref name="hasta"/>],
    /// dentro de su temporada y <b>sin</b> las que afecta un cierre por mantenimiento: durante un cierre la franja fija
    /// queda suspendida y la hora cuenta como cerrada (RF-014; aclaración de 000-core). Sin repetir y por inicio.
    /// </summary>
    public async Task<IReadOnlyList<Franja>> FranjasBloqueadas(int instalacionId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var franjas = await Expandir(instalacionId, desde, hasta, ct);
        if (franjas.Count == 0)
        {
            return [];
        }

        var cierresDelPeriodo = await cierres.Cierres(
            instalacionId,
            HorarioMadrid.AUtc(desde, TimeOnly.MinValue),
            HorarioMadrid.AUtc(hasta.AddDays(1), TimeOnly.MinValue),
            ct);

        return franjas
            .Select(f => f.Franja)
            .Where(f => !cierresDelPeriodo.Any(c => c.Afecta(f.InicioUtc, f.FinUtc)))
            .DistinctBy(f => f.InicioUtc)
            .OrderBy(f => f.InicioUtc)
            .ToList();
    }

    /// <summary>
    /// Representantes de los clubes con alguna franja fija aprobada en la instalación que se solapa con
    /// [<paramref name="inicioUtc"/>, <paramref name="finUtc"/>), sin repetir (RF-016). Aquí no se descuentan los cierres:
    /// es justo el cierre el que las suspende y HU-07 avisa a estos representantes.
    /// </summary>
    public async Task<IReadOnlyList<int>> RepresentantesAfectados(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct)
    {
        var desde = HorarioMadrid.ALocal(inicioUtc).Fecha;
        var hasta = HorarioMadrid.ALocal(finUtc).Fecha;

        var clubes = (await Expandir(instalacionId, desde, hasta, ct))
            .Where(f => f.Franja.InicioUtc < finUtc && inicioUtc < f.Franja.FinUtc)
            .Select(f => f.ClubId)
            .Distinct()
            .ToList();

        if (clubes.Count == 0)
        {
            return [];
        }

        return await db.Usuarios.AsNoTracking()
            .Where(u => u.ClubId != null && clubes.Contains(u.ClubId.Value) && u.Rol == Rol.RepresentanteClub)
            .Select(u => u.Id)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(ct);
    }

    /// <summary>Franjas de todas las solicitudes aprobadas de la instalación en el rango, con el club de cada una.</summary>
    private async Task<IReadOnlyList<(Franja Franja, int ClubId)>> Expandir(int instalacionId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var aprobadas = await (
                from s in db.SolicitudesFranjasFijas.AsNoTracking()
                join t in db.Temporadas.AsNoTracking() on s.TemporadaId equals t.Id
                where s.InstalacionId == instalacionId && s.Estado == EstadoSolicitud.Aprobada
                      && t.FechaInicio <= hasta && desde <= t.FechaFin
                select new { Solicitud = s, Temporada = t })
            .ToListAsync(ct);

        return aprobadas
            .SelectMany(a => ReglaFranjasFijas.Expandir(a.Solicitud, a.Temporada, desde, hasta).Select(f => (f, a.Solicitud.ClubId)))
            .ToList();
    }
}
