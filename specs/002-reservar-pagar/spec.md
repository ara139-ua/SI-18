# Especificación: 002 · Reservar y pagar una pista (HU-02)

**Rama de trabajo**: `feature/002-reservar-pagar`

**Creada**: 2026-11-25

**Estado**: Implementada (todas las tareas de `tasks.md` cerradas; estado actualizado el 26/11/2026)

**Responsable**: Saul (módulo `Reservas`, `docs/agrupamiento.md`)

**Entrada**: Descripción del usuario: "Crea la spec 002 en specs/002-reservar-pagar (número 002, nombre corto "reservar-pagar") en la rama feature/002-reservar-pagar. Es la HU-02 de SI-18, de Saul (módulo Reservas): "Como ciudadano, quiero reservar una pista y pagarla en el mismo momento, para tener la franja asegurada sin hacer cola". Detalles del README: la reserva se confirma solo tras el pago correcto; no se permite reservar con más de 7 días de antelación; mientras el pago está pendiente la franja no debe poder reservarla otro usuario; al confirmar se genera el código QR asociado. COS: una reserva sin pago completado no queda confirmada ni bloquea la pista de forma permanente; un intento de reservar a más de 7 días se rechaza con un mensaje explicativo; dos usuarios no pueden confirmar la misma franja; tras el pago la reserva aparece como confirmada con su QR. No redefine nada de 000-core (principio VII): referencia spec.md, data-model.md y contracts/ de 000-core para la máquina de estados de Reserva y Pago (RF-015, RF-016), una reserva activa por franja (RF-014, índice ux_reservas_activa), disponibilidad y precio iguales que en la búsqueda (RF-013, vía IConsultaDisponibilidad de HU-01), el bloqueo de 10 minutos, la caducidad perezosa y el barrido (R8), la pasarela simulada (R9, IPasarelaPago, tarjetas que terminan en 0002 rechazadas), el código de acceso opaco (RF-017, R14), el catálogo de errores (FRANJA_OCUPADA, RESERVA_CADUCADA, PAGO_RECHAZADO, PRECIO_CAMBIADO, FUERA_DE_ANTELACION…), el prefijo /api/reservas y el flujo de reserva y caducidad de plan.md. Quién reserva: ciudadano y representante de club (EsCiudadanoORepresentante; el representante paga tarifa club). HU-02 es dueña de implementar IConsultaReservas (la consume HU-01) y debe sustituir y borrar la implementación PROVISIONAL ConsultaReservasProvisional de spec-001 (specs/001-buscar-pistas, research.md D5, RF-014a), sin cambiar el comportamiento de la búsqueda. También añade el botón «Reservar» en cada franja del resultado de HU-01 (aclaración de spec-001). Todo en español de España."

## Alcance de esta spec

HU-02 permite a un ciudadano o a un representante de club **reservar una franja libre de una pista y
pagarla en el mismo momento**. La reserva solo queda confirmada si el pago se completa; entonces recibe
su código de acceso (QR). Mientras se paga, nadie más puede quedarse esa franja, pero el bloqueo dura
como mucho 10 minutos.

Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa tal cual:

| Concepto                                                     | Dónde está definido en `000-core`                                   |
| ------------------------------------------------------------ | ------------------------------------------------------------------- |
| Estados de Reserva y Pago y sus transiciones                 | spec.md RF-015, RF-016 y «Estados»; data-model.md § Reserva, § Pago |
| Una sola reserva activa por instalación y franja             | spec.md RF-014; data-model.md (`ux_reservas_activa`)                |
| Cuándo una franja está libre y orden de motivos              | spec.md RF-011; data-model.md § Franja                              |
| Disponibilidad y precio iguales en la búsqueda y la reserva  | spec.md RF-013 (los decide HU-01, spec-001 RF-013)                  |
| Antelación hoy..hoy + 7, bloqueo de 10 min, tarifas          | spec.md «Valores de negocio»                                        |
| Tarifa del usuario (empadronado / no / club)                 | spec.md RF-007; se calcula al iniciar sesión                        |
| Caducidad de las pendientes (al reservar, al pagar, barrido) | plan.md «Flujo de una reserva y su caducidad»; research.md R8       |
| Pasarela de pago simulada                                    | spec.md RF-021; research.md R9                                      |
| Código de acceso (QR) opaco y aleatorio                      | spec.md RF-017; research.md R14                                     |
| Textos de error                                              | contracts/errores.md (catálogo de códigos)                          |
| Quién puede reservar                                         | spec.md «Roles y permisos»: ciudadano y representante de club       |
| Zona horaria `Europe/Madrid`, euros, español                 | constitución, principio IX                                          |

**Fuera de alcance** (lo hacen otras specs): buscar franjas libres (spec-001); la pantalla para
cancelar, la decisión de devolver y la devolución (spec-003); validar el QR en el acceso (spec-006); y
registrar un cierre por mantenimiento, con sus devoluciones y avisos (spec-007). HU-02 solo aporta a
spec-003 y spec-007 los cambios de estado de las reservas (RF-019a).

