namespace SI18.Dominio.CursosInformes;

/// <summary>
/// Curso trimestral en una sala o piscina (data-model.md, «Curso»). Precargado; lo gestiona HU-04.
/// No tiene precio: los cursos son gratuitos (D10).
/// </summary>
public class Curso
{
    private Curso() { }

    public Curso(
        string nombre,
        int deporteId,
        int instalacionId,
        string trimestre,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        short[] diasSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int plazasMaximas)
    {
        Nombre = nombre;
        DeporteId = deporteId;
        InstalacionId = instalacionId;
        Trimestre = trimestre;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        DiasSemana = diasSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        PlazasMaximas = plazasMaximas;
    }

    public int Id { get; private set; }

    /// <summary>varchar(100), obligatorio.</summary>
    public string Nombre { get; private set; } = null!;

    public int DeporteId { get; private set; }

    /// <summary>Sala o piscina (D2).</summary>
    public int InstalacionId { get; private set; }

    /// <summary>varchar(30), por ejemplo «Otoño 2026».</summary>
    public string Trimestre { get; private set; } = null!;

    /// <summary>Primer día del trimestre.</summary>
    public DateOnly FechaInicio { get; private set; }

    /// <summary>Último día del trimestre; posterior a <see cref="FechaInicio"/>.</summary>
    public DateOnly FechaFin { get; private set; }

    /// <summary>ISO 1 (lunes) – 7 (domingo).</summary>
    public short[] DiasSemana { get; private set; } = [];

    public TimeOnly HoraInicio { get; private set; }

    /// <summary>Posterior a <see cref="HoraInicio"/>.</summary>
    public TimeOnly HoraFin { get; private set; }

    /// <summary>Mayor que 0.</summary>
    public int PlazasMaximas { get; private set; }
}
