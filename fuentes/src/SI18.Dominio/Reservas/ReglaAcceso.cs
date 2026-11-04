using SI18.Dominio.Comun;

namespace SI18.Dominio.Reservas;

/// <summary>
/// Regla de validez de un código QR en la entrada (RF-018; data-model.md, «ValidacionAcceso»).
/// La comprobación de que la instalación es del centro del personal (<c>QR_OTRO_CENTRO</c>) es de la spec-006.
/// </summary>
public static class ReglaAcceso
{
    // TODO(T018): sustituir por las constantes de CodigosError cuando entre.
    public const string QrDesconocido = "QR_DESCONOCIDO";
    public const string QrReservaNoConfirmada = "QR_RESERVA_NO_CONFIRMADA";
    public const string QrFueraDeVentana = "QR_FUERA_DE_VENTANA";

    /// <returns>
    /// El código de rechazo, o <c>null</c> si el código es válido: la reserva existe, está confirmada y
    /// <c>InicioUtc − MargenQrAntesDelInicio ≤ ahoraUtc ≤ FinUtc</c> (ambos extremos incluidos).
    /// </returns>
    public static string? MotivoRechazo(Reserva? reserva, DateTime ahoraUtc)
    {
        if (reserva is null)
        {
            return QrDesconocido;
        }

        if (reserva.Estado != EstadoReserva.Confirmada)
        {
            return QrReservaNoConfirmada;
        }

        var dentroDeVentana =
            reserva.InicioUtc - ValoresNegocio.MargenQrAntesDelInicio <= ahoraUtc && ahoraUtc <= reserva.FinUtc;

        return dentroDeVentana ? null : QrFueraDeVentana;
    }
}