## Clarifications

### Session 2026-11-25

- Q: ¿Quién implementa las operaciones de cancelar una reserva por el usuario (HU-03) y aplicar un
  cierre (HU-07), que escriben en reservas y pagos? → A: Las implementa la spec-002 (dueña de esas
  tablas), con sus pruebas, reutilizando la máquina de estados de `000-core`; HU-03 y HU-07 solo las
  usan.
- Q: ¿Puede el usuario soltar a propósito una reserva pendiente de pago antes de los 10 minutos? → A:
  No; no hay ninguna acción para abandonar el pago y la franja se libera sola a los 10 minutos.
- Q: ¿Se limita cuántas reservas (pendientes o confirmadas) puede tener una persona a la vez? → A: No;
  sin límite, porque `000-core` no fija ninguno.
- Q: ¿Qué reservas muestra «Mis reservas»? → A: Todas las del usuario, en cualquier estado: primero las
  que aún no han terminado, por inicio ascendente; después las terminadas, de la más reciente a la más
  antigua; sin paginación.
- Q: ¿La pantalla de pago muestra cuánto queda para que caduque la reserva? → A: Sí, como hora límite
  fija en hora de Madrid («Paga antes de las 18:42; después la reserva caduca»), sin cuenta atrás.
- Q (plan, 2026-11-25): ¿Qué responde el pago de una reserva que el barrido ya ha guardado como
  caducada por tiempo? → A: `RESERVA_CADUCADA`, igual que si aún no se hubiera guardado: el usuario no
  ve un mensaje distinto según haya pasado o no el barrido. Las caducadas por un pago rechazado o por
  un cierre siguen dando `TRANSICION_NO_PERMITIDA` (plan, research.md E4).

### Session 2026-11-26 (sobre la versión funcional, enunciado 5.1)

Pasada de clarify sobre la aplicación ya implementada, después de probar el recorrido habitual, una
entrada inválida o un límite y una condición de permiso o de estado (detalle y evidencias en
[clarify-funcional.md](./clarify-funcional.md)). Respuestas de Saul.

- Q: ¿Se puede pagar una reserva pendiente cuando su franja ya ha empezado, aunque no hayan pasado 10
  minutos desde que se creó? → A: No. Al pagar después del inicio, la reserva caduca, su pago queda
  fallido, no se llama a la pasarela y se responde `FRANJA_YA_EMPEZADA`. La hora límite de la pantalla
  de pago es la más temprana entre «creación + 10 minutos» y el inicio de la franja.
  - **Justificación**: reservar una franja que ya ha empezado está prohibido (`FRANJA_YA_EMPEZADA`,
    RF-011 de `000-core`) y la descripción del sistema solo habla de reservar franjas futuras; dejar
    pagar después del inicio confirmaría por la puerta de atrás una reserva que no se podría crear. La
    versión funcional sí lo permitía (comprobado el 26/11/2026, ver clarify-funcional.md), y no se
    acepta esa conducta solo porque ya funcione así. Se reutiliza un código del catálogo, sin códigos
    nuevos (principio VII).
  - **Historia afectada**: HU-2 (escenario 6), HU-5 (escenario 3), «Casos límite», RF-008a, RF-009a,
    RF-011 y RF-017.
- Q: ¿Puede un representante de club reservar una franja que está bloqueada por una franja fija
  aprobada de su propio club? → A: No; se rechaza con `FRANJA_BLOQUEADA_CLUB`, como a cualquier otra
  persona.
  - **Justificación**: las franjas fijas no se cobran (D9 de `000-core`): el club ya dispone de esa
    franja, y dejar que la reserve además sería cobrarle por algo que ya es suyo. La regla vive en
    HU-01 (`IConsultaDisponibilidad`, de Artur) y no cambia.
  - **Historia afectada**: HU-4 (escenario 3) y «Casos límite».
- Q: Las reservas que hace un representante con la tarifa de club, ¿son del representante o del club?
  → A: Del representante que reserva: solo él las ve y las paga, y en HU-06 aparece su nombre como
  titular.
  - **Justificación**: en `000-core` la reserva pertenece a un usuario, no a un club (spec.md,
    «Entidades clave»); hacerla del club cambiaría el modelo de datos común (principio VII) por un caso
    que la descripción del sistema no pide. El club solo determina la tarifa.
  - **Historia afectada**: HU-1 (escenario 4), RF-013 y «Entidades clave».

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Reservar una franja libre y pagarla en el momento (Prioridad: P1)

Ana, ciudadana empadronada, ha buscado pádel en el Polideportivo Norte para el sábado (HU-01). En la
franja de 18:00 de la Pista de pádel 1 pulsa «Reservar». Ve un resumen con la pista, la fecha, la hora
y el precio de su tarifa (6,00 €), introduce los datos de su tarjeta y paga. La reserva queda
confirmada y ve su código QR.

**Por qué esta prioridad**: es el núcleo de la historia: sin esto no hay reservas, ni QR (HU-06), ni
cancelaciones (HU-03), ni ingresos en el informe (HU-08).

