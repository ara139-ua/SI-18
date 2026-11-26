namespace SI18.Dominio.Comun;

/// <summary>
/// Aviso para un usuario, visible en «Mis avisos» (data-model.md, «Notificacion»).
/// La crean HU-04, HU-05 y HU-07 a través de <c>IServicioAvisos</c>.
/// </summary>
public class Notificacion
{
    private Notificacion() { }

    public Notificacion(int usuarioId, string texto, DateTime fechaUtc)
    {
        UsuarioId = usuarioId;
        Texto = texto;
        FechaUtc = fechaUtc;
    }

    public long Id { get; private set; }

    public int UsuarioId { get; private set; }

    /// <summary>varchar(500), obligatorio, en español.</summary>
    public string Texto { get; private set; } = null!;

    public DateTime FechaUtc { get; private set; }

    /// <summary>Por defecto <c>false</c>.</summary>
    public bool Leida { get; private set; }

    public void MarcarLeida() => Leida = true;
}
