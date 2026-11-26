using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Contratos.Reservas;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Endpoint de <c>/api/validaciones-acceso</c> (specs/006-validar-qr/contracts/api-validaciones-acceso.openapi.yaml;
/// research.md V1, V2, V6, V7). Solo el personal de centro. No registra el código leído en los logs. Dueño: Saul.
/// </summary>
public static class EndpointsValidacionesAcceso
{
    public static IEndpointRouteBuilder MapValidacionesAcceso(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/validaciones-acceso").RequireAuthorization(Politicas.EsPersonal);

        grupo.MapPost("/", Validar);

        return app;
    }

    private static async Task<IResult> Validar(
        PeticionValidacionAcceso peticion, HttpContext http, ServicioValidarAcceso servicio, CancellationToken ct)
    {
        var codigo = peticion.Codigo?.Trim();
        if (string.IsNullOrEmpty(codigo))
        {
            // RF-004: no se llama al servicio, así que no se registra nada.
            var invalido = ManejadorErrores.CrearValidacion(
                http, new Dictionary<string, string[]> { ["codigo"] = ["Escribe el código de acceso."] });
            return Results.Json(invalido, statusCode: invalido.Status, contentType: ManejadorErrores.TipoContenido);
        }

        var resultado = await servicio.Validar(codigo, ct);
        if (resultado.Aceptada is { } aceptada)
        {
            return Results.Created($"/api/validaciones-acceso/{aceptada.ValidacionId}", aceptada);
        }

        var problema = ManejadorErrores.Crear(resultado.Codigo!, http);
        problema.Extensions["validacionId"] = resultado.ValidacionId;
        if (resultado.Reserva is not null)
        {
            problema.Extensions["reserva"] = resultado.Reserva;
        }

        return Results.Json(problema, statusCode: problema.Status, contentType: ManejadorErrores.TipoContenido);
    }
}
