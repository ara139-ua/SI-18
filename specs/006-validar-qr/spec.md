# Especificación: 006 · Validar el acceso con el código QR (HU-06)

**Rama de trabajo**: `feature/006-validar-qr`

**Creada**: 2026-11-26

**Estado**: Borrador

**Responsable**: Saul (módulo `Reservas`, `docs/agrupamiento.md`)

**Entrada**: Descripción del usuario: "Spec número 006, nombre corto "validar-qr". Historia HU-06. Como personal del centro, quiero validar el código QR que muestra el usuario en su móvil, para dejar pasar solo a quien tiene una reserva válida. Detalles: el QR está asociado a una reserva concreta y contiene un código opaco que no permite adivinar otras reservas; el personal lo lee con la cámara del móvil o introduce el código a mano en una pantalla de validación; el código es válido desde 15 minutos antes del inicio de la reserva y hasta su fin; fuera de ese intervalo, si la reserva está cancelada o caducada, si el código no existe o si la reserva es de otro centro, se rechaza indicando el motivo; si se acepta, se muestra el nombre del titular, la instalación y la franja; cada validación (aceptada o rechazada) queda registrada; el resultado debe ser claro y rápido para el personal. Criterios de aceptación: un QR leído dentro de la ventana de validez se acepta; un QR leído más de 15 minutos antes del inicio, o después del fin, se rechaza (probar los dos lados de cada límite: 15 min y 14 min antes, justo al fin y un minuto después); un QR de una reserva cancelada se rechaza; un código inexistente se rechaza; solo el personal de centro puede validar; la pantalla de validación es accesible y legible desde el móvil (WCAG AA): el resultado aceptado o rechazado se distingue con texto e icono, no solo con color, y se anuncia a lectores de pantalla. Dependencias: valida reservas confirmadas y QR generados por HU-02 (spec-002); las cancelaciones de HU-03 (spec-003) y HU-07 (spec-007) deben invalidar el código. Entidad Validación de acceso, estados de Reserva y ventana del QR según specs/000-core; no los redefinas. Fuera de alcance: control de aforo, validar inscripciones a cursos. Todo en español. Contexto actual del repositorio: specs/000-core ya define e implementa la parte de dominio de esta historia (ReglaAcceso con la ventana de 15 minutos antes del inicio hasta el fin, ValidacionAcceso con aceptada y motivo_rechazo, GeneradorCodigoAcceso y la tabla validaciones_acceso), y spec-002 ya muestra el QR (CodigoAcceso.razor) en "Mis reservas". Usa esas entidades, motivos y valores; no los redefinas. Si falta algo en 000-core para esta historia, anótalo como punto a coordinar en lugar de definirlo aquí. Tiene que quedar claro qué se considera "centro del personal" y cómo se asocia un usuario de personal a su centro según 000-core. La lectura con cámara es deseable, pero la introducción manual del código es obligatoria; si la cámara complica el alcance, déjalo para clarify."

## Alcance de esta spec

HU-06 permite al **personal de un centro** comprobar, en la entrada, si el código QR que enseña una
persona corresponde a una reserva que le da acceso en ese momento y en ese centro. El personal ve un
resultado claro (aceptado o rechazado, con el motivo) y cada intento queda registrado.

Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa tal cual:

| Concepto                                                   | Dónde está definido en `000-core`                                          |
| ---------------------------------------------------------- | -------------------------------------------------------------------------- |
| Código de acceso opaco y aleatorio (se genera al confirmar) | spec.md RF-017; research.md R14; data-model.md § Reserva (`codigo_acceso`) |
| Ventana de validez: de 15 min antes del inicio hasta el fin, ambos incluidos | spec.md «Valores de negocio» y RF-018; data-model.md (`ReglaAcceso`) |
| Motivos de rechazo y sus textos                            | contracts/errores.md: `QR_DESCONOCIDO`, `QR_RESERVA_NO_CONFIRMADA`, `QR_FUERA_DE_VENTANA`, `QR_OTRO_CENTRO` |
| Estados de Reserva (solo `confirmada` da acceso)           | spec.md RF-015 y «Estados»; data-model.md § Reserva                        |
| Registro de cada intento (Validación de acceso)            | spec.md «Entidades clave»; data-model.md § ValidacionAcceso               |
| Quién valida: personal de centro, solo en su centro        | spec.md «Roles y permisos», RF-008, RF-009, D7; edge case «QR de otro centro» |
| Centro del personal y centro de la reserva                 | data-model.md § Usuario (`centro_id`) y § Instalacion (`centro_id`)      |
| Zona horaria `Europe/Madrid`, español                      | spec.md RF-026; constitución, principio IX                                 |

