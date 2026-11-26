using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

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
    public async Task<ResultadoValidacion> Validar(string codigo, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var fila = await (
                from r in db.Reservas.AsNoTracking()
                join i in db.Instalaciones.AsNoTracking() on r.InstalacionId equals i.Id
                join c in db.Centros.AsNoTracking() on i.CentroId equals c.Id
                join u in db.Usuarios.AsNoTracking() on r.UsuarioId equals u.Id
                where r.CodigoAcceso == codigo
                select new { Reserva = r, Instalacion = i.Nombre, CentroId = c.Id, Centro = c.Nombre, Titular = u.Nombre })
            .SingleOrDefaultAsync(ct);

        // Orden de RF-005: desconocido → otro centro → no confirmada → fuera de ventana. La comprobación de
        // centro es de este módulo (ReglaAcceso no la incluye) y no usa PermisosCentro.
        var motivo = fila is null
            ? CodigosError.QrDesconocido
            : fila.CentroId != usuario.CentroId
                ? CodigosError.QrOtroCentro
                : ReglaAcceso.MotivoRechazo(fila.Reserva, ahora);

        // RF-006a: la aceptada más reciente de esta reserva, antes de insertar la de ahora.
        DateTime? anterior = null;
        if (motivo is null)
        {
            var reservaId = fila!.Reserva.Id;
            anterior = await db.ValidacionesAcceso.AsNoTracking()
                .Where(v => v.ReservaId == reservaId && v.Aceptada)
                .MaxAsync(v => (DateTime?)v.FechaUtc, ct);
        }

        // RF-008 y RF-009: una fila por intento y un solo SaveChanges; si falla, no hay resultado.
        var validacion = new ValidacionAcceso(fila?.Reserva.Id, codigo, motivo is null, motivo, ahora, usuario.Id);
        db.ValidacionesAcceso.Add(validacion);
        await db.SaveChangesAsync(ct);

        if (motivo is not null)
        {
            return ResultadoValidacion.Rechazar(motivo, validacion.Id, null);
        }

        var reserva = fila!.Reserva;
        return ResultadoValidacion.Aceptar(new ValidacionAccesoDto(
            validacion.Id,
            fila.Titular,
            fila.Instalacion,
            fila.Centro,
            Fecha(reserva.InicioUtc),
            Hora(reserva.InicioUtc),
            Hora(reserva.FinUtc),
            anterior,
            anterior is null ? null : Hora(anterior.Value)));
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
