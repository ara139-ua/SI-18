using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Infraestructura;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using static SI18.Api.Tests.Reservas.DatosReservas;
using static SI18.Api.Tests.Reservas.DatosValidaciones;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// Aviso de la validación aceptada anterior (specs/006-validar-qr tasks.md T008; RF-006a, CE-007). Con
/// <see cref="DatosValidaciones.ConServicio{T}(DateTime, string, Func{ServicioValidarAcceso, Task{T}})"/> y reloj propio.
/// Cada caso tiene su propia reserva de Carla en Este «Pista de pádel 2», de 10:00 a 11:00, en su propia fecha
/// (análisis C2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisoValidacionAnteriorTests(FabricaApi fabrica)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosValidaciones _datos = new(fabrica);

    private async Task<string> Codigo(long reservaId) => (await _datos.Reservas.Leer(reservaId)).CodigoAcceso!;

    [Fact]
    public async Task HU1_esc6_la_primera_aceptada_no_lleva_aviso_y_las_siguientes_la_hora_de_la_mas_reciente()
    {
        var codigo = await Codigo(await _datos.ConfirmadaALasDiez(Dia18));

        var primera = await _datos.ConServicio(Utc(Dia18, 9, 50), PersonalEste, s => s.Validar(codigo, Ct));
        var segunda = await _datos.ConServicio(Utc(Dia18, 10, 20), PersonalEste, s => s.Validar(codigo, Ct));
        var tercera = await _datos.ConServicio(Utc(Dia18, 10, 40), PersonalEste, s => s.Validar(codigo, Ct));

        Assert.Null(primera.Aceptada!.ValidacionAnteriorUtc);
        Assert.Null(primera.Aceptada.ValidacionAnteriorHora);
        Assert.Equal(Utc(Dia18, 9, 50), segunda.Aceptada!.ValidacionAnteriorUtc);
        Assert.Equal("09:50", segunda.Aceptada.ValidacionAnteriorHora);
        Assert.Equal(Utc(Dia18, 10, 20), tercera.Aceptada!.ValidacionAnteriorUtc);
        Assert.Equal("10:20", tercera.Aceptada.ValidacionAnteriorHora);
    }

    [Fact]
    public async Task HU1_esc7_un_rechazo_anterior_no_cuenta_como_validacion_previa()
    {
        var codigo = await Codigo(await _datos.ConfirmadaALasDiez(Dia20));

        var rechazada = await _datos.ConServicio(Utc(Dia20, 9, 40), PersonalEste, s => s.Validar(codigo, Ct));
        var aceptada = await _datos.ConServicio(Utc(Dia20, 9, 50), PersonalEste, s => s.Validar(codigo, Ct));

        Assert.Equal(CodigosError.QrFueraDeVentana, rechazada.Codigo);
        Assert.Null(aceptada.Aceptada!.ValidacionAnteriorUtc);
        Assert.Null(aceptada.Aceptada.ValidacionAnteriorHora);
    }

    [Fact]
    public async Task Una_validacion_aceptada_por_otra_persona_del_personal_tambien_cuenta()
    {
        var reservaId = await _datos.ConfirmadaALasDiez(Dia22);
        var codigo = await Codigo(reservaId);
        var otra = await _datos.Reservas.Usuario(PersonalOeste);

        // La siembra solo tiene una persona del personal por centro: la validación anterior se inserta directamente.
        using (var ambito = fabrica.Services.CreateScope())
        {
            var db = ambito.ServiceProvider.GetRequiredService<Si18DbContext>();
            db.ValidacionesAcceso.Add(new ValidacionAcceso(reservaId, codigo, true, null, Utc(Dia22, 9, 48), otra));
            await db.SaveChangesAsync(Ct);
        }

        var resultado = await _datos.ConServicio(Utc(Dia22, 9, 55), PersonalEste, s => s.Validar(codigo, Ct));

        Assert.Equal("09:48", resultado.Aceptada!.ValidacionAnteriorHora);
        Assert.Equal(Utc(Dia22, 9, 48), resultado.Aceptada.ValidacionAnteriorUtc);
    }
}
