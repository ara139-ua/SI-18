using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// Inicio y cierre de sesión contra la API y PostgreSQL reales (quickstart.md V1–V6; HU-C3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SesionTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> IniciarSesion(HttpClient cliente, string correo, string contrasena = FabricaApi.ClaveDemo) =>
        await cliente.PostAsJsonAsync("/api/sesion", new PeticionInicioSesion { Correo = correo, Contrasena = contrasena }, OpcionesJson.Predeterminadas, Ct);

    private static async Task<JsonElement> Json(HttpResponseMessage respuesta) =>
        (await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct)).RootElement.Clone();

    // ── V1–V4: rol, tarifa, centro y club de cada usuario precargado ────────────

    [Fact]
    public async Task V1_ciudadana_empadronada_con_cookie_segura()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await IniciarSesion(cliente, "ciudadana.empadronada@si18.test");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await Json(respuesta);
        Assert.Equal("ciudadano", cuerpo.GetProperty("rol").GetString());
        Assert.Equal("empadronado", cuerpo.GetProperty("tipoTarifa").GetString());
        Assert.False(cuerpo.TryGetProperty("hashContrasena", out _));

        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("si18.sesion=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V2_ciudadano_no_empadronado()
    {
        using var cliente = fabrica.CreateClient();

        var usuario = await (await IniciarSesion(cliente, "ciudadano.noempadronado@si18.test"))
            .Content.ReadFromJsonAsync<UsuarioActualDto>(OpcionesJson.Predeterminadas, Ct);

        Assert.Equal(Rol.Ciudadano, usuario!.Rol);
        Assert.Equal(TipoTarifa.NoEmpadronado, usuario.TipoTarifa);
    }

    [Fact]
    public async Task V3_representante_no_empadronado_tiene_tarifa_club()
    {
        using var cliente = fabrica.CreateClient();

        var usuario = await (await IniciarSesion(cliente, "representante.padel@si18.test"))
            .Content.ReadFromJsonAsync<UsuarioActualDto>(OpcionesJson.Predeterminadas, Ct);

        Assert.Equal(Rol.RepresentanteClub, usuario!.Rol);
        Assert.Equal(TipoTarifa.Club, usuario.TipoTarifa);
        Assert.NotNull(usuario.ClubId);
        Assert.Equal("Club de Pádel Ejemplo", usuario.ClubNombre);
        Assert.Null(usuario.CentroId);
    }

    [Fact]
    public async Task V4_personal_tiene_su_centro_y_sin_tarifa()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await IniciarSesion(cliente, "personal.norte@si18.test");
        var cuerpo = await Json(respuesta);

        Assert.Equal("personal_centro", cuerpo.GetProperty("rol").GetString());
        Assert.Equal("Polideportivo Norte", cuerpo.GetProperty("centroNombre").GetString());
        Assert.Equal(JsonValueKind.Null, cuerpo.GetProperty("tipoTarifa").ValueKind);
    }

    [Fact]
    public async Task El_correo_se_compara_sin_mayusculas_ni_espacios()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await IniciarSesion(cliente, "  Tecnico@SI18.test ");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // ── V5: mismo error exista o no el correo ───────────────────────────────────

    [Fact]
    public async Task V5_contrasena_incorrecta_y_correo_inexistente_dan_el_mismo_401()
    {
        using var cliente = fabrica.CreateClient();

        var malaContrasena = await IniciarSesion(cliente, "ciudadana.empadronada@si18.test", "otra-clave");
        var sinCorreo = await IniciarSesion(cliente, "nadie@si18.test");

        Assert.Equal(HttpStatusCode.Unauthorized, malaContrasena.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sinCorreo.StatusCode);
        Assert.Equal("application/problem+json", malaContrasena.Content.Headers.ContentType?.MediaType);

        var a = await Json(malaContrasena);
        var b = await Json(sinCorreo);
        Assert.Equal("CREDENCIALES_INVALIDAS", a.GetProperty("codigo").GetString());
        Assert.Equal("CREDENCIALES_INVALIDAS", b.GetProperty("codigo").GetString());
        Assert.Equal("Correo o contraseña incorrectos.", a.GetProperty("detail").GetString());
        Assert.Equal(a.GetProperty("detail").GetString(), b.GetProperty("detail").GetString());
        Assert.False(malaContrasena.Headers.Contains("Set-Cookie"));
    }

    // ── V6: sin sesión → 401 NO_AUTENTICADO, nunca una redirección ──────────────
    // Se comprueba sobre /api/sesion/usuario; /api/avisos (T066) usará el mismo mecanismo y lo probará T069.

    [Fact]
    public async Task V6_sin_cookie_da_401_no_autenticado_en_problem_json()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/sesion/usuario", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await Json(respuesta);
        Assert.Equal("NO_AUTENTICADO", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal("Tienes que iniciar sesión para continuar.", cuerpo.GetProperty("detail").GetString());
    }

    // ── Quién soy y cierre de sesión ────────────────────────────────────────────

    [Fact]
    public async Task Con_sesion_usuario_devuelve_el_mismo_usuario_y_tras_cerrarla_da_401()
    {
        using var cliente = await fabrica.CrearClienteConSesion("representante.norte@si18.test");

        var yo = await cliente.GetFromJsonAsync<UsuarioActualDto>("/api/sesion/usuario", OpcionesJson.Predeterminadas, Ct);
        Assert.Equal("representante.norte@si18.test", yo!.Correo);
        Assert.Equal(TipoTarifa.Club, yo.TipoTarifa);

        var cierre = await cliente.DeleteAsync("/api/sesion", Ct);
        Assert.Equal(HttpStatusCode.NoContent, cierre.StatusCode);

        var despues = await cliente.GetAsync("/api/sesion/usuario", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, despues.StatusCode);
    }

    // ── Peticiones mal formadas ─────────────────────────────────────────────────

    [Fact]
    public async Task Un_correo_sin_formato_da_400_con_el_campo_marcado()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await IniciarSesion(cliente, "no-es-un-correo");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await Json(respuesta);
        Assert.Equal("PETICION_INVALIDA", cuerpo.GetProperty("codigo").GetString());
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty("correo", out _));
    }

    [Fact]
    public async Task Un_json_mal_formado_da_400_peticion_invalida()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsync("/api/sesion", new StringContent("{\"correo\": ", Encoding.UTF8, "application/json"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("PETICION_INVALIDA", (await Json(respuesta)).GetProperty("codigo").GetString());
    }
}
