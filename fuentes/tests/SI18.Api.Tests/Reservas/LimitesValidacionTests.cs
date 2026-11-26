using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Reservas.DatosReservas;
using static SI18.Api.Tests.Reservas.DatosValidaciones;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Límites de la ventana del código y cambios de hora (specs/006-validar-qr tasks.md T014; CE-002, principio II).
/// Con <see cref="DatosValidaciones.ConServicio{T}(DateTime, string, Func{ServicioValidarAcceso, Task{T}})"/> y reloj
/// propio: ninguna prueba adelanta <c>fabrica.Reloj</c>. Una sola reserva de Carla el 21/01/2027 de 10:00 a 11:00,
/// compartida de solo lectura por todos los casos (el resultado no depende del historial de validaciones).
/// </summary>
[Collection(ColeccionApi.Nombre)]
[Trait("Categoria", "Limites")]
public class LimitesValidacionTests(FabricaApi fabrica) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosValidaciones _datos = new(fabrica);
    private string _codigo = null!;

    public async ValueTask InitializeAsync() =>
        _codigo = (await _datos.Reservas.Leer(await _datos.ConfirmadaALasDiez(Dia21))).CodigoAcceso!;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Instante a <paramref name="segundos"/> de las 10:00 de Madrid del 21/01/2027 (el inicio de la reserva).</summary>
    private DateTime Desde10(int segundos) => Utc(Dia21, 10).AddSeconds(segundos);

    [Theory]
    [InlineData(-15 * 60)] // 09:45:00, exactamente 15 minutos antes (HU1 esc. 2)
    [InlineData(-14 * 60)] // 09:46:00
    [InlineData(60 * 60)] // 11:00:00, el fin de la reserva, incluido
    public async Task Dentro_de_la_ventana_se_acepta(int segundosDesdeElInicio)
    {
        var resultado = await _datos.ConServicio(
            Desde10(segundosDesdeElInicio), PersonalEste, s => s.Validar(_codigo, Ct));

        Assert.NotNull(resultado.Aceptada);
    }

    [Theory]
    [InlineData(-15 * 60 - 1)] // 09:44:59, un segundo antes del margen (HU2 esc. 1)
    [InlineData(60 * 60 + 1)] // 11:00:01
    [InlineData(61 * 60)] // 11:01:00 (HU2 esc. 2)
    public async Task Fuera_de_la_ventana_se_rechaza_con_los_datos_de_la_reserva(int segundosDesdeElInicio)
    {
        var resultado = await _datos.ConServicio(
            Desde10(segundosDesdeElInicio), PersonalEste, s => s.Validar(_codigo, Ct));

        Assert.Equal(CodigosError.QrFueraDeVentana, resultado.Codigo);
        Assert.NotNull(resultado.Reserva);
    }

    [Theory]
    [MemberData(nameof(DiasDeCambioDeHora))]
    public async Task El_dia_del_cambio_de_hora_el_margen_son_15_minutos_reales(DateOnly dia)
    {
        var inicio = HorarioMadrid.AUtc(dia, new TimeOnly(10, 0));
        var codigo = (await _datos.Reservas.Leer(await _datos.ConfirmadaEn(inicio))).CodigoAcceso!;
        var limite = HorarioMadrid.AUtc(dia, new TimeOnly(9, 45));
        Assert.Equal(TimeSpan.FromMinutes(15), inicio - limite);

        var justo = await _datos.ConServicio(limite, PersonalEste, s => s.Validar(codigo, Ct));
        var antes = await _datos.ConServicio(limite.AddSeconds(-1), PersonalEste, s => s.Validar(codigo, Ct));

        Assert.NotNull(justo.Aceptada);
        Assert.Equal(CodigosError.QrFueraDeVentana, antes.Codigo);
    }

    public static TheoryData<DateOnly> DiasDeCambioDeHora() => new() { FinHorarioVerano, InicioHorarioVerano };
}
