# Investigación y decisiones: 006 · Validar el acceso con el código QR

**Entrada**: [spec.md](./spec.md) (con las aclaraciones del 2026-11-26), la base común de
[`000-core`](../000-core/plan.md) y lo que ya está en `develop` de [spec-002](../002-reservar-pagar/plan.md).
Las decisiones de 000-core (R1–R19), de spec-001 (D), de spec-002 (E) y de spec-005 (F) **no se
repiten**: aquí solo figuran las que añade HU-06. Se numeran V1–V12 para no confundirlas con ninguna.

No queda ningún «NEEDS CLARIFICATION» en el contexto técnico: la pila, el almacenamiento, las pruebas,
el reloj, el código de acceso, la regla de la ventana y los motivos de rechazo ya los fija 000-core.
**No hace falta ningún código de error nuevo, ni tablas, ni migraciones.**

## Punto de partida: lo que ya existe en `develop`

| Pieza                                                                     | Estado                                  | Dueño |
| ------------------------------------------------------------------------- | --------------------------------------- | ----- |
| `ReglaAcceso.MotivoRechazo(reserva, ahora)`: desconocido → no confirmada → fuera de ventana | Hecho en 000-core, con pruebas de límites | Saul |
| `ValidacionAcceso` (entidad) y `ValidacionAccesoConfig` (tabla `validaciones_acceso`, `CHECK` del motivo, índices por `reserva_id` y `personal_id`) | Hecho en 000-core (migración `20261104144623_NucleoComun`) | Saul |
| `reservas.codigo_acceso` con índice único (`ix_reservas_codigo_acceso`)   | Hecho en 000-core                       | Saul |
| `GeneradorCodigoAcceso` (16 bytes, Base64url, 22 caracteres)              | Hecho en 000-core                       | Saul |
| Códigos `QR_DESCONOCIDO`, `QR_RESERVA_NO_CONFIRMADA`, `QR_FUERA_DE_VENTANA` (422) y `QR_OTRO_CENTRO` (403), con sus textos | Hecho en 000-core (`CodigosError`, `ManejadorErrores`) | Todo el grupo |
| Política `Politicas.EsPersonal` y `IUsuarioActual.CentroId` (claim de `usuarios.centro_id`) | Hecho en 000-core                       | Todo el grupo |
| Prefijo `/api/validaciones-acceso` reservado para Reservas (HU-06)        | contracts/README.md de 000-core         | Saul |
| `CodigoAcceso.razor` (QR en SVG + código en texto) en «Mis reservas»       | Hecho en spec-002                       | Saul |
| Hueco del menú del personal en `MainLayout` («validar QR, cierres»)       | Hecho en 000-core (T056)                | Todo el grupo |
| Módulo `Reservas` registrado en `Program.cs` (`AddReservas`/`MapReservas`) | Hecho en spec-002                       | Saul |

HU-06, por tanto, compone lo que hay en un servicio, un endpoint y una pantalla.

## V1. Un único endpoint: `POST /api/validaciones-acceso`

- **Decisión**: `POST /api/validaciones-acceso` con `{ codigo }`, en el prefijo que 000-core reservó a
  Reservas para HU-06. Cada llamada **crea una Validación de acceso** (aceptada o rechazada). Sin
  `GET`: no hay pantalla de historial (spec, «Supuestos»; principio VIII).
- **Motivo**: RF-003, RF-005 y RF-008. Es `POST` porque registra un intento: no es idempotente ni se
  puede cachear, y el código no viaja en la URL (no queda en registros de acceso ni en el historial del
  navegador).
- **Alternativas descartadas**: `GET /api/reservas/por-codigo/{codigo}` (pone el código en la URL y un
  `GET` no debe crear registros); dos llamadas, consultar y luego registrar (la decisión y el registro
  podrían divergir, contra RF-009).

## V2. Forma de la respuesta: 201 si se acepta, Problem Details del catálogo si se rechaza

- **Decisión**:
  - **Aceptada** → `201 Created` con `ValidacionAccesoDto` (titular, instalación, centro, fecha,
    horas y, si la hay, la hora de la validación aceptada anterior; RF-006, RF-006a).
  - **Rechazada** → Problem Details con el código del catálogo **y su estado HTTP de 000-core**
    (`QR_DESCONOCIDO`, `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA` → 422; `QR_OTRO_CENTRO` →
    403), construido con `ManejadorErrores.Crear(codigo, httpContext)`, más dos extensiones:
    - `validacionId`: la validación registrada (siempre);
    - `reserva`: `{ instalacion, centro, fecha, horaInicio, horaFin }`, **solo** con
      `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA`; nunca el titular (RF-007, aclaración 3).
  - El registro se guarda **antes** de responder, igual que en spec-002 «los rechazos guardan antes el
    cambio de estado» (V4).
