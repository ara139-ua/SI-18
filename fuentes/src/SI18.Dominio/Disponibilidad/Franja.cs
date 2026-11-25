namespace SI18.Dominio.Disponibilidad;

/// <summary>
/// Franja de 1 hora de una instalación (data-model.md, «Franja»). No se almacena: se calcula con
/// <see cref="CalculadoraFranjas"/>. <see cref="Fecha"/> y <see cref="HoraInicio"/> son hora de pared de
/// Madrid; <see cref="InicioUtc"/> y <see cref="FinUtc"/> son los instantes reales.
/// </summary>
public sealed record Franja(int InstalacionId, DateOnly Fecha, TimeOnly HoraInicio, DateTime InicioUtc, DateTime FinUtc);
