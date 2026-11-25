using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Contratos.Reservas;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;
using EstadoReserva = SI18.Dominio.Comun.EstadoReserva;
using EstadoReservaDto = SI18.Contratos.Reservas.EstadoReserva;

namespace SI18.Api.Modulos.Reservas;

/// <summary>
/// Casos de uso de HU-02 para el usuario de la sesión: resumen, crear, pagar y ver sus reservas
/// (specs/002-reservar-pagar/research.md E2–E6). La disponibilidad y el precio los decide siempre HU-01
/// (<see cref="IConsultaDisponibilidad"/>), la exclusión mutua la base de datos (<c>ux_reservas_activa</c>) y las
/// transiciones la máquina de estados de <see cref="Reserva"/>. Dueño: Saul.
/// </summary>
public sealed class ServicioReservar(
    Si18DbContext db,
    TimeProvider reloj,
    IConsultaDisponibilidad disponibilidad,
    IPasarelaPago pasarela,
    IUsuarioActual usuario)
{
    /// <summary>
    /// Resumen de una franja antes de reservarla (research.md E2). Hace las mismas comprobaciones que
    /// <see cref="Crear"/> y no escribe nada.
    /// </summary>
    /// <exception cref="ErrorNegocio">
    /// <c>PETICION_INVALIDA</c>, <c>NO_ENCONTRADO</c> o el motivo de RF-011 de 000-core por el que la franja no está libre.
    /// </exception>
    public async Task<ResumenReservaDto> Resumen(int instalacionId, DateTime inicioUtc, CancellationToken ct)
    {
        await ExigirLibre(instalacionId, inicioUtc, ct);
        var (instalacion, centro) = await LeerPista(instalacionId, ct);
        var importe = await disponibilidad.Precio(instalacionId, Tarifa, ct);
        var (fecha, horaInicio) = HorarioMadrid.ALocal(inicioUtc);
        var (_, horaFin) = HorarioMadrid.ALocal(inicioUtc + ValoresNegocio.DuracionFranja);

        return new ResumenReservaDto(
            instalacion.Id, instalacion.Nombre, centro.Nombre, fecha, Hora(horaInicio), Hora(horaFin), inicioUtc, importe);
    }

    /// <summary>
    /// Crea la reserva pendiente de pago con su pago pendiente (research.md E3): bloquea la franja durante
    /// <see cref="ValoresNegocio.TiempoBloqueoPago"/>.
    /// </summary>
    /// <exception cref="ErrorNegocio">
    /// Los de <see cref="Resumen"/>, <c>PRECIO_CAMBIADO</c> si el importe mostrado no es el actual y
    /// <c>FRANJA_OCUPADA</c> si otra reserva activa gana la franja.
    /// </exception>
    public async Task<ReservaDto> Crear(PeticionReserva peticion, CancellationToken ct)
    {
        var ahora = Ahora;
        var tarifa = Tarifa;

        await ExigirLibre(peticion.InstalacionId, peticion.InicioUtc, ct);
        var (instalacion, centro) = await LeerPista(peticion.InstalacionId, ct);
        var importe = await disponibilidad.Precio(peticion.InstalacionId, tarifa, ct);
        if (importe != peticion.ImporteMostrado)
        {
            throw CodigosError.Error(CodigosError.PrecioCambiado);
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var reserva = new Reserva(usuario.Id, peticion.InstalacionId, peticion.InicioUtc, tarifa, importe, ahora);
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        return ADto(reserva, instalacion, centro, ahora);
    }

    /// <summary>
    /// Paga una reserva propia con la pasarela (research.md E4). El número de tarjeta solo se pasa a
    /// <see cref="IPasarelaPago.Cobrar"/>: no se guarda ni se registra (R9).
    /// </summary>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c> si no existe o es de otra persona.</exception>
    public async Task<ReservaDto> Pagar(long id, string numeroTarjeta, CancellationToken ct)
    {
        var ahora = Ahora;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var reserva = await CargaReservas.CargarParaModificar(db, id, usuario.Id, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        if (reserva.Estado == EstadoReserva.PendientePago && reserva.EstaVigente(ahora)
            && await pasarela.Cobrar(reserva.Importe, numeroTarjeta, ct))
        {
            reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), ahora);
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var (instalacion, centro) = await LeerPista(reserva.InstalacionId, ct);
        return ADto(reserva, instalacion, centro, ahora);
    }

    /// <summary>Una reserva del usuario de la sesión, con su estado efectivo.</summary>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c> si no existe o es de otra persona (RF-013).</exception>
    public async Task<ReservaDto> Ver(long id, CancellationToken ct)
    {
        var usuarioId = usuario.Id;
        var fila = await (
                from r in db.Reservas.AsNoTracking()
                join i in db.Instalaciones.AsNoTracking() on r.InstalacionId equals i.Id
                join c in db.Centros.AsNoTracking() on i.CentroId equals c.Id
                where r.Id == id && r.UsuarioId == usuarioId
                select new { Reserva = r, Instalacion = i, Centro = c })
            .SingleOrDefaultAsync(ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        return ADto(fila.Reserva, fila.Instalacion, fila.Centro, Ahora);
    }

    /// <summary>La franja tiene que estar libre ahora, con la misma regla que la búsqueda (RF-004).</summary>
    private async Task ExigirLibre(int instalacionId, DateTime inicioUtc, CancellationToken ct)
    {
        // PETICION_INVALIDA y NO_ENCONTRADO los lanza la propia consulta y llegan tal cual al manejador de errores.
        if (await disponibilidad.MotivoNoDisponible(instalacionId, inicioUtc, ct) is { } motivo)
        {
            throw CodigosError.Error(motivo);
        }
    }

    private async Task<(Instalacion Instalacion, Centro Centro)> LeerPista(int instalacionId, CancellationToken ct)
    {
        var fila = await (
                from i in db.Instalaciones.AsNoTracking()
                join c in db.Centros.AsNoTracking() on i.CentroId equals c.Id
                where i.Id == instalacionId
                select new { Instalacion = i, Centro = c })
            .SingleOrDefaultAsync(ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        return (fila.Instalacion, fila.Centro);
    }

    /// <summary>
    /// Modelo de lectura de una reserva (data-model.md § 3). El estado es el efectivo (RF-015): una pendiente de
    /// pago que ya no está vigente se devuelve como caducada aunque el barrido aún no la haya guardado así.
    /// </summary>
    private static ReservaDto ADto(Reserva r, Instalacion i, Centro c, DateTime ahora)
    {
        var estado = r.Estado == EstadoReserva.PendientePago && !r.EstaVigente(ahora)
            ? EstadoReserva.Caducada
            : r.Estado;
        var pendiente = estado == EstadoReserva.PendientePago;
        var limitePago = r.CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago;
        var (fecha, horaInicio) = HorarioMadrid.ALocal(r.InicioUtc);
        var (_, horaFin) = HorarioMadrid.ALocal(r.FinUtc);

        return new ReservaDto(
            r.Id,
            r.InstalacionId,
            i.Nombre,
            c.Nombre,
            fecha,
            Hora(horaInicio),
            Hora(horaFin),
            r.InicioUtc,
            r.FinUtc,
            r.Importe,
            Enum.Parse<EstadoReservaDto>(estado.ToString()),
            pendiente ? limitePago : null,
            // «HH:mm» trunca los segundos: nunca promete más tiempo del que queda (RF-008a).
            pendiente ? Hora(HorarioMadrid.ALocal(limitePago).Hora) : null,
            estado == EstadoReserva.Confirmada ? r.CodigoAcceso : null);
    }

    /// <summary>La política solo deja pasar a ciudadanos y representantes, que siempre tienen tarifa.</summary>
    private TipoTarifa Tarifa => usuario.TipoTarifa ?? throw CodigosError.Error(CodigosError.RolNoPermitido);

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    private static string Hora(TimeOnly hora) => hora.ToString("HH':'mm", CultureInfo.InvariantCulture);
}
