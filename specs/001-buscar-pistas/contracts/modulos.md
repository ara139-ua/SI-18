# Contratos entre módulos de HU-01

HU-01 **no define interfaces nuevas**: implementa y consume las de
[`000-core/contracts/modulos.md`](../../000-core/contracts/modulos.md), sin cambiar ninguna firma.

## Lo que implementa (definitivo)

| Interfaz                    | Implementación                                        | La consume          |
| --------------------------- | ----------------------------------------------------- | ------------------- |
| `IConsultaDisponibilidad`   | `Modulos/Disponibilidad/ServicioDisponibilidad.cs`    | Reservas (HU-02)    |

- `MotivoNoDisponible(instalacionId, inicioUtc, ct)`: `null` si la franja está libre, o el código del
  primer motivo de RF-011 de 000-core (mismo orden que `ReglaDisponibilidad`). Nunca devuelve otro
  código. Las peticiones mal formadas lanzan `ErrorNegocio`:
  - si `inicioUtc` no es el inicio de una franja válida (no está en punto o está fuera de
    08:00–21:00 en Madrid): `PETICION_INVALIDA`;
  - si la instalación no existe: `NO_ENCONTRADO`.
- `Precio(instalacionId, tarifa, ct)`: `CalculadoraPrecio` con el tipo de la instalación; sin tarifa
  para ese tipo, `INSTALACION_NO_RESERVABLE` (igual que en 000-core).
- La búsqueda y estos dos métodos usan el mismo código interno (research.md D4): no pueden divergir.

## Lo que implementa de forma provisional (RF-014a)

Solo lectura, registradas con `TryAddScoped`. Cada dueño las sustituye y borra la provisional en su PR
(research.md D5).

| Interfaz                | Provisional (en `Modulos/Disponibilidad/Provisional/`) | Dueño real                 | Aviso |
| ----------------------- | ------------------------------------------------------ | -------------------------- | ----- |
| `IConsultaReservas`     | `ConsultaReservasProvisional`                          | Saul · Reservas (spec-002) | Se anota en spec-002 |
| `IConsultaCierres`      | `ConsultaCierresProvisional`                           | C · Cancelaciones (spec-007) | Se anota en spec-007 |
| `IConsultaFranjasFijas` | `ConsultaFranjasFijasProvisional`                      | Artur · Disponibilidad (spec-005) | Se anota en spec-005 |

> **Alcance (26/11/2026)**: HU-03 y HU-07 quedaron **fuera del alcance comprometido** por acuerdo con el
> profesor (el grupo son dos personas; ver [docs/agrupamiento.md](../../../docs/agrupamiento.md)). La fila de `IConsultaCierres` (dueño «C», spec-007) ya no tiene destinatario: `ConsultaCierresProvisional` sigue siendo la implementación registrada. Se conserva
> como base para trabajo futuro: no hay nadie asignado y no es trabajo pendiente.

`IConsultaFranjasFijas.RepresentantesAfectados` no lo usa HU-01 y en la provisional lanza
`NotImplementedException`; lo implementa spec-005 para HU-07.

## Lo que consume

| Interfaz                | Para qué                                  |
| ----------------------- | ----------------------------------------- |
| `IConsultaReservas`     | Reservas activas del día (franjas ocupadas) |
| `IConsultaCierres`      | Cierres que se solapan con el día         |
| `IConsultaFranjasFijas` | Franjas bloqueadas por club en la fecha   |
| `IUsuarioActual`        | Tarifa de quien busca                     |

El grafo de dependencias es el permitido por 000-core: Disponibilidad → Reservas (`IConsultaReservas`)
y Cancelaciones (`IConsultaCierres`).