**Prueba independiente**: con los datos de demostración, un ciudadano reserva una franja libre, paga
con una tarjeta aceptada por la pasarela simulada y comprueba que la reserva queda confirmada, con el
importe de su tarifa y con un código QR.

**Escenarios de aceptación**:

1. **Dado** una franja libre en el resultado de la búsqueda, **cuando** Ana pulsa «Reservar»,
   **entonces** ve un resumen con la pista, el centro, la fecha, la hora de inicio y de fin en hora de
   Madrid y el importe de su tarifa, igual al mostrado en la búsqueda.
2. **Dado** el resumen, **cuando** Ana confirma que quiere reservar, **entonces** la reserva se crea
   pendiente de pago, la franja deja de estar libre para cualquier otra persona y Ana ve la pantalla de
   pago con la hora límite («Paga antes de las 18:42; después la reserva caduca» si la creó a las
   18:32).
3. **Dado** una reserva pendiente de pago creada hace menos de 10 minutos, **cuando** Ana paga con una
   tarjeta que la pasarela acepta, **entonces** el pago se completa, la reserva pasa a confirmada y Ana
   ve un mensaje de confirmación con su código QR.
4. **Dado** un representante de club (esté o no empadronado), **cuando** reserva y paga, **entonces**
   se le cobra la tarifa club (4,00 €). La reserva es suya (de su usuario), no del club: ningún otro
   representante del mismo club la ve ni la paga (aclaración 2026-11-26).
5. **Dado** cualquier reserva confirmada, **cuando** se consulta, **entonces** su importe es
   exactamente el que se mostró en la búsqueda y en el resumen.

---

### Historia de usuario 2 - Nadie más puede quedarse la franja mientras pago, pero el bloqueo no es eterno (Prioridad: P1)

Mientras Ana introduce los datos de la tarjeta, Bruno busca la misma pista y fecha: la franja de las
18:00 no le aparece libre. Si Ana abandona el pago, la franja vuelve a estar libre como mucho 10 minutos
después de crear la reserva.

**Por qué esta prioridad**: son dos criterios de aceptación explícitos («dos usuarios no pueden
confirmar la misma franja» y «una reserva sin pago completado no bloquea la pista de forma
permanente») y garantizan que no se cobra dos veces la misma franja.

**Prueba independiente**: se crea una reserva pendiente de pago y se comprueba que otra persona no
puede reservar esa franja; se avanza el reloj 10 minutos y se comprueba que la franja vuelve a estar
libre, que la reserva queda caducada y que ya no se puede pagar.

**Escenarios de aceptación**:

1. **Dado** que Ana tiene una reserva pendiente de pago vigente, **cuando** Bruno intenta reservar la
   misma pista y hora, **entonces** se rechaza con `FRANJA_OCUPADA` y su texto del catálogo.
2. **Dado** que Ana y Bruno pulsan «Reservar» sobre la misma franja libre a la vez, **cuando** el
   sistema procesa las dos peticiones, **entonces** solo una crea la reserva y la otra recibe
   `FRANJA_OCUPADA`.
3. **Dado** que Ana creó su reserva hace exactamente 10 minutos sin pagar, **cuando** intenta pagar,
   **entonces** se rechaza con `RESERVA_CADUCADA`, la reserva pasa a caducada, su pago a fallido y no
   se cobra nada.
4. **Dado** que la reserva pendiente de Ana tiene ya 10 minutos, **cuando** Bruno reserva esa franja,
   **entonces** lo consigue: la reserva de Ana queda caducada y la de Bruno, pendiente de pago.
5. **Dado** que nadie intenta reservar ni pagar, **cuando** pasan 10 minutos desde que Ana creó su
   reserva, **entonces** la reserva acaba guardada como caducada y su pago como fallido sin que nadie
   haga nada (barrido de `000-core`).
6. **Dado** que Ana creó a las 20:55 una reserva pendiente para la franja de las 21:00, **cuando**
   intenta pagarla a las 21:00:00 o después (aunque no hayan pasado 10 minutos), **entonces** se
   rechaza con `FRANJA_YA_EMPEZADA`, la reserva pasa a caducada, su pago a fallido y no se cobra nada
   (aclaración 2026-11-26).

---

### Historia de usuario 3 - Saber qué pasa si el pago no se completa (Prioridad: P1)

Si la pasarela rechaza la tarjeta, Ana ve un mensaje claro: el pago no se ha completado, la reserva ha
caducado y, si la franja sigue libre, puede hacer una reserva nueva. No se cobra nada.

**Por qué esta prioridad**: «la reserva se confirma solo tras el pago correcto» es la primera regla de
la historia, y el pago fallido no se reintenta (aclaración de `000-core`).

**Prueba independiente**: se paga con una tarjeta que la pasarela simulada rechaza y se comprueba el
mensaje, el estado caducado de la reserva y el estado fallido del pago.

**Escenarios de aceptación**:

