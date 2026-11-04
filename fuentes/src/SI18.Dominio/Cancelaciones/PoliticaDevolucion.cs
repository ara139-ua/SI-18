using SI18.Dominio.Comun;

namespace SI18.Dominio.Cancelaciones;

/// <summary>
/// Decide si una cancelación lleva devolución (data-model.md, «Devolucion»; RF-019; research.md R5).
/// Las 24 h se miden en tiempo real (UTC), no en horas de pared: el fin de semana del cambio de hora,
/// 25 h de pared pueden ser 24 h reales.
/// </summary>
public static class PoliticaDevolucion
{
    /// <returns>
    /// <c>true</c> siempre si el motivo es el cierre por mantenimiento; si cancela el usuario, solo cuando
    /// faltan MÁS de <see cref="ValoresNegocio.PlazoDevolucion"/> para el inicio.
    /// </returns>
    public static bool HayDevolucion(MotivoDevolucion motivo, DateTime inicioUtc, DateTime ahoraUtc) => motivo switch
    {
        MotivoDevolucion.CierreMantenimiento => true,
        MotivoDevolucion.CancelacionUsuario => inicioUtc - ahoraUtc > ValoresNegocio.PlazoDevolucion,
        _ => throw new ArgumentOutOfRangeException(nameof(motivo), motivo, null),
    };
}
