using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    /// <summary>Índice único de 000-core: una sola reserva activa por instalación y franja.</summary>
    private const string IndiceReservaActiva = "ux_reservas_activa";

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

        // R8.2: las pendientes de esa franja que ya no están vigentes siguen en ux_reservas_activa hasta que se
        // caducan. Se caducan aquí, bloqueadas, antes de insertar la nueva (data-model.md § 2.1).
        await CaducarPendientesNoVigentes(peticion.InstalacionId, peticion.InicioUtc, ahora, ct);

        var reserva = new Reserva(usuario.Id, peticion.InstalacionId, peticion.InicioUtc, tarifa, importe, ahora);
        db.Reservas.Add(reserva);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException error) when (EsFranjaOcupada(error))
        {
            // Otra petición ha ganado la franja entre la comprobación y el INSERT (R7; RF-007).
            throw CodigosError.Error(CodigosError.FranjaOcupada);
        }

        await transaccion.CommitAsync(ct);

        return ADto(reserva, instalacion, centro, ahora);
    }

    /// <summary>
    /// Paga una reserva propia con la pasarela (research.md E4). El número de tarjeta solo se pasa a
    /// <see cref="IPasarelaPago.Cobrar"/>: no se guarda ni se registra (R9).
    /// </summary>
    /// <exception cref="ErrorNegocio">
    /// En este orden (research.md E4; data-model.md § 2.2): <c>NO_ENCONTRADO</c> si no existe o es de otra persona;
    /// <c>RESERVA_CADUCADA</c> si ya estaba guardada como caducada por tiempo; <c>TRANSICION_NO_PERMITIDA</c> si está
    /// confirmada, cancelada o caducada por un pago rechazado o un cierre; <c>RESERVA_CADUCADA</c> si han pasado los
    /// 10 minutos (queda caducada); <c>PAGO_RECHAZADO</c> si la pasarela rechaza el cobro (queda caducada).
    /// </exception>
    public async Task<ReservaDto> Pagar(long id, string numeroTarjeta, CancellationToken ct)
    {
        // Se lee una vez: una reserva vigente aquí no puede caducar entre el cobro y Confirmar.
        var ahora = Ahora;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var reserva = await CargaReservas.CargarParaModificar(db, id, usuario.Id, ct)
            ?? throw CodigosError.Error(CodigosError.NoEncontrado);

        // La misma respuesta antes y después del barrido o de la caducidad al reservar (research.md E4).
        if (reserva.Estado == EstadoReserva.Caducada && CaducadaPorTiempo(reserva))
        {
            throw CodigosError.Error(CodigosError.ReservaCaducada);
        }

        // Confirmada, cancelada o caducada por un pago rechazado o un cierre: sin reintento (RF-011).
        if (reserva.Estado != EstadoReserva.PendientePago)
        {
            throw CodigosError.Error(CodigosError.TransicionNoPermitida);
        }

        if (!reserva.EstaVigente(ahora))
        {
            // R8.3: se guarda la caducidad y después se responde, sin llamar a la pasarela (RF-009).
            reserva.Caducar(ahora);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            throw CodigosError.Error(CodigosError.ReservaCaducada);
        }

        if (reserva.YaEmpezada(ahora))
        {
            // RF-009a: vigente pero con la franja ya empezada. Se caduca y se guarda sin llamar a la pasarela, como la
            // rama anterior; un segundo intento responde TRANSICION_NO_PERMITIDA (no es una caducidad por tiempo).
            reserva.Caducar(ahora);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            throw CodigosError.Error(CodigosError.FranjaYaEmpezada);
        }

        if (!await pasarela.Cobrar(reserva.Importe, numeroTarjeta, ct))
        {
            // Pago rechazado: queda caducada y el pago fallido antes de responder (RF-011).
            reserva.Caducar(ahora);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            throw CodigosError.Error(CodigosError.PagoRechazado);
        }

        reserva.Confirmar(GeneradorCodigoAcceso.Nuevo(), ahora);
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

    /// <summary>
    /// «Mis reservas»: todas las del usuario de la sesión, en cualquier estado y sin paginar (research.md E6). Orden
    /// de RF-015a con la hora del servidor: primero las que no han terminado (<c>FinUtc &gt; ahora</c>) por inicio
    /// ascendente; después las terminadas por inicio descendente; a igualdad, por <c>Id</c>. No escribe nada.
    /// </summary>
    public async Task<IReadOnlyList<ReservaDto>> Mias(CancellationToken ct)
    {
        var ahora = Ahora;
        var usuarioId = usuario.Id;
        var filas = await (
                from r in db.Reservas.AsNoTracking()
                join i in db.Instalaciones.AsNoTracking() on r.InstalacionId equals i.Id
                join c in db.Centros.AsNoTracking() on i.CentroId equals c.Id
                where r.UsuarioId == usuarioId
                select new { Reserva = r, Instalacion = i, Centro = c })
            .ToListAsync(ct);

        return filas
            .OrderBy(f => f.Reserva.FinUtc > ahora ? 0 : 1)
            .ThenBy(f => f.Reserva.FinUtc > ahora ? f.Reserva.InicioUtc.Ticks : -f.Reserva.InicioUtc.Ticks)
            .ThenBy(f => f.Reserva.Id)
            .Select(f => ADto(f.Reserva, f.Instalacion, f.Centro, ahora))
            .ToList();
    }

    /// <summary>Caduca, con bloqueo, las pendientes de pago no vigentes de una franja (R8.2; RF-006).</summary>
    private async Task CaducarPendientesNoVigentes(int instalacionId, DateTime inicioUtc, DateTime ahora, CancellationToken ct)
    {
        var limite = ahora - ValoresNegocio.TiempoBloqueoPago;
        var caducables = await db.Reservas
            .FromSql($"SELECT * FROM reservas WHERE instalacion_id = {instalacionId} AND inicio_utc = {inicioUtc} AND estado = 'pendiente_pago' AND creada_en_utc <= {limite} FOR UPDATE")
            .ToListAsync(ct);
        if (caducables.Count == 0)
        {
            return;
        }

        await CargaReservas.CargarPagos(db, caducables, ct);
        foreach (var caducable in caducables)
        {
            caducable.Caducar(ahora);
        }

        // Se guarda antes de insertar la nueva: si no, el INSERT podría llegar antes que el UPDATE y chocar con el índice.
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Una reserva guardada como caducada lo está por tiempo si su pago se resolvió cuando ya no estaba vigente
    /// (barrido, caducidad al reservar o el propio pago tardío). Un pago rechazado o un cierre la caducan antes.
    /// </summary>
    private static bool CaducadaPorTiempo(Reserva reserva) =>
        reserva.Pago!.ResueltoEnUtc >= reserva.CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago;

    /// <summary>Violación de <c>ux_reservas_activa</c> (como en <c>ReservaActivaUnicaTests</c> de 000-core).</summary>
    private static bool EsFranjaOcupada(DbUpdateException error) =>
        error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceReservaActiva };

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
        // RF-008a: la más temprana entre el fin del bloqueo y el inicio de la franja (después ya no se puede pagar, RF-009a).
        var finBloqueo = r.CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago;
        var limitePago = finBloqueo < r.InicioUtc ? finBloqueo : r.InicioUtc;
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
