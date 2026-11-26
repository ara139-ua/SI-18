using System.Net;
using System.Net.Http.Json;
using SI18.Api.Tests.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Disponibilidad;
using SI18.Contratos.Reservas;
using static SI18.Api.Tests.Reservas.DatosReservas;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// De extremo a extremo, el importe cobrado es el precio que muestra la búsqueda (specs/002-reservar-pagar
/// tasks.md T020; CE-005, HU1 esc. 5). Polideportivo Norte, «Tenis», 26 y 27/12/2026.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReservarComoEnLaBusquedaTests(FabricaApi fabrica)
{
    private const string Norte = "Polideportivo Norte";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosReservas _datos = new(fabrica);

    [Theory]
    [InlineData(Ana, "2026-12-26", 6.00)]
    [InlineData(Bruno, "2026-12-27", 10.00)]
    [InlineData(Elena, "2026-12-26", 4.00)]
    public async Task El_precio_de_la_busqueda_es_el_del_resumen_el_de_la_reserva_y_el_del_pago(string correo, string fecha, decimal tarifa)
    {
        using var cliente = await fabrica.CrearClienteConSesion(correo);
        var busqueda = await cliente.GetFromJsonAsync<ResultadoBusquedaDto>(
            $"/api/disponibilidad/franjas?deporteId={await _datos.Disponibilidad.Deporte("Tenis")}&centroId={await _datos.Disponibilidad.Centro(Norte)}&fecha={fecha}",
            OpcionesJson.Predeterminadas, Ct);

        var pista = busqueda!.Pistas[0];
        var franjas = pista.Franjas.Take(3).ToList();
        Assert.Equal(3, franjas.Count);

        foreach (var franja in franjas)
        {
            Assert.Equal(tarifa, franja.Precio);

            var resumen = await Json<ResumenReservaDto>(await PedirResumen(cliente, pista.InstalacionId, franja.InicioUtc));
            Assert.Equal(franja.Precio, resumen.Importe);

            var creada = await PedirReserva(cliente, pista.InstalacionId, franja.InicioUtc, resumen.Importe);
            Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
            var reserva = await Json<ReservaDto>(creada);
            Assert.Equal(franja.Precio, reserva.Importe);

            var pagada = await PedirPago(cliente, reserva.Id, TarjetaAceptada);
            Assert.Equal(HttpStatusCode.OK, pagada.StatusCode);
            Assert.Equal(franja.Precio, (await Json<ReservaDto>(pagada)).Importe);

            Assert.Equal(franja.Precio, (await _datos.Leer(reserva.Id)).Pago!.Importe);
        }
    }
}
