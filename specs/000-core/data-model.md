# Modelo de datos: 000-core · Núcleo común de SI-18

**Fecha**: 2026-10-28 · **Spec**: [spec.md](./spec.md) · **Investigación**: [research.md](./research.md)

Este es el modelo de datos **compartido** que referencian las specs 001–008 (constitución, principio
VII). Detalla las «Entidades clave» de la spec: no añade entidades ni reglas de negocio nuevas.
Cualquier cambio se propone aquí mediante un pull request revisado (RF-003).

## Convenciones

- **Nombres**: las entidades C# van en PascalCase y en español; las tablas y columnas, en `snake_case`
  (`EFCore.NamingConventions`).
- **Claves**: `id` de tipo `integer` con identidad generada; `bigint` en las tablas que crecen
  (`reservas`, `pagos`, `devoluciones`, `validaciones_acceso`, `notificaciones`).
- **Instantes**: `timestamptz`, siempre en UTC, con sufijo `_utc` (R5).
- **Fechas y horas de pared** de Madrid: `date` / `time` (`DateOnly` / `TimeOnly`).
- **Importes**: `numeric(8,2)` ↔ `decimal`. Nunca coma flotante.
- **Enumerados**: texto en `snake_case` con `CHECK` sobre los valores permitidos.
- **Módulo dueño**: el único módulo que inserta o actualiza la tabla. Los demás la leen o usan su
  interfaz ([contracts/modulos.md](./contracts/modulos.md)).

## Enumerados

| Enumerado          | Valores                                                                    |
| ------------------ | -------------------------------------------------------------------------- |
| `Rol`              | `ciudadano`, `representante_club`, `personal_centro`, `tecnico_concejalia` |
| `TipoTarifa`       | `empadronado`, `no_empadronado`, `club`                                    |
| `TipoInstalacion`  | `pista_tenis`, `pista_padel`, `pabellon`, `sala`, `piscina`                |
| `EstadoReserva`    | `pendiente_pago`, `confirmada`, `caducada`, `cancelada`                    |
| `MotivoCancelacion`| `usuario`, `cierre_mantenimiento`                                          |
| `EstadoPago`       | `pendiente`, `completado`, `fallido`                                       |
| `MotivoDevolucion` | `cancelacion_usuario`, `cierre_mantenimiento`                              |
| `EstadoSolicitud`  | `pendiente`, `aprobada`, `rechazada`                                       |
| `EstadoInscripcion`| `confirmada`, `lista_espera`, `baja`                                       |

## Diagrama de relaciones

```text
Centro 1──* Instalacion *──* Deporte            Club 1──* Usuario(representante)
Centro 1──* Usuario(personal)                   Club 1──* SolicitudFranjasFijas
Instalacion 1──* Reserva 1──0..1 Pago 1──0..1 Devolucion
Instalacion 1──* CierreMantenimiento            Temporada 1──* SolicitudFranjasFijas
Instalacion 1──* SolicitudFranjasFijas          Instalacion 1──* Curso *──1 Deporte
Usuario 1──* Reserva                            Curso 1──* Inscripcion *──1 Usuario
Usuario 1──* Notificacion                       Reserva 0..1──* ValidacionAcceso *──1 Usuario(personal)
Tarifa: (TipoInstalacion, TipoTarifa) → precio_hora    Franja: calculada, no se almacena
```

---

## Entidades

### Centro · tabla `centros` · dueño: `Comun` (precargado)

| Campo       | Tipo           | Reglas            |
| ----------- | -------------- | ----------------- |
| `id`        | integer        | PK                |
| `nombre`    | varchar(100)   | obligatorio, único |
| `direccion` | varchar(200)   | obligatorio       |

El horario de apertura (08:00–22:00) no se guarda en la tabla: sale de `ValoresNegocio` y la API lo
devuelve como dato derivado (R6).

### Deporte · `deportes` · dueño: `Comun` (precargado)

| Campo    | Tipo        | Reglas             |
| -------- | ----------- | ------------------ |
| `id`     | integer     | PK                 |
| `nombre` | varchar(50) | obligatorio, único |

### Instalacion · `instalaciones` (+ `instalacion_deportes`) · dueño: `Comun` (precargado)

