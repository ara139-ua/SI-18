namespace SI18.Dominio.Comun;

/// <summary>
/// PROVISIONAL hasta que entre <c>CodigosError</c> (tasks.md T018, Artur). Reúne en un único sitio los
/// códigos y textos de contracts/errores.md que ya usa el dominio, copiados literalmente.
/// Al entrar T018: sustituir cada uso por <c>CodigosError.Error(...)</c> y borrar este fichero.
/// </summary>
internal static class ErroresProvisionales
{
    public const string TransicionNoPermitida = "TRANSICION_NO_PERMITIDA";
    public const string ReservaCaducada = "RESERVA_CADUCADA";
    public const string ReservaYaEmpezada = "RESERVA_YA_EMPEZADA";
    public const string OtroCentro = "OTRO_CENTRO";
    public const string QrDesconocido = "QR_DESCONOCIDO";
    public const string QrReservaNoConfirmada = "QR_RESERVA_NO_CONFIRMADA";
    public const string QrFueraDeVentana = "QR_FUERA_DE_VENTANA";

    private static readonly Dictionary<string, string> Detalles = new()
    {
        [TransicionNoPermitida] = "Esta reserva ya no se puede modificar en su estado actual.",
        [ReservaCaducada] = "Han pasado más de 10 minutos y la reserva ha caducado. Si la franja sigue libre, haz una reserva nueva.",
        [ReservaYaEmpezada] = "La reserva ya ha empezado y no se puede cancelar.",
        [OtroCentro] = "Esta instalación pertenece a otro centro; solo puedes actuar en el tuyo.",
    };

    public static ErrorNegocio Error(string codigo) => new(codigo, Detalles[codigo]);
}
