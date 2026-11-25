namespace SI18.Contratos.Comun;

/// <summary>Centro deportivo (OpenAPI, <c>Centro</c>). Las horas van en formato <c>HH:mm</c>.</summary>
public sealed record CentroDto(int Id, string Nombre, string Direccion, string HoraApertura, string HoraCierre);
