# Contratos entre módulos (interfaces públicas)

**Ámbito**: las únicas vías por las que un módulo usa las tablas de otro (constitución, «Acuerdos de
colaboración»: «los demás usan su interfaz pública»). Las firmas son C# y se definen en
`SI18.Api/Modulos/<Modulo>/I*.cs`, salvo las de dominio puro, que van en `SI18.Dominio`. Cambiar una
firma exige un PR revisado por el dueño del módulo y por quien la consume.

Todas las operaciones de escritura reciben el `Si18DbContext` de la transacción en curso a través de
la inyección de dependencias con ámbito por petición. Así, el cambio de estado y el aviso se guardan
juntos (R18).

## Comun (todo el grupo)

```csharp
// SI18.Dominio/Comun
public static class ValoresNegocio { /* constantes de R6 */ }
public static class HorarioMadrid
{
    DateOnly Hoy(DateTime ahoraUtc);
    DateTime AUtc(DateOnly fecha, TimeOnly hora);     // hora de pared de Madrid → UTC
    (DateOnly Fecha, TimeOnly Hora) ALocal(DateTime utc);
}
public sealed class ErrorNegocio(string codigo, string mensaje) : Exception;
public static class CodigosError { /* ver contracts/errores.md */ }
public static class CalculadoraTarifa { TipoTarifa Para(Rol rol, bool empadronado); }
public static class PermisosCentro { void ExigirMismoCentro(int? centroPersonal, int centroInstalacion); }

// SI18.Api/Modulos/Comun
public interface IUsuarioActual
{
    int Id { get; } Rol Rol { get; } TipoTarifa? TipoTarifa { get; }
    int? CentroId { get; } int? ClubId { get; }
}
public interface IServicioAvisos
{
    void Avisar(int usuarioId, string texto);          // se guarda con el SaveChanges del llamador
}
```

## Disponibilidad (HU-01, HU-05) → lo consumen Reservas (HU-02) y CursosInformes (HU-08)

```csharp
// SI18.Dominio/Disponibilidad
public sealed record Franja(int InstalacionId, DateOnly Fecha, TimeOnly HoraInicio, DateTime InicioUtc, DateTime FinUtc);
public static class CalculadoraFranjas { IReadOnlyList<Franja> FranjasDelDia(int instalacionId, DateOnly fecha); }
public static class CalculadoraPrecio { decimal Precio(TipoInstalacion tipo, TipoTarifa tarifa, IReadOnlyList<Tarifa> tarifas); }

// SI18.Api/Modulos/Disponibilidad
public interface IConsultaDisponibilidad
{
    // Misma regla para buscar (HU-01) y para reservar (HU-02) — RF-013.
    // Devuelve null si la franja está libre o el CodigoError del primer motivo que la descarta.
    Task<string?> MotivoNoDisponible(int instalacionId, DateTime inicioUtc, CancellationToken ct);
    Task<decimal> Precio(int instalacionId, TipoTarifa tarifa, CancellationToken ct);
}
public interface IConsultaFranjasFijas
{
    // Para HU-01 (disponibilidad) y HU-08 (horas ocupadas por clubes); excluye periodos de cierre.
    Task<IReadOnlyList<Franja>> FranjasBloqueadas(int instalacionId, DateOnly desde, DateOnly hasta, CancellationToken ct);
    // Para HU-07: representantes de los clubes con franjas fijas aprobadas suspendidas por un cierre.
    Task<IReadOnlyList<int>> RepresentantesAfectados(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct);
}
```

## Reservas (HU-02, HU-06) → lo consumen Disponibilidad, Cancelaciones y CursosInformes

```csharp
// SI18.Dominio/Reservas
public interface IPasarelaPago
{
    Task<bool> Cobrar(decimal importe, string numeroTarjetaFicticia, CancellationToken ct);  // R9
    Task Devolver(long pagoId, decimal importe, CancellationToken ct);                       // siempre completa
}
public static class ReglaAcceso { string? MotivoRechazo(Reserva? r, DateTime ahoraUtc); }   // RF-018

// SI18.Api/Modulos/Reservas
public interface IConsultaReservas
{
    // Reservas activas (pendiente vigente o confirmada) de una instalación en un rango [desdeUtc, hastaUtc).
    Task<IReadOnlyList<ReservaActiva>> Activas(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
}
public interface IServicioReservas
{
    // HU-03: comprueba titularidad (si no es suya → NO_ENCONTRADO) y que no ha empezado.
    Task<ReservaAfectada> CancelarPorUsuario(long reservaId, int usuarioId, CancellationToken ct);
    // HU-07: cancela las confirmadas y caduca las pendientes (pago → fallido) que se solapan con el cierre.
    Task<ResultadoCierre> AplicarCierre(int instalacionId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct);
}
public sealed record ReservaActiva(long Id, int UsuarioId, DateTime InicioUtc, EstadoReserva Estado);
// PagoId e Importe permiten a Cancelaciones crear la Devolucion sin leer la tabla pagos.
public sealed record ReservaAfectada(long ReservaId, int UsuarioId, DateTime InicioUtc, long PagoId, decimal Importe);
public sealed record ResultadoCierre(IReadOnlyList<ReservaAfectada> Canceladas, IReadOnlyList<ReservaAfectada> Caducadas);
```

## Cancelaciones (HU-03, HU-07) → lo consumen Disponibilidad y CursosInformes

```csharp
// SI18.Dominio/Cancelaciones
public static class PoliticaDevolucion { bool HayDevolucion(MotivoDevolucion m, DateTime inicioUtc, DateTime ahoraUtc); }

// SI18.Api/Modulos/Cancelaciones
public interface IConsultaCierres
{
    Task<IReadOnlyList<CierreMantenimiento>> Cierres(int instalacionId, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
}
```

## CursosInformes (HU-04, HU-08)

No expone interfaces a otros módulos. Lee el resto mediante las interfaces anteriores y a través de
consultas de solo lectura sobre el `Si18DbContext` (`AsNoTracking`). Una lectura nunca viola la
regla del dueño único.

## Grafo de dependencias permitido

```text
Comun ◄── todos
Disponibilidad ──► Reservas (IConsultaReservas), Cancelaciones (IConsultaCierres)
Reservas ──► Disponibilidad (IConsultaDisponibilidad)
Cancelaciones ──► Reservas (IServicioReservas), Disponibilidad (IConsultaFranjasFijas), Comun (IServicioAvisos), IPasarelaPago
CursosInformes ──► Disponibilidad, Reservas, Cancelaciones (solo lectura), Comun (IServicioAvisos)
```

La dependencia mutua entre Disponibilidad y Reservas se limita a interfaces de consulta, resueltas en
tiempo de ejecución por inyección de dependencias. Ninguna de las dos depende de la implementación
concreta de la otra.
