using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Carga de una reserva ya guardada para cambiar su estado (specs/002-reservar-pagar/research.md E5): dentro de la
/// transacción abierta, con <c>SELECT … FOR UPDATE</c> y con su <see cref="Pago"/>. Así dos operaciones sobre la
/// misma reserva (dos pagos, un pago y un cierre…) no se cruzan: la segunda espera y ve el estado ya cambiado.
/// </summary>
internal static class CargaReservas
{
    /// <summary>
    /// La reserva <paramref name="reservaId"/> bloqueada hasta el final de la transacción, o <c>null</c> si no existe
    /// o, cuando se pasa <paramref name="usuarioId"/>, si no es de ese usuario.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay una transacción abierta.</exception>
    public static async Task<Reserva?> CargarParaModificar(Si18DbContext db, long reservaId, int? usuarioId, CancellationToken ct)
    {
        ExigirTransaccion(db);

        var consulta = db.Reservas
            .FromSql($"SELECT * FROM reservas WHERE id = {reservaId} FOR UPDATE")
            .Include(r => r.Pago);

        return usuarioId is { } id
            ? await consulta.SingleOrDefaultAsync(r => r.UsuarioId == id, ct)
            : await consulta.SingleOrDefaultAsync(ct);
    }

    /// <summary>Sin transacción, <c>FOR UPDATE</c> no bloquea nada: es un error de programación.</summary>
    /// <exception cref="InvalidOperationException">Si no hay una transacción abierta.</exception>
    public static void ExigirTransaccion(Si18DbContext db)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Hay que abrir la transacción antes de cargar una reserva para modificarla.");
        }
    }
}
