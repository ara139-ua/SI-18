namespace SI18.Contratos.Disponibilidad;

/// <summary>
/// Franja libre (data-model.md § 4). <see cref="HoraInicio"/> y <see cref="HoraFin"/> son horas de pared de
/// Madrid en formato «HH:mm», calculadas por la API. <see cref="Precio"/> es el de la tarifa de quien busca,
/// en euros con dos decimales. <see cref="InicioUtc"/> identifica la franja junto con la pista (lo usa HU-02).
/// </summary>
public sealed record FranjaLibreDto(
    DateTime InicioUtc,
    DateTime FinUtc,
    string HoraInicio,
    string HoraFin,
    decimal Precio);
