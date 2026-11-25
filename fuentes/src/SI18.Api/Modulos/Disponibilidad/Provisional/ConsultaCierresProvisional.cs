using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Cancelaciones;
using SI18.Dominio.Cancelaciones;

namespace SI18.Api.Modulos.Disponibilidad.Provisional;

/// <summary>
/// PROVISIONAL (spec-001, RF-014a): sustituir por la implementación de spec-007 (C) y borrar este fichero.
/// Lectura de solo consulta de los cierres por mantenimiento (specs/001-buscar-pistas/research.md D5).
/// Su comportamiento lo fija <c>ConsultasProvisionalesTests</c>.
/// </summary>
public sealed class ConsultaCierresProvisional(Si18DbContext db) : IConsultaCierres
{
    /// <summary>Cierres de la instalación que se solapan con [desdeUtc, hastaUtc) (intervalos semiabiertos).</summary>
    public async Task<IReadOnlyList<CierreMantenimiento>> Cierres(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct) =>
        await db.CierresMantenimiento.AsNoTracking()
            .Where(c => c.InstalacionId == instalacionId && c.InicioUtc < hastaUtc && desdeUtc < c.FinUtc)
            .OrderBy(c => c.InicioUtc)
            .ToListAsync(ct);
}
