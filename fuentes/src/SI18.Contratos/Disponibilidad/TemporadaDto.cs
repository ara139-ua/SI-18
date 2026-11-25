namespace SI18.Contratos.Disponibilidad;

/// <summary>Temporada de franjas fijas (septiembre a junio).</summary>
public sealed record TemporadaDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