### Qué es el «centro del personal»

- Según `000-core` (RF-008, data-model.md § Usuario), cada usuario con rol **personal de centro** está
  asignado a **exactamente un centro**: su campo «centro de trabajo» es obligatorio para ese rol y está
  vacío para los demás. La asignación es **precargada** (no hay pantallas para cambiarla) y se conoce
  al iniciar sesión, junto con el rol.
- El **centro de una reserva** es el centro de la instalación reservada.
- Un código es «de otro centro» cuando el centro de la instalación de su reserva no coincide con el
  centro del personal que valida.

**Fuera de alcance**: control de aforo; validar inscripciones a cursos; generar o mostrar el QR (lo
hace spec-002); cancelar reservas (spec-003) o cerrar instalaciones (spec-007); pantallas para consultar
o exportar el historial de validaciones; cambiar el centro asignado a una persona del personal; leer
el QR con la cámara (aclaración 2026-11-26).

## Clarifications

### Session 2026-11-26

- Q: ¿La lectura con la cámara del móvil entra en esta spec, o se deja fuera y solo se ofrece la
  introducción manual del código? → A: Solo introducción manual; la cámara queda fuera de alcance.
  - **Justificación**: la descripción del sistema solo dice que el personal «valida» el QR que el
    usuario muestra en el móvil, sin mencionar la cámara; la entrada de la historia la daba como
    alternativa («o» introduce el código a mano); spec-002 ya muestra el código en texto bajo el QR; y
    es la opción más simple (principio VIII).
  - **Historia afectada**: HU-1 (escenario 5), «Fuera de alcance», RF-003 y «Supuestos».
- Q: ¿Un código ya aceptado se vuelve a aceptar en cada validación dentro de su ventana, o solo la
  primera vez? → A: Se acepta siempre dentro de la ventana, pero si esa reserva ya tiene una validación
  aceptada, el resultado muestra además un aviso con la hora de la anterior.
  - **Justificación**: la descripción dice que el código es válido «durante el tiempo que dura» la
    reserva, así que no se puede rechazar una segunda lectura; el aviso ayuda a detectar una captura
    compartida sin bloquear el acceso y sin crear un motivo nuevo en `000-core`, porque se calcula con
    las validaciones ya registradas.
  - **Historia afectada**: HU-1 (escenarios 6 y 7), RF-006a, «Casos límite» y «Supuestos».
- Q: Cuando se rechaza un código de una reserva del mismo centro porque está cancelada o porque se lee
  fuera de su ventana, ¿se muestran al personal la instalación y la franja de esa reserva? → A: Sí, en
  los dos casos se muestran la instalación y la franja (fecha y horas), nunca el titular.
  - **Justificación**: con la franja el personal puede indicar cuándo volver (fuera de la ventana) y
    explicar una cancelación que el usuario quizá no conoce, por ejemplo por un cierre por
    mantenimiento de HU-07; nunca se muestra el titular y solo ocurre con reservas del mismo centro,
    así que solo se expone lo que ya sabe quien enseña el código.
  - **Historia afectada**: HU-2 (escenarios 1, 2 y 3), RF-007 y CE-003.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Validar un código y saber al momento si la persona puede pasar (Prioridad: P1)

Como persona del personal de un centro, quiero introducir el código de acceso que me enseña quien
llega y ver al momento si se acepta, con el nombre del titular, la instalación y la franja, para dejar
pasar solo a quien tiene una reserva válida.

