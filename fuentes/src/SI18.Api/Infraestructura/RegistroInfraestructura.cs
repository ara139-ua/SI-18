namespace SI18.Api.Infraestructura;

/// <summary>
/// Registro de la infraestructura: base de datos, reloj, inicializador de datos y barrido de caducidades.
/// Dueño: Saul (tasks.md T036, T068).
/// </summary>
public static class RegistroInfraestructura
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
