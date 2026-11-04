using SI18.Dominio.Comun;

namespace SI18.Dominio.Reservas;

/// <summary>
/// Único pago de una reserva, por la pasarela simulada (data-model.md, «Pago»; dueño: HU-02).
/// Estados: pendiente → completado | fallido, ambos finales y sin reintento.
/// Los métodos de cambio de estado llegan en T060.
/// </summary>
public class Pago
{
    private Pago() { }

    /// <summary>Se crea pendiente, junto con la reserva.</summary>
    internal Pago(decimal importe, DateTime creadoEnUtc)
    {
        Importe = importe;
        Estado = EstadoPago.Pendiente;
        CreadoEnUtc = creadoEnUtc;
    }

    public long Id { get; private set; }

    /// <summary>Único: un solo pago por reserva.</summary>
    public long ReservaId { get; private set; }

    /// <summary>numeric(8,2), igual al importe de la reserva.</summary>
    public decimal Importe { get; private set; }

    public EstadoPago Estado { get; private set; }

    /// <summary>Fecha del pago.</summary>
    public DateTime CreadoEnUtc { get; private set; }

    /// <summary>Obligatorio si y solo si el estado no es pendiente; decide a qué mes se imputa el ingreso (spec-008).</summary>
    public DateTime? ResueltoEnUtc { get; private set; }
}
