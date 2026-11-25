using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SI18.Dominio.Comun;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Traduce cualquier excepción a <c>application/problem+json</c> según contracts/errores.md (principio IV,
/// RF-028). Contiene la única tabla código → estado HTTP del sistema. Dueño: Artur (tasks.md T019).
/// </summary>
public sealed class ManejadorErrores(ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public const string TipoContenido = "application/problem+json";

    /// <summary>Estado HTTP de cada código del catálogo (contracts/errores.md).</summary>
    public static readonly IReadOnlyDictionary<string, int> Estados = new Dictionary<string, int>
    {
        [CodigosError.PeticionInvalida] = StatusCodes.Status400BadRequest,
        [CodigosError.NoAutenticado] = StatusCodes.Status401Unauthorized,
        [CodigosError.CredencialesInvalidas] = StatusCodes.Status401Unauthorized,
        [CodigosError.RolNoPermitido] = StatusCodes.Status403Forbidden,
        [CodigosError.OtroCentro] = StatusCodes.Status403Forbidden,
        [CodigosError.NoEncontrado] = StatusCodes.Status404NotFound,
        [CodigosError.TransicionNoPermitida] = StatusCodes.Status409Conflict,
        [CodigosError.FranjaOcupada] = StatusCodes.Status409Conflict,
        [CodigosError.ReservaCaducada] = StatusCodes.Status409Conflict,
        [CodigosError.PagoRechazado] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.FueraDeAntelacion] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.FranjaYaEmpezada] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.InstalacionNoReservable] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.InstalacionCerrada] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.FranjaBloqueadaClub] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.PrecioCambiado] = StatusCodes.Status409Conflict,
        [CodigosError.ReservaYaEmpezada] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.QrDesconocido] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.QrReservaNoConfirmada] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.QrFueraDeVentana] = StatusCodes.Status422UnprocessableEntity,
        [CodigosError.QrOtroCentro] = StatusCodes.Status403Forbidden,
        [CodigosError.SolicitudConConflictos] = StatusCodes.Status409Conflict,
        [CodigosError.FranjaFijaSolapada] = StatusCodes.Status409Conflict,
        [CodigosError.SolicitudYaResuelta] = StatusCodes.Status409Conflict,
        [CodigosError.ErrorInterno] = StatusCodes.Status500InternalServerError,
    };

    /// <summary>
    /// Código que corresponde a una respuesta vacía con ese estado (UseStatusCodePages o un
    /// <c>Results.NotFound()</c>), para que también lleve <c>codigo</c>.
    /// </summary>
    public static string? CodigoPorEstado(int estado) => estado switch
    {
        StatusCodes.Status400BadRequest => CodigosError.PeticionInvalida,
        StatusCodes.Status401Unauthorized => CodigosError.NoAutenticado,
        StatusCodes.Status403Forbidden => CodigosError.RolNoPermitido,
        StatusCodes.Status404NotFound => CodigosError.NoEncontrado,
        >= 500 => CodigosError.ErrorInterno,
        _ => null,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problema;

        switch (exception)
        {
            case ErrorNegocio error when Estados.ContainsKey(error.Codigo):
                problema = Crear(error.Codigo, httpContext);
                break;

            case BadHttpRequestException peticionMala:
                // Errores de enlace de parámetros o JSON mal formado (requiere ThrowOnBadRequest, ver RegistroErrores).
                problema = CrearValidacion(httpContext, ErroresDeEnlace(peticionMala));
                break;

            default:
                // Nunca se devuelve la traza ni el mensaje interno (contracts/errores.md, «Formato»).
                logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);
                problema = Crear(CodigosError.ErrorInterno, httpContext);
                break;
        }

        httpContext.Response.StatusCode = problema.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problema, problema.GetType(), (JsonSerializerOptions?)null, TipoContenido, cancellationToken);
        return true;
    }

    /// <summary>Problem Details completo para un código del catálogo.</summary>
    public static ProblemDetails Crear(string codigo, HttpContext httpContext)
    {
        var problema = new ProblemDetails();
        Rellenar(problema, codigo, httpContext);
        return problema;
    }

    /// <summary>Rellena <paramref name="problema"/> con los textos y extensiones de <paramref name="codigo"/>.</summary>
    public static void Rellenar(ProblemDetails problema, string codigo, HttpContext httpContext)
    {
        problema.Type = Tipo(codigo);
        problema.Title = CodigosError.Titulo(codigo);
        problema.Status = Estados[codigo];
        problema.Detail = CodigosError.Detalle(codigo);
        problema.Extensions["codigo"] = codigo;
        problema.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }

    /// <summary><c>https://si18.test/errores/{codigo-en-kebab-case}</c>.</summary>
    public static string Tipo(string codigo) =>
        $"https://si18.test/errores/{codigo.ToLowerInvariant().Replace('_', '-')}";

    /// <summary>400 <c>PETICION_INVALIDA</c> con los errores por campo (<c>errors</c>).</summary>
    public static HttpValidationProblemDetails CrearValidacion(HttpContext httpContext, IDictionary<string, string[]> errores)
    {
        var problema = new HttpValidationProblemDetails(errores);
        Rellenar(problema, CodigosError.PeticionInvalida, httpContext);
        return problema;
    }

    private static Dictionary<string, string[]> ErroresDeEnlace(BadHttpRequestException excepcion)
    {
        // El mensaje de ASP.NET Core está en inglés y nombra tipos internos: se sustituye por un texto propio.
        var campo = excepcion.InnerException is JsonException { Path: { Length: > 0 } ruta }
            ? ruta.TrimStart('$', '.')
            : "peticion";

        return new Dictionary<string, string[]> { [campo] = ["El formato no es válido."] };
    }
}
