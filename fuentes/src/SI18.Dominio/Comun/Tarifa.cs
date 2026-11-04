namespace SI18.Dominio.Comun;

/// <summary>
/// Precio por hora de un tipo de instalación para un tipo de tarifa (data-model.md, «Tarifa»).
/// Precargada; es la única fuente de los precios (R6).
/// </summary>
public class Tarifa
{
    private Tarifa() { }

    public Tarifa(TipoInstalacion tipoInstalacion, TipoTarifa tipoTarifa, decimal precioHora)
    {
        TipoInstalacion = tipoInstalacion;
        TipoTarifa = tipoTarifa;
        PrecioHora = precioHora;
    }

    public int Id { get; private set; }

    /// <summary>Solo <see cref="TipoInstalacion.PistaTenis"/> o <see cref="TipoInstalacion.PistaPadel"/>.</summary>
    public TipoInstalacion TipoInstalacion { get; private set; }

    public TipoTarifa TipoTarifa { get; private set; }

    /// <summary>numeric(8,2), mayor que 0. Euros por hora.</summary>
    public decimal PrecioHora { get; private set; }
}
