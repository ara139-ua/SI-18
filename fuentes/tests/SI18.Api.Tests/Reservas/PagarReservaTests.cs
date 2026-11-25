using System.Net;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using static SI18.Api.Tests.Reservas.DatosReservas;
using EstadoReserva = SI18.Contratos.Reservas.EstadoReserva;
using EstadoReservaDominio = SI18.Dominio.Comun.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <c>POST /api/reservas/{id}/pago</c> y <c>GET /api/reservas/{id}</c> (specs/002-reservar-pagar tasks.md T019 y
/// siguientes). Polideportivo Este, «Pista de tenis 1» y «Pista de tenis 2», del 22 al 28/12/2026. Las reservas se
/// insertan ya creadas en el instante que pide cada caso, sin adelantar el reloj.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PagarReservaTests(FabricaApi fabrica)
{
    private const string Este = "Polideportivo Este";

    private readonly DatosReservas _datos = new(fabrica);

    private Task<int> Tenis1() => _datos.Pista(Este, "Pista de tenis 1");

    private Task<int> Tenis2() => _datos.Pista(Este, "Pista de tenis 2");

    [Fact]
    public async Task HU1_esc3_con_una_tarjeta_aceptada_queda_confirmada_con_su_codigo_de_acceso()
    {
        var pista = await Tenis1();
        var id = await _datos.Pendiente(Ana, pista, Utc(new DateOnly(2026, 12, 22), 10), _datos.Ahora - TimeSpan.FromMinutes(1));
        using var cliente = await fabrica.CrearClienteConSesion(Ana);

        var respuesta = await PedirPago(cliente, id, "4000 0000 0000 0001");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var reserva = await Json<ReservaDto>(respuesta);
        Assert.Equal(EstadoReserva.Confirmada, reserva.Estado);
        Assert.Equal(GeneradorCodigoAcceso.Longitud, reserva.CodigoAcceso?.Length);
        Assert.Null(reserva.LimitePagoUtc);
        Assert.Null(reserva.HoraLimitePago);

        var guardada = await _datos.Leer(id);
        Assert.Equal(EstadoReservaDominio.Confirmada, guardada.Estado);
        Assert.Equal(reserva.CodigoAcceso, guardada.CodigoAcceso);
        Assert.Equal(EstadoPago.Completado, guardada.Pago!.Estado);
        Assert.NotNull(guardada.Pago.ResueltoEnUtc);

        var vista = await Json<ReservaDto>(await cliente.GetAsync($"/api/reservas/{id}", TestContext.Current.CancellationToken));
        Assert.Equal(EstadoReserva.Confirmada, vista.Estado);
        Assert.Equal(reserva.CodigoAcceso, vista.CodigoAcceso);
    }
}
