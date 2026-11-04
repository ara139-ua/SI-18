namespace SI18.Dominio.Cancelaciones;

/// <summary>
/// Cierre de una instalación por mantenimiento (data-model.md, «CierreMantenimiento»; dueño: HU-07).
/// Mientras dura, la instalación está cerrada; fuera de él, operativa.
/// </summary>
public class CierreMantenimiento
{
    private CierreMantenimiento() { }

    public CierreMantenimiento(
        int instalacionId,
        DateTime inicioUtc,
        DateTime finUtc,
        string motivo,
        int registradoPorId,
        DateTime registradoEnUtc)
    {
        InstalacionId = instalacionId;
        InicioUtc = inicioUtc;
        FinUtc = finUtc;
        Motivo = motivo;
        RegistradoPorId = registradoPorId;
        RegistradoEnUtc = registradoEnUtc;
    }

    public int Id { get; private set; }

    public int InstalacionId { get; private set; }

    /// <summary>En punto (hora de Madrid).</summary>
    public DateTime InicioUtc { get; private set; }

    /// <summary>En punto (hora de Madrid); posterior a <see cref="InicioUtc"/>.</summary>
    public DateTime FinUtc { get; private set; }

    /// <summary>varchar(500), obligatorio.</summary>
    public string Motivo { get; private set; } = null!;

    /// <summary>Personal del centro de la instalación.</summary>
    public int RegistradoPorId { get; private set; }

    public DateTime RegistradoEnUtc { get; private set; }

    /// <summary>
    /// Indica si el intervalo semiabierto [inicioUtc, finUtc) se solapa con el del cierre
    /// [InicioUtc, FinUtc). Una franja que termina justo cuando empieza el cierre, o que empieza
    /// justo cuando acaba, no se ve afectada.
    /// </summary>
    public bool Afecta(DateTime inicioUtc, DateTime finUtc) => inicioUtc < FinUtc && InicioUtc < finUtc;
}
