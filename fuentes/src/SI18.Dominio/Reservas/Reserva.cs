using SI18.Dominio.Comun;

namespace SI18.Dominio.Reservas;

/// <summary>
/// Reserva de una franja de 1 h (data-model.md, «Reserva»; dueño: HU-02).
/// Solo campos: la máquina de estados (Confirmar, Caducar, Cancelar, EstaVigente) llega en T060.
/// </summary>
public class Reserva
{
    private Reserva() { }

    /// <summary>Crea la reserva en <see cref="EstadoReserva.PendientePago"/> con su pago pendiente.</summary>
    public Reserva(
        int usuarioId,
        int instalacionId,
        DateTime inicioUtc,
        TipoTarifa tipoTarifa,
        decimal importe,
        DateTime creadaEnUtc)
    {
        UsuarioId = usuarioId;
        InstalacionId = instalacionId;
        InicioUtc = inicioUtc;
        FinUtc = inicioUtc + ValoresNegocio.DuracionFranja;
        TipoTarifa = tipoTarifa;
        Importe = importe;
        Estado = EstadoReserva.PendientePago;
        CreadaEnUtc = creadaEnUtc;
        Pago = new Pago(importe, creadaEnUtc);
    }

    public long Id { get; private set; }

    /// <summary>Ciudadano o representante de club.</summary>
    public int UsuarioId { get; private set; }

    /// <summary>Instalación reservable por franjas.</summary>
    public int InstalacionId { get; private set; }

    /// <summary>En punto en hora de Madrid.</summary>
    public DateTime InicioUtc { get; private set; }

    /// <summary><see cref="InicioUtc"/> + <see cref="ValoresNegocio.DuracionFranja"/>.</summary>
    public DateTime FinUtc { get; private set; }

    /// <summary>La del usuario al reservar; queda fijada.</summary>
    public TipoTarifa TipoTarifa { get; private set; }

    /// <summary>numeric(8,2), mayor que 0: el precio mostrado y cobrado.</summary>
    public decimal Importe { get; private set; }

    public EstadoReserva Estado { get; private set; }

    /// <summary>Marca el inicio del bloqueo de <see cref="ValoresNegocio.TiempoBloqueoPago"/>.</summary>
    public DateTime CreadaEnUtc { get; private set; }

    /// <summary>varchar(32), único; nulo si y solo si está pendiente de pago o caducada (R14).</summary>
    public string? CodigoAcceso { get; private set; }

    /// <summary>No nulo si y solo si está cancelada.</summary>
    public DateTime? CanceladaEnUtc { get; private set; }

    /// <summary>Obligatorio si y solo si está cancelada.</summary>
    public MotivoCancelacion? MotivoCancelacion { get; private set; }

    /// <summary>Único pago de la reserva.</summary>
    public Pago? Pago { get; private set; }
}
