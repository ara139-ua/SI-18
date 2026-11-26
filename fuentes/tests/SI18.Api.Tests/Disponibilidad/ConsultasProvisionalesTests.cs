using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Disponibilidad.Provisional;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// Fija el comportamiento de las consultas provisionales de spec-001 (tasks.md T007; research.md D5), para que
/// las definitivas de spec-002 y spec-007 lo puedan reutilizar. Polideportivo Oeste, enero de 2027. Las de franjas
/// fijas pasaron a ConsultaFranjasFijasTests al publicar spec-005 su consulta definitiva.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ConsultasProvisionalesTests(FabricaApi fabrica)
{
    private const string Centro = "Polideportivo Oeste";
    private static readonly DateOnly Martes = new(2027, 1, 12);
    private static readonly DateOnly Miercoles = new(2027, 1, 13);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<T> ConContexto<T>(Func<Si18DbContext, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await accion(ambito.ServiceProvider.GetRequiredService<Si18DbContext>());
    }

    // ── IConsultaCierres ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Los_cierres_se_solapan_con_intervalos_semiabiertos()
    {
        var pista = await _datos.Pista(Centro, "Pista de pádel 1");
        await _datos.Cierre(pista, Utc(Martes, 10), Utc(Martes, 12));

        var antes = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 9), Utc(Martes, 10), Ct));
        var dentro = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 11), Utc(Martes, 12), Ct));
        var despues = await ConContexto(db => new ConsultaCierresProvisional(db).Cierres(pista, Utc(Martes, 12), Utc(Martes, 13), Ct));

        Assert.Empty(antes);
        Assert.Single(dentro);
        Assert.Empty(despues);
    }
}