- **Motivo**: principio IV y contracts/errores.md: los `QR_*` ya tienen estado HTTP en el catálogo y el
  cliente decide «solo por `codigo`». Respetarlo evita dos formas de rechazo en la API.
- **Consecuencia para el cliente — punto a coordinar C2**: `ClienteApi` convierte hoy los Problem Details
  en `ErrorApi` con `Codigo`, `Detalle`, `Estado` y `Errores`, pero **descarta las demás extensiones**.
  Se propone un cambio aditivo en `SI18.Cliente/Comun` (código común): `ErrorApi.Extensiones`
  (`IReadOnlyDictionary<string, JsonElement>`), rellenado por `LeerProblemaAsync`. No cambia ningún
  comportamiento existente; revisión de @ara139-ua.
- **Alternativas descartadas**:
  - `200` con `{ aceptada, motivo, mensaje, … }` para todo (no toca código común, pero contradice el
    estado HTTP que el catálogo de 000-core fija para los `QR_*` y crea una segunda forma de devolver un
    rechazo; habría que cambiar errores.md en 000-core);
  - un `HttpClient` propio en la pantalla de Reservas que lea el Problem Details (duplica la
    redirección a «Iniciar sesión» con 401 y el resto de `ClienteApi`).

## V3. Decisión en `ServicioValidarAcceso` (Reservas), con la regla de 000-core

- **Decisión**: `Modulos/Reservas/ServicioValidarAcceso.cs`. Con `ahora` leído **una vez** de
  `TimeProvider`:
  1. busca la reserva por `codigo_acceso` (índice único), con su instalación y su centro y el nombre del
     titular (`AsNoTracking`);
  2. si no existe → `QR_DESCONOCIDO`;
  3. si `instalacion.CentroId ≠ IUsuarioActual.CentroId` → `QR_OTRO_CENTRO`;
  4. si no, `ReglaAcceso.MotivoRechazo(reserva, ahora)` → `QR_RESERVA_NO_CONFIRMADA`,
     `QR_FUERA_DE_VENTANA` o `null` (aceptada).

  Es exactamente el orden de RF-005: la regla de 000-core ya devuelve primero «desconocido», luego «no
  confirmada» y luego «fuera de ventana»; HU-06 solo intercala la comprobación de centro entre la 1.ª
  y la 2.ª, como dice el propio comentario de `ReglaAcceso` («la comprobación … `QR_OTRO_CENTRO` es de la
  spec-006»).
- **No se usa `PermisosCentro.ExigirMismoCentro`**: lanza `OTRO_CENTRO` (otro código, pensado para
  acciones sobre instalaciones) y cortaría la petición sin registrar la validación (RF-008).
- **Motivo**: RF-005; principio III (la regla vive en el backend, en un único sitio, y reutiliza la de
  000-core sin redefinirla).
- **Alternativa — punto a coordinar C3 (opcional)**: mover la comprobación de centro a 000-core como
  `ReglaAcceso.MotivoRechazo(reserva, centroPersonal, centroInstalacion, ahora)`. Cambia una firma de
  contracts/modulos.md y las pruebas de dominio de 000-core, para un único consumidor. **No se propone
  ahora** (principio VIII); queda anotado por si otra historia necesitara la misma regla.

## V4. Decisión y registro en la misma operación

- **Decisión**: el servicio crea la `ValidacionAcceso` (`reserva_id` si el código existe; `codigo_leido`
  recortado por la propia entidad a 64; `aceptada`; `motivo_rechazo`; `fecha_utc = ahora`;
  `personal_id = IUsuarioActual.Id`) y hace **un solo `SaveChanges`** antes de devolver el resultado. Si
  falla, la petición termina en 500 `ERROR_INTERNO` y no se muestra ningún resultado (RF-009).
- **Sin transacción explícita ni `FOR UPDATE`**: solo hay una escritura (la validación) y la reserva no se
  modifica (RF-010). El `SaveChanges` único ya es atómico.
