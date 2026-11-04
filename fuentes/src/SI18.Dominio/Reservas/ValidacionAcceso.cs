namespace SI18.Dominio.Reservas;

/// <summary>
/// Intento de validación de un código QR en la entrada, aceptado o no (data-model.md,
/// «ValidacionAcceso»; dueño: HU-06; RF-018).
/// </summary>
public class ValidacionAcceso
{
    /// <summary>Longitud máxima de la columna <c>codigo_leido</c>.</summary>
    public const int LongitudMaximaCodigo = 64;

    private ValidacionAcceso() { }

    public ValidacionAcceso(
        long? reservaId,
        string codigoLeido,
        bool aceptada,
        string? motivoRechazo,
        DateTime fechaUtc,
        int personalId)
    {
        ReservaId = reservaId;
        CodigoLeido = codigoLeido.Length > LongitudMaximaCodigo
            ? codigoLeido[..LongitudMaximaCodigo]
            : codigoLeido;
        Aceptada = aceptada;
        MotivoRechazo = motivoRechazo;
        FechaUtc = fechaUtc;
        PersonalId = personalId;
    }

    public long Id { get; private set; }

    /// <summary>Nulo si el código leído no corresponde a ninguna reserva.</summary>
    public long? ReservaId { get; private set; }

    /// <summary>Texto recibido, recortado a <see cref="LongitudMaximaCodigo"/> caracteres.</summary>
    public string CodigoLeido { get; private set; } = null!;

    public bool Aceptada { get; private set; }

    /// <summary>Código de error (por ejemplo, <c>QR_FUERA_DE_VENTANA</c>); obligatorio si y solo si no se acepta.</summary>
    public string? MotivoRechazo { get; private set; }

    public DateTime FechaUtc { get; private set; }

    /// <summary>Persona del personal de centro que valida.</summary>
    public int PersonalId { get; private set; }
}
