namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// Entrada del módulo Disponibilidad en la API (000-core plan.md, «Registro de los módulos en la API»).
/// Dueño: Artur (spec-001 HU-01, spec-005 HU-05).
/// </summary>
public static class RegistroDisponibilidad
{
    public static IServiceCollection AddDisponibilidad(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapDisponibilidad(this WebApplication app)
    {
        return app;
    }
}
