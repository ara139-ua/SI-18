namespace SI18.Contratos.Comun;

/// <summary>Aviso de «Mis avisos» (OpenAPI, <c>Aviso</c>).</summary>
public sealed record AvisoDto(long Id, string Texto, DateTime FechaUtc, bool Leido);
