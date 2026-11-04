namespace SI18.Dominio.Comun;

/// <summary>
/// Polideportivo del municipio (data-model.md, «Centro»). Precargado.
/// El horario de apertura no se guarda: sale de <see cref="ValoresNegocio"/> (R6).
/// </summary>
public class Centro
{
    private Centro() { }

    public Centro(string nombre, string direccion)
    {
        Nombre = nombre;
        Direccion = direccion;
    }

    public int Id { get; private set; }

    /// <summary>varchar(100), obligatorio, único.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>varchar(200), obligatorio.</summary>
    public string Direccion { get; private set; } = null!;
}
