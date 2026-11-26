# Contratos entre módulos de HU-05

HU-05 **no define interfaces nuevas**: implementa y consume las de
[`000-core/contracts/modulos.md`](../../000-core/contracts/modulos.md), sin cambiar ninguna firma.

## Lo que implementa (definitivo)

| Interfaz                | Implementación                                       | La consumen                         |
| ----------------------- | ---------------------------------------------------- | ----------------------------------- |
| `IConsultaFranjasFijas` | `Modulos/Disponibilidad/ConsultaFranjasFijas.cs`     | HU-01 y HU-02 (vía `ServicioDisponibilidad`), HU-07, HU-08 |

- `FranjasBloqueadas(instalacionId, desde, hasta, ct)`: franjas de las solicitudes **aprobadas** de esa
  instalación en `[desde, hasta]`, dentro de su temporada y **sin** las que afecta un cierre por
  mantenimiento (RF-014; aclaración de 000-core). Sin repetir, ordenadas por `InicioUtc`.
- `RepresentantesAfectados(instalacionId, inicioUtc, finUtc, ct)`: identificadores de los usuarios
  representantes de los clubes con alguna franja fija aprobada que se solapa con `[inicioUtc, finUtc)` en
  esa instalación, sin repetir (RF-016). Aquí **no** se descuentan los cierres: el cierre es justo lo que
  las afecta.

**Sustituye** a `Provisional/ConsultaFranjasFijasProvisional.cs` de spec-001 (RF-014a de spec-001), que
se borra en el mismo PR. El registro pasa de `TryAddScoped` a `AddScoped`. La búsqueda de HU-01 debe dar
el mismo resultado (CE-005).

## Lo que consume

| Interfaz            | Para qué                                                                |
| ------------------- | ----------------------------------------------------------------------- |
| `IConsultaReservas` | Reservas activas de la temporada que entran en conflicto (F4)           |
| `IConsultaCierres`  | Cierres que suspenden franjas fijas en `FranjasBloqueadas` (F6)         |
| `IServicioAvisos`   | Avisar a los representantes al aprobar o rechazar (F5)                  |
| `IUsuarioActual`    | Club y rol de quien solicita o revisa                                   |

Mientras HU-02 y HU-07 no publiquen las suyas, `IConsultaReservas` e `IConsultaCierres` son las
provisionales de spec-001.

## Coordinación

> **Alcance (26/11/2026)**: HU-03 y HU-07 quedaron **fuera del alcance comprometido** por acuerdo con el
> profesor (el grupo son dos personas; ver [docs/agrupamiento.md](../../../docs/agrupamiento.md)). La fila de «C (spec-007)» ya no tiene destinatario; `RepresentantesAfectados` sigue implementado y probado. Se conserva
> como base para trabajo futuro: no hay nadie asignado y no es trabajo pendiente.

| Con                    | Qué                                                                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Saul (spec-002)        | Los dos PR quitan cada uno su provisional de `RegistroDisponibilidad.cs`, `ConsultasProvisionalesTests.cs` y `DatosDisponibilidad.cs`. El segundo en llegar hace `rebase` y resuelve (research.md F11) |
| C (spec-007)           | `RepresentantesAfectados` ya está disponible para avisar de la suspensión de las franjas fijas al cerrar una instalación             |
