using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Cancelaciones;
using SI18.Api.Modulos.Reservas;
using SI18.Contratos.Disponibilidad;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;
using TipoInstalacionDto = SI18.Contratos.Comun.TipoInstalacion;

namespace SI18.Api.Modulos.Disponibilidad;

/// <summary>
/// Disponibilidad y precio de las franjas (spec-001 HU-01; research.md D3 y D4). Compone las reglas puras de
/// 000-core (<see cref="CalculadoraFranjas"/>, <see cref="ReglaDisponibilidad"/>, <see cref="CalculadoraPrecio"/>)
/// y es la única fuente de «¿está libre y cuánto cuesta?»: la búsqueda y <see cref="IConsultaDisponibilidad"/>
/// (que usa HU-02) pasan por el mismo <see cref="EvaluarPista"/>, así que no pueden divergir (RF-013).
/// Dueño: Artur.
/// </summary>
public sealed class ServicioDisponibilidad(
    Si18DbContext db,
    TimeProvider reloj,
    IConsultaReservas reservas,
    IConsultaCierres cierres,
    IConsultaFranjasFijas franjasFijas) : IConsultaDisponibilidad
{
    /// <summary>
    /// Franjas libres de un deporte en un centro y una fecha de Madrid, con el precio de <paramref name="tarifa"/>
    /// (spec-001 RF-005–RF-012). Valida en el orden de research.md D2: la fecha antes que la existencia del
    /// deporte y del centro.
    /// </summary>
    /// <exception cref="ErrorNegocio"><c>FUERA_DE_ANTELACION</c> o <c>NO_ENCONTRADO</c>.</exception>
    public async Task<ResultadoBusquedaDto> Buscar(int deporteId, int centroId, DateOnly fecha, TipoTarifa tarifa, CancellationToken ct)
    {
        var hoy = HorarioMadrid.Hoy(Ahora);
        if (fecha < hoy || fecha > hoy.AddDays(ValoresNegocio.AntelacionMaximaDias))
        {
            throw CodigosError.Error(CodigosError.FueraDeAntelacion);
        }

        if (!await db.Deportes.AnyAsync(d => d.Id == deporteId, ct) || !await db.Centros.AnyAsync(c => c.Id == centroId, ct))
        {
            throw CodigosError.Error(CodigosError.NoEncontrado);
        }

        var candidatas = await db.Instalaciones.AsNoTracking()
            .Where(i => i.CentroId == centroId && i.ReservablePorFranjas && i.Deportes.Any(d => d.Id == deporteId))
            .OrderBy(i => i.Nombre)
            .ToListAsync(ct);

        var tarifas = await db.Tarifas.AsNoTracking().ToListAsync(ct);
        var pistas = new List<PistaLibreDto>();

        foreach (var pista in candidatas)
        {
            var libres = (await EvaluarPista(pista, fecha, ct)).Where(e => e.Motivo is null).Select(e => e.Franja).ToList();
            if (libres.Count == 0)
            {
                continue;
            }

            var precio = CalculadoraPrecio.Precio(pista.Tipo, tarifa, tarifas);
            pistas.Add(new PistaLibreDto(
                pista.Id,
                pista.Nombre,
                Enum.Parse<TipoInstalacionDto>(pista.Tipo.ToString()),
                libres.Select(f => new FranjaLibreDto(f.InicioUtc, f.FinUtc, HoraDePared(f.InicioUtc), HoraDePared(f.FinUtc), precio)).ToList()));
        }

        return new ResultadoBusquedaDto(fecha, centroId, deporteId, candidatas.Count, pistas);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Solo devuelve uno de los seis motivos de RF-011 de 000-core, o <c>null</c>. Las peticiones mal formadas
    /// lanzan <see cref="ErrorNegocio"/> (contracts/modulos.md de spec-001).
    /// </remarks>
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c> si la instalación no existe; <c>PETICION_INVALIDA</c> si
    /// <paramref name="inicioUtc"/> no es el inicio de una franja.</exception>
    public async Task<string?> MotivoNoDisponible(int instalacionId, DateTime inicioUtc, CancellationToken ct)
    {
        var pista = await BuscarInstalacion(instalacionId, ct);
        var (fecha, _) = HorarioMadrid.ALocal(inicioUtc);

        var evaluadas = await EvaluarPista(pista, fecha, ct);
        var franja = evaluadas.Where(e => e.Franja.InicioUtc == inicioUtc).ToList();

        return franja.Count == 1
            ? franja[0].Motivo
            : throw CodigosError.Error(CodigosError.PeticionInvalida);
    }

    /// <inheritdoc />
    /// <exception cref="ErrorNegocio"><c>NO_ENCONTRADO</c> o <c>INSTALACION_NO_RESERVABLE</c>.</exception>
    public async Task<decimal> Precio(int instalacionId, TipoTarifa tarifa, CancellationToken ct)
    {
        var pista = await BuscarInstalacion(instalacionId, ct);
        var tarifas = await db.Tarifas.AsNoTracking().ToListAsync(ct);
        return CalculadoraPrecio.Precio(pista.Tipo, tarifa, tarifas);
    }

    /// <summary>
    /// Único sitio que decide qué franjas de una pista están libres en una fecha de Madrid: las 14 franjas del día
    /// con el motivo de <see cref="ReglaDisponibilidad"/> (<c>null</c> = libre), evaluado ahora.
    /// </summary>
    private async Task<IReadOnlyList<(Franja Franja, string? Motivo)>> EvaluarPista(Instalacion pista, DateOnly fecha, CancellationToken ct)
    {
        var desdeUtc = HorarioMadrid.AUtc(fecha, TimeOnly.MinValue);
        var hastaUtc = HorarioMadrid.AUtc(fecha.AddDays(1), TimeOnly.MinValue);

        var cierresDelDia = await cierres.Cierres(pista.Id, desdeUtc, hastaUtc, ct);
        var bloqueos = await franjasFijas.FranjasBloqueadas(pista.Id, fecha, fecha, ct);
        var ocupadas = (await reservas.Activas(pista.Id, desdeUtc, hastaUtc, ct)).Select(r => r.InicioUtc).ToList();
        var ahora = Ahora;

        return CalculadoraFranjas.FranjasDelDia(pista.Id, fecha)
            .Select(f => (f, ReglaDisponibilidad.MotivoNoDisponible(f, ahora, pista.ReservablePorFranjas, cierresDelDia, bloqueos, ocupadas)))
            .ToList();
    }

    private async Task<Instalacion> BuscarInstalacion(int instalacionId, CancellationToken ct) =>
        await db.Instalaciones.AsNoTracking().SingleOrDefaultAsync(i => i.Id == instalacionId, ct)
        ?? throw CodigosError.Error(CodigosError.NoEncontrado);

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    private static string HoraDePared(DateTime utc) =>
        HorarioMadrid.ALocal(utc).Hora.ToString("HH':'mm", CultureInfo.InvariantCulture);
}
