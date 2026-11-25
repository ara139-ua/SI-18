using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>«Mis avisos» contra la API y PostgreSQL reales (tasks.md T069; HU-C4; quickstart.md V6).</summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosTests(FabricaApi fabrica)
{
    private const string Ana = "ciudadana.empadronada@si18.test";
    private const string Bruno = "ciudadano.noempadronado@si18.test";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Crea un aviso con <see cref="IServicioAvisos"/> y lo guarda, como haría un caso de uso.</summary>
    private async Task<long> Avisar(string correo, string texto)
    {
        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
        var usuarioId = await db.Usuarios.Where(u => u.Correo == correo).Select(u => u.Id).SingleAsync(Ct);

        ambito.ServiceProvider.GetRequiredService<IServicioAvisos>().Avisar(usuarioId, texto);
        await db.SaveChangesAsync(Ct);

        return await db.Notificaciones.Where(n => n.Texto == texto).Select(n => n.Id).SingleAsync(Ct);
    }

    private static string Unico(string texto) => $"{texto} {Guid.NewGuid():N}";

    private static async Task<List<AvisoDto>> Listar(HttpClient cliente) =>
        (await cliente.GetFromJsonAsync<List<AvisoDto>>("/api/avisos", OpcionesJson.Predeterminadas, Ct))!;

    [Fact]
    public async Task Un_usuario_solo_ve_sus_avisos_del_mas_reciente_al_mas_antiguo()
    {
        var primero = Unico("Tu reserva se ha cancelado por mantenimiento.");
        var segundo = Unico("Tienes plaza en el curso de Yoga.");
        var deOtro = Unico("Aviso de otra persona.");

        await Avisar(Ana, primero);
        fabrica.Reloj.Advance(TimeSpan.FromMinutes(1));
        await Avisar(Ana, segundo);
        await Avisar(Bruno, deOtro);

        using var cliente = await fabrica.CrearClienteConSesion(Ana);
        var avisos = await Listar(cliente);

        Assert.DoesNotContain(avisos, a => a.Texto == deOtro);
        var mios = avisos.Where(a => a.Texto == primero || a.Texto == segundo).ToList();
        Assert.Equal([segundo, primero], mios.Select(a => a.Texto));
        Assert.All(mios, a => Assert.False(a.Leido));
        Assert.True(avisos.SequenceEqual(avisos.OrderByDescending(a => a.FechaUtc)), "Los avisos deben ir del más reciente al más antiguo.");
    }

    [Fact]
    public async Task La_fecha_del_aviso_sale_del_reloj_de_la_API()
    {
        var texto = Unico("Aviso con fecha.");
        var ahora = fabrica.Reloj.GetUtcNow().UtcDateTime;
        await Avisar(Bruno, texto);

        using var cliente = await fabrica.CrearClienteConSesion(Bruno);
        var aviso = (await Listar(cliente)).Single(a => a.Texto == texto);

        Assert.Equal(ahora, aviso.FechaUtc.ToUniversalTime(), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Marcar_como_leido_dos_veces_da_204_las_dos_veces()
    {
        var texto = Unico("Aviso para leer.");
        var id = await Avisar(Ana, texto);
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var primera = await cliente.PutAsync($"/api/avisos/{id}/leido", content: null, Ct);
        var segunda = await cliente.PutAsync($"/api/avisos/{id}/leido", content: null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        Assert.True((await Listar(cliente)).Single(a => a.Texto == texto).Leido);
    }

    [Fact]
    public async Task Marcar_el_aviso_de_otra_persona_da_404_y_no_lo_cambia()
    {
        var texto = Unico("Aviso ajeno.");
        var id = await Avisar(Bruno, texto);
        using var ana = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await ana.PutAsync($"/api/avisos/{id}/leido", content: null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var cuerpo = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
        Assert.Equal("NO_ENCONTRADO", cuerpo.RootElement.GetProperty("codigo").GetString());

        using var bruno = await fabrica.CrearClienteConSesion(Bruno);
        Assert.False((await Listar(bruno)).Single(a => a.Texto == texto).Leido);
    }

    [Fact]
    public async Task Marcar_un_aviso_que_no_existe_da_el_mismo_404()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await cliente.PutAsync("/api/avisos/999999999/leido", content: null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task V6_sin_cookie_los_avisos_dan_401_no_autenticado()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/avisos", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
        Assert.Equal("NO_AUTENTICADO", cuerpo.RootElement.GetProperty("codigo").GetString());
    }
}
