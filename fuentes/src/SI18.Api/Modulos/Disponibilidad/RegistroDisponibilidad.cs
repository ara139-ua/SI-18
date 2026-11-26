using Microsoft.Extensions.DependencyInjection.Extensions;
using SI18.Api.Modulos.Cancelaciones;
using SI18.Api.Modulos.Disponibilidad.Provisional;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// Entrada del módulo Disponibilidad en la API (000-core plan.md, «Registro de los módulos en la API»).
/// Dueño: Artur (spec-001 HU-01, spec-005 HU-05).
/// </summary>
public static class RegistroDisponibilidad
{
    public static IServiceCollection AddDisponibilidad(this IServiceCollection services)
    {
        // Consultas provisionales de solo lectura (spec-001, RF-014a; research.md D5). Con TryAdd, si el dueño ya
        // registró la suya, la provisional no se añade; si la registra después, la suya gana (el contenedor resuelve
        // el último registro). Cada dueño borra la provisional en el PR que publica la definitiva.
        services.TryAddScoped<IConsultaCierres, ConsultaCierresProvisional>();

        services.AddScoped<ServicioDisponibilidad>();

        // HU-02 pregunta aquí si una franja está libre y su precio: el mismo cálculo que la búsqueda (RF-013).
        services.AddScoped<IConsultaDisponibilidad>(sp => sp.GetRequiredService<ServicioDisponibilidad>());

        // Solicitudes de franjas fijas (spec-005, HU-05) y su consulta definitiva, que sustituye a la provisional de
        // spec-001 (RF-015): descuenta los cierres y ofrece RepresentantesAfectados a HU-07.
        services.AddScoped<ServicioSolicitudesFranjasFijas>();
        services.AddScoped<IConsultaFranjasFijas, ConsultaFranjasFijas>();

        return services;
    }

    public static WebApplication MapDisponibilidad(this WebApplication app)
    {
        app.MapEndpointsDisponibilidad();
        app.MapEndpointsSolicitudesFranjasFijas();
        return app;
    }
}
