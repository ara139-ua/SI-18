using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// API cuya base de datos no responde: un puerto cerrado y sin siembra, para que el arranque no la toque.
/// </summary>
public sealed class FabricaSinBaseDatos : FabricaApi
{
    protected override bool SembrarDatos => false;

    public override ValueTask InitializeAsync()
    {
        CadenaConexion = "Host=127.0.0.1;Port=1;Database=si18;Username=si18;Password=no-importa;Timeout=3";
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Un error no controlado da 500 <c>ERROR_INTERNO</c> en Problem Details, sin detalles internos (tasks.md T071;
/// contracts/errores.md). No hace falta ningún endpoint de prueba: basta con que falle la base de datos.
/// </summary>
public class ErroresTests : IClassFixture<FabricaSinBaseDatos>
{
    private readonly FabricaSinBaseDatos _fabrica;

    public ErroresTests(FabricaSinBaseDatos fabrica) => _fabrica = fabrica;

    [Fact]
    public async Task Sin_base_de_datos_la_API_da_500_error_interno_sin_traza()
    {
        // T071 pide GET /api/centros; ese endpoint llega con T054. POST /api/sesion también es anónimo y
        // consulta la base de datos en cuanto la petición es válida.
        using var cliente = _fabrica.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new PeticionInicioSesion { Correo = "tecnico@si18.test", Contrasena = "cualquiera" },
            OpcionesJson.Predeterminadas,
            ct);

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        var cuerpo = JsonDocument.Parse(texto).RootElement;
        Assert.Equal("ERROR_INTERNO", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal("Ha ocurrido un error inesperado. Inténtalo de nuevo más tarde.", cuerpo.GetProperty("detail").GetString());
        Assert.DoesNotContain("Npgsql", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", texto, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", texto, StringComparison.Ordinal);
    }
}
