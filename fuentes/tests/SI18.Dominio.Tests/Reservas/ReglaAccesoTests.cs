using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Dominio.Tests.Reservas;

[Trait("Categoria", "Limites")]
public class ReglaAccesoTests
{
    private static readonly DateTime Inicio = new(2026, 11, 20, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = Inicio + ValoresNegocio.DuracionFranja;
    private static readonly TimeSpan UnSegundo = TimeSpan.FromSeconds(1);

    /// <summary>Reserva en el estado pedido, llevada hasta él con la máquina de estados.</summary>
    private static Reserva ReservaEn(EstadoReserva estado)
    {
        var creada = Inicio - TimeSpan.FromDays(1);
        var reserva = new Reserva(1, 1, Inicio, TipoTarifa.Empadronado, 6.00m, creada);
        var poco = creada + TimeSpan.FromMinutes(1);

        switch (estado)
        {
            case EstadoReserva.Confirmada:
                reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), poco);
                break;
            case EstadoReserva.Cancelada:
                reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), poco);
                reserva.Cancelar(MotivoCancelacion.Usuario, poco);
                break;
            case EstadoReserva.Caducada:
                reserva.Caducar(poco);
                break;
        }

        return reserva;
    }

    [Fact]
    public void Quince_minutos_antes_del_inicio_es_valido()
    {
        Assert.Null(ReglaAcceso.MotivoRechazo(ReservaEn(EstadoReserva.Confirmada), Inicio - TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Quince_minutos_y_1_s_antes_del_inicio_esta_fuera_de_ventana()
    {
        var ahora = Inicio - TimeSpan.FromMinutes(15) - UnSegundo;

        Assert.Equal(ReglaAcceso.QrFueraDeVentana, ReglaAcceso.MotivoRechazo(ReservaEn(EstadoReserva.Confirmada), ahora));
    }

    [Fact]
    public void En_el_fin_es_valido()
    {
        Assert.Null(ReglaAcceso.MotivoRechazo(ReservaEn(EstadoReserva.Confirmada), Fin));
    }

    [Fact]
    public void Un_segundo_despues_del_fin_esta_fuera_de_ventana()
    {
        Assert.Equal(ReglaAcceso.QrFueraDeVentana, ReglaAcceso.MotivoRechazo(ReservaEn(EstadoReserva.Confirmada), Fin + UnSegundo));
    }

    [Theory]
    [InlineData(EstadoReserva.Cancelada)]
    [InlineData(EstadoReserva.Caducada)]
    [InlineData(EstadoReserva.PendientePago)]
    public void Reserva_no_confirmada_se_rechaza_aunque_este_en_ventana(EstadoReserva estado)
    {
        Assert.Equal(ReglaAcceso.QrReservaNoConfirmada, ReglaAcceso.MotivoRechazo(ReservaEn(estado), Inicio));
    }

    [Fact]
    public void Sin_reserva_el_codigo_es_desconocido()
    {
        Assert.Equal(ReglaAcceso.QrDesconocido, ReglaAcceso.MotivoRechazo(null, Inicio));
    }
}
