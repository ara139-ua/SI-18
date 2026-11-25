using Microsoft.AspNetCore.Http.Json;
using SI18.Contratos.Comun;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Registro del manejo de errores con Problem Details y de la pasarela simulada.
/// Dueño: Artur (tasks.md T019, T063).
/// </summary>
public static class RegistroErrores
{
    public static IServiceCollection AddErrores(this IServiceCollection services)
    {
        services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = contexto =>
        {
            // Respuestas de error generadas por el framework (UseStatusCodePages, Results.NotFound(), validación):
            // se completan con el código del catálogo si aún no lo llevan.
            var problema = contexto.ProblemDetails;
            if (problema.Extensions.ContainsKey("codigo") || problema.Status is not { } estado)
            {
                return;
            }

            if (ManejadorErrores.CodigoPorEstado(estado) is { } codigo)
            {
                ManejadorErrores.Rellenar(problema, codigo, contexto.HttpContext);
            }
        });

        services.AddExceptionHandler<ManejadorErrores>();

        // Los errores de enlace de parámetros llegan al manejador como excepción también fuera de Development.
        services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);

        // La API serializa igual que el cliente: camelCase y enumerados en snake_case (contracts).
        services.Configure<JsonOptions>(opciones => OpcionesJson.Aplicar(opciones.SerializerOptions));

        return services;
    }

    public static WebApplication UseErrores(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
