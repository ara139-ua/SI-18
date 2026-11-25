# Modelo de datos: 002 · Reservar y pagar una pista

**HU-02 no añade tablas, columnas, índices ni migraciones.** Las tablas `reservas` y `pagos`, sus
restricciones, el índice `ux_reservas_activa` y la máquina de estados ya están en
[`000-core/data-model.md`](../000-core/data-model.md) (§ Reserva, § Pago) y en la migración
`NucleoComun` (principio VII). Este documento recoge solo:

1. qué lee y escribe HU-02, y por qué vía;
2. cómo usa la máquina de estados de 000-core en cada operación;
3. los modelos de lectura y de petición de la API (no se almacenan).

## 1. Qué lee y escribe, y por qué vía

| Dato (tabla de 000-core)            | Dueño            | Uso en HU-02 | Vía                                                                  |
| ----------------------------------- | ---------------- | ------------ | -------------------------------------------------------------------- |
| `reservas`, `pagos`                 | **Reservas (HU-02)** | Lee y escribe | `Si18DbContext` (entidades `Reserva` y `Pago`)                    |
| `instalaciones`, `centros`          | Comun            | Lee          | `Si18DbContext`, `AsNoTracking` (nombres para el resumen y la lista) |
| Disponibilidad y precio de una franja | Disponibilidad (HU-01) | Lee    | `IConsultaDisponibilidad.MotivoNoDisponible` y `.Precio` (RF-013)    |
| Tarifa e identidad del usuario      | Comun (sesión)   | Lee          | `IUsuarioActual.Id` y `.TipoTarifa`                                  |
| Cobro                               | —                | —            | `IPasarelaPago.Cobrar` (R9); no deja nada guardado                   |

HU-02 **no escribe** en ninguna tabla de otro módulo. HU-03 y HU-07 escriben en `reservas` y `pagos`
**solo** a través de `IServicioReservas` (§ 2.4 y § 2.5).

## 2. Operaciones sobre la máquina de estados de 000-core

Todas usan los métodos de la entidad (`Reserva.Confirmar`, `Caducar`, `Cancelar`; `EstaVigente`), que
son la única regla de transiciones (RF-015, RF-016 de 000-core). Toda operación que cambia una reserva
**ya guardada** la carga dentro de una transacción con `SELECT … FOR UPDATE` y con su `Pago`
(research.md E5). `ahora` = `TimeProvider.GetUtcNow()`, leído una vez por operación.

### 2.1 Crear (`POST /api/reservas`, research.md E3)

```text
(nada) ──new Reserva(usuario, instalación, inicio, tarifa, importe, ahora)──► pendiente_pago + pago pendiente
```

| Campo de `reservas` | Valor                                                                       |
| ------------------- | --------------------------------------------------------------------------- |
| `usuario_id`        | `IUsuarioActual.Id`                                                         |
| `instalacion_id`, `inicio_utc` | Los de la franja (validados por `MotivoNoDisponible`)            |
| `fin_utc`           | `inicio_utc + DuracionFranja` (lo pone la entidad)                          |
| `tipo_tarifa`       | `IUsuarioActual.TipoTarifa` (queda fijada)                                  |
| `importe`           | `IConsultaDisponibilidad.Precio(…)`, igual a `importeMostrado`              |
| `estado`            | `pendiente_pago`                                                            |
| `creada_en_utc`     | `ahora` (inicio del bloqueo de `TiempoBloqueoPago`)                          |

Antes del `INSERT`, en la misma transacción: las reservas `pendiente_pago` **no vigentes** de la misma
`(instalacion_id, inicio_utc)` pasan por `Caducar(ahora)` (R8.2). Un `23505` en `ux_reservas_activa`
al guardar se traduce en `FRANJA_OCUPADA`.

### 2.2 Pagar (`POST /api/reservas/{id}/pago`, research.md E4)

