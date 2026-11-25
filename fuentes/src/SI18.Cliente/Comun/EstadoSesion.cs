using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using SI18.Contratos.Comun;

namespace SI18.Cliente.Comun;

/// <summary>
/// Estado de sesión del cliente (tasks.md T056). Pregunta a la API quién es el usuario
/// (<c>GET /api/sesion/usuario</c>) y avisa a <c>AuthorizeView</c> y al menú cuando se inicia o se cierra
/// la sesión. La cookie es HttpOnly: el cliente no la lee nunca, solo la API.
/// </summary>
public sealed class EstadoSesion(HttpClient http, ClienteApi api) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private Task<AuthenticationState>? _estado;

    /// <summary>Usuario con sesión, o <c>null</c> si no la hay o aún no se ha consultado.</summary>
    public UsuarioActualDto? Usuario { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _estado ??= Consultar();

    /// <summary>Inicia sesión. Si falla, lanza <see cref="ErrorApi"/> con el código y el texto de la API.</summary>
    public async Task IniciarSesion(PeticionInicioSesion peticion)
    {
        var usuario = await api.PostAsync<PeticionInicioSesion, UsuarioActualDto>("/api/sesion", peticion);
        Cambiar(usuario);
    }

    public async Task CerrarSesion()
    {
        await api.DeleteAsync("/api/sesion");
        Cambiar(null);
    }

    private async Task<AuthenticationState> Consultar()
    {
        // Sin ClienteApi a propósito: aquí un 401 solo significa «sin sesión» y no debe llevar a /iniciar-sesion.
        using var respuesta = await http.GetAsync("/api/sesion/usuario");
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized || !respuesta.IsSuccessStatusCode)
        {
            Usuario = null;
            return Anonimo;
        }

        Usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioActualDto>(OpcionesJson.Predeterminadas);
        return Crear(Usuario);
    }

    private void Cambiar(UsuarioActualDto? usuario)
    {
        Usuario = usuario;
        _estado = Task.FromResult(Crear(usuario));
        NotifyAuthenticationStateChanged(_estado);
    }

    private static AuthenticationState Crear(UsuarioActualDto? usuario)
    {
        if (usuario is null)
        {
            return Anonimo;
        }

        // El rol va con el mismo texto que en la API (representante_club), para usar <AuthorizeView Roles="...">.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, TextoRol(usuario.Rol)),
        };

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "si18.sesion")));
    }

    /// <summary><c>RepresentanteClub</c> → <c>representante_club</c>.</summary>
    public static string TextoRol(Rol rol) => JsonNamingPolicy.SnakeCaseLower.ConvertName(rol.ToString());
}
