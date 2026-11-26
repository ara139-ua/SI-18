using System.Globalization;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Caso de uso de HU-06: validar el código de acceso de una reserva en la entrada de un centro y registrar el
/// intento (specs/006-validar-qr/research.md V3–V5). La decisión es de <c>ReglaAcceso</c> más la comprobación de
/// centro; la reserva no se modifica (RF-010). Dueño: Saul.
/// </summary>
public sealed class ServicioValidarAcceso(Si18DbContext db, TimeProvider reloj, IUsuarioActual usuario)
{
    /// <summary>
    /// Decide, registra (un solo <c>SaveChanges</c>) y devuelve el resultado de validar <paramref name="codigo"/>,
    /// ya sin espacios en los extremos y no vacío.
    /// </summary>
    public Task<ResultadoValidacion> Validar(string codigo, CancellationToken ct)
    {
        // Esqueleto de T003; lo completa T009.
        _ = (db, reloj, usuario, codigo, ct);
        throw new NotImplementedException();
    }

    /// <summary>Fecha de Madrid del instante, «yyyy-MM-dd» como <see cref="DateOnly"/>.</summary>
    private static DateOnly Fecha(DateTime utc) => HorarioMadrid.ALocal(utc).Fecha;

    /// <summary>Hora de pared de Madrid del instante en «HH:mm».</summary>
    private static string Hora(DateTime utc) =>
        HorarioMadrid.ALocal(utc).Hora.ToString("HH':'mm", CultureInfo.InvariantCulture);
}

/// <summary>
/// Resultado de <see cref="ServicioValidarAcceso.Validar"/>: o bien aceptada (<see cref="Aceptada"/>), o bien
/// rechazada (<see cref="Codigo"/>, <see cref="ValidacionId"/> y, con dos motivos, <see cref="Reserva"/>).
/// </summary>
public sealed record ResultadoValidacion
{
    public ValidacionAccesoDto? Aceptada { get; init; }

    /// <summary>Código del catálogo del rechazo; nulo si se aceptó.</summary>
    public string? Codigo { get; init; }

    public long ValidacionId { get; init; }

    /// <summary>Solo con <c>QR_RESERVA_NO_CONFIRMADA</c> y <c>QR_FUERA_DE_VENTANA</c>; nunca el titular.</summary>
    public ReservaValidadaDto? Reserva { get; init; }

    public static ResultadoValidacion Aceptar(ValidacionAccesoDto dto) =>
        new() { Aceptada = dto, ValidacionId = dto.ValidacionId };

    public static ResultadoValidacion Rechazar(string codigo, long validacionId, ReservaValidadaDto? reserva) =>
        new() { Codigo = codigo, ValidacionId = validacionId, Reserva = reserva };
}
