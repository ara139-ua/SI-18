using SI18.Contratos.Comun;

namespace SI18.Contratos.Disponibilidad;

/// <summary>Pista con al menos una franja libre, con sus franjas ordenadas por hora (data-model.md § 4).</summary>
public sealed record PistaLibreDto(
    int InstalacionId,
    string Nombre,
    TipoInstalacion Tipo,
    IReadOnlyList<FranjaLibreDto> Franjas);
