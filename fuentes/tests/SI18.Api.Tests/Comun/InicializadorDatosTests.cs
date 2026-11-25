using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// Siembra de los datos de demostración ficticios (tasks.md T040; R13; data-model.md, «Datos de inicialización»;
/// quickstart.md V10). El arranque de <see cref="FabricaApi"/> ya ha sembrado una vez.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class InicializadorDatosTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Recuentos de T038.</summary>
    private static readonly Recuentos Esperados = new(
        Centros: 4,
        Deportes: 7,
        Instalaciones: 26,
        Tarifas: 6,
        Clubes: 2,
        Temporadas: 1,
        Usuarios: 10,
        Cursos: 3);

    private sealed record Recuentos(
        int Centros, int Deportes, int Instalaciones, int Tarifas, int Clubes, int Temporadas, int Usuarios, int Cursos);

    private async Task<Recuentos> Contar()
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return new Recuentos(
            await db.Centros.CountAsync(Ct),
            await db.Deportes.CountAsync(Ct),
            await db.Instalaciones.CountAsync(Ct),
            await db.Tarifas.CountAsync(Ct),
            await db.Clubes.CountAsync(Ct),
            await db.Temporadas.CountAsync(Ct),
            await db.Usuarios.CountAsync(Ct),
            await db.Cursos.CountAsync(Ct));
    }

    private async Task Sembrar()
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        await InicializadorDatos.SembrarAsync(db, FabricaApi.ClaveDemo, NullLogger.Instance, Ct);
    }

    [Fact]
    public async Task La_siembra_crea_los_recuentos_de_T038()
    {
        Assert.Equal(Esperados, await Contar());
    }

    [Fact]
    public async Task Ejecutar_el_inicializador_dos_veces_no_cambia_los_recuentos()
    {
        await Sembrar();
        await Sembrar();

        Assert.Equal(Esperados, await Contar());
    }

    [Fact]
    public async Task V10_el_hash_de_la_contrasena_no_contiene_la_clave_en_claro()
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var hashes = await db.Usuarios.AsNoTracking().Select(u => u.HashContrasena).ToListAsync(Ct);

        Assert.Equal(Esperados.Usuarios, hashes.Count);
        Assert.All(hashes, h =>
        {
            Assert.False(string.IsNullOrWhiteSpace(h));
            Assert.DoesNotContain(FabricaApi.ClaveDemo, h, StringComparison.Ordinal);
        });
    }
}
