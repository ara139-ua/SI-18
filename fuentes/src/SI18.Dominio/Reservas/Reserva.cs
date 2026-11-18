using SI18.Dominio.Comun;

namespace SI18.Dominio.Reservas;

/// <summary>
/// Reserva de una franja de 1 h (data-model.md, «Reserva»; dueño: HU-02).
/// <code>
/// pendiente_pago ──Confirmar──► confirmada ──Cancelar──► cancelada (final)
///       └──────────Caducar────► caducada (final)
/// </code>
/// Cualquier otra transición lanza <c>TRANSICION_NO_PERMITIDA</c> (RF-015).
/// Para usar los métodos de estado hay que cargar el <see cref="Pago"/>.
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

    /// <summary>
    /// Regla de vigencia única (RF-016, R8):
    /// <c>{ ahora &lt; CreadaEnUtc + TiempoBloqueoPago, si PendientePago ; true, si Confirmada ; false, en otro caso }</c>.
    /// </summary>
    public bool EstaVigente(DateTime ahoraUtc) => Estado switch
    {
        EstadoReserva.PendientePago => ahoraUtc < CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago,
        EstadoReserva.Confirmada => true,
        _ => false,
    };

    /// <summary>Pago completado: pasa a confirmada con su código de acceso. Responsable: HU-02.</summary>
    /// <exception cref="ErrorNegocio">
    /// <c>TRANSICION_NO_PERMITIDA</c> si no está pendiente de pago; <c>RESERVA_CADUCADA</c> si ya no está vigente.
    /// </exception>
    public void Confirmar(string codigoAcceso, DateTime ahoraUtc)
    {
        ExigirEstado(EstadoReserva.PendientePago);
        if (!EstaVigente(ahoraUtc))
        {
            throw ErroresProvisionales.Error(ErroresProvisionales.ReservaCaducada);
        }

        PagoCargado().Completar(ahoraUtc);
        Estado = EstadoReserva.Confirmada;
        CodigoAcceso = codigoAcceso;
    }

    /// <summary>
    /// Pago fallido, tiempo agotado o cierre por mantenimiento: pasa a caducada y el pago a fallido.
    /// Responsables: HU-02 y HU-07 (por cierre).
    /// </summary>
    /// <exception cref="ErrorNegocio"><c>TRANSICION_NO_PERMITIDA</c> si no está pendiente de pago.</exception>
    public void Caducar(DateTime ahoraUtc)
    {
        ExigirEstado(EstadoReserva.PendientePago);

        PagoCargado().Fallar(ahoraUtc);
        Estado = EstadoReserva.Caducada;
    }

    /// <summary>
    /// Cancela una reserva confirmada. El usuario solo puede hacerlo antes del inicio; el cierre por
    /// mantenimiento, sin límite de hora. Responsables: HU-03 (usuario) y HU-07 (cierre).
    /// La devolución la decide <c>PoliticaDevolucion</c>.
    /// </summary>
    /// <exception cref="ErrorNegocio">
    /// <c>TRANSICION_NO_PERMITIDA</c> si no está confirmada; <c>RESERVA_YA_EMPEZADA</c> si cancela el
    /// usuario con <c>ahoraUtc ≥ InicioUtc</c>.
    /// </exception>
    public void Cancelar(MotivoCancelacion motivo, DateTime ahoraUtc)
    {
        ExigirEstado(EstadoReserva.Confirmada);
        if (motivo == Comun.MotivoCancelacion.Usuario && ahoraUtc >= InicioUtc)
        {
            throw ErroresProvisionales.Error(ErroresProvisionales.ReservaYaEmpezada);
        }

        Estado = EstadoReserva.Cancelada;
        CanceladaEnUtc = ahoraUtc;
        MotivoCancelacion = motivo;
    }

    private void ExigirEstado(EstadoReserva esperado)
    {
        if (Estado != esperado)
        {
            throw ErroresProvisionales.Error(ErroresProvisionales.TransicionNoPermitida);
        }
    }

    private Pago PagoCargado() =>
        Pago ?? throw new InvalidOperationException("Hay que cargar el pago de la reserva para cambiar su estado.");
}
