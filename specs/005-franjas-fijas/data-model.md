# Modelo de datos: 005 · Franjas fijas de temporada para clubes

**HU-05 no añade tablas, columnas, índices, estados ni migraciones.** La tabla
`solicitudes_franjas_fijas` y sus reglas ya están en [`000-core/data-model.md`](../000-core/data-model.md)
§ SolicitudFranjasFijas (principio VII). Los tres códigos de error que necesita ya están en `develop`
(PR #32). Este documento recoge solo:

1. qué escribe y qué lee HU-05;
2. las reglas que se calculan (no se almacenan);
3. los DTO de la API.

## 1. Qué escribe y qué lee

| Dato (tabla de 000-core)       | Dueño                  | HU-05                                                                                          |
| ------------------------------ | ---------------------- | ---------------------------------------------------------------------------------------------- |
| `solicitudes_franjas_fijas`    | **Disponibilidad (HU-05)** | **Escribe**: crea (pendiente), aprueba y rechaza con los métodos del dominio                |
| `notificaciones`               | Comun                  | **Escribe** a través de `IServicioAvisos.Avisar` (mismo `SaveChanges` que el cambio de estado) |
| `clubes`, `usuarios`           | Comun                  | Lee: nombre del club y sus representantes (`club_id`, `rol = representante_club`)             |
| `temporadas`                   | Comun                  | Lee: temporadas no terminadas y fechas de cada solicitud                                      |
| `instalaciones`, `centros`     | Comun                  | Lee: que sea pista o pabellón, y los nombres para mostrar y para los avisos                   |
| `reservas`                     | Reservas (HU-02)       | Lee solo con `IConsultaReservas.Activas`                                                      |
| `cierres_mantenimiento`        | Cancelaciones (HU-07)  | Lee solo con `IConsultaCierres.Cierres`                                                       |

## 2. Reglas calculadas (`ReglaFranjasFijas`, research.md F3)

| Regla                         | Definición                                                                                                                         |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Instalación admisible         | `tipo ∈ {pista_tenis, pista_padel, pabellon}` (D2 de 000-core)                                                                     |
| Temporada admisible           | `fecha_fin ≥ hoy` en Madrid                                                                                                        |
| `Cubre(s, fecha, hora)`       | díaIso(fecha) ∈ `s.dias_semana` ∧ `s.hora_inicio ≤ hora < s.hora_fin`                                                              |
| `SeSolapan(a, b)`             | misma instalación ∧ misma temporada ∧ `a.dias_semana ∩ b.dias_semana ≠ ∅` ∧ `a.hora_inicio < b.hora_fin` ∧ `b.hora_inicio < a.hora_fin` |
| `Expandir(s, t, desde, hasta)` | franjas de `CalculadoraFranjas` en `[desde, hasta] ∩ [t.fecha_inicio, t.fecha_fin]` que `Cubre(s, …)`                             |
| Reserva en conflicto          | reserva activa (confirmada o pendiente vigente) de la instalación, de cualquier titular, con `ahora ≤ inicio_utc < AUtc(t.fecha_fin + 1, 00:00)` y `Cubre(s, fecha, hora)` de su inicio en Madrid |
| Solicitud solapada            | solicitud `aprobada` `b` con `SeSolapan(s, b)`                                                                                     |
| Franja bloqueada              | franja de `Expandir` de una solicitud `aprobada` a la que no afecta ningún cierre de esa instalación                               |
| Representante afectado        | usuario `representante_club` de un club con alguna franja de `Expandir` de una aprobada que se solapa con el cierre                |

### Transiciones (ya implementadas en el dominio)

```text
pendiente ──Aprobar(ahora)──► aprobada   (solo si no hay conflictos: lo comprueba el servicio)
pendiente ──Rechazar(ahora)─► rechazada
aprobada / rechazada ──Aprobar o Rechazar──► SOLICITUD_YA_RESUELTA (409)
```

## 3. DTO (`SI18.Contratos/Disponibilidad/`)

JSON según las convenciones de 000-core: camelCase, enumerados en `snake_case`, instantes UTC ISO 8601,
horas de pared `"HH:mm"` y fechas `"yyyy-MM-dd"`.

### `PeticionSolicitudFranjasFijas` (cuerpo de `POST`)

| Campo           | Tipo           | Regla                                                         |
| --------------- | -------------- | ------------------------------------------------------------- |
| `instalacionId` | entero         | pista o pabellón existente                                    |
| `temporadaId`   | entero         | temporada existente y no terminada                            |
| `diasSemana`    | lista de enteros | ≥ 1, valores 1–7 (ISO), sin repetir                         |
| `horaInicio`    | `"HH:mm"`      | en punto, ≥ `"08:00"`                                          |
| `horaFin`       | `"HH:mm"`      | en punto, ≤ `"22:00"`, posterior a `horaInicio`                |

El club **no** va en el cuerpo: sale de la sesión.

### `SolicitudFranjasFijasDto`

| Campo                | Tipo                   | Notas                                      |
| -------------------- | ---------------------- | ------------------------------------------ |
| `id`                 | entero                 |                                            |
| `clubNombre`         | texto                  | nombre del club (no es un dato personal)   |
| `instalacionId`      | entero                 |                                            |
| `instalacionNombre`  | texto                  | «Pabellón»                                 |
| `centroNombre`       | texto                  | «Polideportivo Norte»                      |
| `temporadaNombre`    | texto                  | «2026-2027»                                |
| `diasSemana`         | lista de enteros       | ordenados, 1–7                             |
| `horaInicio`, `horaFin` | `"HH:mm"`           |                                            |
| `estado`             | `EstadoSolicitud`      | `pendiente` · `aprobada` · `rechazada`     |
| `creadaEnUtc`        | instante UTC           |                                            |
| `resueltaEnUtc`      | instante UTC o `null`  | `null` si está pendiente                   |

Orden de la lista: pendientes primero; dentro de cada estado, por `creadaEnUtc` (más antigua primero).

### `ConflictosSolicitudDto`

| Campo          | Tipo                              |
| -------------- | --------------------------------- |
| `reservas`     | lista de `ReservaEnConflictoDto`  |
| `solapadas`    | lista de `SolicitudSolapadaDto`   |
| `puedeAprobarse` | booleano (`reservas` y `solapadas` vacías) |

- `ReservaEnConflictoDto`: `fecha` (`yyyy-MM-dd`), `horaInicio`, `horaFin` (`"HH:mm"`, Madrid) y `estado`
  (`EstadoReservaEnConflicto`: `confirmada` · `pendiente_pago`). **Sin** identificador de usuario ni
  datos personales (aclaración 3). Ordenadas por fecha y hora.
- `SolicitudSolapadaDto`: `id`, `clubNombre`, `diasSemana`, `horaInicio` y `horaFin`.

### `TemporadaDto`

`id`, `nombre`, `fechaInicio` y `fechaFin` (`yyyy-MM-dd`).