| Campo                    | Tipo            | Reglas                                                                      |
| ------------------------ | --------------- | --------------------------------------------------------------------------- |
| `id`                     | integer         | PK                                                                          |
| `centro_id`              | integer         | FK → `centros`                                                              |
| `nombre`                 | varchar(100)    | obligatorio; único por centro                                               |
| `tipo`                   | TipoInstalacion | obligatorio                                                                 |
| `reservable_por_franjas` | boolean         | `CHECK (reservable_por_franjas = (tipo IN ('pista_tenis','pista_padel')))` (RF-012) |

- `instalacion_deportes (instalacion_id, deporte_id)`, con PK compuesta, guarda los deportes que
  admite cada instalación.
- **Estado (operativa / cerrada por mantenimiento)**: no se guarda. Se calcula en cada momento `t`:
  la instalación está *cerrada por mantenimiento* si existe un `CierreMantenimiento` con
  `inicio_utc ≤ t < fin_utc`; si no, está *operativa*. Así no hay dos fuentes de verdad. HU-07 es
  responsable del estado porque es la única que crea o modifica cierres: reabrir antes de tiempo
  consiste en adelantar el `fin_utc` del cierre a la siguiente hora en punto.

### Tarifa · `tarifas` · dueño: `Comun` (precargada)

| Campo              | Tipo            | Reglas                                                     |
| ------------------ | --------------- | ---------------------------------------------------------- |
| `id`               | integer         | PK                                                         |
| `tipo_instalacion` | TipoInstalacion | solo `pista_tenis` o `pista_padel`                         |
| `tipo_tarifa`      | TipoTarifa      | obligatorio                                                |
| `precio_hora`      | numeric(8,2)    | `> 0`                                                      |
|                    |                 | UNIQUE (`tipo_instalacion`, `tipo_tarifa`)                 |

El precio de una franja es `precio_hora` × 1 h para el tipo de la instalación y la tarifa del usuario.
`CalculadoraPrecio` (en `Dominio/Disponibilidad`) es el único cálculo y lo usan HU-01 y HU-02 (RF-013).

### Usuario · `usuarios` · dueño: `Comun` (precargado)

| Campo             | Tipo         | Reglas                                                                 |
| ----------------- | ------------ | ---------------------------------------------------------------------- |
| `id`              | integer      | PK                                                                     |
| `nombre`          | varchar(100) | obligatorio                                                            |
| `correo`          | varchar(254) | obligatorio; se guarda en minúsculas; UNIQUE                           |
| `hash_contrasena` | varchar(200) | obligatorio; `PasswordHasher` PBKDF2 (R10); nunca se expone            |
| `rol`             | Rol          | obligatorio; exactamente uno (RF-006)                                  |
| `empadronado`     | boolean      | obligatorio                                                            |
| `club_id`         | integer null | FK → `clubes`; `CHECK ((rol = 'representante_club') = (club_id IS NOT NULL))` |
| `centro_id`       | integer null | FK → `centros`; `CHECK ((rol = 'personal_centro') = (centro_id IS NOT NULL))` |

**Tipo de tarifa** (RF-007): se calcula, no se guarda.
`CalculadoraTarifa.Para(usuario)` =
`{ club, si rol = representante_club ; empadronado, si empadronado ; no_empadronado, en otro caso }`.
Solo tiene sentido para ciudadanos y representantes.

### Club · `clubes` · dueño: `Comun` (precargado)

| Campo    | Tipo         | Reglas             |
| -------- | ------------ | ------------------ |
| `id`     | integer      | PK                 |
| `nombre` | varchar(100) | obligatorio, único |

### Franja · no se almacena · cálculo: `Disponibilidad` (HU-01)

Objeto de valor `Franja(InstalacionId, Fecha: DateOnly, HoraInicio: TimeOnly, InicioUtc, FinUtc)`.

- `CalculadoraFranjas.FranjasDelDia(instalacion, fecha)` genera 14 franjas (08:00…21:00, hora de
  Madrid) y las convierte a UTC con `HorarioMadrid` (R5).
