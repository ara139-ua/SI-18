using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>ManejadorErrores escribe Problem Details según contracts/errores.md (tasks.md T071, sin base de datos).</summary>
public class ManejadorErroresTests
{
    private static async Task<(int Estado, string? Tipo, JsonElement Cuerpo)> Manejar(Exception excepcion)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        var manejador = new ManejadorErrores(NullLogger<ManejadorErrores>.Instance);

        var manejado = await manejador.TryHandleAsync(contexto, excepcion, TestContext.Current.CancellationToken);

        Assert.True(manejado);
        contexto.Response.Body.Position = 0;
        var cuerpo = await JsonDocument.ParseAsync(contexto.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        return (contexto.Response.StatusCode, contexto.Response.ContentType, cuerpo.RootElement.Clone());
    }

    [Fact]
    public async Task Un_error_de_negocio_da_su_estado_y_problem_json_completo()
    {
        var (estado, tipo, cuerpo) = await Manejar(CodigosError.Error(CodigosError.FranjaOcupada));

        Assert.Equal(409, estado);
        Assert.StartsWith("application/problem+json", tipo);
        Assert.Equal("https://si18.test/errores/franja-ocupada", cuerpo.GetProperty("type").GetString());
        Assert.Equal("Franja no disponible", cuerpo.GetProperty("title").GetString());
        Assert.Equal(409, cuerpo.GetProperty("status").GetInt32());
        Assert.Equal("Otra persona acaba de reservar esta franja. Elige otra hora.", cuerpo.GetProperty("detail").GetString());
        Assert.Equal("FRANJA_OCUPADA", cuerpo.GetProperty("codigo").GetString());
        Assert.False(string.IsNullOrEmpty(cuerpo.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Una_excepcion_cualquiera_da_500_sin_detalles_internos()
    {
        var (estado, _, cuerpo) = await Manejar(new InvalidOperationException("Npgsql: conexión rechazada en 10.0.0.5"));

        Assert.Equal(500, estado);
        Assert.Equal("ERROR_INTERNO", cuerpo.GetProperty("codigo").GetString());
        Assert.DoesNotContain("Npgsql", cuerpo.GetRawText());
        Assert.DoesNotContain("10.0.0.5", cuerpo.GetRawText());
    }

    [Fact]
    public async Task Un_error_de_negocio_con_codigo_desconocido_da_500()
    {
        var (estado, _, cuerpo) = await Manejar(new ErrorNegocio("NO_EXISTE", "texto interno"));

        Assert.Equal(500, estado);
        Assert.Equal("ERROR_INTERNO", cuerpo.GetProperty("codigo").GetString());
        Assert.DoesNotContain("texto interno", cuerpo.GetRawText());
    }

    [Fact]
    public async Task Un_json_mal_formado_da_400_peticion_invalida_con_errors()
    {
        var json = new JsonException("mal", "$.correo", 1, 10);
        var (estado, _, cuerpo) = await Manejar(new BadHttpRequestException("Failed to read parameter", 400, json));

        Assert.Equal(400, estado);
        Assert.Equal("PETICION_INVALIDA", cuerpo.GetProperty("codigo").GetString());
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty("correo", out _));
        Assert.DoesNotContain("Failed to read", cuerpo.GetRawText());
    }
}
