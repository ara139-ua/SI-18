using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Dominio.Comun;
using RolDto = SI18.Contratos.Comun.Rol;
using TipoTarifaDto = SI18.Contratos.Comun.TipoTarifa;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Inicio y cierre de sesión (contracts/api-comun.openapi.yaml, <c>/api/sesion</c>; research.md R10;
/// HU-C3). Dueño: Ambos (tasks.md T053).
/// </summary>
public static class EndpointsSesion
{
    /// <summary>
    /// Hash de una contraseña aleatoria que nadie conoce. Si el correo no existe se verifica contra él, para
    /// que la respuesta tarde lo mismo que con un correo existente (HU-C3 esc. 5).
    /// </summary>
    private static readonly Lazy<string> HashFicticio = new(() =>
        new PasswordHasher<Usuario>().HashPassword(null!, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));

    public static IEndpointRouteBuilder MapSesion(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/sesion");

        grupo.MapPost("/", IniciarSesion).AllowAnonymous();
        // Conversión explícita: con solo HttpContext como parámetro se tomaría por un RequestDelegate y se
        // descartaría el resultado (ASP0016).
        grupo.MapDelete("/", (Delegate)CerrarSesion).AllowAnonymous();
        grupo.MapGet("/usuario", UsuarioDeLaSesion).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> IniciarSesion(
        PeticionInicioSesion peticion,
        HttpContext http,
        Si18DbContext db,
        IPasswordHasher<Usuario> hasher,
        CancellationToken ct)
    {
        if (Validar(peticion) is { Count: > 0 } errores)
        {
            var problema = ManejadorErrores.CrearValidacion(http, errores);
            return Results.Json(problema, statusCode: problema.Status, contentType: ManejadorErrores.TipoContenido);
        }

        var correo = Usuario.NormalizarCorreo(peticion.Correo);
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Correo == correo, ct);

        // Se verifica siempre un hash, exista o no el correo; el error es el mismo en los dos casos.
        var resultado = hasher.VerifyHashedPassword(usuario!, usuario?.HashContrasena ?? HashFicticio.Value, peticion.Contrasena);
        if (usuario is null || resultado == PasswordVerificationResult.Failed)
        {
            throw CodigosError.Error(CodigosError.CredencialesInvalidas);
        }

        await http.SignInAsync(ClaimsSesion.Esquema, ClaimsSesion.Crear(usuario), new AuthenticationProperties { IsPersistent = false });

        return Results.Ok(await ADto(usuario, db, ct));
    }

    private static async Task<IResult> CerrarSesion(HttpContext http)
    {
        await http.SignOutAsync(ClaimsSesion.Esquema);
        return Results.NoContent();
    }

    private static async Task<IResult> UsuarioDeLaSesion(IUsuarioActual actual, Si18DbContext db, CancellationToken ct)
    {
        var id = actual.Id;
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct)
            ?? throw CodigosError.Error(CodigosError.NoAutenticado);

        return Results.Ok(await ADto(usuario, db, ct));
    }

    private static async Task<UsuarioActualDto> ADto(Usuario usuario, Si18DbContext db, CancellationToken ct)
    {
        var centroNombre = usuario.CentroId is { } centroId
            ? await db.Centros.Where(c => c.Id == centroId).Select(c => c.Nombre).SingleAsync(ct)
            : null;
        var clubNombre = usuario.ClubId is { } clubId
            ? await db.Clubes.Where(c => c.Id == clubId).Select(c => c.Nombre).SingleAsync(ct)
            : null;
        var tarifa = CalculadoraTarifa.Para(usuario.Rol, usuario.Empadronado);

        return new UsuarioActualDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Rol = Enum.Parse<RolDto>(usuario.Rol.ToString()),
            TipoTarifa = tarifa is { } t ? Enum.Parse<TipoTarifaDto>(t.ToString()) : null,
            CentroId = usuario.CentroId,
            CentroNombre = centroNombre,
            ClubId = usuario.ClubId,
            ClubNombre = clubNombre,
        };
    }

    /// <summary>Errores de las anotaciones de <see cref="PeticionInicioSesion"/>, por campo en camelCase.</summary>
    private static Dictionary<string, string[]> Validar(PeticionInicioSesion peticion)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(peticion, new ValidationContext(peticion), resultados, validateAllProperties: true);

        return resultados
            .SelectMany(r => r.MemberNames.Select(campo => (Campo: JsonNamingPolicy.CamelCase.ConvertName(campo), r.ErrorMessage!)))
            .GroupBy(e => e.Campo)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Item2).ToArray());
    }
}
