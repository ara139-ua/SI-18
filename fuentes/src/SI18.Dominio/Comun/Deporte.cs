namespace SI18.Dominio.Comun;

/// <summary>Deporte (tenis, pádel, natación…). Precargado (data-model.md, «Deporte»).</summary>
public class Deporte
{
    private Deporte() { }

    public Deporte(string nombre) => Nombre = nombre;

    public int Id { get; private set; }

    /// <summary>varchar(50), obligatorio, único.</summary>
    public string Nombre { get; private set; } = null!;
}
