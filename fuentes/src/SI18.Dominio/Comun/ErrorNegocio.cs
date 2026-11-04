namespace SI18.Dominio.Comun;

/// <summary>
/// Error de una regla de negocio. <see cref="Codigo"/> es uno de los códigos de contracts/errores.md;
/// la API lo traduce a application/problem+json.
/// </summary>
public sealed class ErrorNegocio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