**Por qué esta prioridad**: es el núcleo de la historia; sin ella el QR que genera HU-02 no sirve
para nada en la entrada.

**Prueba independiente**: con una reserva confirmada de una pista del centro de la persona del
personal, se introduce su código dentro de la ventana y se comprueba que se acepta y que se ven el
titular, la instalación y la franja.

**Escenarios de aceptación**:

1. **Dado** una reserva confirmada de 18:00 a 19:00 en una pista del centro del personal, **cuando** el
   personal introduce su código a las 17:45, **entonces** se acepta y se muestran el nombre del
   titular, la instalación y la franja «18:00–19:00» con su fecha.
2. **Dado** la misma reserva, **cuando** se introduce su código a las 19:00 exactas, **entonces** se
   acepta.
3. **Dado** que el código se acepta, **cuando** el personal mira la pantalla, **entonces** el resultado
   se distingue por un texto («Acceso permitido») y un icono, no solo por el color, y el lector de
   pantalla lo anuncia.
4. **Dado** que se ha mostrado un resultado, **cuando** el personal quiere validar al siguiente,
   **entonces** puede introducir otro código sin salir de la pantalla.
5. **Dado** una persona del personal en la pantalla de validación, **cuando** la abre, **entonces**
   encuentra un campo para escribir el código y un botón para validarlo, y la aplicación no le pide
   permiso para usar la cámara (aclaración 2026-11-26).
6. **Dado** una reserva confirmada cuyo código ya se aceptó a las 17:50, **cuando** se vuelve a validar
   a las 18:20, dentro de su ventana, **entonces** se acepta otra vez y el resultado muestra además el
   aviso «Este código ya se validó hoy a las 17:50», con texto e icono, que el lector de pantalla
   anuncia junto con el resultado.
7. **Dado** una reserva cuyo código se validó antes solo con rechazos (por ejemplo, fuera de la
   ventana), **cuando** se acepta por primera vez, **entonces** no se muestra ningún aviso de validación
   anterior.

---

### Historia de usuario 2 - Rechazar con un motivo claro los códigos que no dan acceso (Prioridad: P1)

Como persona del personal, quiero que un código que no da acceso se rechace diciéndome por qué, para
explicárselo a la persona que intenta entrar.

**Por qué esta prioridad**: dejar pasar a alguien sin reserva válida es justo lo que la historia quiere
evitar; el motivo evita discusiones en la entrada.

**Prueba independiente**: se introduce un código en cada situación de rechazo y se comprueba que se
rechaza con el motivo de `000-core` correspondiente.

**Escenarios de aceptación**:

1. **Dado** una reserva confirmada del centro del personal que empieza a las 18:00, **cuando** se
   introduce su código a las 17:44, **entonces** se rechaza con el motivo «El código solo es válido
   desde 15 minutos antes del inicio hasta el final de la reserva» y se muestran la instalación y la
   franja «18:00–19:00» con su fecha, pero no el titular (aclaración 2026-11-26).
2. **Dado** la misma reserva (fin a las 19:00), **cuando** se introduce su código a las 19:01,
   **entonces** se rechaza con el mismo motivo y se muestran la instalación y la franja, pero no el
   titular.
3. **Dado** una reserva del centro del personal que estaba confirmada y se ha cancelado (por su titular
   o por un cierre por mantenimiento), **cuando** se introduce su código dentro de lo que era su
   ventana, **entonces** se rechaza indicando que la reserva no está confirmada y se muestran la
   instalación y la franja, pero no el titular (aclaración 2026-11-26).
4. **Dado** un código que no corresponde a ninguna reserva, **cuando** se introduce, **entonces** se
   rechaza con «Código no válido» y no se muestra ningún dato de ninguna reserva.
5. **Dado** una reserva confirmada de una pista de otro centro, **cuando** el personal la valida dentro
   de su ventana, **entonces** se rechaza indicando que el código pertenece a una reserva de otro
   centro, sin mostrar el titular ni la franja.
