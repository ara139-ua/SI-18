namespace SI18.Api.Infraestructura;

/// <summary>
/// Registro del manejo de errores con Problem Details y de la pasarela simulada.
/// Dueño: Artur (tasks.md T019, T063).
/// </summary>
public static class RegistroErrores
{
    public static IServiceCollection AddErrores(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication UseErrores(this WebApplication app)
    {
        return app;
    }
}
