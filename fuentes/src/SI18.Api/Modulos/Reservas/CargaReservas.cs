using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Carga de reservas ya guardadas para cambiar su estado (specs/002-reservar-pagar/research.md E5): dentro de la
/// transacción abierta, con <c>SELECT … FOR UPDATE</c> y con su <see cref="Pago"/>. Así dos operaciones sobre la
/// misma reserva (dos pagos, un pago y un cierre…) no se cruzan: la segunda espera y ve el estado ya cambiado.
/// <para>
/// El pago se lee en una consulta aparte, después de tener el bloqueo. Con <c>READ COMMITTED</c>, si
/// <c>FOR UPDATE</c> ha tenido que esperar, PostgreSQL devuelve la versión nueva de la fila bloqueada, pero las
/// tablas unidas en la misma consulta se leen con la foto de antes de esperar: un <c>Include</c> de <c>pagos</c>
/// podría traer el pago todavía pendiente de una reserva que otra transacción acaba de caducar.
/// </para>
/// </summary>
internal static class CargaReservas
{
    /// <summary>
    /// La reserva <paramref name="reservaId"/> bloqueada hasta el final de la transacción, con su pago, o <c>null</c>
    /// si no existe o, cuando se pasa <paramref name="usuarioId"/>, si no es de ese usuario.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay una transacción abierta.</exception>
    public static async Task<Reserva?> CargarParaModificar(Si18DbContext db, long reservaId, int? usuarioId, CancellationToken ct)
    {
        ExigirTransaccion(db);

        var consulta = db.Reservas.FromSql($"SELECT * FROM reservas WHERE id = {reservaId} FOR UPDATE");
        var reserva = usuarioId is { } id
            ? await consulta.SingleOrDefaultAsync(r => r.UsuarioId == id, ct)
            : await consulta.SingleOrDefaultAsync(ct);

        if (reserva is not null)
        {
            await CargarPagos(db, [reserva], ct);
        }

        return reserva;
    }

    /// <summary>
    /// Carga el pago de cada reserva ya bloqueada, con una consulta posterior al bloqueo (ver el comentario de la
    /// clase). Las reservas tienen que estar en seguimiento en <paramref name="db"/>.
    /// </summary>
    public static async Task CargarPagos(Si18DbContext db, IReadOnlyCollection<Reserva> reservas, CancellationToken ct)
    {
        if (reservas.Count == 0)
        {
            return;
        }

        var ids = reservas.Select(r => r.Id).ToList();
        await db.Pagos.Where(p => ids.Contains(p.ReservaId)).LoadAsync(ct);
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
