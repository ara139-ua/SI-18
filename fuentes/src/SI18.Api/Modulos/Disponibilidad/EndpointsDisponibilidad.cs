using System.Globalization;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// <c>GET /api/disponibilidad/franjas</c> (specs/001-buscar-pistas/contracts/api-disponibilidad.openapi.yaml).
/// Dueño: Artur (spec-001 T010).
/// </summary>
public static class EndpointsDisponibilidad
{
    public static IEndpointRouteBuilder MapEndpointsDisponibilidad(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/disponibilidad/franjas", BuscarFranjas)
            .RequireAuthorization(Politicas.EsCiudadanoORepresentante);

        return app;
    }

    private static async Task<IResult> BuscarFranjas(
        int? deporteId,
        int? centroId,
        string? fecha,
        HttpContext http,
        IUsuarioActual usuario,
        ServicioDisponibilidad servicio,
        CancellationToken ct)
    {
        // 1.º en el orden de research.md D2: parámetros que faltan o con formato incorrecto.
        var errores = new Dictionary<string, string[]>();
        if (deporteId is null)
        {
            errores["deporteId"] = ["Elige un deporte."];
        }

        if (centroId is null)
        {
            errores["centroId"] = ["Elige un centro."];
        }

        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
        {
            errores["fecha"] = ["Elige una fecha válida."];
        }

        if (errores.Count > 0)
        {
            var problema = ManejadorErrores.CrearValidacion(http, errores);
            return Results.Json(problema, statusCode: problema.Status, contentType: ManejadorErrores.TipoContenido);
        }

        // La política solo deja pasar a ciudadanos y representantes, que siempre tienen tarifa (RF-007 de 000-core).
        var tarifa = usuario.TipoTarifa ?? throw CodigosError.Error(CodigosError.RolNoPermitido);

        return Results.Ok(await servicio.Buscar(deporteId!.Value, centroId!.Value, dia, tarifa, ct));
    }
}