1. **Dado** una reserva pendiente de pago vigente, **cuando** Ana paga con una tarjeta que la pasarela
   rechaza, **entonces** ve el mensaje de `PAGO_RECHAZADO`, la reserva pasa a caducada, su pago a
   fallido y no tiene QR.
2. **Dado** una reserva caducada por un pago rechazado, **cuando** Ana intenta pagarla otra vez,
   **entonces** se rechaza con `TRANSICION_NO_PERMITIDA`: un pago fallido no se reintenta.
3. **Dado** una reserva caducada, **cuando** Ana vuelve a buscar, **entonces** la franja aparece libre
   (si nada más la ocupa) y puede crear una reserva nueva.
4. **Dado** un número de tarjeta con un formato no válido (no tiene 16 dígitos), **cuando** Ana intenta
   pagar, **entonces** se le indica que revise el dato (`PETICION_INVALIDA`), no se llama a la pasarela
   y la reserva sigue pendiente de pago mientras esté vigente.

---

### Historia de usuario 4 - Entender por qué no puedo reservar una franja (Prioridad: P2)

Si una franja ya no se puede reservar cuando Ana lo intenta (porque está fuera del plazo de 7 días, ya
ha empezado, se ha ocupado, se ha cerrado, está bloqueada por un club o su precio ha cambiado), la
reserva no se crea y Ana ve el motivo concreto.

**Por qué esta prioridad**: «un intento de reservar a más de 7 días se rechaza con un mensaje
explicativo» es un criterio de aceptación, y la reserva tiene que volver a comprobar disponibilidad y
precio (RF-013 de `000-core`); pero solo aporta valor una vez que reservar funciona.

**Prueba independiente**: se intenta reservar una franja en cada una de las situaciones de RF-011 de
`000-core` y se comprueba que no se crea ninguna reserva y que el mensaje es el del catálogo.

**Escenarios de aceptación**:

1. **Dado** que hoy es lunes 21/12/2026 a las 18:00 de Madrid, **cuando** Ana intenta reservar una
   franja del 29/12, **entonces** se rechaza con `FUERA_DE_ANTELACION` («Solo puedes reservar desde hoy
   hasta dentro de 7 días.») y no se crea ninguna reserva; la franja de 21:00 del 28/12 sí se puede
   reservar.
2. **Dado** una franja que ha empezado hace un minuto, **cuando** Ana intenta reservarla, **entonces**
   se rechaza con `FRANJA_YA_EMPEZADA`.
3. **Dado** una franja cerrada por mantenimiento o bloqueada por una franja fija de club, **cuando**
   Ana intenta reservarla, **entonces** se rechaza con `INSTALACION_CERRADA` o
   `FRANJA_BLOQUEADA_CLUB`. También se rechaza con `FRANJA_BLOQUEADA_CLUB` si quien intenta reservar
   es un representante del club dueño de esa franja fija (aclaración 2026-11-26).
4. **Dado** una sala, una piscina o un pabellón, **cuando** alguien intenta reservarlo por franjas,
   **entonces** se rechaza con `INSTALACION_NO_RESERVABLE`.
5. **Dado** que el precio de la franja ha cambiado desde que Ana la vio, **cuando** intenta reservar
   con el precio que vio, **entonces** se rechaza con `PRECIO_CAMBIADO`, no se crea la reserva y Ana ve
   el importe nuevo para decidir.
6. **Dado** cualquiera de estos rechazos, **cuando** Ana lee el mensaje, **entonces** está en español,
   explica el motivo y no contiene detalles técnicos.

---

### Historia de usuario 5 - Ver mis reservas con su estado y su QR (Prioridad: P2)

Ana abre «Mis reservas» y ve sus reservas con la pista, el centro, la fecha y la hora, el importe y el
estado. Las confirmadas muestran su código QR para enseñarlo en el acceso (HU-06).

**Por qué esta prioridad**: cumple «tras el pago, la reserva aparece como confirmada con su QR» más
allá del momento del pago: sin esta lista, Ana no podría recuperar su QR el día del partido.

**Prueba independiente**: un usuario con reservas en varios estados abre «Mis reservas» y comprueba
que solo ve las suyas, con el estado correcto, y el QR solo en las confirmadas.

**Escenarios de aceptación**:

1. **Dado** que Ana tiene una reserva confirmada, **cuando** abre «Mis reservas», **entonces** la ve
   como «Confirmada» con su código QR y el código también escrito en texto.
2. **Dado** que Ana tiene reservas caducadas o canceladas, **cuando** abre «Mis reservas», **entonces**
   las ve con su estado y sin QR.
3. **Dado** que Ana tiene una reserva pendiente de pago vigente, **cuando** abre «Mis reservas»,
   **entonces** la ve como «Pendiente de pago» y puede volver a pagarla mientras siga vigente y su
   franja no haya empezado (aclaración 2026-11-26).
4. **Dado** las reservas de otra persona, **cuando** Ana abre «Mis reservas» o intenta ver o pagar una
   reserva ajena, **entonces** no la ve, y el intento se rechaza con `NO_ENCONTRADO` (sin revelar que
   existe).
