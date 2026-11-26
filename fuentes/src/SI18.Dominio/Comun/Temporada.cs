namespace SI18.Dominio.Comun;

/// <summary>
/// Temporada de septiembre a junio (data-model.md, «Temporada»; <see cref="ValoresNegocio.MesInicioTemporada"/>).
/// Precargada.
/// </summary>
public class Temporada
{
    private Temporada() { }

    public Temporada(string nombre, DateOnly fechaInicio, DateOnly fechaFin)
    {
        Nombre = nombre;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public int Id { get; private set; }

    /// <summary>varchar(20), único (por ejemplo, «2026-2027»).</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>1 de septiembre.</summary>
    public DateOnly FechaInicio { get; private set; }

    /// <summary>30 de junio del año siguiente; posterior a <see cref="FechaInicio"/>.</summary>
    public DateOnly FechaFin { get; private set; }
}
