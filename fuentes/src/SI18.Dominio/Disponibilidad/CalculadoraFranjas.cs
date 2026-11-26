using SI18.Dominio.Comun;

namespace SI18.Dominio.Disponibilidad;

/// <summary>Genera las franjas de un día (data-model.md, «Franja»; contracts/modulos.md).</summary>
public static class CalculadoraFranjas
{
    /// <summary>
    /// Las franjas de <paramref name="fecha"/> (hora de Madrid) desde <see cref="ValoresNegocio.HoraApertura"/>
    /// hasta <see cref="ValoresNegocio.HoraCierre"/> − <see cref="ValoresNegocio.DuracionFranja"/>, en orden:
    /// 14 franjas, de 08:00 a 21:00. Cada una dura <see cref="ValoresNegocio.DuracionFranja"/> de tiempo real,
    /// también los días de cambio de hora.
    /// </summary>
    public static IReadOnlyList<Franja> FranjasDelDia(int instalacionId, DateOnly fecha)
    {
        var franjas = new List<Franja>();
        var ultimaHoraInicio = ValoresNegocio.HoraCierre.Add(-ValoresNegocio.DuracionFranja);

        for (var hora = ValoresNegocio.HoraApertura; hora <= ultimaHoraInicio; hora = hora.Add(ValoresNegocio.DuracionFranja))
        {
            var inicioUtc = HorarioMadrid.AUtc(fecha, hora);
            franjas.Add(new Franja(instalacionId, fecha, hora, inicioUtc, inicioUtc + ValoresNegocio.DuracionFranja));
        }

        return franjas;
    }
}
