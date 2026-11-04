namespace SI18.Dominio.Comun;

/// <summary>
/// Conversión entre instantes UTC y fecha/hora de pared de Madrid (research.md R5).
/// Todas las marcas de tiempo del sistema son UTC; aquí se centraliza el paso a hora local.
/// </summary>
public static class HorarioMadrid
{
    private static readonly TimeZoneInfo Zona =
        TimeZoneInfo.FindSystemTimeZoneById(ValoresNegocio.ZonaHoraria);

    /// <summary>Fecha de hoy en Madrid para el instante UTC dado.</summary>
    public static DateOnly Hoy(DateTime ahoraUtc) => ALocal(ahoraUtc).Fecha;

    /// <summary>Instante UTC (<see cref="DateTimeKind.Utc"/>) de una fecha y hora de pared de Madrid.</summary>
    public static DateTime AUtc(DateOnly fecha, TimeOnly hora)
    {
        var local = fecha.ToDateTime(hora, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zona);
    }

    /// <summary>Fecha y hora de pared de Madrid de un instante UTC.</summary>
    /// <exception cref="ArgumentException">Si <paramref name="utc"/> no es <see cref="DateTimeKind.Utc"/>.</exception>
    public static (DateOnly Fecha, TimeOnly Hora) ALocal(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Se esperaba un DateTime en UTC.", nameof(utc));
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Zona);
        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }
}
