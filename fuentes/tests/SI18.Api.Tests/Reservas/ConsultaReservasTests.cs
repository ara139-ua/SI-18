using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Modulos.Reservas;
using SI18.Api.Tests.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// <see cref="IConsultaReservas"/> definitiva (specs/002-reservar-pagar tasks.md T008; research.md E7; RF-019,
/// CE-007). Los dos primeros casos se han movido sin cambios desde <c>ConsultasProvisionalesTests</c> de spec-001:
/// fijan el mismo comportamiento que tenía la provisional. Polideportivo Oeste, enero de 2027.
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class ConsultaReservasTests(FabricaApi fabrica)
{
    private const string Centro = "Polideportivo Oeste";
    private static readonly DateOnly Martes = new(2027, 1, 12);
    private static readonly DateOnly Miercoles = new(2027, 1, 13);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    /// <summary>La <see cref="IConsultaReservas"/> que tiene registrada la API.</summary>
    private async Task<T> ConConsulta<T>(Func<IConsultaReservas, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await accion(ambito.ServiceProvider.GetRequiredService<IConsultaReservas>());
    }

    [Fact]
    public void La_API_registra_la_consulta_definitiva_de_Reservas()
    {
        using var ambito = fabrica.Services.CreateScope();
        Assert.IsType<ConsultaReservas>(ambito.ServiceProvider.GetRequiredService<IConsultaReservas>());
    }

    [Fact]
    public async Task Una_pendiente_bloquea_hasta_los_10_minutos_y_no_despues()
    {
        var pista = await _datos.Pista(Centro, "Pista de tenis 1");
        var ahora = _datos.Ahora;
        var hace959 = Utc(Martes, 9);
        var hace10 = Utc(Martes, 10);
        await _datos.Reserva(pista, hace959, EstadoPrueba.PendienteVigente, ahora - TimeSpan.FromSeconds(599));
        await _datos.Reserva(pista, hace10, EstadoPrueba.PendienteVigente, ahora - ValoresNegocio.TiempoBloqueoPago);

        var activas = await ConConsulta(consulta =>
            consulta.Activas(pista, Utc(Martes, 0), Utc(Martes.AddDays(1), 0), Ct));

        Assert.Contains(activas, r => r.InicioUtc == hace959);
        Assert.DoesNotContain(activas, r => r.InicioUtc == hace10);
    }

    [Fact]
    public async Task Solo_cuentan_confirmadas_y_pendientes_vigentes_dentro_del_rango()
    {
        var pista = await _datos.Pista(Centro, "Pista de tenis 2");
        await _datos.Reserva(pista, Utc(Martes, 11), EstadoPrueba.Confirmada);
        await _datos.Reserva(pista, Utc(Martes, 12), EstadoPrueba.Caducada);
        await _datos.Reserva(pista, Utc(Martes, 13), EstadoPrueba.Cancelada);
        await _datos.Reserva(pista, Utc(Miercoles, 11), EstadoPrueba.Confirmada);

        var activas = await ConConsulta(consulta =>
            consulta.Activas(pista, Utc(Martes, 0), Utc(Miercoles, 0), Ct));

        Assert.Equal([Utc(Martes, 11)], activas.Select(r => r.InicioUtc));
    }
}
