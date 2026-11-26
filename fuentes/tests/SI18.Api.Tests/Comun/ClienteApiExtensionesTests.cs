using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using SI18.Cliente.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// <c>ErrorApi.Extensiones</c> (spec-006, C2): <c>ClienteApi</c> conserva las propiedades del Problem Details que no
/// son de las conocidas, sin cambiar lo que ya leían las demás pantallas. Sin base de datos.
/// </summary>
public class ClienteApiExtensionesTests
{
    private const string Rechazo = """
        {
          "type": "about:blank",
          "title": "Unprocessable Entity",
          "status": 422,
          "detail": "La reserva no está confirmada.",
          "instance": "/api/validaciones-acceso",
          "codigo": "QR_RESERVA_NO_CONFIRMADA",
          "traceId": "abc",
          "errors": { "codigo": ["Escribe el código de acceso."] },
          "validacionId": 42,
          "reserva": { "instalacion": "Pista de pádel 2", "centro": "Polideportivo Este", "fecha": "2027-01-21", "horaInicio": "10:00", "horaFin": "11:00" }
        }
        """;

    [Fact]
    public async Task Las_propiedades_no_conocidas_del_problema_llegan_en_Extensiones()
    {
        var error = await Fallar(HttpStatusCode.UnprocessableEntity, Rechazo, "application/problem+json");

        Assert.Equal(["reserva", "validacionId"], error.Extensiones.Keys.Order());
        Assert.Equal(42, error.Extensiones["validacionId"].GetInt64());
        Assert.Equal("Pista de pádel 2", error.Extensiones["reserva"].GetProperty("instalacion").GetString());
    }

    [Fact]
    public async Task Codigo_detalle_estado_y_errores_no_cambian()
    {
        var error = await Fallar(HttpStatusCode.UnprocessableEntity, Rechazo, "application/problem+json");

        Assert.Equal("QR_RESERVA_NO_CONFIRMADA", error.Codigo);
        Assert.Equal("La reserva no está confirmada.", error.Detalle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.Estado);
        Assert.Equal(["Escribe el código de acceso."], error.Errores["codigo"]);
    }

    [Fact]
    public async Task Sin_extensiones_el_diccionario_esta_vacio()
    {
        var error = await Fallar(
            HttpStatusCode.Forbidden,
            """{ "status": 403, "detail": "No permitido.", "codigo": "ROL_NO_PERMITIDO", "traceId": "t" }""",
            "application/problem+json");

        Assert.Empty(error.Extensiones);
        Assert.Equal("ROL_NO_PERMITIDO", error.Codigo);
    }

    [Fact]
    public async Task Una_respuesta_que_no_es_JSON_deja_las_extensiones_vacias()
    {
        var error = await Fallar(HttpStatusCode.BadGateway, "<html>error</html>", "text/html");

        Assert.Empty(error.Extensiones);
        Assert.Equal("HTTP_502", error.Codigo);
    }

    private static async Task<ErrorApi> Fallar(HttpStatusCode estado, string cuerpo, string tipo)
    {
        using var http = new HttpClient(new RespuestaFija(estado, cuerpo, tipo)) { BaseAddress = new Uri("http://localhost/") };
        var api = new ClienteApi(http, new NavegacionDePrueba());

        return await Assert.ThrowsAsync<ErrorApi>(() => api.PostAsync("/api/prueba", new { }));
    }

    private sealed class RespuestaFija(HttpStatusCode estado, string cuerpo, string tipo) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(estado) { Content = new StringContent(cuerpo, Encoding.UTF8, tipo) });
    }

    /// <summary>Navegación mínima: <c>ClienteApi</c> la necesita para el 401; aquí no navega.</summary>
    private sealed class NavegacionDePrueba : NavigationManager
    {
        public NavegacionDePrueba() => Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions opciones)
        {
        }
    }
}