- `ReglaDisponibilidad.EstaLibre(franja, ahora, …)` aplica RF-011, en este orden:
  1. dentro de la ventana de antelación (fecha local);
  2. no ha empezado (`ahora < InicioUtc`);
  3. la instalación es reservable por franjas;
  4. no hay cierre que la cubra;
  5. no la bloquea una franja fija aprobada;
  6. no tiene reserva confirmada ni pendiente de pago vigente.

  La primera condición que falla da el código de rechazo. HU-02 reutiliza la misma regla al reservar.

### Reserva · `reservas` · dueño: `Reservas` (HU-02)

| Campo                | Tipo              | Reglas                                                                          |
| -------------------- | ----------------- | ------------------------------------------------------------------------------- |
| `id`                 | bigint            | PK                                                                              |
| `usuario_id`         | integer           | FK → `usuarios` (ciudadano o representante)                                     |
| `instalacion_id`     | integer           | FK → `instalaciones` (`reservable_por_franjas`)                                 |
| `inicio_utc`         | timestamptz       | en punto en hora de Madrid                                                      |
| `fin_utc`            | timestamptz       | `CHECK (fin_utc = inicio_utc + interval '1 hour')`                              |
| `tipo_tarifa`        | TipoTarifa        | la del usuario al reservar (queda fijada)                                       |
| `importe`            | numeric(8,2)      | `> 0`; el precio mostrado y cobrado                                             |
| `estado`             | EstadoReserva     | obligatorio                                                                     |
| `creada_en_utc`      | timestamptz       | marca el inicio del bloqueo de 10 min                                           |
| `codigo_acceso`      | varchar(32) null  | UNIQUE; se genera al confirmar (R14); `CHECK ((estado IN ('pendiente_pago','caducada')) = (codigo_acceso IS NULL))` |
| `cancelada_en_utc`   | timestamptz null  | `CHECK ((estado = 'cancelada') = (cancelada_en_utc IS NOT NULL))`              |
| `motivo_cancelacion` | MotivoCancelacion null | obligatorio si y solo si `estado = 'cancelada'`                            |

**Índices**:

- `ux_reservas_activa` UNIQUE (`instalacion_id`, `inicio_utc`) WHERE
  `estado IN ('pendiente_pago','confirmada')` (RF-014, R7).
- (`usuario_id`, `inicio_utc`) para «Mis reservas».
- (`estado`, `creada_en_utc`) para el barrido de caducidades (R8).

**Máquina de estados** (RF-015 y RF-016, métodos de la entidad):

```text
             Confirmar(codigo, ahora)            Cancelar(motivo, ahora)
pendiente_pago ───────────────────► confirmada ───────────────────────► cancelada  (final)
      │  requiere EstaVigente(ahora)                 motivo=usuario ⇒ ahora < inicio_utc
      │                                              motivo=cierre  ⇒ sin límite de hora
      │ Caducar(ahora)
      └──────────────────► caducada (final)
         pago fallido · tiempo agotado (ahora ≥ creada_en_utc + 10 min) · cierre por mantenimiento
```

- `EstaVigente(ahora)` = `{ ahora < creada_en_utc + 10 min, si estado = pendiente_pago ; true, si estado = confirmada ; false, en otro caso }`.
- Cualquier otra transición (incluida `pendiente_pago → cancelada`) lanza `ErrorNegocio`
  `TRANSICION_NO_PERMITIDA`.
- **Responsables**: HU-02 → `Confirmar`, `Caducar` (pago fallido o tiempo agotado); HU-03 →
  `Cancelar(usuario)`; HU-07 → `Cancelar(cierre)` y `Caducar` por cierre. HU-03 y HU-07 los invocan a
  través de `IServicioReservas` (R4).
- **Al caducar**, el `Pago` pendiente asociado pasa a `fallido` en la misma transacción.

### Pago · `pagos` · dueño: `Reservas` (HU-02)

| Campo              | Tipo             | Reglas                                                        |
| ------------------ | ---------------- | ------------------------------------------------------------- |
| `id`               | bigint           | PK                                                            |
| `reserva_id`       | bigint           | FK → `reservas`; **UNIQUE** (un único pago por reserva)       |
| `importe`          | numeric(8,2)     | = `reservas.importe`                                          |
| `estado`           | EstadoPago       | obligatorio                                                   |
| `creado_en_utc`    | timestamptz      | fecha del pago                                                |
| `resuelto_en_utc`  | timestamptz null | obligatorio si y solo si `estado ≠ 'pendiente'`               |

