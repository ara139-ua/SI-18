using SI18.Dominio.Disponibilidad;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// Franjas fijas aprobadas de los clubes (contracts/modulos.md, «Disponibilidad»). La implementación es de
/// la spec-005.
/// </summary>
public interface IConsultaFranjasFijas
{
    /// <summary>
    /// Franjas bloqueadas por clubes entre <paramref name="desde"/> y <paramref name="hasta"/>, sin los
    /// periodos de cierre. Para HU-01 (disponibilidad) y HU-08 (horas ocupadas por clubes).
    /// </summary>
    Task<IReadOnlyList<Franja>> FranjasBloqueadas(int instalacionId, DateOnly desde, DateOnly hasta, CancellationToken ct);

    /// <summary>
    /// Para HU-07: representantes de los clubes cuyas franjas fijas aprobadas quedan suspendidas por un cierre.
    /// </summary>
    Task<IReadOnlyList<int>> RepresentantesAfectados(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct);
}
