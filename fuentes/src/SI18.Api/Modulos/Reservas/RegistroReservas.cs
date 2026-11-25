namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Entrada del módulo Reservas en la API (000-core plan.md, «Registro de los módulos en la API»;
/// specs/002-reservar-pagar/research.md E9). Dueño: Saul (spec-002 HU-02).
/// </summary>
public static class RegistroReservas
{
    public static IServiceCollection AddReservas(this IServiceCollection services)
    {
        // Consulta de reservas activas para Disponibilidad y CursosInformes (RF-018, RF-019).
        services.AddScoped<IConsultaReservas, ConsultaReservas>();

        // Casos de uso de los endpoints de /api/reservas (research.md E2–E6).
        services.AddScoped<ServicioReservar>();

        return services;
    }

    public static WebApplication MapReservas(this WebApplication app)
    {
        app.MapEndpointsReservas();
        return app;
    }
}