**Estados**: `pendiente → completado | fallido`. Ambos son finales y no hay reintento (aclaración
2026-10-28). El pago se crea `pendiente` junto con la reserva y lo resuelve
`IPasarelaPago.Cobrar` (R9). Se guarda `resuelto_en_utc` para que la spec-008 decida a qué mes
imputa el ingreso.

### Devolucion · `devoluciones` · dueño: `Cancelaciones` (HU-03, HU-07)

| Campo         | Tipo             | Reglas                                                    |
| ------------- | ---------------- | --------------------------------------------------------- |
| `id`          | bigint           | PK                                                        |
| `pago_id`     | bigint           | FK → `pagos` (en estado `completado`); UNIQUE             |
| `importe`     | numeric(8,2)     | = `pagos.importe` (siempre íntegra)                       |
| `motivo`      | MotivoDevolucion | obligatorio                                               |
| `fecha_utc`   | timestamptz      | obligatorio                                               |

Se registra siempre como completada (RF-021), así que no tiene columna de estado.
`PoliticaDevolucion.HayDevolucion(motivo, inicioUtc, ahora)` =
`{ true, si motivo = cierre_mantenimiento ; inicioUtc − ahora > 24 h, si motivo = cancelacion_usuario }` (RF-019, R5).

### CierreMantenimiento · `cierres_mantenimiento` · dueño: `Cancelaciones` (HU-07)

| Campo                | Tipo         | Reglas                                                   |
| -------------------- | ------------ | -------------------------------------------------------- |
| `id`                 | integer      | PK                                                       |
| `instalacion_id`     | integer      | FK → `instalaciones`                                     |
| `inicio_utc`         | timestamptz  | en punto (hora de Madrid)                                |
| `fin_utc`            | timestamptz  | en punto; `CHECK (fin_utc > inicio_utc)`                 |
| `motivo`             | varchar(500) | obligatorio                                              |
| `registrado_por_id`  | integer      | FK → `usuarios` (personal del centro de la instalación)  |
| `registrado_en_utc`  | timestamptz  | obligatorio                                              |

Una franja está **afectada** por un cierre si `[inicio, fin)` de la franja se solapa con
`[inicio_utc, fin_utc)` del cierre. Al registrarlo, en una sola transacción:
- las reservas confirmadas afectadas pasan a `Cancelar(cierre)` y se crea su devolución íntegra;
- las pendientes de pago pasan a `Caducar` (pago `fallido`);
- las franjas fijas aprobadas afectadas quedan suspendidas (no cambian de estado);
- se envía un aviso a cada titular y a cada representante de club afectados (RF-020).

Que los inicios y fines de cierre caigan en punto es una decisión de diseño para que el cómputo de
horas del informe sea exacto.

### Temporada · `temporadas` · dueño: `Comun` (precargada)

| Campo          | Tipo        | Reglas                                                    |
| -------------- | ----------- | --------------------------------------------------------- |
| `id`           | integer     | PK                                                        |
| `nombre`       | varchar(20) | UNIQUE (por ejemplo, «2026-2027»)                         |
| `fecha_inicio` | date        | 1 de septiembre                                           |
| `fecha_fin`    | date        | 30 de junio del año siguiente; `CHECK (fecha_fin > fecha_inicio)` |

### SolicitudFranjasFijas · `solicitudes_franjas_fijas` · dueño: `Disponibilidad` (HU-05)

| Campo             | Tipo             | Reglas                                                                   |
| ----------------- | ---------------- | ------------------------------------------------------------------------ |
| `id`              | integer          | PK                                                                       |
| `club_id`         | integer          | FK → `clubes`                                                            |
| `instalacion_id`  | integer          | FK → `instalaciones` (pista o pabellón, D2)                              |
| `dias_semana`     | smallint[]       | 1 = lunes … 7 = domingo (ISO); no vacío; sin repetidos                   |
| `hora_inicio`     | time             | en punto, ≥ 08:00                                                        |
| `hora_fin`        | time             | en punto, ≤ 22:00, `> hora_inicio`                                       |
| `temporada_id`    | integer          | FK → `temporadas`                                                        |
| `estado`          | EstadoSolicitud  | obligatorio                                                              |
| `creada_en_utc`   | timestamptz      | obligatorio                                                              |
| `resuelta_en_utc` | timestamptz null | obligatorio si y solo si `estado ≠ 'pendiente'` (fecha de resolución)    |

