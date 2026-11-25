using SI18.Dominio.Reservas;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Registro de la pasarela de pago simulada. Se llama desde <see cref="RegistroErrores.AddErrores"/> para
/// no tocar Program.cs ni RegistroInfraestructura.cs. Dueño: Artur (tasks.md T063).
/// </summary>
public static class RegistroPasarela
{
    public static IServiceCollection AddPasarela(this IServiceCollection services)
    {
        // Sin estado: una única instancia para toda la aplicación.
        services.AddSingleton<IPasarelaPago, PasarelaSimulada>();
        return services;
    }
}
