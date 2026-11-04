namespace SI18.Dominio.Comun;

/// <summary>Club deportivo (data-model.md, «Club»). Precargado.</summary>
public class Club
{
    private Club() { }

    public Club(string nombre) => Nombre = nombre;

    public int Id { get; private set; }

    /// <summary>varchar(100), obligatorio, único.</summary>
    public string Nombre { get; private set; } = null!;
}
