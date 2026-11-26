using System.Net;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Exclusión mutua con peticiones simultáneas (specs/002-reservar-pagar tasks.md T029; CE-002, HU2 esc. 2, RF-007).
/// Polideportivo Oeste, «Pista de pádel 1», 23/12/2026 a las 19:00.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReservaConcurrenteTests(FabricaApi fabrica)
{
    private const int Peticiones = 20;

    private readonly DatosReservas _datos = new(fabrica);

    [Fact]
    [Trait("Categoria", "Limites")]
    public async Task De_20_peticiones_simultaneas_a_la_misma_franja_solo_una_crea_la_reserva()
    {
        var pista = await _datos.Pista("Polideportivo Oeste", "Pista de pádel 1");
        var inicio = Utc(new DateOnly(2026, 12, 23), 19);

        // La siembra solo tiene estas tres personas que puedan reservar fuera de MisReservasTests.
        var sesiones = new[]
        {
            (Cliente: await fabrica.CrearClienteConSesion(Ana), Importe: 6.00m),
            (Cliente: await fabrica.CrearClienteConSesion(Bruno), Importe: 10.00m),
            (Cliente: await fabrica.CrearClienteConSesion(Elena), Importe: 4.00m),
        };
        try
        {
            var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var peticiones = Enumerable.Range(0, Peticiones).Select(i => Task.Run(async () =>
            {
                var (cliente, importe) = sesiones[i % sesiones.Length];
                await salida.Task;
                var respuesta = await PedirReserva(cliente, pista, inicio, importe);
                return (respuesta.StatusCode, respuesta.IsSuccessStatusCode ? null : (await Problema(respuesta)).Codigo);
            })).ToArray();

            salida.SetResult();
            var resultados = await Task.WhenAll(peticiones);

            Assert.Equal(1, resultados.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(Peticiones - 1, resultados.Count(r => r.StatusCode == HttpStatusCode.Conflict && r.Item2 == CodigosError.FranjaOcupada));

            var enLaBase = await _datos.DeLaFranja(pista, inicio);
            var activa = Assert.Single(enLaBase);
            Assert.Equal(EstadoReserva.PendientePago, activa.Estado);
            Assert.NotNull(activa.Pago);
        }
        finally
        {
            foreach (var (cliente, _) in sesiones)
            {
                cliente.Dispose();
            }
        }
    }
}
