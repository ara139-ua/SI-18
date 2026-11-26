namespace SI18.Contratos.Disponibilidad;

/// <summary>
/// Resultado de <c>GET /api/disponibilidad/franjas</c> (specs/001-buscar-pistas/data-model.md § 4).
/// Con <see cref="TotalPistas"/> = 0, el centro no tiene pistas reservables del deporte; con
/// <see cref="TotalPistas"/> &gt; 0 y <see cref="Pistas"/> vacía, no quedan franjas libres.
/// </summary>
public sealed record ResultadoBusquedaDto(
    DateOnly Fecha,
    int CentroId,
    int DeporteId,
    int TotalPistas,
    IReadOnlyList<PistaLibreDto> Pistas);
