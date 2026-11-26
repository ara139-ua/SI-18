using System.Globalization;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;

namespace SI18.Cliente.Reservas;

/// <summary>
/// Textos en español de las pantallas de Reservas (specs/002-reservar-pagar/research.md E10, E11). Los días de la
/// semana van fijados aquí, como los separadores de <c>FormatoEs</c>, para no depender de los datos de idioma del
/// navegador.
/// </summary>
public static class TextosReservas
{
    private static readonly string[] DiasSemana = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    /// <summary>Fecha con formato <c>dd/MM/yyyy</c>: «26/12/2026».</summary>
    public static string Fecha(DateOnly fecha) => fecha.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture);

    /// <summary>Día de la semana y fecha: «sábado 26/12/2026».</summary>
    public static string DiaYFecha(DateOnly fecha) => $"{DiasSemana[(int)fecha.DayOfWeek]} {Fecha(fecha)}";

    /// <summary>Horas de la franja: «de 18:00 a 19:00».</summary>
    public static string Horas(string horaInicio, string horaFin) => $"de {horaInicio} a {horaFin}";

    /// <summary>Estado escrito, para que nunca se entienda solo por el color (RF-021).</summary>
    public static string Estado(EstadoReserva estado) => estado switch
    {
        EstadoReserva.PendientePago => "Pendiente de pago",
        EstadoReserva.Confirmada => "Confirmada",
        EstadoReserva.Caducada => "Caducada",
        EstadoReserva.Cancelada => "Cancelada",
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null),
    };

    /// <summary>Descripción de una reserva para el QR y los nombres accesibles: «Pista de pádel 1, sábado 26/12/2026, de 18:00 a 19:00».</summary>
    public static string Descripcion(ReservaDto reserva) =>
        $"{reserva.Pista}, {DiaYFecha(reserva.Fecha)}, {Horas(reserva.HoraInicio, reserva.HoraFin)}";

    /// <summary>
    /// Aviso de validación anterior (RF-006a): «hoy a las HH:mm» si la anterior es del mismo día de Madrid que
    /// <paramref name="ahoraUtc"/>, y la fecha y la hora si no. Usa <see cref="FormatoEs.FechaHora"/>, que convierte a
    /// Madrid con la misma zona que <c>HorarioMadrid</c> (lo comprueba <c>ZonaHorariaFormatoEsTests</c>).
    /// </summary>
    public static string AvisoValidacionAnterior(DateTime anteriorUtc, DateTime ahoraUtc)
    {
        // «dd/MM/yyyy HH:mm»: los 10 primeros caracteres son la fecha y los 5 últimos, la hora.
        var anterior = FormatoEs.FechaHora(anteriorUtc);
        var fecha = anterior[..10];
        var hora = anterior[^5..];

        return fecha == FormatoEs.FechaHora(ahoraUtc)[..10]
            ? $"Este código ya se validó hoy a las {hora}."
            : $"Este código se validó por última vez el {fecha} a las {hora}.";
    }
}
