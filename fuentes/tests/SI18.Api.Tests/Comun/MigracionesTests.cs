using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// La migración <c>NucleoComun</c> crea el esquema compartido de data-model.md sobre una base de datos vacía
/// (tasks.md T039; HU-C1). Se comprueba en el catálogo de PostgreSQL, no en el modelo de EF.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MigracionesTests(FabricaApi fabrica)
{
    /// <summary>16 entidades persistidas más <c>instalacion_deportes</c>; la Franja no se almacena.</summary>
    private static readonly string[] TablasModelo =
    [
        "centros",
        "deportes",
        "instalaciones",
        "instalacion_deportes",
        "tarifas",
        "usuarios",
        "clubes",
        "reservas",
        "pagos",
        "devoluciones",
        "cierres_mantenimiento",
        "temporadas",
        "solicitudes_franjas_fijas",
        "cursos",
        "inscripciones",
        "notificaciones",
        "validaciones_acceso",
    ];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<List<string>> Consultar(string sql)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        return await db.Database.SqlQueryRaw<string>(sql).ToListAsync(Ct);
    }

    [Fact]
    public async Task Existen_las_17_tablas_de_data_model()
    {
        var tablas = await Consultar("""
            SELECT table_name AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            """);

        Assert.Equal(17, TablasModelo.Length);
        Assert.Equal(TablasModelo.Order(), tablas.Order());
    }

    [Fact]
    public async Task Existe_el_indice_ux_reservas_activa_unico_y_con_su_filtro()
    {
        var definiciones = await Consultar("""
            SELECT indexdef AS "Value"
            FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'reservas' AND indexname = 'ux_reservas_activa'
            """);

        // PostgreSQL normaliza el filtro, por ejemplo:
        // CREATE UNIQUE INDEX ux_reservas_activa ON public.reservas USING btree (instalacion_id, inicio_utc)
        //   WHERE ((estado)::text = ANY ((ARRAY['pendiente_pago'::character varying, 'confirmada'::character varying])::text[]))
        var definicion = Assert.Single(definiciones);
        Assert.Contains("CREATE UNIQUE INDEX", definicion, StringComparison.Ordinal);
        Assert.Contains("(instalacion_id, inicio_utc)", definicion, StringComparison.Ordinal);

        var filtro = definicion[definicion.IndexOf(" WHERE ", StringComparison.Ordinal)..];
        Assert.Contains("estado", filtro, StringComparison.Ordinal);
        Assert.Contains("'pendiente_pago'", filtro, StringComparison.Ordinal);
        Assert.Contains("'confirmada'", filtro, StringComparison.Ordinal);
        Assert.DoesNotContain("'caducada'", filtro, StringComparison.Ordinal);
        Assert.DoesNotContain("'cancelada'", filtro, StringComparison.Ordinal);
    }
}
