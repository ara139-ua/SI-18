using Microsoft.EntityFrameworkCore;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Registro de la infraestructura: base de datos, reloj, inicializador de datos y barrido de caducidades.
/// Dueño: Saul (tasks.md T036, T068).
/// </summary>
public static class RegistroInfraestructura
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString("Si18");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException("Falta ConnectionStrings:Si18 (ver quickstart.md §2)");
        }

        services.AddDbContext<Si18DbContext>(opciones => opciones
            .UseNpgsql(cadena)
            .UseSnakeCaseNamingConvention());

        // La hora actual sale siempre de aquí (research.md R5; BannedSymbols.txt).
        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<InicializadorDatos>();

        return services;
    }
}