| Situación al pagar (reserva del usuario) | Transición                                   | Respuesta                    |
| ---------------------------------------- | -------------------------------------------- | ---------------------------- |
| Estado guardado ≠ `pendiente_pago`       | ninguna                                      | 409 `TRANSICION_NO_PERMITIDA` |
| `pendiente_pago` y `!EstaVigente(ahora)` | `Caducar` (pago → `fallido`)                 | 409 `RESERVA_CADUCADA`       |
| Vigente y la pasarela acepta             | `Confirmar(GeneradorCodigoAcceso.Nuevo(), ahora)` (pago → `completado`) | 200 con `codigoAcceso` |
| Vigente y la pasarela rechaza            | `Caducar` (pago → `fallido`)                 | 422 `PAGO_RECHAZADO`         |

Los cambios de estado se guardan antes de responder el error. Del número de tarjeta no se guarda nada.

### 2.3 Barrido (`BarridoCaducidades`, 000-core R8.4)

Sin cambios de regla. Solo cambia cómo carga: `FOR UPDATE SKIP LOCKED` (research.md E5).

### 2.4 Cancelar por el usuario (`IServicioReservas.CancelarPorUsuario`, para HU-03)

```text
confirmada ──Cancelar(usuario, ahora)──► cancelada   (cancelada_en_utc = ahora, motivo = usuario)
```

- Reserva inexistente o de otro usuario → `NO_ENCONTRADO`.
- No confirmada → `TRANSICION_NO_PERMITIDA`; `ahora ≥ inicio_utc` → `RESERVA_YA_EMPEZADA` (los lanza la
  entidad).
- El pago no cambia (sigue `completado`): la devolución es de HU-03.
- Devuelve `ReservaAfectada(ReservaId, UsuarioId, InicioUtc, PagoId, Importe)`.

### 2.5 Aplicar un cierre (`IServicioReservas.AplicarCierre`, para HU-07)

Afecta a las reservas de la instalación con estado guardado `confirmada` o `pendiente_pago` cuyo
`[inicio_utc, fin_utc)` se solapa con el cierre `[inicioUtc, finUtc)`:
`r.inicio_utc < finUtc AND r.fin_utc > inicioUtc`.

| Estado guardado  | Transición                                           | Lista del resultado |
| ---------------- | ---------------------------------------------------- | ------------------- |
| `confirmada`     | `Cancelar(cierre_mantenimiento, ahora)` (sin límite de hora) | `Canceladas` |
| `pendiente_pago` (vigente o no) | `Caducar(ahora)` (pago → `fallido`)   | `Caducadas`         |

Las dos listas van ordenadas por `InicioUtc`.

**Para 2.4 y 2.5**: no llaman a `SaveChanges` ni abren transacciones. Exigen que el llamador ya tenga
una abierta en el `Si18DbContext` de la petición y la confirme junto con su devolución y su aviso
(research.md E8).

## 3. Modelos de la API (no se almacenan)

DTO en `SI18.Contratos/Reservas/`, compartidos por la API y el cliente. JSON según las convenciones de
000-core (contracts/README.md): camelCase, enumerados en `snake_case`, instantes UTC ISO 8601, horas de
pared `HH:mm` y fechas `yyyy-MM-dd`.

### `EstadoReserva` (copia del enumerado de dominio)

`pendiente_pago`, `confirmada`, `caducada`, `cancelada`: mismos nombres y orden que
`SI18.Dominio.Comun.EstadoReserva`, con su prueba espejo (research.md E12).

### `PeticionReserva` (cuerpo de `POST /api/reservas`)

| Campo             | Tipo             | Regla                                                               |
| ----------------- | ---------------- | ------------------------------------------------------------------- |
| `instalacionId`   | entero           | Obligatorio; `PistaLibreDto.InstalacionId` de HU-01                 |
| `inicioUtc`       | instante UTC     | Obligatorio; `FranjaLibreDto.InicioUtc` de HU-01                    |
| `importeMostrado` | decimal (2 dec.) | Obligatorio, `> 0`; el importe del resumen. Si difiere del actual → `PRECIO_CAMBIADO` |

### `PeticionPago` (cuerpo de `POST /api/reservas/{id}/pago`)

