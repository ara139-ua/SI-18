using System.Net;
using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <c>GET /api/reservas</c>, «Mis reservas» (specs/002-reservar-pagar tasks.md T039; HU5, RF-015–RF-017).
/// Polideportivo Oeste, «Pista de tenis 1» y «Pista de tenis 2». Solo con <c>representante.norte@si18.test</c>, que no
/// usa ninguna otra prueba, para que su lista no dependa de ellas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MisReservasTests(FabricaApi fabrica)
{
    private const string Oeste = "Polideportivo Oeste";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Tenis1() => _datos.Pista(Oeste, "Pista de tenis 1");

    private Task<int> Tenis2() => _datos.Pista(Oeste, "Pista de tenis 2");

    private async Task<List<ReservaDto>> MisReservas()
    {
        using var cliente = await fabrica.CrearClienteConSesion(Diego);
        var respuesta = await cliente.GetAsync("/api/reservas", Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await Json<List<ReservaDto>>(respuesta);
    }

    [Fact]
    public async Task HU5_esc6_primero_las_que_no_han_terminado_por_inicio_y_despues_las_terminadas()
    {
        var ahora = _datos.Ahora;
        var (hoy, hora) = HorarioMadrid.ALocal(ahora);
        var enCurso = await _datos.Confirmada(Diego, await Tenis2(), Utc(hoy, hora.Hour), ahora - TimeSpan.FromHours(2));
        var dia27 = await _datos.Confirmada(Diego, await Tenis1(), Utc(new DateOnly(2026, 12, 27), 10), ahora - TimeSpan.FromHours(1));
        var dia26 = await _datos.Confirmada(Diego, await Tenis1(), Utc(new DateOnly(2026, 12, 25), 10), ahora - TimeSpan.FromHours(1));
        var dia20Nov = await _datos.Confirmada(Diego, await Tenis1(), Utc(new DateOnly(2026, 11, 20), 10), Utc(new DateOnly(2026, 11, 19), 9));

        var lista = await MisReservas();

        // La que está en curso (inicio ≤ ahora < fin) aún no ha terminado y es la que antes empieza.
        Assert.Equal(enCurso, lista[0].Id);
        long[] propias = [enCurso, dia26, dia27, dia20Nov];
        Assert.Equal(propias, lista.Select(r => r.Id).Where(propias.Contains));
    }

    [Fact]
    public async Task HU5_esc1_a_3_el_codigo_solo_en_las_confirmadas_y_la_hora_limite_solo_en_las_pendientes()
    {
        var pista = await Tenis2();
        var dia = new DateOnly(2026, 12, 22);
        var ahora = _datos.Ahora;
        var creadaPendiente = ahora - TimeSpan.FromSeconds(30);
        var confirmada = await _datos.Confirmada(Diego, pista, Utc(dia, 10), ahora - TimeSpan.FromMinutes(5));
        var caducada = await _datos.CaducadaPorTiempo(Diego, pista, Utc(dia, 11), ahora - TimeSpan.FromMinutes(30));
        var cancelada = await _datos.Cancelada(Diego, pista, Utc(dia, 12), ahora - TimeSpan.FromMinutes(5));
        var pendiente = await _datos.Pendiente(Diego, pista, Utc(dia, 13), creadaPendiente);

        var lista = (await MisReservas()).ToDictionary(r => r.Id);

        Assert.Equal(EstadoReserva.Confirmada, lista[confirmada].Estado);
        Assert.Equal(22, lista[confirmada].CodigoAcceso?.Length);
        Assert.Null(lista[confirmada].HoraLimitePago);

        Assert.Equal(EstadoReserva.Caducada, lista[caducada].Estado);
        Assert.Null(lista[caducada].CodigoAcceso);
        Assert.Equal(EstadoReserva.Cancelada, lista[cancelada].Estado);
        Assert.Null(lista[cancelada].CodigoAcceso);

        // creada − 30 s: el límite cae a mitad de minuto y la hora se trunca hacia abajo.
        var limite = creadaPendiente + ValoresNegocio.TiempoBloqueoPago;
        Assert.Equal(EstadoReserva.PendientePago, lista[pendiente].Estado);
        Assert.Null(lista[pendiente].CodigoAcceso);
        Assert.Equal(limite, lista[pendiente].LimitePagoUtc!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(HoraMadrid(limite), lista[pendiente].HoraLimitePago);
        Assert.Equal("13:00", lista[pendiente].HoraInicio);
        Assert.Equal(Oeste, lista[pendiente].Centro);
        Assert.Equal("Pista de tenis 2", lista[pendiente].Pista);
    }

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task HU5_esc5_una_pendiente_de_10_minutos_exactos_se_ve_caducada_sin_escribir_nada()
    {
        var id = await _datos.Pendiente(Diego, await Tenis2(), Utc(new DateOnly(2026, 12, 22), 14), _datos.Ahora - ValoresNegocio.TiempoBloqueoPago);

        var reserva = (await MisReservas()).Single(r => r.Id == id);

        Assert.Equal(EstadoReserva.Caducada, reserva.Estado);
        Assert.Null(reserva.HoraLimitePago);
        Assert.Null(reserva.LimitePagoUtc);
        Assert.Equal(SI18.Dominio.Comun.EstadoReserva.PendientePago, (await _datos.Leer(id)).Estado);
    }

    [Fact]
    public async Task HU5_esc4_no_aparecen_ni_se_pueden_ver_las_reservas_de_otra_persona()
    {
        var deAna = await _datos.Confirmada(Ana, await Tenis1(), Utc(new DateOnly(2026, 12, 22), 15), _datos.Ahora - TimeSpan.FromMinutes(5));
        using var cliente = await fabrica.CrearClienteConSesion(Diego);

        var lista = await cliente.GetFromJsonAsync<List<ReservaDto>>("/api/reservas", OpcionesJson.Predeterminadas, Ct);
        var ver = await cliente.GetAsync($"/api/reservas/{deAna}", Ct);

        Assert.DoesNotContain(lista!, r => r.Id == deAna);
        Assert.Equal(HttpStatusCode.NotFound, ver.StatusCode);
        Assert.Equal(CodigosError.NoEncontrado, (await Problema(ver)).Codigo);
    }

    [Fact]
    public async Task El_personal_de_centro_recibe_403()
    {
        using var cliente = await fabrica.CrearClienteConSesion("personal.oeste@si18.test");

        var respuesta = await cliente.GetAsync("/api/reservas", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(CodigosError.RolNoPermitido, (await Problema(respuesta)).Codigo);
    }
}
