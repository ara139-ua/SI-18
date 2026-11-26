# Modelo de datos: 006 · Validar el acceso con el código QR

**Fecha**: 2026-11-26 · **Spec**: [spec.md](./spec.md) · **Investigación**: [research.md](./research.md)

HU-06 **no crea tablas, columnas, índices, estados ni migraciones**. Usa el modelo compartido de
[`000-core/data-model.md`](../000-core/data-model.md) tal cual (principio VII). Este documento dice qué
lee y escribe, con qué regla y por qué vía, y define los modelos de la API (que no se almacenan).

## 1. Qué lee y escribe, y por qué vía

| Dato (tabla de 000-core)            | Dueño                | Uso en HU-06  | Vía                                                                  |
| ----------------------------------- | -------------------- | ------------- | -------------------------------------------------------------------- |
| `validaciones_acceso`               | **Reservas (HU-06)** | Escribe y lee | `Si18DbContext` (entidad `ValidacionAcceso`): una fila por intento; lectura de la aceptada más reciente de una reserva (V5) |
| `reservas`                          | Reservas (HU-02)     | Solo lee      | `Si18DbContext`, `AsNoTracking`, por `codigo_acceso` (índice único). **Nunca se modifica** (RF-010) |
| `instalaciones`, `centros`          | Comun                | Solo lee      | `Si18DbContext`, `AsNoTracking`: centro de la reserva y nombres       |
| `usuarios` (titular)                | Comun                | Solo lee      | `Si18DbContext`, `AsNoTracking`: nombre del titular                   |
| Rol, id y centro del personal       | Comun (sesión)       | Lee           | `IUsuarioActual.Rol`, `.Id`, `.CentroId` (claim de `usuarios.centro_id`) |

Reservas es dueño de `reservas` y `validaciones_acceso` (`docs/agrupamiento.md`), así que HU-06 no
escribe en ninguna tabla de otro módulo ni necesita interfaces nuevas
([contracts/modulos.md](./contracts/modulos.md)).

## 2. La operación: validar un código (`POST /api/validaciones-acceso`, research.md V3–V5)

`ahora` = `TimeProvider.GetUtcNow()`, leído una vez. `codigo` = el recibido sin espacios en los extremos
(vacío → 400, sin registro, V6).

```text
reserva  ← reservas WHERE codigo_acceso = codigo   (+ instalación → centro, + titular)   AsNoTracking
motivo   ← reserva = null                         → QR_DESCONOCIDO
           reserva.centro ≠ IUsuarioActual.CentroId → QR_OTRO_CENTRO
           ReglaAcceso.MotivoRechazo(reserva, ahora) → QR_RESERVA_NO_CONFIRMADA | QR_FUERA_DE_VENTANA | null
anterior ← motivo = null ? MAX(fecha_utc) FROM validaciones_acceso WHERE reserva_id = reserva.id AND aceptada : —
INSERT validaciones_acceso (…)   ──  un solo SaveChanges  ──►  201 (aceptada) | Problem Details (rechazada)
```

| Campo de `validaciones_acceso` | Valor                                                                 |
| ------------------------------ | --------------------------------------------------------------------- |
| `reserva_id`                   | `reserva.id` si el código existe (también con `QR_OTRO_CENTRO`); nulo con `QR_DESCONOCIDO` |
| `codigo_leido`                 | `codigo`, recortado a 64 por la entidad (`ValidacionAcceso.LongitudMaximaCodigo`) |
| `aceptada`                     | `motivo = null`                                                       |
| `motivo_rechazo`               | `motivo` (el `CHECK` de 000-core exige que esté si y solo si no se acepta) |
| `fecha_utc`                    | `ahora`                                                               |
| `personal_id`                  | `IUsuarioActual.Id`                                                   |

**Ventana** (sin redefinirla): `reserva.InicioUtc − ValoresNegocio.MargenQrAntesDelInicio ≤ ahora ≤
reserva.FinUtc`, dentro de `ReglaAcceso`. Una reserva `caducada` o `pendiente_pago` nunca tiene código
(`CHECK` de `reservas`), así que no puede llegar aquí: su caso es `QR_DESCONOCIDO`.

## 3. Modelos de la API (no se almacenan)

