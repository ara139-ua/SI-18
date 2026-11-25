using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// Disponibilidad y precio de una franja (contracts/modulos.md, «Disponibilidad»). La consume Reservas
/// (HU-02) para aplicar al reservar la misma regla que al buscar (RF-013). La implementación es de la spec-001.
/// </summary>
public interface IConsultaDisponibilidad
{
    /// <summary><c>null</c> si la franja está libre, o el código de error del primer motivo que la descarta.</summary>
    Task<string?> MotivoNoDisponible(int instalacionId, DateTime inicioUtc, CancellationToken ct);

    /// <summary>Precio de una franja de la instalación para esa tarifa.</summary>
    Task<decimal> Precio(int instalacionId, TipoTarifa tarifa, CancellationToken ct);
}