6. **Dado** un rechazo, **cuando** el personal mira la pantalla, **entonces** se distingue del
   aceptado por el texto («Acceso denegado» y el motivo) y por el icono, no solo por el color, y el
   lector de pantalla lo anuncia.

---

### Historia de usuario 3 - Que cada intento de validación quede registrado (Prioridad: P1)

Como responsable del servicio, quiero que cada validación, aceptada o rechazada, quede registrada con
quién la hizo, cuándo, qué código se leyó y el resultado, para tener evidencia de los accesos y de los
intentos fallidos.

**Por qué esta prioridad**: es un requisito de `000-core` (RF-018) y del enunciado; sin registro no hay
forma de comprobar después qué pasó en la entrada.

**Prueba independiente**: se hacen varias validaciones (aceptadas y rechazadas por cada motivo) y se
comprueba que hay un registro por intento con sus datos.

**Escenarios de aceptación**:

1. **Dado** un código aceptado, **cuando** se valida, **entonces** queda un registro con la reserva, el
   código leído, «aceptada», la fecha y hora y la persona del personal.
2. **Dado** un código rechazado por cualquier motivo, **cuando** se valida, **entonces** queda un
   registro con el código leído, «rechazada», el motivo, la fecha y hora y la persona del personal; y,
   si el código corresponde a una reserva, también esa reserva.
3. **Dado** un código que no existe, **cuando** se valida, **entonces** el registro no se asocia a
   ninguna reserva.

---

### Historia de usuario 4 - Solo el personal de centro puede validar (Prioridad: P1)

Como responsable del servicio, quiero que solo el personal de centro pueda validar códigos, para que
nadie más pueda comprobar si un código existe ni ver datos de las reservas.

**Por qué esta prioridad**: es un criterio de aceptación explícito y protege los datos de los
titulares.

**Prueba independiente**: se intenta validar con un usuario de cada rol y sin sesión.

**Escenarios de aceptación**:

1. **Dado** un ciudadano, un representante de club o un técnico de concejalía, **cuando** intenta
   validar un código, **entonces** se rechaza indicando que su perfil no lo permite, y no se registra
   ninguna validación.
2. **Dado** alguien sin sesión iniciada, **cuando** intenta validar, **entonces** se le pide iniciar
   sesión.
3. **Dado** una persona del personal de centro, **cuando** abre la aplicación, **entonces** encuentra
   en el menú el acceso a la pantalla de validación; los demás roles no lo ven.

---

### Historia de usuario 5 - Validar desde el móvil sin barreras de accesibilidad (Prioridad: P2)

Como persona del personal, quiero usar la pantalla de validación desde el móvil, con el teclado o con
un lector de pantalla, para trabajar en la entrada sin depender de un ordenador.

**Por qué esta prioridad**: la accesibilidad es un principio no negociable (I) y el enunciado pide que
la pantalla sea legible desde el móvil; se comprueba sobre la pantalla ya construida.

**Prueba independiente**: se valida un código aceptado y otro rechazado solo con el teclado, con un
lector de pantalla y con la pantalla a 320 px de ancho.

**Escenarios de aceptación**:

1. **Dado** la pantalla de validación a 320 px de ancho, **cuando** se usa, **entonces** el campo, el
   botón y el resultado se ven enteros sin desplazamiento horizontal.
2. **Dado** un usuario de teclado, **cuando** valida un código, **entonces** llega al campo y al botón
   en un orden lógico, con el foco siempre visible, y tras el resultado el foco vuelve al campo del
   código.
3. **Dado** un lector de pantalla, **cuando** se muestra un resultado, **entonces** lo anuncia una sola
   vez, con el texto completo (permitido o denegado y el motivo).

---

### Casos límite

- **Ventana del QR, inicio** (reserva a las 18:00): a las 17:45:00 (15 min antes) se acepta; a las
  17:46 (14 min antes) se acepta; a las 17:44:59 se rechaza (`QR_FUERA_DE_VENTANA`).