- **Motivo**: RF-008–RF-010 y RF-018 de 000-core.
- **Alternativas descartadas**: registrar en segundo plano o después de responder (se podría mostrar un
  resultado sin registro); bloquear la reserva (no se escribe en ella).

## V5. Aviso de validación anterior (RF-006a), sin estado nuevo

- **Decisión**: solo si el resultado es **aceptado**, y **antes** de insertar la nueva validación, el
  servicio consulta
  `MAX(fecha_utc) FROM validaciones_acceso WHERE reserva_id = @id AND aceptada` (índice
  `ix_validaciones_acceso_reserva_id`). Si hay valor, el DTO lleva `validacionAnteriorUtc` y
  `validacionAnteriorHora` (`HH:mm` de Madrid). Cuentan las de cualquier persona del personal; las
  rechazadas no cuentan.
- **Concurrencia**: dos validaciones simultáneas del mismo código se aceptan las dos y quizá ninguna
  avise. Se acepta: el aviso es una ayuda, no un control (aclaración 2), y bloquear para garantizarlo
  sería alcance de más (principio VIII).
- **Motivo**: aclaración 2 y RF-006a; sin cambiar el modelo ni el catálogo de 000-core.
- **Alternativas descartadas**: un estado «utilizada» en `Reserva` (cambia 000-core y la aclaración lo
  descarta); guardar la hora anterior en la propia validación (dato derivable, duplicado).

## V6. Normalización y formato del código

- **Decisión**: el endpoint quita los espacios del principio y del final (`Trim`). Si el código falta o
  queda vacío → **400 `PETICION_INVALIDA`** con `errors.codigo = ["Escribe el código de acceso."]` y
  **no se registra** nada (RF-004; spec, «Casos límite»). Cualquier otro texto, aunque sea largo o tenga
  caracteres raros, se busca tal cual y, si no existe, es `QR_DESCONOCIDO` (se registra recortado a 64).
  No se cambian mayúsculas ni minúsculas: el código es Base64url y distingue entre ellas.
- **Motivo**: RF-004 y casos límite de la spec; el cliente también impide enviar el campo vacío, pero la
  regla de verdad está en la API (principio III).
- **Alternativas descartadas**: rechazar con 400 los códigos que no tengan 22 caracteres (revelaría el
  formato y dejaría sin registrar intentos que la spec quiere registrados).

## V7. Quién valida y cuál es su centro

- **Decisión**: el grupo `/api/validaciones-acceso` exige `Politicas.EsPersonal` (000-core). Sin sesión →
  401 `NO_AUTENTICADO`; con otro rol → 403 `ROL_NO_PERMITIDO`. En los dos casos la petición no llega al
  servicio y **no se registra** nada (RF-001; CE-004). El centro del personal es `IUsuarioActual.CentroId`,
  que la sesión toma de `usuarios.centro_id` al iniciar sesión (RF-002; RF-008 de 000-core). El `CHECK`
  de `usuarios` garantiza que no es nulo para ese rol.
- **Motivo**: RF-001 y RF-002; «Roles y permisos» de 000-core.
- **Alternativas descartadas**: pedir el centro en la petición (lo podría falsear el cliente).

## V8. Fechas y horas en hora de Madrid, sin literales

- **Decisión**: la API convierte `inicio_utc`, `fin_utc` y la hora de la validación anterior con
  `HorarioMadrid.ALocal` (que usa `ValoresNegocio.ZonaHoraria`) y manda `fecha` (`yyyy-MM-dd`) y horas
  `HH:mm`, como `ReservaDto` de spec-002. Ni la API ni el cliente escriben `"Europe/Madrid"`; el cliente
  solo da formato a la fecha con `TextosReservas.DiaYFecha`. La ventana de 15 minutos la evalúa
  `ReglaAcceso` con instantes UTC, así que es exacta también los días de cambio de hora.
- **Motivo**: principio IX y RF-017; lección de la convergencia de 000-core (F2 → T078).
- **Alternativas descartadas**: mandar instantes UTC y convertir en el cliente (repite la zona en el
  cliente, como el caso de `BuscarPistas.razor:229` que se avisó a Artur).

## V9. DTO compartidos

- **Decisión**: en `SI18.Contratos/Reservas/` (dueño: Saul): `PeticionValidacionAcceso`,
  `ValidacionAccesoDto` (aceptada) y `ReservaValidadaDto` (el contenido de la extensión `reserva` de un
  rechazo). Detalle en [data-model.md](./data-model.md) § 3.
