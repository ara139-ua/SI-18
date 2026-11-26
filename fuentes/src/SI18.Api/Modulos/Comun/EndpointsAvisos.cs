using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// «Mis avisos» del usuario identificado (contracts/api-comun.openapi.yaml, <c>/api/avisos</c>).
/// Dueño: Ambos (tasks.md T066).
/// </summary>
public static class EndpointsAvisos
{
    public static IEndpointRouteBuilder MapAvisos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/avisos").RequireAuthorization();

        grupo.MapGet("/", Listar);
        grupo.MapPut("/{id:long}/leido", MarcarLeido);

        return app;
    }

    /// <summary>Solo los avisos del usuario, del más reciente al más antiguo.</summary>
    private static async Task<IResult> Listar(IUsuarioActual actual, Si18DbContext db, CancellationToken ct)
    {
        var usuarioId = actual.Id;
        var avisos = await db.Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaUtc)
            .ThenByDescending(n => n.Id)
            .Select(n => new AvisoDto(n.Id, n.Texto, n.FechaUtc, n.Leida))
            .ToListAsync(ct);

        return Results.Ok(avisos);
    }

    /// <summary>
    /// Idempotente: marcar dos veces da 204 las dos veces. Si el aviso no existe o es de otra persona, 404
    /// <c>NO_ENCONTRADO</c>, sin revelar cuál de los dos casos es.
    /// </summary>
    private static async Task<IResult> MarcarLeido(long id, IUsuarioActual actual, Si18DbContext db, CancellationToken ct)
    {
        var usuarioId = actual.Id;
        var aviso = await db.Notificaciones.SingleOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        if (!aviso.Leida)
        {
            aviso.MarcarLeida();
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }
}