5. **Dado** una reserva pendiente que ya ha superado los 10 minutos, **cuando** Ana abre «Mis
   reservas», **entonces** la ve como caducada aunque el barrido aún no la haya guardado así.
6. **Dado** que Ana tiene reservas para el sábado y el domingo y otra del mes pasado, **cuando** abre
   «Mis reservas», **entonces** ve primero la del sábado, después la del domingo y al final la del mes
   pasado.

---

### Historia de usuario 6 - Reservar y pagar sin barreras de accesibilidad (Prioridad: P2)

Una persona que usa solo el teclado o un lector de pantalla puede pulsar «Reservar», revisar el
resumen, pagar y leer el resultado (incluido el código de acceso) con la misma información que el
resto (constitución, principio I).

**Por qué esta prioridad**: es un principio no negociable; se comprueba sobre las pantallas ya
construidas.

**Prueba independiente**: se completa una reserva y su pago solo con el teclado y con un lector de
pantalla, y se pasa la revisión automática de accesibilidad de quickstart.md §6 de `000-core`.

**Escenarios de aceptación**:

1. **Dado** el resultado de la búsqueda, **cuando** se recorre con el tabulador, **entonces** cada
   botón «Reservar» tiene un nombre accesible que incluye la pista, la fecha y la hora.
2. **Dado** el formulario de pago, **cuando** se recorre, **entonces** cada campo tiene etiqueta
   visible, el foco siempre se ve y los errores se asocian a su campo.
3. **Dado** el resultado del pago (confirmada, rechazada o caducada), **cuando** llega, **entonces** se
   anuncia a los lectores de pantalla y no se transmite solo con color.
4. **Dado** el código QR, **cuando** se muestra, **entonces** tiene una alternativa en texto (el código
   escrito y una descripción de la reserva).

---

### Casos límite

Los límites de tiempo son los de `000-core` (spec.md, «Casos límite»); aquí se concreta cómo se ven
al reservar y pagar:

- **Antelación**: la fecha hoy + 7 se puede reservar; hoy + 8, no. «Hoy» es la fecha actual en
  `Europe/Madrid`.
- **Franja empezando**: a las 18:00:00 en punto ya no se puede reservar la franja de las 18:00; a las
  17:59:59, sí.
- **Pagar en el límite del bloqueo**: pagar 9 minutos y 59 segundos después de crear la reserva → se
  intenta el cobro; pagar a los 10 minutos exactos → `RESERVA_CADUCADA`, sin cobro.
- **Pagar cuando la franja ya ha empezado** (aclaración 2026-11-26): reserva pendiente creada a las
  20:55 para la franja de las 21:00; pagar a las 20:59:59 → se intenta el cobro; pagar a las 21:00:00 o
  después → `FRANJA_YA_EMPEZADA`, la reserva queda caducada y su pago fallido, sin cobro. Si además ya
  han pasado los 10 minutos, se responde `RESERVA_CADUCADA` (RF-009 se comprueba primero).
- **Franja fija del propio club** (aclaración 2026-11-26): el representante del club dueño de una franja
  fija aprobada tampoco puede reservarla (`FRANJA_BLOQUEADA_CLUB`).
- **Caducada por el barrido**: pagar una reserva que el barrido ya ha guardado como caducada por tiempo
  → `RESERVA_CADUCADA` (el mismo mensaje que antes del barrido), sin cobro.
- **Reservar una franja con una pendiente ajena que acaba de caducar**: con la otra reserva creada hace
  exactamente 10 minutos, se puede reservar; la anterior queda caducada en el mismo momento.
- **Doble clic**: si la misma persona envía dos veces la misma reserva, la segunda recibe
  `FRANJA_OCUPADA`; nunca hay dos reservas activas en la misma franja.
- **Pagar dos veces**: pagar una reserva ya confirmada, o ya caducada por un pago rechazado →
  `TRANSICION_NO_PERMITIDA`, sin cobro.
- **Cambio de hora**: los días 25/10/2026 y 28/03/2027 se pueden reservar las 14 franjas de 08:00 a
  21:00 en hora de Madrid, cada una de una hora real.
- **Cierre posterior**: si se registra un cierre por mantenimiento que cubre una reserva pendiente
  (HU-07), esa reserva queda caducada y ya no se puede pagar (`TRANSICION_NO_PERMITIDA`).
- **Precio**: el importe que se cobra es el de la tarifa del usuario en el momento de reservar; si ha
  cambiado respecto al mostrado, no se reserva (`PRECIO_CAMBIADO`).
- **Datos de la tarjeta**: nunca se guardan ni se registran, ni siquiera los últimos dígitos (R9 de
  `000-core`).
- **Petición mal formada**: si la pista no existe se rechaza con `NO_ENCONTRADO`, y si la hora no es el
  inicio de una franja (no está en punto o queda fuera de 08:00–21:00 en Madrid), con
  `PETICION_INVALIDA`; son los mismos rechazos que da la comprobación de HU-01 (spec-001,
  contracts/modulos.md).
