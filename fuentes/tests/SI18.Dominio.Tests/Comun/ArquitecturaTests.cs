using SI18.Dominio.Comun;

namespace SI18.Dominio.Tests.Comun;

/// <summary>El dominio no depende de ASP.NET Core ni de EF Core (plan.md, «Decisión de estructura»).</summary>
public class ArquitecturaTests
{
    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    public void SI18_Dominio_no_referencia(string prefijoProhibido)
    {
        var referencias = typeof(ValoresNegocio).Assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name ?? string.Empty);

        Assert.DoesNotContain(referencias, n => n.StartsWith(prefijoProhibido, StringComparison.Ordinal));
    }
}
