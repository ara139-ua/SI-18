using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>Los valores en base de datos coinciden con la tabla «Enumerados» de data-model.md (sin base de datos).</summary>
public class ConversorEnumSnakeCaseTests
{
    [Fact]
    public void Los_valores_coinciden_con_data_model()
    {
        Assert.Equal(["ciudadano", "representante_club", "personal_centro", "tecnico_concejalia"], ConversorEnumSnakeCase<Rol>.ValoresPermitidos);
        Assert.Equal(["empadronado", "no_empadronado", "club"], ConversorEnumSnakeCase<TipoTarifa>.ValoresPermitidos);
        Assert.Equal(["pista_tenis", "pista_padel", "pabellon", "sala", "piscina"], ConversorEnumSnakeCase<TipoInstalacion>.ValoresPermitidos);
        Assert.Equal(["pendiente_pago", "confirmada", "caducada", "cancelada"], ConversorEnumSnakeCase<EstadoReserva>.ValoresPermitidos);
        Assert.Equal(["usuario", "cierre_mantenimiento"], ConversorEnumSnakeCase<MotivoCancelacion>.ValoresPermitidos);
        Assert.Equal(["pendiente", "completado", "fallido"], ConversorEnumSnakeCase<EstadoPago>.ValoresPermitidos);
        Assert.Equal(["cancelacion_usuario", "cierre_mantenimiento"], ConversorEnumSnakeCase<MotivoDevolucion>.ValoresPermitidos);
        Assert.Equal(["pendiente", "aprobada", "rechazada"], ConversorEnumSnakeCase<EstadoSolicitud>.ValoresPermitidos);
        Assert.Equal(["confirmada", "lista_espera", "baja"], ConversorEnumSnakeCase<EstadoInscripcion>.ValoresPermitidos);
    }

    [Theory]
    [InlineData(EstadoReserva.PendientePago)]
    [InlineData(EstadoReserva.Cancelada)]
    public void Ida_y_vuelta(EstadoReserva valor)
    {
        var conversor = new ConversorEnumSnakeCase<EstadoReserva>();

        var texto = (string)conversor.ConvertToProvider(valor)!;

        Assert.Equal(valor, conversor.ConvertFromProvider(texto));
    }
}
