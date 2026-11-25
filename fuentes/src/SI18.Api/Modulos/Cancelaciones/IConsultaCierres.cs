using SI18.Dominio.Cancelaciones;

namespace SI18.Api.Modulos.Cancelaciones;

/// <summary>
/// Cierres de mantenimiento (contracts/modulos.md, «Cancelaciones»). La consumen Disponibilidad y
/// CursosInformes. La implementación es de la spec-007.
/// </summary>
public interface IConsultaCierres
{
    /// <summary>Cierres de la instalación que se solapan con [<paramref name="desdeUtc"/>, <paramref name="hastaUtc"/>).</summary>
    Task<IReadOnlyList<CierreMantenimiento>> Cierres(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
}