- **Usuario sin permiso**: el personal de centro y el técnico de concejalía no pueden reservar; se
  rechaza con `ROL_NO_PERMITIDO`.
- **Sin sesión**: quien no ha iniciado sesión es llevado a «Iniciar sesión».

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Quién reserva**

- **RF-001**: Solo los ciudadanos y los representantes de club DEBEN poder reservar, pagar y ver «Mis
  reservas» («Roles y permisos» de `000-core`). Cualquier otro rol DEBE recibir `ROL_NO_PERMITIDO`, y
  sin sesión DEBE pedirse iniciarla (`NO_AUTENTICADO`).
- **RF-002**: La opción «Mis reservas» DEBE aparecer en el menú solo para los roles que pueden usarla.

**Crear la reserva**

- **RF-003**: Cada franja libre del resultado de la pantalla «Buscar pistas» de HU-01 DEBE ofrecer la
  acción «Reservar», que lleva a un resumen con la pista, el centro, la fecha, la hora de inicio y de
  fin en hora de Madrid y el importe de la tarifa del usuario. La franja se identifica por la pista y
  su hora de inicio, tal como la entrega el resultado de HU-01.
- **RF-004**: Al confirmar el resumen, el sistema DEBE volver a comprobar en ese momento que la franja
  está libre y su precio, con la misma regla que la búsqueda (RF-013 de `000-core`), consultándolos a
  HU-01 sin recalcularlos por su cuenta. Si no está libre, DEBE rechazarse con el código del primer
  motivo, en el orden de RF-011 de `000-core`. Si el precio difiere del que vio el usuario, DEBE
  rechazarse con `PRECIO_CAMBIADO`.
- **RF-005**: Si la franja está libre y el precio coincide, el sistema DEBE crear la reserva pendiente
  de pago, con la tarifa y el importe del usuario y su pago pendiente (RF-015 y RF-016 de `000-core`).
  Desde ese momento la franja NO DEBE estar libre para nadie mientras la reserva esté vigente.
- **RF-006**: Antes de crear la reserva, el sistema DEBE caducar (y dejar su pago fallido) cualquier
  reserva pendiente de pago no vigente de esa misma instalación y franja, para que no impida la nueva
  (caducidad al reservar, R8 de `000-core`).
- **RF-007**: El sistema DEBE garantizar que dos peticiones simultáneas sobre la misma franja no crean
  dos reservas activas: una se crea y la otra recibe `FRANJA_OCUPADA` (RF-014 de `000-core`), también
  si las envía la misma persona.

**Pagar**

- **RF-008**: El usuario DEBE poder pagar su reserva pendiente introduciendo los datos de una tarjeta
  ficticia, que se envían a la pasarela simulada de `000-core` (RF-021). Los datos de la tarjeta NO
  DEBEN guardarse ni registrarse.
- **RF-008a**: La pantalla de pago DEBE mostrar, como texto fijo, la hora límite para pagar en hora de
  Madrid: la más temprana entre «creación de la reserva + el tiempo de bloqueo de `000-core`» y el
  inicio de la franja (aclaración 2026-11-26), por ejemplo «Paga antes de las 18:42; después la reserva
  caduca». No hay cuenta atrás que se actualice en pantalla.
- **RF-009**: Si al pagar la reserva ya no está vigente (10 minutos o más desde su creación), el
  sistema DEBE caducarla, dejar su pago fallido, no llamar a la pasarela y responder
  `RESERVA_CADUCADA`. Lo mismo si la reserva ya está guardada como caducada por tiempo (por el barrido o
  por la caducidad al reservar): la respuesta no depende de si el barrido ha pasado.
- **RF-009a**: Si al pagar la reserva sigue vigente pero su franja ya ha empezado (la hora actual es
  igual o posterior al inicio), el sistema DEBE caducarla, dejar su pago fallido, no llamar a la
  pasarela y responder `FRANJA_YA_EMPEZADA` (aclaración 2026-11-26). RF-009 se comprueba antes.
- **RF-010**: Si la pasarela acepta el cobro, la reserva DEBE pasar a confirmada, su pago a completado
  y la reserva DEBE recibir un código de acceso opaco y aleatorio (RF-017 de `000-core`), que se
  muestra al usuario como QR y como texto.
- **RF-011**: Si la pasarela rechaza el cobro, la reserva DEBE pasar a caducada y su pago a fallido, y
  el usuario DEBE ver el mensaje de `PAGO_RECHAZADO`. El pago NO DEBE poder reintentarse; cualquier
  intento de pagar una reserva que no está pendiente DEBE rechazarse con `TRANSICION_NO_PERMITIDA`,
  salvo una caducada por tiempo, que responde `RESERVA_CADUCADA` (RF-009). Pagar una reserva vigente
  cuya franja ya ha empezado responde `FRANJA_YA_EMPEZADA` (RF-009a).
- **RF-012**: Un número de tarjeta con un formato no válido DEBE rechazarse con `PETICION_INVALIDA`
  sin llamar a la pasarela y sin cambiar el estado de la reserva.