- **Motivo**: contracts/README.md de 000-core («Los DTO están en `SI18.Contratos/<Modulo>/`»), igual que
  E12 de spec-002.
- **Alternativas descartadas**: devolver la entidad `Reserva` (expone el código y el usuario).

## V10. Pantalla `/validar-acceso`

- **Decisión**: `SI18.Cliente/Reservas/ValidarAcceso.razor` con su `ValidarAcceso.razor.css` (sin tocar
  `app.css`):
  - **Acceso por URL** como en spec-002: sin sesión, a `/iniciar-sesion`; con otro rol, aviso sin
    formulario. Se añade `AccesoReservas.ComprobarPersonal` junto al `Comprobar` que ya existe.
  - **Formulario**: `MudTextField` «Código de acceso» con etiqueta visible, texto de ayuda («El código
    que aparece bajo el QR»), `autocomplete="off"`, `autocapitalize="none"`, `spellcheck="false"` y
    botón «Validar». Error de campo vacío con el **patrón de `PagarReserva.razor`** (MudBlazor 8.15):
    `<span id="error-codigo" role="alert" class="solo-lector">` y `aria-errormessage="error-codigo"` en
    `UserAttributes` solo mientras hay error.
  - **Resultado** (en la misma página, debajo del formulario, sin cambiar de ruta):
    - aceptada: encabezado «Acceso permitido» con icono, y una lista de definiciones con titular,
      instalación y centro, fecha (`TextosReservas.DiaYFecha`) y «de HH:mm a HH:mm»; si hay validación
      anterior, un aviso con icono: «Este código ya se validó hoy a las HH:mm» si la anterior es del mismo
      día de Madrid, y «Este código se validó por última vez el dd/MM/yyyy a las HH:mm» si no
      (`TextosReservas.AvisoValidacionAnterior`, T032; documentado en T035);
    - rechazada: encabezado «Acceso denegado» con icono distinto, el `detail` del catálogo y, si la
      extensión `reserva` viene, instalación, centro, fecha y horas (nunca titular);
    - el color (verde / rojo del tema) solo refuerza; el texto y el icono bastan (RF-013).
  - **Anuncio y foco**: una frase completa se anuncia **una vez** con `MensajeAccesible` («Acceso
    permitido. Carla Vecina, Pista de pádel 2, Polideportivo Este, de 10:00 a 11:00. Este código ya se
    validó hoy a las 09:50.»); el campo se vacía y el **foco vuelve al campo** para el siguiente
    (RF-014). No se mueve el foco al resultado: el personal valida en cadena.
  - **Móvil**: una sola columna; campo y botón al 100 % del ancho por debajo de 600 px; sin
    desplazamiento horizontal a 320 px (RF-015).
  - **Menú — punto a coordinar C1**: «Validar acceso» (`/validar-acceso`) en el hueco del personal de
    `MainLayout.razor` (código común, revisión de @ara139-ua), igual que «Mis reservas» en spec-002.
- **Motivo**: RF-003, RF-006, RF-006a, RF-007, RF-013–RF-017 y HU-5 de la spec, con los componentes de
  000-core (`MensajeAccesible`, `ClienteApi`) y los de Reservas de spec-002 (`TextosReservas`,
  `AccesoReservas`).
- **Alternativas descartadas**: lectura con cámara (aclaración 1); mover el foco al resultado (obliga a
  volver al campo con el teclado en cada validación); un diálogo modal (peor con lector de pantalla).

## V11. Pruebas

