namespace SI18.Contratos.Disponibilidad;

/// <summary>
/// Cuerpo de <c>POST /api/solicitudes-franjas-fijas</c> (specs/005-franjas-fijas/data-model.md § 3). El club no va
/// aquí: sale de la sesión. <see cref="DiasSemana"/>: ≥ 1, valores 1–7 (ISO, 1 = lunes), sin repetir. Horas
/// «HH:mm» en punto, inicio ≥ 08:00, fin ≤ 22:00 y posterior al inicio.
/// </summary>
public sealed record PeticionSolicitudFranjasFijas(
    int InstalacionId,
    int TemporadaId,
    IReadOnlyList<int> DiasSemana,
    string HoraInicio,
    string HoraFin);
