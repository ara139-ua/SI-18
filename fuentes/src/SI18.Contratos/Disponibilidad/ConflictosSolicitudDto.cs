namespace SI18.Contratos.Disponibilidad;

/// <summary>
/// Lo que impide aprobar una solicitud (spec-005 RF-008): reservas en conflicto y solicitudes aprobadas solapadas.
/// </summary>
public sealed record ConflictosSolicitudDto(
    IReadOnlyList<ReservaEnConflictoDto> Reservas,
    IReadOnlyList<SolicitudSolapadaDto> Solapadas,
    bool PuedeAprobarse);

/// <summary>
/// Reserva que choca con una solicitud. <b>Sin</b> identificador de usuario ni datos personales del titular
/// (aclaración 3 de spec-005). Horas de pared de Madrid.
/// </summary>
public sealed record ReservaEnConflictoDto(DateOnly Fecha, string HoraInicio, string HoraFin, EstadoReservaEnConflicto Estado);

/// <summary>Solicitud aprobada que se solapa con la que se revisa.</summary>
public sealed record SolicitudSolapadaDto(int Id, string ClubNombre, IReadOnlyList<int> DiasSemana, string HoraInicio, string HoraFin);
