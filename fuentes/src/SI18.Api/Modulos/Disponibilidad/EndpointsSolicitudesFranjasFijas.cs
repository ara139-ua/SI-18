using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Contratos.Disponibilidad;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// <c>/api/solicitudes-franjas-fijas</c> (specs/005-franjas-fijas/contracts/api-solicitudes-franjas-fijas.openapi.yaml;
/// research.md F1). Dueño: Artur.
/// </summary>
public static class EndpointsSolicitudesFranjasFijas
{
    public static IEndpointRouteBuilder MapEndpointsSolicitudesFranjasFijas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/solicitudes-franjas-fijas");

        grupo.MapGet("/", Listar).RequireAuthorization();
        grupo.MapPost("/", Crear).RequireAuthorization(Politicas.EsRepresentante);
        grupo.MapGet("/temporadas", Temporadas).RequireAuthorization(Politicas.EsRepresentante);
        grupo.MapGet("/{id:int}/conflictos", Conflictos).RequireAuthorization(Politicas.EsTecnico);
        grupo.MapPost("/{id:int}/aprobacion", Aprobar).RequireAuthorization(Politicas.EsTecnico);
        grupo.MapPost("/{id:int}/rechazo", Rechazar).RequireAuthorization(Politicas.EsTecnico);

        return app;
    }

    /// <summary>Representante: las de su club. Técnico: todas. Otro rol: 403 (sin política nueva en Comun, F1).</summary>
    private static async Task<IResult> Listar(IUsuarioActual usuario, ServicioSolicitudesFranjasFijas servicio, CancellationToken ct) =>
        usuario.Rol switch
        {
            Rol.RepresentanteClub => Results.Ok(await servicio.Listar(ClubDe(usuario), ct)),
            Rol.TecnicoConcejalia => Results.Ok(await servicio.Listar(null, ct)),
            _ => throw CodigosError.Error(CodigosError.RolNoPermitido),
        };

    private static async Task<IResult> Crear(
        PeticionSolicitudFranjasFijas peticion,
        HttpContext http,
        IUsuarioActual usuario,
        ServicioSolicitudesFranjasFijas servicio,
        CancellationToken ct)
    {
        // El club nunca llega en el cuerpo: sale de la sesión (RF-001).
        var resultado = await servicio.Crear(peticion, ClubDe(usuario), ct);
        if (resultado.Solicitud is null)
        {
            var problema = ManejadorErrores.CrearValidacion(http, resultado.Errores.ToDictionary(e => e.Key, e => e.Value));
            return Results.Json(problema, statusCode: problema.Status, contentType: ManejadorErrores.TipoContenido);
        }

        return Results.Created($"/api/solicitudes-franjas-fijas/{resultado.Solicitud.Id}", resultado.Solicitud);
    }

    private static async Task<IResult> Temporadas(ServicioSolicitudesFranjasFijas servicio, CancellationToken ct) =>
        Results.Ok(await servicio.TemporadasAbiertas(ct));

    private static async Task<IResult> Conflictos(int id, ServicioSolicitudesFranjasFijas servicio, CancellationToken ct) =>
        Results.Ok(await servicio.ConflictosDe(id, ct));

    private static async Task<IResult> Aprobar(int id, ServicioSolicitudesFranjasFijas servicio, CancellationToken ct) =>
        Results.Ok(await servicio.Aprobar(id, ct));

    private static async Task<IResult> Rechazar(int id, ServicioSolicitudesFranjasFijas servicio, CancellationToken ct) =>
        Results.Ok(await servicio.Rechazar(id, ct));

    private static int ClubDe(IUsuarioActual usuario) =>
        usuario.ClubId ?? throw CodigosError.Error(CodigosError.RolNoPermitido);
}