- **Estados**: `pendiente → aprobada | rechazada`, ambos finales.
- `Aprobar` exige que no haya reservas confirmadas ni pendientes vigentes en conflicto en la
  temporada, y que no se solape con otra solicitud aprobada (RF-022, D8).
- **Bloqueo**: una aprobada bloquea cada franja de la instalación cuya fecha esté en la temporada, cuyo
  día de la semana esté en `dias_semana` y cuya hora esté en `[hora_inicio, hora_fin)`, salvo durante
  un cierre (aclaración 2026-10-28).

### Curso · `cursos` · dueño: `CursosInformes` (precargado; HU-04 lo gestiona)

| Campo            | Tipo        | Reglas                                                |
| ---------------- | ----------- | ----------------------------------------------------- |
| `id`             | integer     | PK                                                    |
| `nombre`         | varchar(100)| obligatorio                                           |
| `deporte_id`     | integer     | FK → `deportes`                                       |
| `instalacion_id` | integer     | FK → `instalaciones` (sala o piscina, D2)             |
| `trimestre`      | varchar(30) | por ejemplo, «Otoño 2026»                             |
| `fecha_inicio`   | date        | primer día del trimestre                              |
| `fecha_fin`      | date        | último día; `> fecha_inicio`                          |
| `dias_semana`    | smallint[]  | ISO 1–7                                               |
| `hora_inicio`    | time        |                                                       |
| `hora_fin`       | time        | `> hora_inicio`                                       |
| `plazas_maximas` | integer     | `> 0`                                                 |

No tiene precio (D10, aclaración 2026-10-28). El trimestre se guarda como nombre más fechas para
poder ordenar y filtrar; es la representación del atributo «trimestre» de la spec, no un dato nuevo.

### Inscripcion · `inscripciones` · dueño: `CursosInformes` (HU-04)

| Campo            | Tipo              | Reglas                                                  |
| ---------------- | ----------------- | ------------------------------------------------------- |
| `id`             | integer           | PK                                                      |
| `usuario_id`     | integer           | FK → `usuarios`                                         |
| `curso_id`       | integer           | FK → `cursos`                                           |
| `estado`         | EstadoInscripcion | obligatorio                                             |
| `llegada_en_utc` | timestamptz       | ordena la lista de espera                               |

- **Posición en la lista de espera** (se calcula): 1 + número de inscripciones en `lista_espera` del
  mismo curso con un `(llegada_en_utc, id)` menor.
- **Estados**: `confirmada | lista_espera → baja`; `lista_espera → confirmada` (promoción del primero
  de la lista, con aviso). `baja` es final.
- **Índice propuesto para la spec-004** (no se crea en la migración `NucleoComun`, principio VIII): UNIQUE (`usuario_id`, `curso_id`)
  WHERE `estado <> 'baja'`.
- La concurrencia se controla con `FOR UPDATE` sobre el curso (R19).

### Notificacion · `notificaciones` · dueño: `Comun` (la crean HU-04, HU-05 y HU-07 vía `IServicioAvisos`)

| Campo        | Tipo          | Reglas                    |
| ------------ | ------------- | ------------------------- |
| `id`         | bigint        | PK                        |
| `usuario_id` | integer       | FK → `usuarios`           |
| `texto`      | varchar(500)  | obligatorio, en español   |
| `fecha_utc`  | timestamptz   | obligatorio               |
| `leida`      | boolean       | por defecto `false`       |

Índice: (`usuario_id`, `fecha_utc` DESC).

### ValidacionAcceso · `validaciones_acceso` · dueño: `Reservas` (HU-06)

| Campo           | Tipo          | Reglas                                                     |
| --------------- | ------------- | ---------------------------------------------------------- |
| `id`            | bigint        | PK                                                         |
| `reserva_id`    | bigint null   | FK → `reservas`; nulo si el código no existe               |
| `codigo_leido`  | varchar(64)   | texto recibido (recortado a 64)                            |
| `aceptada`      | boolean       |                                                            |
| `motivo_rechazo`| varchar(40) null | código de error (por ejemplo, `QR_FUERA_DE_VENTANA`); obligatorio si y solo si `NOT aceptada` |
| `fecha_utc`     | timestamptz   |                                                            |
| `personal_id`   | integer       | FK → `usuarios` (personal de centro)                       |

