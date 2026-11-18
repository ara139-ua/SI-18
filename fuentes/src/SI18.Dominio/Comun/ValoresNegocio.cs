namespace SI18.Dominio.Comun;

/// <summary>
/// Cifras de negocio de SI-18, escritas una sola vez (research.md R6; constitución, principio III).
/// Cada constante corresponde a una fila de la tabla «Valores de negocio» de specs/000-core/spec.md.
/// Los precios NO están aquí: viven en la tabla <c>tarifas</c>.
/// </summary>
public static class ValoresNegocio
{
    /// <summary>«Duración de una franja»: 1 hora, en punto.</summary>
    public static readonly TimeSpan DuracionFranja = TimeSpan.FromHours(1);

    /// <summary>«Horario de apertura de los centros»: abre a las 08:00 todos los días.</summary>
    public static readonly TimeOnly HoraApertura = new(8, 0);

    /// <summary>«Horario de apertura de los centros»: cierra a las 22:00 (última franja 21:00–22:00).</summary>
    public static readonly TimeOnly HoraCierre = new(22, 0);

    /// <summary>«Antelación máxima de reserva»: entre hoy y hoy + 7 días naturales, ambos incluidos.</summary>
    public const int AntelacionMaximaDias = 7;

    /// <summary>«Tiempo de bloqueo mientras se paga»: 10 minutos desde que se crea la reserva.</summary>
    public static readonly TimeSpan TiempoBloqueoPago = TimeSpan.FromMinutes(10);

    /// <summary>«Plazo para devolución al cancelar»: íntegra si faltan más de 24 h para el inicio.</summary>
    public static readonly TimeSpan PlazoDevolucion = TimeSpan.FromHours(24);

    /// <summary>«Ventana de validez del QR»: desde 15 minutos antes del inicio hasta el fin.</summary>
    public static readonly TimeSpan MargenQrAntesDelInicio = TimeSpan.FromMinutes(15);

    /// <summary>«Duración de la temporada»: empieza en septiembre.</summary>
    public const int MesInicioTemporada = 9;

    /// <summary>«Duración de la temporada»: termina en junio.</summary>
    public const int MesFinTemporada = 6;

    /// <summary>«Zona horaria»: Europe/Madrid (las fechas se almacenan en UTC).</summary>
    public const string ZonaHoraria = "Europe/Madrid";
}
