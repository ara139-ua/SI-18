using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;

namespace SI18.Dominio.Disponibilidad;

/// <summary>
/// Regla única de disponibilidad de una franja (RF-011; data-model.md, «Franja»). La usan la búsqueda
/// (HU-01) y la reserva (HU-02), así que las dos rechazan exactamente lo mismo. Función pura: los datos
/// llegan ya cargados.
/// </summary>
public static class ReglaDisponibilidad
{
    /// <summary>
    /// El código del primer motivo por el que <paramref name="franja"/> no se puede reservar, o <c>null</c>
    /// si está libre. Orden:
    /// <list type="number">
    /// <item><c>FUERA_DE_ANTELACION</c>: la fecha no está entre hoy y hoy + 7 días (fecha de Madrid);</item>
    /// <item><c>FRANJA_YA_EMPEZADA</c>: <c>ahoraUtc ≥ InicioUtc</c>;</item>
    /// <item><c>INSTALACION_NO_RESERVABLE</c>;</item>
    /// <item><c>INSTALACION_CERRADA</c>: un cierre de esa instalación se solapa con la franja;</item>
    /// <item><c>FRANJA_BLOQUEADA_CLUB</c>: una franja fija aprobada de esa instalación empieza a la misma hora;</item>
    /// <item><c>FRANJA_OCUPADA</c>: hay una reserva confirmada o pendiente de pago vigente a esa hora.</item>
    /// </list>
    /// </summary>
    /// <param name="cierres">Cierres de mantenimiento; se ignoran los de otras instalaciones.</param>
    /// <param name="bloqueosClub">Franjas fijas aprobadas de clubes; se ignoran las de otras instalaciones.</param>
    /// <param name="iniciosConReservaActiva">Inicios UTC con reserva activa en la instalación de la franja.</param>
    public static string? MotivoNoDisponible(
        Franja franja,
        DateTime ahoraUtc,
        bool reservablePorFranjas,
        IReadOnlyList<CierreMantenimiento> cierres,
        IReadOnlyList<Franja> bloqueosClub,
        IReadOnlyList<DateTime> iniciosConReservaActiva)
    {
        var hoy = HorarioMadrid.Hoy(ahoraUtc);
        if (franja.Fecha < hoy || franja.Fecha > hoy.AddDays(ValoresNegocio.AntelacionMaximaDias))
        {
            return CodigosError.FueraDeAntelacion;
        }

        if (ahoraUtc >= franja.InicioUtc)
        {
            return CodigosError.FranjaYaEmpezada;
        }

        if (!reservablePorFranjas)
        {
            return CodigosError.InstalacionNoReservable;
        }

        if (cierres.Any(c => c.InstalacionId == franja.InstalacionId && c.Afecta(franja.InicioUtc, franja.FinUtc)))
        {
            return CodigosError.InstalacionCerrada;
        }

        if (bloqueosClub.Any(b => b.InstalacionId == franja.InstalacionId && b.InicioUtc == franja.InicioUtc))
        {
            return CodigosError.FranjaBloqueadaClub;
        }

        if (iniciosConReservaActiva.Contains(franja.InicioUtc))
        {
            return CodigosError.FranjaOcupada;
        }

        return null;
    }
}
