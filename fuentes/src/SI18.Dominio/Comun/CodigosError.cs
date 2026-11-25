namespace SI18.Dominio.Comun;

/// <summary>
/// Catálogo de códigos de error de 000-core (contracts/errores.md, «Catálogo de códigos de 000-core»).
/// Es el único sitio del código que escribe el texto de un error: los textos de <see cref="Detalle"/>
/// están copiados literalmente de la columna <c>detail</c> del contrato.
/// Cada spec 001–008 añade aquí sus códigos, por PR a 000-core, antes de usarlos (RF-003).
/// </summary>
public static class CodigosError
{
    public const string PeticionInvalida = "PETICION_INVALIDA";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string CredencialesInvalidas = "CREDENCIALES_INVALIDAS";
    public const string RolNoPermitido = "ROL_NO_PERMITIDO";
    public const string OtroCentro = "OTRO_CENTRO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string TransicionNoPermitida = "TRANSICION_NO_PERMITIDA";
    public const string FranjaOcupada = "FRANJA_OCUPADA";
    public const string ReservaCaducada = "RESERVA_CADUCADA";
    public const string PagoRechazado = "PAGO_RECHAZADO";
    public const string FueraDeAntelacion = "FUERA_DE_ANTELACION";
    public const string FranjaYaEmpezada = "FRANJA_YA_EMPEZADA";
    public const string InstalacionNoReservable = "INSTALACION_NO_RESERVABLE";
    public const string InstalacionCerrada = "INSTALACION_CERRADA";
    public const string FranjaBloqueadaClub = "FRANJA_BLOQUEADA_CLUB";
    public const string PrecioCambiado = "PRECIO_CAMBIADO";
    public const string ReservaYaEmpezada = "RESERVA_YA_EMPEZADA";
    public const string QrDesconocido = "QR_DESCONOCIDO";
    public const string QrReservaNoConfirmada = "QR_RESERVA_NO_CONFIRMADA";
    public const string QrFueraDeVentana = "QR_FUERA_DE_VENTANA";
    public const string QrOtroCentro = "QR_OTRO_CENTRO";
    public const string ErrorInterno = "ERROR_INTERNO";

    private static readonly Dictionary<string, (string Titulo, string Detalle)> Textos = new()
    {
        [PeticionInvalida] = ("Datos no válidos", "Revisa los datos marcados."),
        [NoAutenticado] = ("Sesión necesaria", "Tienes que iniciar sesión para continuar."),
        [CredencialesInvalidas] = ("Credenciales incorrectas", "Correo o contraseña incorrectos."),
        [RolNoPermitido] = ("Acción no permitida", "Tu perfil no permite realizar esta acción."),
        [OtroCentro] = ("Instalación de otro centro", "Esta instalación pertenece a otro centro; solo puedes actuar en el tuyo."),
        [NoEncontrado] = ("No encontrado", "No se ha encontrado lo que buscas."),
        [TransicionNoPermitida] = ("Cambio no permitido", "Esta reserva ya no se puede modificar en su estado actual."),
        [FranjaOcupada] = ("Franja no disponible", "Otra persona acaba de reservar esta franja. Elige otra hora."),
        [ReservaCaducada] = ("Reserva caducada", "Han pasado más de 10 minutos y la reserva ha caducado. Si la franja sigue libre, haz una reserva nueva."),
        [PagoRechazado] = ("Pago rechazado", "El pago no se ha completado y la reserva ha caducado. Si la franja sigue libre, haz una reserva nueva."),
        [FueraDeAntelacion] = ("Fecha fuera de plazo", "Solo puedes reservar desde hoy hasta dentro de 7 días."),
        [FranjaYaEmpezada] = ("Franja ya empezada", "Esa franja ya ha empezado y no se puede reservar."),
        [InstalacionNoReservable] = ("Instalación no reservable", "Esta instalación no se reserva por franjas."),
        [InstalacionCerrada] = ("Instalación cerrada", "La instalación está cerrada por mantenimiento en esa franja."),
        [FranjaBloqueadaClub] = ("Franja reservada para un club", "La franja está reservada para un club durante la temporada."),
        [PrecioCambiado] = ("Precio cambiado", "El precio de la franja ha cambiado. Revisa el nuevo importe."),
        [ReservaYaEmpezada] = ("Reserva ya empezada", "La reserva ya ha empezado y no se puede cancelar."),
        [QrDesconocido] = ("Código no válido", "Código no válido."),
        [QrReservaNoConfirmada] = ("Reserva no confirmada", "La reserva de este código no está confirmada (cancelada o caducada)."),
        [QrFueraDeVentana] = ("Código fuera de horario", "El código solo es válido desde 15 minutos antes del inicio hasta el final de la reserva."),
        [QrOtroCentro] = ("Código de otro centro", "Este código pertenece a una reserva de otro centro."),
        [ErrorInterno] = ("Error inesperado", "Ha ocurrido un error inesperado. Inténtalo de nuevo más tarde."),
    };

    /// <summary>Todos los códigos del catálogo.</summary>
    public static IReadOnlyCollection<string> Todos => Textos.Keys;

    /// <summary>Título corto en español (campo <c>title</c> de Problem Details).</summary>
    /// <exception cref="ArgumentException">Si el código no está en el catálogo.</exception>
    public static string Titulo(string codigo) => Buscar(codigo).Titulo;

    /// <summary>Texto para el usuario en español (campo <c>detail</c> de Problem Details).</summary>
    /// <exception cref="ArgumentException">Si el código no está en el catálogo.</exception>
    public static string Detalle(string codigo) => Buscar(codigo).Detalle;

    /// <summary>Crea la excepción de negocio con el texto del catálogo.</summary>
    public static ErrorNegocio Error(string codigo) => new(codigo, Detalle(codigo));

    private static (string Titulo, string Detalle) Buscar(string codigo) =>
        Textos.TryGetValue(codigo, out var textos)
            ? textos
            : throw new ArgumentException($"El código «{codigo}» no está en contracts/errores.md.", nameof(codigo));
}
