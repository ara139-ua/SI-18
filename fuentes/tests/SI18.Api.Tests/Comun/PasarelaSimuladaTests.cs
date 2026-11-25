using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>Regla determinista de la pasarela simulada (research.md R9; sin base de datos).</summary>
public class PasarelaSimuladaTests
{
    private readonly PasarelaSimulada _pasarela = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("5500000000000004")]
    public async Task Una_tarjeta_de_16_digitos_se_acepta(string tarjeta)
    {
        Assert.True(await _pasarela.Cobrar(6.00m, tarjeta, Ct));
    }

    [Fact]
    public async Task Una_tarjeta_que_termina_en_0002_se_rechaza()
    {
        Assert.False(await _pasarela.Cobrar(6.00m, "4000000000000002", Ct));
    }

    [Theory]
    [InlineData("411111111111111")]   // 15 dígitos
    [InlineData("41111111111111111")] // 17 dígitos
    [InlineData("4111-1111-1111-111")]
    [InlineData("")]
    public async Task Sin_16_digitos_es_peticion_invalida(string tarjeta)
    {
        var error = await Assert.ThrowsAsync<ErrorNegocio>(() => _pasarela.Cobrar(6.00m, tarjeta, Ct));

        Assert.Equal(CodigosError.PeticionInvalida, error.Codigo);
    }

    [Fact]
    public async Task Devolver_siempre_termina_bien()
    {
        await _pasarela.Devolver(1, 6.00m, Ct);
    }
}
