using System.Globalization;
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
}