- **Ventana del QR, fin** (fin a las 19:00): a las 19:00:00 se acepta; a las 19:00:01 y a las 19:01 se
  rechaza.
- **Cambio de hora**: la ventana se evalúa con los instantes reales de la reserva (guardados en UTC),
  así que una reserva del 25/10/2026 o del 28/03/2027 se acepta exactamente desde 15 minutos reales
  antes de su inicio en hora de Madrid.
- **Reserva cancelada** (por su titular, HU-03, o por un cierre, HU-07): conserva su código, pero se
  rechaza con `QR_RESERVA_NO_CONFIRMADA` aunque se lea dentro de la que era su ventana. No hace falta
  que HU-03 ni HU-07 hagan nada más para invalidar el código: basta con que la reserva deje de estar
  confirmada.
- **Reserva caducada o pendiente de pago**: según `000-core` nunca tienen código de acceso (solo se
  genera al confirmar), así que un código no puede llevar a ellas; cualquier código que no esté en una
  reserva se rechaza con `QR_DESCONOCIDO`.
- **Código de otro centro dentro de su ventana**: se rechaza con `QR_OTRO_CENTRO`.
- **Varios motivos a la vez** (por ejemplo, de otro centro y fuera de la ventana): se muestra uno solo,
  en este orden: código desconocido → otro centro → reserva no confirmada → fuera de la ventana. Así el
  personal de un centro no averigua el estado ni el horario de las reservas de otro centro.
- **Código con espacios** al principio o al final (al copiarlo o dictarlo): se ignoran esos espacios.
- **Campo vacío**: no se envía; se pide escribir el código y no se registra ninguna validación.
- **Código muy largo o con caracteres extraños**: se rechaza como `QR_DESCONOCIDO`; se registra lo
  leído recortado a la longitud máxima de `000-core` (64 caracteres).
- **Mismo código validado varias veces** (aclaración 2026-11-26): dentro de la ventana se acepta cada
  vez (por ejemplo, alguien sale y vuelve a entrar). Si la reserva ya tiene una validación aceptada, el
  resultado avisa de la hora de la más reciente, aunque la hiciera otra persona del personal. Las
  validaciones rechazadas no cuentan para el aviso.
- **Código adivinado o probado al azar**: con 128 bits aleatorios no se puede adivinar (R14); cada
  intento queda registrado como rechazado.
- **Personal sin centro asignado**: no puede darse; `000-core` exige el centro para ese rol.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Quién valida**

- **RF-001**: Solo DEBEN poder validar códigos las personas con rol **personal de centro** (`000-core`,
  «Roles y permisos»). Cualquier otro rol DEBE recibir el rechazo `ROL_NO_PERMITIDO` y una petición
  sin sesión, `NO_AUTENTICADO`; en ninguno de los dos casos se registra una validación.
- **RF-002**: El centro del personal DEBE ser el centro asignado a su usuario en `000-core` (RF-008),
  conocido desde el inicio de sesión. HU-06 no permite elegirlo ni cambiarlo.

**Validar un código**

- **RF-003**: La pantalla de validación DEBE permitir **introducir el código a mano** y validarlo. Es
  la única forma de lectura: la cámara queda fuera de alcance (aclaración 2026-11-26).
- **RF-004**: Antes de validar, se DEBEN quitar los espacios del principio y del final del código. Un
  código vacío NO DEBE enviarse a validar.
- **RF-005**: La decisión DEBE tomarse siempre en el servidor, a partir del código recibido y de la hora
  actual, aplicando en este orden la primera condición que se cumpla:
  1. ninguna reserva tiene ese código → `QR_DESCONOCIDO`;
  2. la instalación de la reserva es de un centro distinto del del personal → `QR_OTRO_CENTRO`;
  3. la reserva no está confirmada → `QR_RESERVA_NO_CONFIRMADA`;
  4. la hora actual está fuera de la ventana de `000-core` (desde 15 minutos antes del inicio hasta el
     fin, ambos incluidos) → `QR_FUERA_DE_VENTANA`;
  5. en otro caso, se **acepta**.

  Las condiciones 1, 3 y 4 son exactamente las de la regla de acceso de `000-core` (RF-018); HU-06
  añade la 2 (D7). No se redefinen la ventana ni los motivos.