- **Decisión**:
  - **Dominio**: `ReglaAcceso` ya tiene pruebas de límites en 000-core (`ReglaAccesoTests`: −15 min,
    −16 min, fin, fin + 1 min). No se duplican.
  - **Integración** (`tests/SI18.Api.Tests/Reservas/`, con `FabricaApi` y `DatosReservas` de spec-002, sin
    cambiarlo):
    - `ValidarAccesoTests` (HTTP): 401 sin sesión, 403 `ROL_NO_PERMITIDO` con ciudadano, representante y
      técnico, y **ninguna fila nueva** en `validaciones_acceso` (CE-004); 400 con código vacío o de
      espacios, sin registro; aceptada → 201 con titular, instalación, centro, fecha y horas, y su fila;
      `QR_DESCONOCIDO` → 422, sin extensión `reserva`, fila con `reserva_id` nulo; `QR_OTRO_CENTRO` →
      403, sin extensión `reserva`, fila con su `reserva_id`; cancelada → 422
      `QR_RESERVA_NO_CONFIRMADA` con la extensión `reserva` sin titular; espacios al principio y al final
      se ignoran; un código de 100 caracteres se registra recortado a 64.
    - `LimitesValidacionTests` **`[Trait("Categoria", "Limites")]`**, con el servicio y **reloj propio**
      (`DatosValidaciones.ConServicio(ahoraUtc, personal, …)`, como `DatosReservas.ConServicio`):
      reserva de 10:00 a 11:00 → 09:45:00 aceptada, 09:46 aceptada, 09:44:59 rechazada
      (`QR_FUERA_DE_VENTANA`), 11:00:00 aceptada, 11:00:01 y 11:01 rechazadas; los dos días de cambio de
      hora (25/10/2026 y 28/03/2027): una reserva a las 10:00 de Madrid se acepta a las 09:45:00 de Madrid y
      se rechaza a las 09:44:59 (15 min **reales**, CE-002).
    - `AvisoValidacionAnteriorTests` (RF-006a, CE-007): primera aceptada sin aviso; segunda con la hora de
      la primera; tercera con la de la segunda (la más reciente); rechazos previos no cuentan; una
      aceptada por otra persona del personal del mismo centro sí cuenta.
  - **Accesibilidad**: comprobación manual de quickstart §5 y Lighthouse/axe, como T073 de 000-core.
- **Datos de las pruebas (lección del #37)**: `FabricaApi` comparte base de datos y reloj, y las 16 pistas
  ya las usa alguna prueba. Para no pisar a nadie, HU-06 usa **titular, personal, pista, días y horas que
  no usa ninguna otra prueba** (comprobado con `git grep` el 2026-11-26):
  - titular: `ciudadana2.empadronada@si18.test` (Carla; ninguna prueba la usa);
  - personal: `personal.este@si18.test` (ninguna prueba lo usa); para «otro centro»,
    `personal.oeste@si18.test` (sus validaciones no chocan con nada);
  - pista: «Pista de pádel 2» de **Polideportivo Este** (solo la usan una prueba del 24/12/2026 con
    cierre y franja fija, y una solicitud de franja fija de los miércoles 19–21 que se **rechaza**);
  - fechas: del **16 al 31/01/2027** a las 10:00, **una fecha por reserva** para no chocar con
    `ux_reservas_activa` ni depender del orden de ejecución (18, 20, 21, 22 y 25/01/2027; la asignación
    está en tasks.md, «Datos de las pruebas de integración»; ninguna prueba usa enero entre el 16 y el
    31, y la franja fija de los miércoles de esa pista es de 19 a 21); los cambios de hora,
    **25/10/2026** y **28/03/2027** a las **10:00** (las demás pruebas de esos días usan otras pistas);
  - las pruebas HTTP que necesitan una reserva dentro de su ventana con el reloj compartido usan **una
    sola** reserva confirmada, creada de forma idempotente en la hora en punto actual de la fábrica
    (21/12/2026 a las 18:00 de Madrid) en esa misma pista, que nadie usa ese día (ajuste de
    analyze-1, C1).

  Antes de escribir las pruebas, la tarea correspondiente repite el `git grep` y anota en tasks.md si
  algo ha cambiado. Ninguna prueba de esta spec adelanta `fabrica.Reloj` (E13 de spec-002) ni cuenta
  filas globales de `validaciones_acceso`: comprueba por `validacionId` o por `reserva_id`.
- **Motivo**: principio II y CE-002–CE-007 de la spec.
- **Alternativas descartadas**: pruebas E2E con navegador (R16 de 000-core: no se piden).

## V12. Rendimiento y escala

- **Decisión**: sin caché. Una validación hace una consulta por índice único (`codigo_acceso`) con dos
  uniones pequeñas, una agregación por índice (`reserva_id`) solo si se acepta, y un `INSERT`. El
  objetivo es el de 000-core para el QR (< 300 ms p95, plan.md «Objetivos de rendimiento»), muy por
  debajo de CE-001 (< 1 s).
- **Motivo**: escala municipal; principio VIII.
- **Alternativas descartadas**: limitar intentos por persona (spec, «Supuestos»: no se pide; 128 bits no
  se adivinan y todo queda registrado).
