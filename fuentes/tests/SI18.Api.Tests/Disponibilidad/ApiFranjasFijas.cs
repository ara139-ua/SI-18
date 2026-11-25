using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>Llamadas a <c>/api/solicitudes-franjas-fijas</c> para las pruebas de spec-005.</summary>
public sealed class ApiFranjasFijas(FabricaApi fabrica)
{
    public const string Ruta = "/api/solicitudes-franjas-fijas";
    public const string RepresentanteNorte = "representante.norte@si18.test";
    public const string RepresentantePadel = "representante.padel@si18.test";
    public const string ClubNorte = "Club Deportivo Ficticio Norte";
    public const string Tecnico = "tecnico@si18.test";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<HttpResponseMessage> Enviar(HttpClient cliente, int instalacionId, int temporadaId, int[] dias, string inicio, string fin) =>
        await cliente.PostAsJsonAsync(Ruta, new PeticionSolicitudFranjasFijas(instalacionId, temporadaId, dias, inicio, fin), OpcionesJson.Predeterminadas, Ct);

    /// <summary>Crea una solicitud como <paramref name="representante"/> y comprueba que da 201.</summary>
    public async Task<SolicitudFranjasFijasDto> Crear(int instalacionId, int[] dias, int inicio, int fin, string representante = RepresentanteNorte)
    {
        using var cliente = await fabrica.CrearClienteConSesion(representante);
        var temporada = await new DatosDisponibilidad(fabrica).Temporada("2026-2027");
        var respuesta = await Enviar(cliente, instalacionId, temporada, dias, $"{inicio:00}:00", $"{fin:00}:00");
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await Leer<SolicitudFranjasFijasDto>(respuesta))!;
    }

    public async Task<HttpResponseMessage> Aprobar(int id, string usuario = Tecnico)
    {
        using var cliente = await fabrica.CrearClienteConSesion(usuario);
        return await cliente.PostAsync($"{Ruta}/{id}/aprobacion", content: null, Ct);
    }

    public async Task<HttpResponseMessage> Rechazar(int id, string usuario = Tecnico)
    {
        using var cliente = await fabrica.CrearClienteConSesion(usuario);
        return await cliente.PostAsync($"{Ruta}/{id}/rechazo", content: null, Ct);
    }

    public async Task<ConflictosSolicitudDto> Conflictos(int id)
    {
        using var cliente = await fabrica.CrearClienteConSesion(Tecnico);
        return (await cliente.GetFromJsonAsync<ConflictosSolicitudDto>($"{Ruta}/{id}/conflictos", OpcionesJson.Predeterminadas, Ct))!;
    }

    public async Task<IReadOnlyList<SolicitudFranjasFijasDto>> Listar(string usuario)
    {
        using var cliente = await fabrica.CrearClienteConSesion(usuario);
        return (await cliente.GetFromJsonAsync<List<SolicitudFranjasFijasDto>>(Ruta, OpcionesJson.Predeterminadas, Ct))!;
    }

    /// <summary>Avisos guardados para los representantes de un club (para comprobar «todos los representantes»).</summary>
    public async Task<(int Representantes, IReadOnlyList<string> Textos)> AvisosDelClub(string club)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var clubId = await db.Clubes.Where(c => c.Nombre == club).Select(c => c.Id).SingleAsync(Ct);
        var representantes = await db.Usuarios.Where(u => u.ClubId == clubId && u.Rol == SI18.Dominio.Comun.Rol.RepresentanteClub).Select(u => u.Id).ToListAsync(Ct);
        var textos = await db.Notificaciones.Where(n => representantes.Contains(n.UsuarioId)).Select(n => n.Texto).ToListAsync(Ct);
        return (representantes.Count, textos);
    }

    public static async Task<T?> Leer<T>(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson.Predeterminadas, Ct);

    public static async Task<JsonElement> Cuerpo(HttpResponseMessage respuesta) =>
        (await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct)).RootElement.Clone();

    public static async Task<string?> Codigo(HttpResponseMessage respuesta) => (await Cuerpo(respuesta)).GetProperty("codigo").GetString();
}
