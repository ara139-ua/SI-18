using SI18.Dominio.Comun;

namespace SI18.Dominio.Disponibilidad;

/// <summary>
/// Solicitud de franjas fijas de temporada de un club (data-model.md, «SolicitudFranjasFijas»; HU-05).
/// Estados: pendiente → aprobada | rechazada, ambos finales. La comprobación de conflictos al
/// aprobar (RF-022, D8) es de la spec-005.
/// </summary>
public class SolicitudFranjasFijas
{
    // TODO(T018): sustituir por CodigosError.Error(CodigosError.TransicionNoPermitida) cuando entre CodigosError.
    private const string CodigoTransicionNoPermitida = "TRANSICION_NO_PERMITIDA";
    private const string DetalleTransicionNoPermitida = "Esta reserva ya no se puede modificar en su estado actual.";

    private SolicitudFranjasFijas() { }

    public SolicitudFranjasFijas(
        int clubId,
        int instalacionId,
        int temporadaId,
        short[] diasSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateTime creadaEnUtc)
    {
        ValidarDiasSemana(diasSemana);
        ValidarHoras(horaInicio, horaFin);

        ClubId = clubId;
        InstalacionId = instalacionId;
        TemporadaId = temporadaId;
        DiasSemana = diasSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        Estado = EstadoSolicitud.Pendiente;
        CreadaEnUtc = creadaEnUtc;
    }

    public int Id { get; private set; }

    public int ClubId { get; private set; }

    /// <summary>Pista o pabellón (D2).</summary>
    public int InstalacionId { get; private set; }

    public int TemporadaId { get; private set; }

    /// <summary>1 = lunes … 7 = domingo (ISO); no vacío; sin repetidos.</summary>
    public short[] DiasSemana { get; private set; } = [];

    /// <summary>En punto, a partir de las 08:00 (<see cref="ValoresNegocio.HoraApertura"/>).</summary>
    public TimeOnly HoraInicio { get; private set; }

    /// <summary>En punto, hasta las 22:00 (<see cref="ValoresNegocio.HoraCierre"/>), posterior a <see cref="HoraInicio"/>.</summary>
    public TimeOnly HoraFin { get; private set; }

    public EstadoSolicitud Estado { get; private set; }

    public DateTime CreadaEnUtc { get; private set; }

    /// <summary>Fecha de resolución; obligatoria si y solo si el estado no es pendiente.</summary>
    public DateTime? ResueltaEnUtc { get; private set; }

    public void Aprobar(DateTime ahoraUtc) => Resolver(EstadoSolicitud.Aprobada, ahoraUtc);

    public void Rechazar(DateTime ahoraUtc) => Resolver(EstadoSolicitud.Rechazada, ahoraUtc);

    private void Resolver(EstadoSolicitud nuevoEstado, DateTime ahoraUtc)
    {
        if (Estado != EstadoSolicitud.Pendiente)
        {
            throw new ErrorNegocio(CodigoTransicionNoPermitida, DetalleTransicionNoPermitida);
        }

        Estado = nuevoEstado;
        ResueltaEnUtc = ahoraUtc;
    }

    private static void ValidarDiasSemana(short[] diasSemana)
    {
        if (diasSemana.Length == 0)
        {
            throw new ArgumentException("Hay que indicar al menos un día de la semana.", nameof(diasSemana));
        }

        if (diasSemana.Any(d => d is < 1 or > 7))
        {
            throw new ArgumentException("Los días de la semana van de 1 (lunes) a 7 (domingo).", nameof(diasSemana));
        }

        if (diasSemana.Distinct().Count() != diasSemana.Length)
        {
            throw new ArgumentException("Los días de la semana no se pueden repetir.", nameof(diasSemana));
        }
    }

    private static void ValidarHoras(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (!EsEnPunto(horaInicio) || !EsEnPunto(horaFin))
        {
            throw new ArgumentException("Las horas de inicio y fin deben ser en punto.");
        }

        if (horaInicio < ValoresNegocio.HoraApertura || horaFin > ValoresNegocio.HoraCierre)
        {
            throw new ArgumentException("El horario debe estar dentro del horario de apertura.");
        }

        if (horaFin <= horaInicio)
        {
            throw new ArgumentException("La hora de fin debe ser posterior a la de inicio.");
        }
    }

    private static bool EsEnPunto(TimeOnly hora) => hora is { Minute: 0, Second: 0, Millisecond: 0 };
}
