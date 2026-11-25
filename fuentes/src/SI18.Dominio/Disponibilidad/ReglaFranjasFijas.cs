using SI18.Dominio.Comun;

namespace SI18.Dominio.Disponibilidad;

/// <summary>
/// Regla única de las franjas fijas de temporada (spec-005; research.md F3). La usan el solape entre solicitudes,
/// los conflictos con reservas y las franjas que bloquea una solicitud aprobada, así que no puede haber tres
/// versiones. Función pura: sin base de datos ni reloj.
/// </summary>
public static class ReglaFranjasFijas
{
    /// <summary>
    /// Dos solicitudes se solapan si son de la misma instalación y temporada, comparten algún día y sus horarios se
    /// cruzan como intervalos semiabiertos [inicio, fin): 19–20 y 20–21 no se solapan; 19–21 y 20–22, sí.
    /// </summary>
    public static bool SeSolapan(SolicitudFranjasFijas a, SolicitudFranjasFijas b)
    {
        if (a.Id != 0 && a.Id == b.Id)
        {
            return false;
        }

        return a.InstalacionId == b.InstalacionId
            && a.TemporadaId == b.TemporadaId
            && a.DiasSemana.Intersect(b.DiasSemana).Any()
            && a.HoraInicio < b.HoraFin
            && b.HoraInicio < a.HoraFin;
    }

    /// <summary>Si la solicitud cubre esa fecha y hora de Madrid: su día de la semana y HoraInicio ≤ hora &lt; HoraFin.</summary>
    public static bool Cubre(SolicitudFranjasFijas s, DateOnly fecha, TimeOnly hora) =>
        s.DiasSemana.Contains(DiaIso(fecha)) && s.HoraInicio <= hora && hora < s.HoraFin;

    /// <summary>
    /// Franjas de 1 h que la solicitud ocupa en las fechas de [<paramref name="desde"/>, <paramref name="hasta"/>]
    /// que caen dentro de su temporada, ordenadas por inicio. Las horas son de Madrid, también en el cambio de hora.
    /// </summary>
    public static IReadOnlyList<Franja> Expandir(SolicitudFranjasFijas s, Temporada temporada, DateOnly desde, DateOnly hasta)
    {
        var primera = desde > temporada.FechaInicio ? desde : temporada.FechaInicio;
        var ultima = hasta < temporada.FechaFin ? hasta : temporada.FechaFin;
        var franjas = new List<Franja>();

        for (var fecha = primera; fecha <= ultima; fecha = fecha.AddDays(1))
        {
            if (!s.DiasSemana.Contains(DiaIso(fecha)))
            {
                continue;
            }

            franjas.AddRange(CalculadoraFranjas.FranjasDelDia(s.InstalacionId, fecha).Where(f => Cubre(s, fecha, f.HoraInicio)));
        }

        return franjas;
    }

    /// <summary>Día ISO de la semana: lunes = 1 … domingo = 7.</summary>
    public static short DiaIso(DateOnly fecha) => (short)(fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek);
}
