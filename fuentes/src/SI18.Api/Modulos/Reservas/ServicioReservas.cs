using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Implementación de <see cref="IServicioReservas"/> para Cancelaciones: HU-03 y HU-07 (specs/002-reservar-pagar
/// research.md E8; contracts/modulos.md; RF-019a). Así solo Reservas escribe en <c>reservas</c> y <c>pagos</c>.
/// <para>
/// <b>La transacción es de quien llama</b>: los dos métodos exigen que ya esté abierta en el
/// <see cref="Si18DbContext"/> de la petición (si no, <see cref="InvalidOperationException"/>), cargan las reservas
/// con <c>FOR UPDATE</c> y cambian su estado, pero no llaman a <c>SaveChanges</c> ni confirman nada. Quien llama
/// guarda su devolución y su aviso y hace <c>SaveChanges</c> y <c>Commit</c>: todo o nada.
/// </para>
/// Dueño: Saul.
/// </summary>
public sealed class ServicioReservas(Si18DbContext db, TimeProvider reloj) : IServicioReservas
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Si no hay una transacción abierta.</exception>
    /// <exception cref="ErrorNegocio">
    /// <c>NO_ENCONTRADO</c> si no existe o no es de <paramref name="usuarioId"/>; <c>TRANSICION_NO_PERMITIDA</c> si
    /// no está confirmada; <c>RESERVA_YA_EMPEZADA</c> si ya ha empezado (los dos últimos, de la entidad y en ese orden).
    /// </exception>
    public async Task<ReservaAfectada> CancelarPorUsuario(long reservaId, int usuarioId, CancellationToken ct)
    {
        var reserva = await CargaReservas.CargarParaModificar(db, reservaId, usuarioId, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        // El pago no cambia (sigue completado): la devolución es de HU-03.
        reserva.Cancelar(MotivoCancelacion.Usuario, Ahora);

        return Afectada(reserva);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Afecta a las reservas de la instalación guardadas como <c>confirmada</c> o <c>pendiente_pago</c> cuyo
    /// <c>[inicio, fin)</c> se solapa con <c>[inicioUtc, finUtc)</c> (data-model.md § 2.5): las confirmadas se
    /// cancelan por cierre (sin límite de hora) y las pendientes, vigentes o no, se caducan (pago → fallido).
    /// Cada lista va ordenada por inicio.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Si no hay una transacción abierta.</exception>
    public async Task<ResultadoCierre> AplicarCierre(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct)
    {
        CargaReservas.ExigirTransaccion(db);
        var ahora = Ahora;

        var afectadas = await db.Reservas
            .FromSql($"SELECT * FROM reservas WHERE instalacion_id = {instalacionId} AND estado IN ('confirmada', 'pendiente_pago') AND inicio_utc < {finUtc} AND fin_utc > {inicioUtc} FOR UPDATE")
            .ToListAsync(ct);
        await CargaReservas.CargarPagos(db, afectadas, ct);

        var canceladas = new List<ReservaAfectada>();
        var caducadas = new List<ReservaAfectada>();
        foreach (var reserva in afectadas.OrderBy(r => r.InicioUtc))
        {
            if (reserva.Estado == EstadoReserva.Confirmada)
            {
                reserva.Cancelar(MotivoCancelacion.CierreMantenimiento, ahora);
                canceladas.Add(Afectada(reserva));
            }
            else
            {
                reserva.Caducar(ahora);
                caducadas.Add(Afectada(reserva));
            }
        }

        return new ResultadoCierre(canceladas, caducadas);
    }

    private static ReservaAfectada Afectada(Reserva r) => new(r.Id, r.UsuarioId, r.InicioUtc, r.Pago!.Id, r.Pago.Importe);

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
}
