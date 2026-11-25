using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SI18.Contratos.Comun;

namespace SI18.Cliente.Comun;

/// <summary>
/// Acceso a la API de SI-18 con las opciones JSON comunes (<see cref="OpcionesJson"/>). Ante una respuesta de
/// error lee el Problem Details y lanza <see cref="ErrorApi"/>. Si no hay sesión o ha caducado
/// (<c>NO_AUTENTICADO</c>), además lleva a <c>/iniciar-sesion</c>.
/// </summary>
public sealed class ClienteApi(HttpClient http, NavigationManager navegacion)
{
    public const string RutaInicioSesion = "/iniciar-sesion";
    private const string CodigoNoAutenticado = "NO_AUTENTICADO";

    public async Task<T> GetAsync<T>(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await http.GetAsync(ruta, ct);
        return await LeerAsync<T>(respuesta, ct);
    }

    public async Task<TRespuesta> PostAsync<TCuerpo, TRespuesta>(string ruta, TCuerpo cuerpo, CancellationToken ct = default)
    {
        using var respuesta = await http.PostAsJsonAsync(ruta, cuerpo, OpcionesJson.Predeterminadas, ct);
        return await LeerAsync<TRespuesta>(respuesta, ct);
    }

    public async Task PostAsync<TCuerpo>(string ruta, TCuerpo cuerpo, CancellationToken ct = default)
    {
        using var respuesta = await http.PostAsJsonAsync(ruta, cuerpo, OpcionesJson.Predeterminadas, ct);
        await ComprobarAsync(respuesta, ct);
    }

    public async Task PutAsync(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await http.PutAsync(ruta, content: null, ct);
        await ComprobarAsync(respuesta, ct);
    }

    public async Task DeleteAsync(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await http.DeleteAsync(ruta, ct);
        await ComprobarAsync(respuesta, ct);
    }

    private async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, CancellationToken ct)
    {
        await ComprobarAsync(respuesta, ct);
        return (await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson.Predeterminadas, ct))!;
    }

    private async Task ComprobarAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        var problema = await LeerProblemaAsync(respuesta, ct);
        var codigo = problema?.Codigo ?? $"HTTP_{(int)respuesta.StatusCode}";
        var detalle = problema?.Detail ?? $"No se ha podido completar la operación (estado {(int)respuesta.StatusCode}).";

        // Un 401 por credenciales incorrectas lo muestra la propia pantalla de inicio de sesión: ahí no se redirige.
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
            && codigo == CodigoNoAutenticado
            && !EstaEnInicioSesion())
        {
            navegacion.NavigateTo(RutaInicioSesion);
        }

        throw new ErrorApi(codigo, detalle, respuesta.StatusCode)
        {
            Errores = problema?.Errors ?? new Dictionary<string, string[]>(),
        };
    }

    private static async Task<ProblemaDto?> LeerProblemaAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.Content.Headers.ContentType?.MediaType is not ("application/problem+json" or "application/json"))
        {
            return null;
        }

        try
        {
            return await respuesta.Content.ReadFromJsonAsync<ProblemaDto>(OpcionesJson.Predeterminadas, ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private bool EstaEnInicioSesion() =>
        navegacion.ToBaseRelativePath(navegacion.Uri)
            .StartsWith(RutaInicioSesion.TrimStart('/'), StringComparison.OrdinalIgnoreCase);
}
