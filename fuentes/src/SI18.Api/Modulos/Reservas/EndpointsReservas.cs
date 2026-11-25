using System.Globalization;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Contratos.Reservas;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Endpoints de <c>/api/reservas</c> (specs/002-reservar-pagar/contracts/api-reservas.openapi.yaml; research.md E1).
/// Solo validan el formato de la petición y llaman a <see cref="ServicioReservar"/>. Ninguno registra el cuerpo de
/// la petición. Dueño: Saul.
/// </summary>
public static class EndpointsReservas
{
    private const int DigitosTarjeta = 16;

    /// <summary>Instantes UTC ISO 8601 que se aceptan en la URL (con o sin fracciones de segundo, terminados en «Z»).</summary>
    private static readonly string[] FormatosInstanteUtc = ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];

    public static IEndpointRouteBuilder MapEndpointsReservas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/reservas").RequireAuthorization(Politicas.EsCiudadanoORepresentante);

        grupo.MapGet("/resumen", Resumen);
        grupo.MapPost("/", Crear);
        grupo.MapPost("/{id:long}/pago", Pagar);
        grupo.MapGet("/{id:long}", Ver);
        grupo.MapGet("/", Mias);

        return app;
    }

    private static async Task<IResult> Resumen(
        int? instalacionId, string? inicioUtc, HttpContext http, ServicioReservar servicio, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        if (instalacionId is null)
        {
            errores["instalacionId"] = ["Indica la pista."];
        }

        if (!DateTime.TryParseExact(inicioUtc, FormatosInstanteUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var inicio))
        {
            errores["inicioUtc"] = ["Indica la hora de inicio."];
        }

        if (errores.Count > 0)
        {
            return Validacion(http, errores);
        }

        return Results.Ok(await servicio.Resumen(instalacionId!.Value, inicio, ct));
    }

    private static async Task<IResult> Crear(
        PeticionReserva peticion, HttpContext http, ServicioReservar servicio, CancellationToken ct)
    {
        // Un campo ausente en el JSON llega con su valor por defecto.
        var errores = new Dictionary<string, string[]>();
        if (peticion.InstalacionId <= 0)
        {
            errores["instalacionId"] = ["Indica la pista."];
        }

        if (peticion.InicioUtc == default || peticion.InicioUtc.Kind != DateTimeKind.Utc)
        {
            errores["inicioUtc"] = ["Indica la hora de inicio."];
        }

        if (peticion.ImporteMostrado <= 0)
        {
            errores["importeMostrado"] = ["Indica el importe."];
        }

        if (errores.Count > 0)
        {
            return Validacion(http, errores);
        }

        var reserva = await servicio.Crear(peticion, ct);
        return Results.Created($"/api/reservas/{reserva.Id}", reserva);
    }

    private static async Task<IResult> Pagar(
        long id, PeticionPago peticion, HttpContext http, ServicioReservar servicio, CancellationToken ct)
    {
        // RF-012: el formato se comprueba antes de cargar la reserva y sin llamar a la pasarela.
        var numero = (peticion.NumeroTarjeta ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (numero.Length != DigitosTarjeta || !numero.All(char.IsAsciiDigit))
        {
            return Validacion(http, new Dictionary<string, string[]>
            {
                ["numeroTarjeta"] = ["Escribe los 16 dígitos de la tarjeta."],
            });
        }

        return Results.Ok(await servicio.Pagar(id, numero, ct));
    }

    private static async Task<IResult> Ver(long id, ServicioReservar servicio, CancellationToken ct) =>
        Results.Ok(await servicio.Ver(id, ct));

    private static async Task<IResult> Mias(ServicioReservar servicio, CancellationToken ct) =>
        Results.Ok(await servicio.Mias(ct));

    private static IResult Validacion(HttpContext http, Dictionary<string, string[]> errores)
    {
        var problema = ManejadorErrores.CrearValidacion(http, errores);
        return Results.Json(problema, statusCode: problema.Status, contentType: ManejadorErrores.TipoContenido);
    }
}
