using SI18.Dominio.Comun;

namespace SI18.Dominio.Disponibilidad;

/// <summary>Precio de una franja según la tarifa del usuario (RF-013; contracts/modulos.md).</summary>
public static class CalculadoraPrecio
{
    /// <summary>
    /// <c>precio_hora × DuracionFranja</c> de la tarifa de <paramref name="tipo"/> y <paramref name="tarifa"/>.
    /// Los precios salen siempre de la tabla <c>tarifas</c> (R6), nunca del código.
    /// </summary>
    /// <exception cref="ErrorNegocio"><c>INSTALACION_NO_RESERVABLE</c> si ese tipo de instalación no tiene tarifa.</exception>
    public static decimal Precio(TipoInstalacion tipo, TipoTarifa tarifa, IReadOnlyList<Tarifa> tarifas)
    {
        var encontrada = tarifas.FirstOrDefault(t => t.TipoInstalacion == tipo && t.TipoTarifa == tarifa)
            ?? throw CodigosError.Error(CodigosError.InstalacionNoReservable);

        return encontrada.PrecioHora * (decimal)ValoresNegocio.DuracionFranja.TotalHours;
    }
}
