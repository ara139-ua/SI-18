namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Registro del módulo Comun: sesión, políticas por rol, catálogo y «Mis avisos».
/// Dueño: Ambos (tasks.md T052–T054, T066).
/// </summary>
public static class RegistroComun
{
    public static IServiceCollection AddComun(this IServiceCollection services)
    {
        // Mínimo para que UseAuthentication/UseAuthorization de Program.cs arranquen; T052 añade la cookie y las políticas.
        services.AddAuthentication();
        services.AddAuthorization();
        return services;
    }

    public static WebApplication MapComun(this WebApplication app)
    {
        return app;
    }
}
