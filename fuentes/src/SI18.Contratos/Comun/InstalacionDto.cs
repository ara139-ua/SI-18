namespace SI18.Contratos.Comun;

/// <summary>Instalación de un centro (OpenAPI, <c>Instalacion</c>).</summary>
public sealed record InstalacionDto(
    int Id,
    int CentroId,
    string Nombre,
    TipoInstalacion Tipo,
    bool ReservablePorFranjas,
    IReadOnlyList<int> DeporteIds);
