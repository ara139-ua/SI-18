using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Dominio.Comun;
using TipoInstalacionDto = SI18.Contratos.Comun.TipoInstalacion;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Catálogo precargado, anónimo (contracts/api-comun.openapi.yaml, <c>/api/centros</c>, <c>/api/deportes</c> e
/// <c>/api/instalaciones</c>; quickstart.md V7–V8). Dueño: Ambos (tasks.md T054).
/// </summary>
public static class EndpointsCatalogo
{
    public static IEndpointRouteBuilder MapCatalogo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/centros", ListarCentros).AllowAnonymous();
        app.MapGet("/api/deportes", ListarDeportes).AllowAnonymous();
        app.MapGet("/api/instalaciones", ListarInstalaciones).AllowAnonymous();

        return app;
    }

    /// <summary>El horario no se guarda en <c>centros</c>: sale de <see cref="ValoresNegocio"/> (R6).</summary>
    private static async Task<IResult> ListarCentros(Si18DbContext db, CancellationToken ct)
    {
        var apertura = AHora(ValoresNegocio.HoraApertura);
        var cierre = AHora(ValoresNegocio.HoraCierre);

        var centros = await db.Centros.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CentroDto(c.Id, c.Nombre, c.Direccion, apertura, cierre))
            .ToListAsync(ct);

        return Results.Ok(centros);
    }

    private static async Task<IResult> ListarDeportes(Si18DbContext db, CancellationToken ct)
    {
        var deportes = await db.Deportes.AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => new DeporteDto(d.Id, d.Nombre))
            .ToListAsync(ct);

        return Results.Ok(deportes);
    }

    /// <summary>Los tres filtros son opcionales y se combinan.</summary>
    private static async Task<IResult> ListarInstalaciones(
        int? centroId,
        int? deporteId,
        bool? reservablePorFranjas,
        Si18DbContext db,
        CancellationToken ct)
    {
        var consulta = db.Instalaciones.AsNoTracking();

        if (centroId is { } centro)
        {
            consulta = consulta.Where(i => i.CentroId == centro);
        }

        if (deporteId is { } deporte)
        {
            consulta = consulta.Where(i => i.Deportes.Any(d => d.Id == deporte));
        }

        if (reservablePorFranjas is { } reservable)
        {
            consulta = consulta.Where(i => i.ReservablePorFranjas == reservable);
        }

        var filas = await consulta
            .OrderBy(i => i.Id)
            .Select(i => new
            {
                i.Id,
                i.CentroId,
                i.Nombre,
                i.Tipo,
                i.ReservablePorFranjas,
                DeporteIds = i.Deportes.OrderBy(d => d.Id).Select(d => d.Id).ToList(),
            })
            .ToListAsync(ct);

        var instalaciones = filas
            .Select(i => new InstalacionDto(
                i.Id,
                i.CentroId,
                i.Nombre,
                Enum.Parse<TipoInstalacionDto>(i.Tipo.ToString()),
                i.ReservablePorFranjas,
                i.DeporteIds))
            .ToList();

        return Results.Ok(instalaciones);
    }

    private static string AHora(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);
}