- **RF-013**: Solo el titular DEBE poder pagar o ver una reserva. El titular es el usuario que la
  creó; en las reservas con tarifa club, el representante que reservó, no el club (aclaración
  2026-11-26); para cualquier otra persona, la
  reserva DEBE comportarse como inexistente (`NO_ENCONTRADO`).

**Caducidad**

- **RF-014**: Una reserva pendiente de pago NO DEBE bloquear la franja de forma permanente: deja de
  bloquearla a los 10 minutos de su creación (regla de vigencia de `000-core`) y acaba guardada como
  caducada, con su pago fallido, aunque nadie vuelva a intentar reservar ni pagar (barrido de
  `000-core`, R8).

**Mis reservas**

- **RF-015**: El usuario DEBE poder ver una lista de **todas** sus reservas, en cualquier estado, con la
  pista, el centro, la fecha y las horas en hora de Madrid, el importe en euros con formato español y
  el estado. Una reserva pendiente no vigente DEBE mostrarse como caducada.
- **RF-015a**: La lista DEBE ordenarse así: primero las reservas que aún no han terminado (fin
  posterior al momento actual), por hora de inicio ascendente; después las terminadas, de la más
  reciente a la más antigua. No tiene paginación.
- **RF-016**: Las reservas confirmadas DEBEN mostrar su código de acceso como QR y como texto; las que
  no están confirmadas NO DEBEN mostrar ningún código.
- **RF-017**: Desde una reserva pendiente de pago vigente cuya franja no ha empezado, el usuario DEBE
  poder ir a pagarla (aclaración 2026-11-26).

**Datos que se ofrecen a otras historias**

- **RF-018**: Esta historia DEBE ofrecer a HU-01 (y a quien lo necesite según `000-core`,
  contracts/modulos.md) la consulta de las reservas activas de una instalación en un periodo: las
  confirmadas y las pendientes de pago vigentes, con la misma regla de vigencia de `000-core`.
- **RF-019**: Al publicar esa consulta, la versión **provisional** que aportó HU-01 (spec-001,
  RF-014a), ya presente en el código, DEBE retirarse junto con su registro, como acordó spec-001
  (research.md D5). La consulta definitiva DEBE cumplir el mismo comportamiento que fijan las pruebas de
  la provisional (vigencia de 10 minutos y rango semiabierto), y la búsqueda de HU-01 DEBE seguir dando
  exactamente el mismo resultado (spec-001, CE-003).
- **RF-019a**: Esta historia DEBE ofrecer a HU-03 y a HU-07 las dos operaciones de escritura sobre
  reservas que define `000-core` (contracts/modulos.md), sin que esas historias escriban en las tablas
  de reservas y pagos:
  - **cancelar por el usuario** una reserva confirmada: si no es del usuario, `NO_ENCONTRADO`; si ya ha
    empezado, `RESERVA_YA_EMPEZADA`; si no está confirmada, `TRANSICION_NO_PERMITIDA`. Devuelve los
    datos que HU-03 necesita para decidir y registrar la devolución (reserva, titular, inicio, pago e
    importe). La devolución y su política son de HU-03;
  - **aplicar un cierre** a una instalación en un periodo: cancela, con motivo «cierre por
    mantenimiento», las reservas confirmadas que se solapan con el periodo y caduca (pago → fallido)
    las pendientes de pago que se solapan, y devuelve las dos listas para que HU-07 registre las
    devoluciones y los avisos (RF-020 de `000-core`). Las devoluciones, los avisos y el registro del
    cierre son de HU-07.

  Los cambios se guardan en la misma operación que los de quien llama (devolución, aviso), para que se
  guarden juntos o no se guarde nada.

**Mensajes, accesibilidad e idioma**

- **RF-020**: Todo rechazo DEBE mostrarse con el texto del catálogo de errores de `000-core`, en
  español y sin detalles técnicos (RF-028 de `000-core`, principio IV).
- **RF-021**: Las pantallas de resumen, pago, resultado y «Mis reservas» DEBEN cumplir WCAG 2.1 AA
  (constitución, principio I): uso completo con teclado, foco visible, etiquetas en los campos,
  resultados anunciados a los lectores de pantalla, información que no dependa solo del color y una
  alternativa en texto para el QR.
- **RF-022**: Todos los textos DEBEN estar en español de España; las fechas y horas, en hora de
  Madrid; los importes, en euros con formato español (constitución, principio IX).

### Entidades clave

HU-02 no crea entidades nuevas. Usa las de `000-core` (spec.md, «Entidades clave»; data-model.md):

- **Reserva** (propietaria: HU-02): la crea pendiente de pago, la confirma y la caduca. Guarda el
  usuario (en las de tarifa club, el representante que reserva; aclaración 2026-11-26), la instalación, el inicio y el fin, la tarifa aplicada, el importe, el estado, la fecha de
  creación y, si está confirmada, el código de acceso.
- **Pago** (propietaria: HU-02): uno por reserva; pendiente → completado o fallido. No guarda datos de
  tarjeta.