DTO en `SI18.Contratos/Reservas/`, compartidos por la API y el cliente. JSON según las convenciones de
000-core (contracts/README.md): camelCase, instantes UTC ISO 8601, horas de pared `HH:mm` y fechas
`yyyy-MM-dd`, todas de Madrid y calculadas por la API (V8).

### `PeticionValidacionAcceso` (cuerpo de `POST /api/validaciones-acceso`)

| Campo    | Tipo  | Regla                                                                                 |
| -------- | ----- | ------------------------------------------------------------------------------------- |
| `codigo` | texto | Obligatorio; se quitan los espacios de los extremos; vacío → 400 `PETICION_INVALIDA` con `errors.codigo` |

### `ValidacionAccesoDto` (respuesta 201: código aceptado)

| Campo                   | Tipo                | Regla                                                              |
| ----------------------- | ------------------- | ------------------------------------------------------------------ |
| `validacionId`          | entero largo        | La fila registrada                                                 |
| `titular`               | texto               | Nombre del titular de la reserva (RF-006)                          |
| `instalacion`, `centro` | texto               | «Pista de pádel 2», «Polideportivo Este»                           |
| `fecha`                 | `yyyy-MM-dd`        | Fecha de Madrid de la reserva                                      |
| `horaInicio`, `horaFin` | `HH:mm`             | Horas de pared de Madrid                                           |
| `validacionAnteriorUtc` | instante UTC o nulo | La validación **aceptada** más reciente de esa reserva, si la hay (RF-006a) |
| `validacionAnteriorHora`| `HH:mm` o nulo      | La misma, en hora de Madrid                                        |

### Extensiones del Problem Details de un rechazo

| Extensión      | Tipo                           | Cuándo                                                           |
| -------------- | ------------------------------ | ---------------------------------------------------------------- |
| `codigo`       | texto                          | Siempre (000-core): `QR_DESCONOCIDO`, `QR_OTRO_CENTRO`, `QR_RESERVA_NO_CONFIRMADA` o `QR_FUERA_DE_VENTANA` |
| `traceId`      | texto                          | Siempre (000-core)                                               |
| `validacionId` | entero largo                   | Siempre: la fila registrada                                      |
| `reserva`      | `ReservaValidadaDto`           | **Solo** con `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA` (RF-007, aclaración 3) |

### `ReservaValidadaDto` (extensión `reserva`)

| Campo                   | Tipo         | Regla                                  |
| ----------------------- | ------------ | -------------------------------------- |
| `instalacion`, `centro` | texto        | Nombres                                |
| `fecha`                 | `yyyy-MM-dd` | Fecha de Madrid                        |
| `horaInicio`, `horaFin` | `HH:mm`      | Horas de pared de Madrid               |

**Sin titular**: el tipo no tiene ese campo, para que no se pueda devolver por error (CE-003).

## 4. Reglas que se comprueban y dónde

| Regla                                                    | Dónde                                                    | Fuente |
| -------------------------------------------------------- | -------------------------------------------------------- | ------ |
| Solo el personal de centro valida                        | `Politicas.EsPersonal` en el grupo del endpoint          | RF-001; «Roles y permisos» de 000-core |
| Centro del personal                                      | `IUsuarioActual.CentroId`                                | RF-002; RF-008 de 000-core |
| Código no vacío, sin espacios en los extremos            | Validación del endpoint (400, sin registro)              | RF-004 |
| Orden de motivos: desconocido → otro centro → no confirmada → fuera de ventana | `ServicioValidarAcceso` + `ReglaAcceso`  | RF-005 |
| Ventana de 15 min antes del inicio hasta el fin, incluidos | `ReglaAcceso.MotivoRechazo` (000-core)                 | RF-018 de 000-core |
| Una fila por intento, en la misma operación              | `ServicioValidarAcceso` (un `SaveChanges`)               | RF-008, RF-009 |
| Aviso de validación aceptada anterior                    | `ServicioValidarAcceso` (consulta a `validaciones_acceso`) | RF-006a |
| Datos de la reserva en los rechazos, nunca el titular    | `ReservaValidadaDto` solo con dos motivos                | RF-007 |
| La reserva no cambia                                     | El servicio la lee con `AsNoTracking`                     | RF-010 |
