namespace SI18.Contratos.Disponibilidad;

/// <summary>Solicitud de franjas fijas tal como la ven el representante y el técnico (data-model.md § 3).</summary>
public sealed record SolicitudFranjasFijasDto(
    int Id,
    string ClubNombre,
    int InstalacionId,
    string InstalacionNombre,
    string CentroNombre,
    string TemporadaNombre,
    IReadOnlyList<int> DiasSemana,
    string HoraInicio,
    string HoraFin,
    EstadoSolicitud Estado,
    DateTime CreadaEnUtc,
    DateTime? ResueltaEnUtc);