| Campo           | Tipo  | Regla                                                                             |
| --------------- | ----- | --------------------------------------------------------------------------------- |
| `numeroTarjeta` | texto | Obligatorio; 16 dígitos después de quitar los espacios. No se guarda ni se registra; `ToString()` no lo imprime |

### `ResumenReservaDto` (respuesta de `GET /api/reservas/resumen`)

| Campo           | Tipo             | Regla                                                      |
| --------------- | ---------------- | ---------------------------------------------------------- |
| `instalacionId` | entero           | La pedida                                                  |
| `pista`         | texto            | «Pista de pádel 1»                                         |
| `centro`        | texto            | «Polideportivo Norte»                                      |
| `fecha`         | `yyyy-MM-dd`     | Fecha de Madrid de la franja                               |
| `horaInicio`    | `HH:mm`          | Hora de pared de Madrid                                    |
| `horaFin`       | `HH:mm`          | Hora de pared de Madrid                                    |
| `inicioUtc`     | instante UTC     | El pedido                                                  |
| `importe`       | decimal (2 dec.) | `IConsultaDisponibilidad.Precio` para la tarifa del usuario |

### `ReservaDto` (respuesta de crear, pagar, ver una y cada elemento de «Mis reservas»)

| Campo            | Tipo                | Regla                                                                            |
| ---------------- | ------------------- | -------------------------------------------------------------------------------- |
| `id`             | entero largo        |                                                                                  |
| `instalacionId`  | entero              |                                                                                  |
| `pista`, `centro`| texto               | Nombres                                                                          |
| `fecha`          | `yyyy-MM-dd`        | Fecha de Madrid                                                                  |
| `horaInicio`, `horaFin` | `HH:mm`      | Horas de pared de Madrid                                                         |
| `inicioUtc`, `finUtc` | instante UTC   |                                                                                  |
| `importe`        | decimal (2 dec.)    | El fijado al reservar                                                            |
| `estado`         | `EstadoReserva`     | **Efectivo**: `pendiente_pago` no vigente → `caducada` (RF-015)                  |
| `limitePagoUtc`  | instante UTC o nulo | `creada_en_utc + TiempoBloqueoPago`; solo si `estado = pendiente_pago`            |
| `horaLimitePago` | `HH:mm` o nulo      | `limitePagoUtc` en Madrid, truncada al minuto; solo si `estado = pendiente_pago` (RF-008a) |
| `codigoAcceso`   | texto o nulo        | Solo si `estado = confirmada` (RF-016)                                           |

### Orden de «Mis reservas» (RF-015a)

Con `ahora` del servidor: primero las de `finUtc > ahora`, por `inicioUtc` ascendente; después las de
`finUtc ≤ ahora`, por `inicioUtc` descendente. Desempate por `id`. Sin paginación.

## 4. Reglas que se comprueban y dónde

| Regla                                                   | Dónde                                                     | Fuente |
| ------------------------------------------------------- | --------------------------------------------------------- | ------ |
| Franja libre al reservar (mismo orden de motivos)       | `IConsultaDisponibilidad.MotivoNoDisponible` (HU-01)      | RF-004; RF-011/013 de 000-core |
| Precio igual al mostrado                                | `IConsultaDisponibilidad.Precio` vs. `importeMostrado`    | RF-004 |
| Una activa por franja, también con concurrencia         | `ux_reservas_activa` (`23505` → `FRANJA_OCUPADA`)         | RF-007; RF-014 de 000-core |
| Vigencia de 10 minutos                                  | `Reserva.EstaVigente` y la misma fórmula en `ConsultaReservas` | RF-009, RF-014, RF-018 |
| Transiciones permitidas                                 | `Reserva` y `Pago` (000-core)                             | RF-010, RF-011, RF-019a |
| Un solo cobro por reserva                               | `FOR UPDATE` + estado `pendiente` del pago                | research.md E5 |
| Solo el titular ve y paga                               | Filtro `usuario_id = IUsuarioActual.Id` → `NO_ENCONTRADO` | RF-013 |
| Formato de la tarjeta                                   | Validación del endpoint de pago (antes de la pasarela)    | RF-012 |
