using SI18.Dominio.Comun;

namespace SI18.Dominio.Tests.Comun;

public class PermisosCentroTests
{
    [Fact]
    public void Mismo_centro_pasa()
    {
        PermisosCentro.ExigirMismoCentro(centroPersonal: 2, centroInstalacion: 2);
    }

    [Fact]
    public void Otro_centro_lanza_OTRO_CENTRO()
    {
        var error = Assert.Throws<ErrorNegocio>(() => PermisosCentro.ExigirMismoCentro(1, 2));

        Assert.Equal("OTRO_CENTRO", error.Codigo);
    }

    [Fact]
    public void Sin_centro_lanza_OTRO_CENTRO()
    {
        var error = Assert.Throws<ErrorNegocio>(() => PermisosCentro.ExigirMismoCentro(null, 2));

        Assert.Equal("OTRO_CENTRO", error.Codigo);
    }
}