- **RF-006**: Si el código se acepta, se DEBEN mostrar el nombre del titular de la reserva, la
  instalación (con su centro) y la franja (fecha y horas de inicio y fin en hora de Madrid).
- **RF-006a**: Si el código se acepta y la reserva ya tenía al menos una validación **aceptada**
  registrada, el resultado DEBE mostrar además un aviso con la fecha y hora (de Madrid) de la más
  reciente. El aviso no cambia el resultado (sigue aceptado), se distingue con texto e icono y se
  anuncia junto con el resultado. Se calcula solo con las validaciones registradas; no hay estado ni
  motivo nuevo en `000-core` (aclaración 2026-11-26).
- **RF-007**: Si el código se rechaza, se DEBE mostrar «Acceso denegado» con el texto del motivo del
  catálogo de `000-core`. Con `QR_DESCONOCIDO` y `QR_OTRO_CENTRO` NO DEBE mostrarse ningún dato de la
  reserva. Con `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA` (que solo pueden darse con reservas
  del centro del personal, RF-005) DEBEN mostrarse la instalación y la franja (fecha y horas), y NO DEBE
  mostrarse el titular, para que el personal pueda explicar el motivo (aclaración 2026-11-26).

**Registro**

- **RF-008**: Cada validación de un código no vacío por una persona del personal, aceptada o rechazada,
  DEBE registrarse como **Validación de acceso** de `000-core`: reserva (si el código existe), código
  leído (recortado a 64 caracteres), aceptada sí/no, motivo de rechazo (si no se acepta), fecha y hora
  y persona del personal.
- **RF-009**: El registro DEBE guardarse en la misma operación que decide el resultado: si no se puede
  registrar, el resultado no se da por aceptado.
- **RF-010**: Validar un código NO DEBE cambiar el estado de la reserva ni ningún otro dato de otras
  historias.

**Dependencias con otras historias**

- **RF-011**: El código que se valida es el que HU-02 genera al confirmar la reserva y muestra en «Mis
  reservas» (spec-002). HU-06 no genera ni muestra códigos.
- **RF-012**: Una reserva cancelada por HU-03 o por un cierre de HU-07 DEBE quedar rechazada en cuanto
  deja de estar confirmada (RF-005, condición 3), sin ninguna acción adicional de esas historias.

**Mensajes, accesibilidad e idioma**

- **RF-013**: El resultado aceptado y el rechazado DEBEN distinguirse por texto e icono, además del
  color, y DEBEN anunciarse a los lectores de pantalla una sola vez (constitución, principio I).
- **RF-014**: La pantalla DEBE poder usarse entera con el teclado, con el foco visible, y tras mostrar
  un resultado el foco DEBE volver al campo del código para validar el siguiente.
- **RF-015**: La pantalla DEBE verse completa y usarse sin desplazamiento horizontal desde 320 px de
  ancho.
- **RF-016**: La pantalla de validación DEBE aparecer en el menú solo para el personal de centro.
- **RF-017**: Todos los textos DEBEN estar en español, y las fechas y horas en hora de Madrid con
  formato español.

### Entidades clave

HU-06 no crea entidades nuevas ni cambia las de `000-core` (spec.md, «Entidades clave»; data-model.md):

- **Validación de acceso** (propietaria: HU-06): la crea en cada intento. Guarda la reserva (si
  existe), el código leído, si se aceptó, el motivo de rechazo, la fecha y hora y la persona del
  personal.
- **Reserva** (propietaria: HU-02): se lee por su código de acceso; se usan su estado, su instalación y
  su inicio y fin. HU-06 no la modifica.
- **Instalación y Centro**: precargados; dan el centro de la reserva y los nombres que se muestran.
- **Usuario**: el titular (se muestra su nombre) y la persona del personal (su rol y su centro
  asignado).

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El personal ve el resultado de una validación en menos de 1 segundo en el 95 % de los
  intentos, desde que confirma el código.
