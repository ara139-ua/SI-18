using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Registro del módulo Comun: sesión, políticas por rol, catálogo y «Mis avisos».
/// Dueño: Ambos (tasks.md T052–T054, T066).
/// </summary>
public static class RegistroComun
{
    public static IServiceCollection AddComun(this IServiceCollection services)
    {
        // Sesión por cookie del mismo origen (research.md R10). Las respuestas 401/403 son Problem Details,
        // nunca una redirección a una página de login.
        services.AddAuthentication(ClaimsSesion.Esquema)
            .AddCookie(ClaimsSesion.Esquema, opciones =>
            {
                opciones.Cookie.Name = ClaimsSesion.NombreCookie;
                opciones.Cookie.HttpOnly = true;
                opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opciones.Cookie.SameSite = SameSiteMode.Strict;
                opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
                opciones.SlidingExpiration = true;
                opciones.Events.OnRedirectToLogin = contexto => EscribirProblema(contexto, CodigosError.NoAutenticado);
                opciones.Events.OnRedirectToAccessDenied = contexto => EscribirProblema(contexto, CodigosError.RolNoPermitido);
            });

        services.AddAuthorization(Politicas.Registrar);

        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

        return services;
    }

    public static WebApplication MapComun(this WebApplication app)
    {
        app.MapSesion();
        return app;
    }

    private static Task EscribirProblema(RedirectContext<CookieAuthenticationOptions> contexto, string codigo)
    {
        var problema = ManejadorErrores.Crear(codigo, contexto.HttpContext);
        contexto.Response.StatusCode = problema.Status!.Value;
        return contexto.Response.WriteAsJsonAsync(problema, problema.GetType(), options: null, ManejadorErrores.TipoContenido);
    }
}
