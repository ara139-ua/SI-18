# Contratos entre módulos de HU-02

HU-02 **no define interfaces nuevas ni cambia ninguna firma**: implementa y consume las de
[`000-core/contracts/modulos.md`](../../000-core/contracts/modulos.md).

## Lo que implementa (definitivo)

| Interfaz             | Implementación                                 | La consumen                                  |
| -------------------- | ---------------------------------------------- | -------------------------------------------- |
| `IConsultaReservas`  | `Modulos/Reservas/ConsultaReservas.cs`         | Disponibilidad (HU-01, HU-05), CursosInformes (HU-08) |
| `IServicioReservas`  | `Modulos/Reservas/ServicioReservas.cs`         | Cancelaciones: HU-03 (`CancelarPorUsuario`) y HU-07 (`AplicarCierre`), de C |

Se registran con `AddScoped` en `RegistroReservas.AddReservas()` (research.md E9).

### `IConsultaReservas.Activas(instalacionId, desdeUtc, hastaUtc, ct)`

- Reservas de la instalación con `inicioUtc ∈ [desdeUtc, hastaUtc)` y estado `confirmada`, o
  `pendiente_pago` con `creadaEnUtc > ahora − TiempoBloqueoPago` (la vigencia de `Reserva.EstaVigente`).
  Ordenadas por `inicioUtc`. Solo lectura (`AsNoTracking`).
- **Mismo comportamiento que la provisional de spec-001**, fijado por las mismas pruebas (research.md E7).

### `IServicioReservas` — **lo usa C en HU-03 y HU-07** (RF-019a)

Reglas comunes a los dos métodos (research.md E8):

- **El llamador abre la transacción antes** (`await db.Database.BeginTransactionAsync(ct)`) sobre el
  `Si18DbContext` de la petición. Sin transacción abierta → `InvalidOperationException` (error de
  programación).
- **El llamador guarda**: los métodos no llaman a `SaveChanges`. Después de llamar, el llamador crea su
  devolución y su aviso (`IServicioAvisos`), hace `SaveChanges` y `Commit`. Si algo falla, no se guarda
  nada, tampoco el cambio de la reserva.
- Las reservas afectadas se cargan con `FOR UPDATE` (research.md E5): un pago en curso de la misma
  reserva espera a que el llamador confirme o deshaga.

| Método | Qué hace | Errores (`ErrorNegocio`) | Devuelve |
| ------ | -------- | ------------------------ | -------- |
| `CancelarPorUsuario(reservaId, usuarioId, ct)` | `Cancelar(usuario, ahora)`: confirmada → cancelada. El pago no cambia | `NO_ENCONTRADO` (no existe o no es de `usuarioId`); `TRANSICION_NO_PERMITIDA` (no está confirmada); `RESERVA_YA_EMPEZADA` (`ahora ≥ inicio`) | `ReservaAfectada(ReservaId, UsuarioId, InicioUtc, PagoId, Importe)` |
| `AplicarCierre(instalacionId, inicioUtc, finUtc, ct)` | Reservas con `[inicio, fin)` solapado con `[inicioUtc, finUtc)`: confirmadas → `Cancelar(cierre_mantenimiento, ahora)`; pendientes de pago guardadas (vigentes o no) → `Caducar(ahora)` (pago → fallido) | Ninguno de negocio | `ResultadoCierre(Canceladas, Caducadas)`, cada lista ordenada por `InicioUtc` |

Lo que **no** hacen (es de C): decidir si hay devolución (`PoliticaDevolucion`), crear la `Devolucion`,
llamar a `IPasarelaPago.Devolver`, avisar y registrar el cierre.

Uso esperado (esquema, no código definitivo):

```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);
var afectada = await servicioReservas.CancelarPorUsuario(id, usuario.Id, ct);
// HU-03: PoliticaDevolucion + Devolucion + IPasarelaPago.Devolver …
await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

## Lo que retira (provisional de spec-001)

| Qué                                                                 | Dónde (dueño: Artur)                                     | Acción |
| ------------------------------------------------------------------- | -------------------------------------------------------- | ------ |
| `ConsultaReservasProvisional`                                       | `Modulos/Disponibilidad/Provisional/ConsultaReservasProvisional.cs` | Se borra el fichero |
| `services.TryAddScoped<IConsultaReservas, ConsultaReservasProvisional>();` | `Modulos/Disponibilidad/RegistroDisponibilidad.cs` | Se borra **solo esa línea** (y el `using` que deje de usarse) |
| Las dos pruebas de `IConsultaReservas`                              | `tests/SI18.Api.Tests/Disponibilidad/ConsultasProvisionalesTests.cs` | Se mueven a `tests/SI18.Api.Tests/Reservas/ConsultaReservasTests.cs`, con los mismos casos |
| `new ConsultaReservasProvisional(db, reloj)` en `ConServicio`       | `tests/SI18.Api.Tests/Disponibilidad/DatosDisponibilidad.cs` | Se cambia por `new ConsultaReservas(db, reloj)` (una línea y su `using`) |

Es lo acordado en spec-001 (research.md D5, RF-014a). Las provisionales de cierres y franjas fijas no
se tocan.

## Lo que consume

| Interfaz                   | Para qué                                                      |
| -------------------------- | ------------------------------------------------------------- |
| `IConsultaDisponibilidad`  | `MotivoNoDisponible` y `Precio` de la franja al resumir y al reservar (RF-004; RF-013 de 000-core) |
| `IPasarelaPago`            | `Cobrar` al pagar (R9)                                        |
| `IUsuarioActual`           | `Id` (titular y filtro) y `TipoTarifa` (precio)               |

El grafo de dependencias es el permitido por 000-core: Reservas → Disponibilidad
(`IConsultaDisponibilidad`). La dependencia inversa (Disponibilidad → `IConsultaReservas`) se resuelve
por inyección; ninguno de los dos módulos conoce la implementación del otro.

## Componente de cliente que ofrece a Disponibilidad

| Componente                         | Lo usa                                    | Parámetros |
| ---------------------------------- | ----------------------------------------- | ---------- |
| `SI18.Cliente/Reservas/EnlaceReservar.razor` | `/buscar-pistas` (`BuscarPistas.razor`, de Artur) | `Pista` (`PistaLibreDto`), `Franja` (`FranjaLibreDto`), `Fecha` (`DateOnly`) |

Pinta el botón «Reservar» de cada franja, con nombre accesible completo, y lleva a
`/reservas/nueva?instalacionId&inicioUtc`. Cambio en `BuscarPistas.razor`: una línea y un `@using`
(research.md E10). **Punto a coordinar con Artur.**
