using SI18.Dominio.Reservas;

namespace SI18.Dominio.Tests.Reservas;

public class GeneradorCodigoAccesoTests
{
    [Fact]
    public void Tiene_22_caracteres_Base64url_sin_relleno()
    {
        var codigo = GeneradorCodigoAcceso.Nuevo();

        Assert.Equal(22, codigo.Length);
        Assert.Matches("^[A-Za-z0-9_-]{22}$", codigo);
    }

    [Fact]
    public void Mil_codigos_son_distintos()
    {
        var codigos = Enumerable.Range(0, 1000).Select(_ => GeneradorCodigoAcceso.Nuevo()).ToHashSet();

        Assert.Equal(1000, codigos.Count);
    }
}