- **CE-002**: En el 100 % de los casos de prueba de los límites (15 y 14 minutos antes del inicio,
  1 segundo antes de la ventana, justo al fin, 1 segundo y 1 minuto después, y los dos días de cambio
  de hora), el resultado es el esperado.
- **CE-003**: El 100 % de los códigos de reservas canceladas, inexistentes o de otro centro se rechazan
  con su motivo; ningún rechazo muestra el titular, los de códigos inexistentes o de otro centro no
  muestran ningún dato de la reserva, y los de reservas canceladas o fuera de la ventana muestran
  siempre la instalación y la franja.
- **CE-004**: El 100 % de los intentos de validación del personal quedan registrados con su resultado,
  y ningún intento de otro rol o sin sesión queda registrado ni recibe datos de una reserva.
- **CE-005**: Una persona del personal valida un código introducido a mano en menos de 15 segundos,
  contando desde que lo tiene delante.
- **CE-006**: La validación se completa entera solo con el teclado y a 320 px de ancho, la revisión
  automática de accesibilidad (Lighthouse) da 95 o más, la revisión con axe no encuentra infracciones
  graves ni críticas y un lector de pantalla anuncia cada resultado.
- **CE-007**: En el 100 % de los casos de prueba, una validación aceptada de una reserva que ya tenía
  otra aceptada muestra el aviso con la hora correcta de la más reciente, y una primera validación
  aceptada no muestra ningún aviso (aclaración 2026-11-26).

## Supuestos

- **Solo introducción manual** (aclaración 2026-11-26): el código se puede leer en voz alta o copiar,
  porque spec-002 lo muestra también como texto bajo el QR (22 caracteres). La cámara queda fuera de
  alcance; si más adelante se quisiera, se propondría como cambio de esta spec.
- **Orden de los motivos**: `000-core` fija los motivos pero no su orden cuando hay varios; se elige el
  de RF-005 para no revelar al personal de un centro datos de reservas de otro (spec.md de `000-core`,
  caso límite «QR de otro centro»).
- **Sin historial en pantalla**: el enunciado pide que cada validación «quede registrada», no
  consultarla; no se construye una pantalla de historial (principio VIII). El registro sirve como
  evidencia en la base de datos y en la demo.
- **Sin estado «usado»** (aclaración 2026-11-26): `000-core` no tiene un estado de reserva
  «utilizada» y no hace falta. Un código se acepta cada vez que se valida dentro de su ventana; el aviso
  de validación anterior (RF-006a) se obtiene de las validaciones ya registradas, sin cambiar el modelo
  ni el catálogo de motivos de `000-core`.
- **Sin límite de intentos**: no se limita el número de validaciones por persona del personal; los
  códigos no se pueden adivinar (128 bits, R14) y cada intento queda registrado (principio VIII).
- **Datos de demostración**: se usan los centros, pistas y usuarios ficticios de `000-core`, con una
  persona del personal por centro (`personal.norte@si18.test` … `personal.oeste@si18.test`); las
  reservas confirmadas las crean HU-02, las pruebas y la demo.

### Puntos a coordinar

- **Menú común** (todo el grupo, `000-core`): el layout común ya reserva un hueco en el menú del
  personal de centro para los módulos Reservas y Cancelaciones (spec-006 y spec-007). Añadir ahí el
  enlace a la pantalla de validación toca un fichero común y va por PR revisado.
- **HU-03 y HU-07** (C): no tienen que hacer nada para invalidar el código; basta con que la reserva
  pase a cancelada a través de las operaciones que ya ofrece HU-02 (spec-002, RF-019a). Se les avisa
  de la dependencia (constitución, «Acuerdos de colaboración»).
- **Regla de acceso de `000-core`**: hoy no incluye la comprobación de centro (su documentación dice
  que es de esta spec). Si en el plan se prefiere añadirla a la regla común en lugar de hacerla en
  HU-06, el cambio se propone primero en `000-core`.