- **Franja**: la calcula HU-01; HU-02 la identifica por instalación y hora de inicio.
- **Instalación, Centro, Tarifa**: precargadas; se leen para el resumen y el precio.
- **Usuario**: se usa su tarifa, calculada al iniciar sesión.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Una persona que ya ha encontrado una franja libre la reserva y la paga en menos de
  2 minutos.
- **CE-002**: En el 100 % de los casos de prueba con reservas simultáneas sobre la misma franja
  (incluidas 20 a la vez), como mucho una queda activa y el resto recibe un rechazo explicado.
- **CE-003**: El 100 % de las reservas pendientes de pago dejan de bloquear su franja a los 10 minutos
  exactos de su creación, y ninguna antes.
- **CE-004**: El 100 % de las reservas confirmadas tienen el pago completado y un código de acceso
  distinto; ninguna reserva sin pago completado tiene código de acceso.
- **CE-005**: En el 100 % de los casos de prueba, el importe cobrado coincide con el precio mostrado en
  la búsqueda para el mismo usuario y franja (CE-004 de spec-001, CE-005 de `000-core`).
- **CE-006**: En el 100 % de los intentos rechazados (fuera de plazo, ya empezada, ocupada, cerrada,
  bloqueada, precio cambiado, caducada, pago rechazado), el usuario ve un mensaje que explica el motivo.
- **CE-007**: Tras retirar la consulta provisional de HU-01, sus pruebas de búsqueda siguen pasando sin
  cambios, y la consulta definitiva supera los mismos casos que fijaban el comportamiento de la
  provisional.
- **CE-008**: La reserva y el pago se completan enteros solo con el teclado, la revisión automática de
  accesibilidad (Lighthouse) da 95 o más y la revisión con axe no encuentra infracciones graves ni
  críticas.

## Supuestos

- **Punto de partida**: se reserva siempre desde una franja del resultado de la pantalla «Buscar
  pistas» de HU-01; no hay otro formulario para elegir pista y hora. HU-01 entrega cada franja
  identificada (pista y hora de inicio) y HU-02 añade el botón «Reservar» en esa pantalla (aclaración de
  spec-001). Hoy esa pantalla muestra cada franja solo como texto; el cambio se coordina con Artur, su
  dueño.
- **Menú**: «Mis reservas» ocupa el hueco del menú que el layout común ya reserva para el módulo
  Reservas.
- **Dos pasos**: primero se crea la reserva (y se bloquea la franja) y después se paga. Así el bloqueo
  de 10 minutos de `000-core` protege la franja mientras se introducen los datos de la tarjeta.
- **Precio confirmado al reservar**: el usuario envía el precio que vio; si no coincide con el actual,
  no se reserva (`PRECIO_CAMBIADO`). El importe queda fijado en la reserva y es el que se cobra.
- **Datos de la tarjeta**: se pide solo lo que necesita la pasarela simulada (el número de 16 dígitos;
  las tarjetas que terminan en `0002` se rechazan, R9 de `000-core`). Un error de formato no cuenta como
  pago fallido.
- **Sin límite de reservas por persona** (confirmado en «Clarifications»): `000-core` no fija ninguno;
  un usuario puede tener a la vez varias reservas, pendientes o confirmadas, en franjas distintas
  (principio VIII). El abuso queda acotado por el bloqueo de 10 minutos.
- **«Mis reservas» en esta historia**: el prefijo `/api/reservas` de `000-core` incluye «listar las
  mías» para HU-02. La cancelación desde esa lista la añade HU-03.
- **Abandonar el pago** (confirmado en «Clarifications»): no hay ninguna acción para soltar la
  reserva pendiente antes de tiempo (`000-core` no permite pendiente de pago → cancelada); salir de la
  pantalla de pago no cambia nada y la franja se libera sola a los 10 minutos (RF-014).
- **Consulta para otras historias**: HU-02 es dueña de las tablas `reservas` y `pagos`
  (`docs/agrupamiento.md`) y publica la consulta de reservas activas de `000-core`
  (contracts/modulos.md). También implementa las operaciones de cancelar por el usuario y aplicar un
  cierre que piden HU-03 y HU-07 (confirmado en «Clarifications», RF-019a), para que nadie más escriba
  en esas tablas; C (HU-03, HU-07) solo las usa y se le avisa de la dependencia.
  **Alcance (26/11/2026)**: HU-03 y HU-07 quedaron fuera del alcance comprometido por acuerdo con el
  profesor (`docs/agrupamiento.md`); estas operaciones se conservan como base para trabajo futuro.
- **Dependencias**: necesita de HU-01 la comprobación de disponibilidad y precio y la pantalla «Buscar
  pistas», donde va el botón «Reservar»; las dos ya están en `develop`, igual que la consulta provisional
  que hay que retirar. HU-03 pondrá su acción «Cancelar» sobre la pantalla «Mis reservas» de esta
  historia. Esta historia no escribe en datos de otros módulos.
- **Datos de demostración**: se usan los centros, pistas, tarifas y usuarios ficticios sembrados por
  `000-core`; no se siembran reservas.
