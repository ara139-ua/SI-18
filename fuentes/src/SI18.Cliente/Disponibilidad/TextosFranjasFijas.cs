using SI18.Contratos.Disponibilidad;

namespace SI18.Cliente.Disponibilidad;

/// <summary>Textos de las pantallas de franjas fijas (spec-005), siempre en español y sin depender del color.</summary>
public static class TextosFranjasFijas
{
    public static readonly string[] NombresDias = ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    /// <summary>«martes y jueves», «lunes, miércoles y viernes», «lunes».</summary>
    public static string Dias(IEnumerable<int> dias)
    {
        var nombres = dias.Order().Select(d => NombresDias[d - 1]).ToList();
        return nombres.Count == 1 ? nombres[0] : $"{string.Join(", ", nombres[..^1])} y {nombres[^1]}";
    }

    public static string Estado(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Pendiente => "Pendiente",
        EstadoSolicitud.Aprobada => "Aprobada",
        _ => "Rechazada",
    };

    /// <summary>«Polideportivo Norte · Pabellón — martes y jueves de 19:00 a 21:00 — temporada 2026-2027».</summary>
    public static string Resumen(SolicitudFranjasFijasDto s) =>
        $"{s.CentroNombre} · {s.InstalacionNombre} — {Dias(s.DiasSemana)} de {s.HoraInicio} a {s.HoraFin} — temporada {s.TemporadaNombre}";
}