Cada intento de validación se registra, se acepte o no (RF-018).

---

## Reglas de validación transversales (resumen)

| Regla                                                        | Dónde se aplica                                  | Fuente     |
| ------------------------------------------------------------ | ------------------------------------------------ | ---------- |
| Una reserva activa por instalación y franja                  | índice `ux_reservas_activa` + `ReglaDisponibilidad` | RF-014     |
| Vigencia del pendiente de pago (10 min)                      | `Reserva.EstaVigente`                            | RF-016     |
| Antelación (7 días, fecha local)                             | `ReglaDisponibilidad`                            | RF-011, D3 |
| Devolución (> 24 h reales / cierre siempre)                  | `PoliticaDevolucion`                             | RF-019/020 |
| Ventana del QR (−15 min … fin, ambos incluidos)              | `ReglaAcceso.EsValido`                           | RF-018     |
| Personal solo en su centro                                   | `PermisosCentro.ExigirMismoCentro`               | RF-009, D7 |
| Tarifa por rol / empadronamiento                             | `CalculadoraTarifa`                              | RF-007     |
| Importes con dos decimales y formato `es-ES`                 | `numeric(8,2)`; el cliente formatea con `CultureInfo("es-ES")` | RF-027 |

---

## Datos de inicialización

Todos los datos son ficticios (principio VI). Los inserta `InicializadorDatos` (R13), y la contraseña
de los usuarios sale de `SI18_CLAVE_DEMO`.

- **Centros (4)**: Polideportivo Norte, Polideportivo Sur, Polideportivo Este y Polideportivo Oeste,
  con direcciones inventadas («Calle Ficticia 1» …).
- **Deportes**: tenis, pádel, baloncesto, fútbol sala, natación, yoga y pilates.
- **Instalaciones por centro**: Pista de tenis 1 y 2, Pista de pádel 1 y 2, Pabellón (baloncesto y
  fútbol sala) y Sala polivalente (yoga y pilates). Norte y Sur tienen además Piscina (natación).
  En total, 26 instalaciones.
- **Tarifas (6)**: tenis y pádel × {empadronado 6,00 €, no empadronado 10,00 €, club 4,00 €} (D5).
- **Clubes (2)**: Club Deportivo Ficticio Norte y Club de Pádel Ejemplo.
- **Temporada**: «2026-2027», del 01/09/2026 al 30/06/2027.

**Usuarios** (como mínimo uno por rol y caso; CE-004):

| Correo                                    | Rol                | Empadronado | Club / Centro              |
| ----------------------------------------- | ------------------ | ----------- | -------------------------- |
| `ciudadana.empadronada@si18.test`         | ciudadano          | sí          | —                          |
| `ciudadano.noempadronado@si18.test`       | ciudadano          | no          | —                          |
| `ciudadana2.empadronada@si18.test`        | ciudadano          | sí          | — (para la lista de espera)|
| `representante.norte@si18.test`           | representante_club | sí          | Club Deportivo Ficticio Norte |
| `representante.padel@si18.test`           | representante_club | no          | Club de Pádel Ejemplo      |
| `personal.norte@si18.test` … `personal.oeste@si18.test` | personal_centro | no | un centro cada uno (4) |
| `tecnico@si18.test`                       | tecnico_concejalia | no          | —                          |

**Cursos** («Invierno 2027», del 11/01/2027 al 26/03/2027; es un trimestre futuro, así que la demo
no depende de qué pasa con los cursos ya empezados, que decide la spec-004):

| Curso           | Instalación          | Días y horario       | Plazas                   |
| --------------- | -------------------- | -------------------- | ------------------------ |
| Natación adultos | Piscina Norte       | L y X, 19:00–20:00   | 12                       |
| Yoga             | Sala Sur            | M y J, 10:00–11:00   | 2 (para probar la lista de espera) |
| Pilates          | Sala Este           | V, 18:00–19:00       | 10                       |

No se siembran reservas, pagos ni solicitudes: las crean las pruebas y la demo.
