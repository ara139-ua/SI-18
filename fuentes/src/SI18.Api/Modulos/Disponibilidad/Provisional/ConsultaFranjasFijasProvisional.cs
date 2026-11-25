using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Api.Modulos.Disponibilidad.Provisional;

/// <summary>
/// PROVISIONAL (spec-001, RF-014a): sustituir por la implementación definitiva de spec-005 y borrar este fichero.
/// Franjas bloqueadas por solicitudes de franjas fijas aprobadas (specs/001-buscar-pistas/research.md D5).
/// No descuenta los periodos de cierre: para HU-01 da igual, porque <c>INSTALACION_CERRADA</c> se evalúa antes
/// que <c>FRANJA_BLOQUEADA_CLUB</c>. Su comportamiento lo fija <c>ConsultasProvisionalesTests</c>.
/// </summary>
public sealed class ConsultaFranjasFijasProvisional(Si18DbContext db) : IConsultaFranjasFijas
{
    public async Task<IReadOnlyList<Franja>> FranjasBloqueadas(int instalacionId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var aprobadas = await (
                from s in db.SolicitudesFranjasFijas.AsNoTracking()
                join t in db.Temporadas.AsNoTracking() on s.TemporadaId equals t.Id
                where s.InstalacionId == instalacionId && s.Estado == EstadoSolicitud.Aprobada
                      && t.FechaInicio <= hasta && desde <= t.FechaFin
                select new { s.DiasSemana, s.HoraInicio, s.HoraFin, t.FechaInicio, t.FechaFin })
            .ToListAsync(ct);

        var franjas = new List<Franja>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            var diaIso = (short)(fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek);

            foreach (var solicitud in aprobadas.Where(s =>
                         s.FechaInicio <= fecha && fecha <= s.FechaFin && s.DiasSemana.Contains(diaIso)))
            {
                franjas.AddRange(CalculadoraFranjas.FranjasDelDia(instalacionId, fecha)
                    .Where(f => f.HoraInicio >= solicitud.HoraInicio && f.HoraInicio < solicitud.HoraFin));
            }
        }

        return franjas.DistinctBy(f => f.InicioUtc).OrderBy(f => f.InicioUtc).ToList();
    }

    public Task<IReadOnlyList<int>> RepresentantesAfectados(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct) =>
        throw new NotImplementedException("Lo implementa spec-005 para HU-07");
}
