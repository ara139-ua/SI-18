using SI18.Dominio.Comun;

namespace SI18.Dominio.Cancelaciones;

/// <summary>
/// Devolución de un pago completado (data-model.md, «Devolucion»; dueños: HU-03 y HU-07).
/// Siempre íntegra y siempre completada (RF-021), así que no tiene estado.
/// </summary>
public class Devolucion
{
    private Devolucion() { }

    public Devolucion(long pagoId, decimal importe, MotivoDevolucion motivo, DateTime fechaUtc)
    {
        PagoId = pagoId;
        Importe = importe;
        Motivo = motivo;
        FechaUtc = fechaUtc;
    }

    public long Id { get; private set; }

    /// <summary>Pago completado; único (una devolución por pago).</summary>
    public long PagoId { get; private set; }

    /// <summary>numeric(8,2), igual al importe del pago (siempre íntegra).</summary>
    public decimal Importe { get; private set; }

    public MotivoDevolucion Motivo { get; private set; }

    public DateTime FechaUtc { get; private set; }
}
