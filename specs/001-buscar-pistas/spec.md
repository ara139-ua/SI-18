# Especificación: 001 · Buscar pistas libres (HU-01)

**Rama de trabajo**: `feature/001-buscar-pistas`

**Creada**: 2026-11-25

**Estado**: Implementada (todas las tareas de `tasks.md` cerradas; estado actualizado el 26/11/2026)

**Responsable**: Artur (módulo `Disponibilidad`, `docs/agrupamiento.md`)

**Entrada**: Descripción del usuario: "Crea la spec 001 en specs/001-buscar-pistas (número 001, nombre corto "buscar-pistas"). Es la HU-01 de SI-18, de Artur (módulo Disponibilidad): "Como ciudadano, quiero buscar pistas libres filtrando por deporte, centro y fecha, para encontrar rápidamente dónde jugar un partido". Detalles del README: los filtros son deporte, centro y fecha; el resultado muestra las franjas libres de 1 hora; cada franja muestra el precio que le corresponde al usuario según su tarifa (empadronado, no empadronado o club); las franjas ocupadas, bloqueadas por clubes o cerradas por mantenimiento no aparecen como disponibles. Criterios de aceptación (COS): dado un deporte, un centro y una fecha se listan solo las franjas libres; un empadronado y un no empadronado ven precios distintos para la misma franja; si no hay disponibilidad se muestra un mensaje claro; los filtros y resultados cumplen WCAG AA. Esta spec NO redefine nada de 000-core (constitución, principio VII): referencia specs/000-core (spec.md, data-model.md, contracts/) para Franja, la regla de disponibilidad RF-011 con su orden de motivos, el horario 08:00–22:00, la antelación hoy..hoy+7, la tarifa calculada al iniciar sesión (RF-007), el precio desde la tabla tarifas (RF-013: búsqueda y reserva usan el mismo cálculo), las instalaciones no reservables por franjas (salas y piscinas no aparecen), la zona horaria Europe/Madrid y los mensajes de error del catálogo. Quién busca: ciudadano y representante de club (política EsCiudadanoORepresentante); el representante ve la tarifa club. La HU-01 es dueña de implementar IConsultaDisponibilidad (contracts/modulos.md), que consume HU-02. Depende de IConsultaReservas (HU-02), IConsultaCierres (HU-07) e IConsultaFranjasFijas (HU-05) para conocer ocupadas, cerradas y bloqueadas. Todo en español de España."

## Alcance de esta spec

HU-01 permite a un ciudadano o a un representante de club **ver qué pistas tiene libres un centro en
una fecha, para un deporte, y cuánto le costaría cada franja con su tarifa**. Es una consulta: no
reserva, no cobra y no cambia el estado de nada.

Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa tal cual:

| Concepto                                         | Dónde está definido en `000-core`                                            |
| ------------------------------------------------ | ---------------------------------------------------------------------------- |
| Franja (1 h en punto, calculada, no almacenada)  | spec.md RF-010; data-model.md § Franja                                       |
| Cuándo una franja está libre y orden de motivos  | spec.md RF-011; data-model.md § Franja                                       |
| Qué instalaciones se reservan por franjas        | spec.md RF-012 (solo pistas de tenis y pádel)                                |
| Horario 08:00–22:00, antelación hoy..hoy + 7     | spec.md «Valores de negocio»                                                 |
| Tarifa del usuario (empadronado / no / club)     | spec.md RF-007; se calcula al iniciar sesión                                 |
| Precio de una franja                             | spec.md RF-013 y «Valores de negocio» (tabla de tarifas)                     |
| Zona horaria `Europe/Madrid`, euros, español     | constitución, principio IX                                                   |
| Textos de error                                  | contracts/errores.md (catálogo de códigos)                                   |
| Quién puede buscar                               | spec.md «Roles y permisos»: ciudadano y representante de club                |

**Fuera de alcance** (lo hacen otras specs): reservar y pagar la franja (spec-002), las franjas fijas
de club (spec-005) y los cierres por mantenimiento (spec-007). HU-01 solo **lee** su resultado.

## Clarifications

### Session 2026-11-25

- Q: ¿Qué hace HU-01 mientras HU-02, HU-05 y HU-07 no publican sus consultas de reservas, franjas
  fijas y cierres? → A: HU-01 incluye versiones provisionales de solo lectura de esas consultas, con
  las firmas de `000-core`, marcadas como provisionales; cada una se sustituye por la de su dueño
  cuando exista.
- Q: ¿Se puede buscar pistas sin haber iniciado sesión? → A: No; solo con sesión, y se ve el precio
  de la tarifa propia. Sin sesión se lleva a «Iniciar sesión».
- Q: En el resultado, ¿qué se hace con las franjas que no están libres? → A: No se muestran; solo
  aparecen las franjas libres.
- Q: ¿Qué spec pone el botón «Reservar» en cada franja del resultado? → A: La spec-002; la spec-001
  entrega cada franja identificada (pista, fecha y hora de inicio) y no dibuja ningún botón de
  reserva.
- Q: ¿El centro es obligatorio al buscar, o se puede buscar en todos los centros a la vez? → A:
  Obligatorio; no existe la opción «Todos los centros».

### Session 2026-11-26 (sobre la versión funcional, enunciado 5.1)

- Q: Si se pide por la API un deporte que existe pero no se reserva por franjas (natación, yoga…), ¿qué
  responde la búsqueda? → A: Un resultado vacío con `totalPistas = 0`, que la pantalla muestra con el
  mensaje «el centro no tiene pistas del deporte» de RF-011; no es una petición inválida.
- Q: Al buscar para hoy cuando ya no queda ninguna franja por empezar (después de las 21:00), ¿qué se
  muestra? → A: El mensaje general de RF-011 («No hay pistas libres… Prueba otra fecha u otro centro»);
  no hay un mensaje propio para «hoy» y la fecha propuesta sigue siendo hoy.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Ver las franjas libres de un deporte en un centro y una fecha (Prioridad: P1)

Ana, ciudadana empadronada, quiere jugar al pádel el sábado en el Polideportivo Norte. Elige «Pádel»,
«Polideportivo Norte» y el sábado, y ve, pista por pista, las horas que siguen libres ese día. Las
horas ocupadas, cerradas por mantenimiento o reservadas por un club no aparecen.

**Por qué esta prioridad**: es el núcleo de la historia y la puerta de entrada a la reserva (HU-02).
Sin esto no hay forma de encontrar dónde jugar.

**Prueba independiente**: con los datos de demostración, se busca un deporte, un centro y una fecha
dentro de los próximos 7 días y se comprueba que aparecen exactamente las franjas libres según RF-011
de `000-core`, agrupadas por pista y en orden horario.

**Escenarios de aceptación**:

1. **Dado** un centro con 2 pistas de pádel y ninguna reserva, cierre ni franja fija en la fecha
   elegida, **cuando** Ana busca «Pádel», ese centro y esa fecha, **entonces** ve las dos pistas con
   sus 14 franjas (de 08:00 a 21:00), menos las que ya hayan empezado si la fecha es hoy.
2. **Dado** que la Pista de pádel 1 tiene una reserva confirmada de 18:00 a 19:00 y otra pendiente de
   pago todavía vigente de 19:00 a 20:00, **cuando** Ana busca, **entonces** esas dos franjas no
   aparecen en la Pista de pádel 1 y sí aparecen en la Pista de pádel 2.
3. **Dado** que una reserva pendiente de pago ha caducado, **cuando** Ana busca, **entonces** su franja
   vuelve a aparecer libre.
4. **Dado** que la Pista de pádel 2 está cerrada por mantenimiento de 10:00 a 12:00 en esa fecha,
   **cuando** Ana busca, **entonces** las franjas de 10:00 y 11:00 de esa pista no aparecen, y las de
   09:00 y 12:00 sí.
5. **Dado** que un club tiene aprobada una franja fija los sábados de 19:00 a 21:00 en la Pista de
   pádel 1, **cuando** Ana busca un sábado de la temporada, **entonces** las franjas de 19:00 y 20:00
   de esa pista no aparecen.
6. **Dado** que hoy son las 18:00, **cuando** Ana busca la fecha de hoy, **entonces** solo aparecen
   franjas que empiezan a las 19:00 o más tarde.

---

### Historia de usuario 2 - Ver el precio de mi tarifa en cada franja (Prioridad: P1)

Cada franja libre muestra el precio que pagaría quien busca, según su tarifa: empadronado, no
empadronado o club. Así Ana sabe cuánto le costará antes de reservar.

**Por qué esta prioridad**: es parte explícita de la historia y de sus criterios de aceptación, y el
precio mostrado tiene que coincidir con el que se cobrará en HU-02 (RF-013 de `000-core`).

**Prueba independiente**: se hace la misma búsqueda con un usuario de cada tarifa y se comprueba el
precio de cada franja contra la tabla de tarifas de `000-core`.

**Escenarios de aceptación**:

1. **Dado** un ciudadano empadronado y otro no empadronado, **cuando** los dos buscan la misma pista y
   fecha, **entonces** el primero ve «6,00 €» y el segundo «10,00 €» en la misma franja.
2. **Dado** un representante de club (esté o no empadronado), **cuando** busca, **entonces** ve la
   tarifa club, «4,00 €».
3. **Dado** cualquier franja mostrada con un precio, **cuando** ese mismo usuario la reserva en HU-02
   y nadie la ha ocupado entre medias, **entonces** el importe que se le cobra es exactamente el
   mostrado.
4. **Dado** el resultado de una búsqueda, **cuando** se lee el precio, **entonces** está en euros con
   el formato español (coma decimal y el símbolo «€» detrás).

---

### Historia de usuario 3 - Saber por qué no hay resultados (Prioridad: P2)

Si no hay ninguna franja libre, Ana ve un mensaje claro que lo dice y le sugiere probar otra fecha u
otro centro, en lugar de una página vacía.

**Por qué esta prioridad**: es un criterio de aceptación explícito, pero solo aporta valor una vez
que la búsqueda funciona.

**Prueba independiente**: se busca una combinación sin franjas libres (por ejemplo, la fecha de hoy
pasadas las 21:00) y se comprueba que aparece el mensaje, que se anuncia a los lectores de pantalla y
que no depende solo del color.

**Escenarios de aceptación**:

1. **Dado** que todas las franjas de las pistas del deporte en ese centro y fecha están ocupadas,
   cerradas, bloqueadas o ya han empezado, **cuando** Ana busca, **entonces** ve el mensaje «No hay
   pistas libres de {deporte} en {centro} el {fecha}. Prueba otra fecha u otro centro.».
2. **Dado** que el centro elegido no tiene pistas reservables del deporte elegido, **cuando** Ana
   busca, **entonces** ve un mensaje que lo dice («{centro} no tiene pistas de {deporte}.»).
3. **Dado** que Ana intenta buscar una fecha anterior a hoy o posterior a hoy + 7, **cuando** envía
   la búsqueda, **entonces** no se busca y ve el mensaje del catálogo de errores para
   `FUERA_DE_ANTELACION` («Solo puedes reservar desde hoy hasta dentro de 7 días.»).

---

### Historia de usuario 4 - Buscar sin barreras de accesibilidad (Prioridad: P2)

Una persona que usa solo el teclado o un lector de pantalla puede elegir los tres filtros, lanzar la
búsqueda y recorrer los resultados con la misma información que el resto (constitución, principio I).

**Por qué esta prioridad**: es un principio no negociable del proyecto y un criterio de aceptación de
la historia; se comprueba sobre la pantalla ya construida.

**Prueba independiente**: se completa una búsqueda solo con el teclado y con un lector de pantalla, y
se pasa la revisión automática de accesibilidad de quickstart.md §6 de `000-core`.

**Escenarios de aceptación**:

1. **Dado** el formulario de búsqueda, **cuando** se recorre con el tabulador, **entonces** los tres
   filtros y el botón de buscar tienen etiqueta visible, se alcanzan en orden lógico y el foco siempre
   se ve.
2. **Dado** que termina una búsqueda, **cuando** llegan los resultados o el mensaje de «sin
   resultados», **entonces** se anuncia a los lectores de pantalla cuántas franjas libres hay o que no
   hay ninguna, sin mover el foco por sorpresa.
3. **Dado** el resultado, **cuando** se recorre, **entonces** cada franja se identifica por pista,
   hora de inicio y fin y precio en texto (no solo con color o iconos).

---

### Casos límite

Los límites de tiempo son los de `000-core` (spec.md, «Casos límite»); aquí solo se concreta cómo se
ven en la búsqueda:

- **Antelación**: la fecha hoy + 7 se puede buscar y muestra franjas; hoy + 8 no se puede buscar.
  «Hoy» es la fecha actual en `Europe/Madrid`, no en UTC (a las 00:30 de Madrid del día 2, «hoy» ya es
  el día 2 aunque en UTC aún sea el día 1).
- **Franja empezando**: a las 18:00:00 en punto la franja de las 18:00 ya no aparece; a las 17:59:59
  sí.
- **Hoy sin franjas por empezar**: a partir de las 21:00:00 de Madrid, buscar la fecha de hoy no da
  ninguna franja y se muestra el mensaje general de RF-011, que sugiere otra fecha u otro centro. La
  fecha propuesta por defecto sigue siendo hoy (RF-005).
- **Cambio de hora**: los días 25/10/2026 y 28/03/2027 la búsqueda muestra igualmente 14 franjas de
  08:00 a 21:00 en hora de Madrid, cada una de una hora real.
- **Reserva pendiente que caduca**: una reserva pendiente de pago creada hace exactamente 10 minutos
  ya no bloquea la franja; creada hace 9 minutos y 59 segundos, sí.
- **Cierre parcial**: un cierre que termina a las 10:00 no afecta a la franja de 10:00; uno que empieza
  a las 12:00 no afecta a la de 11:00.
- **Franja fija fuera de temporada**: una franja fija aprobada para la temporada 2026-2027 no bloquea
  julio ni agosto.
- **Franja fija suspendida por un cierre**: durante el cierre la franja aparece como cerrada (no
  libre), no como bloqueada por el club; el resultado para quien busca es el mismo: no aparece.
- **Deporte sin pistas reservables**: baloncesto, fútbol sala, natación, yoga y pilates no tienen
  pistas reservables por franjas (RF-012 de `000-core`), así que no se ofrecen como filtro. Si se piden
  igualmente (por ejemplo, escribiendo la petición a mano), el resultado es vacío con `totalPistas = 0`
  y se muestra el mensaje de RF-011 «el centro no tiene pistas del deporte», no un error.
- **Instalación no reservable**: pabellones, salas y piscinas no aparecen nunca en el resultado,
  aunque admitan el deporte elegido.
- **Dos personas a la vez**: la búsqueda muestra la situación en el momento de buscar; si otra
  persona reserva una franja justo después, la búsqueda no se actualiza sola y HU-02 rechazará la
  reserva con el motivo (RF-013 de `000-core`).
- **Usuario sin permiso**: el personal de centro y el técnico de concejalía no pueden buscar (no
  tienen tarifa); si lo intentan, se rechaza con el motivo `ROL_NO_PERMITIDO`.
- **Sin sesión**: quien no ha iniciado sesión es llevado a «Iniciar sesión».

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Quién busca**

- **RF-001**: Solo los ciudadanos y los representantes de club DEBEN poder buscar franjas libres
  («Roles y permisos» de `000-core`). Cualquier otro rol DEBE recibir el rechazo `ROL_NO_PERMITIDO`, y
  sin sesión DEBE pedirse iniciarla (`NO_AUTENTICADO`).
- **RF-002**: La opción de buscar pistas DEBE aparecer en el menú solo para los roles que pueden
  usarla.

**Filtros**

- **RF-003**: La búsqueda DEBE pedir tres filtros, los tres obligatorios: deporte, centro y fecha.
- **RF-004**: El filtro de deporte DEBE ofrecer solo los deportes que tienen al menos una instalación
  reservable por franjas (hoy, tenis y pádel); el de centro, los cuatro centros.
- **RF-005**: El filtro de fecha DEBE proponer hoy por defecto y DEBE limitar la elección a fechas
  entre hoy y hoy + 7 días naturales en `Europe/Madrid`. Una fecha fuera de ese rango DEBE rechazarse
  con el código `FUERA_DE_ANTELACION` y su texto del catálogo, sin buscar.

**Resultado**

- **RF-006**: El resultado DEBE incluir solo las instalaciones del centro elegido que sean
  reservables por franjas (RF-012 de `000-core`) y admitan el deporte elegido.
- **RF-007**: Para cada una de esas instalaciones, el resultado DEBE mostrar solo las franjas de la
  fecha elegida que estén **libres** según RF-011 de `000-core`, evaluadas en el momento de buscar.
  Las franjas no libres (ya empezadas, ocupadas, cerradas por mantenimiento o bloqueadas por una franja
  fija de club) NO DEBEN aparecer.
- **RF-008**: Cada franja libre DEBE mostrar la pista, la hora de inicio y de fin en hora de Madrid y
  el precio de la tarifa de quien busca, en euros con formato español.
- **RF-009**: Las franjas DEBEN ordenarse por pista (por nombre) y, dentro de cada pista, por hora de
  inicio.
- **RF-010**: Si una instalación no tiene ninguna franja libre, NO DEBE aparecer en el resultado.
- **RF-011**: Si no hay ninguna franja libre en total, DEBE mostrarse un mensaje que lo diga y sugiera
  cambiar la fecha o el centro; si el centro no tiene ninguna pista reservable por franjas del deporte
  elegido (también cuando el deporte no se reserva por franjas), el mensaje DEBE decirlo así. Para
  distinguir los dos casos, el resultado DEBE indicar cuántas pistas del deporte tiene el centro.

**Precio y coherencia con la reserva**

- **RF-012**: El precio de cada franja DEBE calcularse con la tarifa del usuario determinada al
  iniciar sesión (RF-007 de `000-core`) y la tabla de tarifas, y con exactamente el mismo cálculo que
  usará HU-02 al reservar (RF-013 de `000-core`).
- **RF-013**: Esta historia DEBE ofrecer a HU-02, para una instalación y una hora de inicio, si la
  franja está libre (o el motivo por el que no, con los códigos y el orden de RF-011 de `000-core`) y
  su precio para una tarifa, con la misma regla que usa la búsqueda. Es la única fuente de esa
  decisión: HU-02 no la recalcula por su cuenta.

**Datos que se leen de otras historias**

- **RF-014**: Para saber qué franjas están ocupadas, cerradas o bloqueadas, esta historia DEBE leer
  las reservas activas (de HU-02), los cierres por mantenimiento (de HU-07) y las franjas fijas
  aprobadas (de HU-05) solo a través de las consultas que esas historias publican (`000-core`,
  contracts/modulos.md). No DEBE escribir en sus datos.
- **RF-014a**: Mientras la historia dueña de una de esas consultas no la haya publicado, HU-01 DEBE
  aportar una versión **provisional de solo lectura** con la misma firma de `000-core`, que aplique las
  mismas reglas (por ejemplo, la vigencia de 10 minutos de una reserva pendiente) y esté marcada como
  provisional. Cuando el dueño publique la suya, la provisional DEBE retirarse sin cambiar el
  comportamiento de la búsqueda.

**Accesibilidad e idioma**

- **RF-015**: La pantalla de búsqueda DEBE cumplir WCAG 2.1 AA (constitución, principio I): etiquetas
  visibles en los filtros, uso completo con teclado, foco visible, resultado y «sin resultados»
  anunciados a los lectores de pantalla, e información que no dependa solo del color.
- **RF-016**: Todos los textos DEBEN estar en español de España; las fechas y horas, en hora de
  Madrid; los importes, en euros con formato español (constitución, principio IX).

### Entidades clave

HU-01 no crea ni modifica ninguna entidad. Usa las de `000-core` (spec.md, «Entidades clave»):

- **Franja**: la calcula esta historia (es su propietaria según `000-core`); no se almacena.
- **Centro, Deporte, Instalación, Tarifa**: precargadas; se leen para los filtros, el resultado y el
  precio.
- **Reserva** (de HU-02), **Cierre por mantenimiento** (de HU-07) y **Solicitud de franjas fijas**
  (de HU-05): se leen, a través de sus consultas, para descartar franjas.
- **Usuario**: se usa su tarifa, calculada al iniciar sesión.

**Resultado de búsqueda** (no se almacena): por cada pista con franjas libres, su nombre y la lista de
franjas libres con hora de inicio, hora de fin y precio.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Una persona encuentra una franja libre del deporte que quiere, en el centro y la fecha
  que quiere, en menos de 1 minuto desde que abre la búsqueda.
- **CE-002**: El resultado de una búsqueda aparece en menos de 2 segundos en el entorno de
  demostración.
- **CE-003**: En el 100 % de los casos de prueba, el conjunto de franjas mostradas coincide
  exactamente con las franjas libres según RF-011 de `000-core` (ninguna libre se omite y ninguna no
  libre se muestra), incluidos los límites de 7 días, del inicio de la franja, de los 10 minutos de
  bloqueo y del cambio de hora.
- **CE-004**: En el 100 % de los casos de prueba, el precio mostrado coincide con el que cobra HU-02
  al reservar esa franja con el mismo usuario (CE-005 de `000-core`).
- **CE-005**: Un usuario empadronado y uno no empadronado ven precios distintos para la misma franja
  en el 100 % de las búsquedas.
- **CE-006**: La búsqueda se completa entera solo con el teclado, la revisión automática de
  accesibilidad (Lighthouse) da 95 o más, y la revisión con axe no encuentra infracciones graves ni
  críticas.
- **CE-007**: Ninguna búsqueda sin resultados deja la pantalla vacía: el 100 % muestra un mensaje que
  explica la situación.

## Supuestos

- **Búsqueda con sesión** (confirmado en «Clarifications»): buscar exige haber iniciado sesión,
  porque el precio depende de la tarifa del usuario y la historia la plantea para ciudadanos y
  representantes. La búsqueda anónima, con o sin precios, queda fuera de alcance.
- **Tres filtros obligatorios** (el centro, confirmado en «Clarifications»): deporte, centro y fecha
  son obligatorios, como en el criterio de aceptación («dado un deporte, un centro y una fecha»).
  Buscar en todos los centros a la vez o en un rango de fechas queda fuera de alcance (constitución,
  principio VIII).
- **Solo franjas libres** (confirmado en «Clarifications»): el resultado lista solo las franjas
  libres; las no disponibles no se muestran, ni como «No disponible» ni con su motivo. Es lo que piden
  los criterios de aceptación y lo más simple.
- **De la búsqueda a la reserva** (confirmado en «Clarifications»): el botón «Reservar» de cada franja
  del resultado lo añade HU-02 (spec-002). HU-01 no dibuja ningún botón de reserva, ni siquiera
  inactivo: solo deja identificada cada franja (pista, fecha y hora de inicio) para que HU-02 pueda
  partir de ella.
- **Sin actualización automática**: el resultado refleja el momento de la búsqueda; para ver cambios
  se vuelve a buscar. HU-02 vuelve a comprobar disponibilidad y precio al reservar.
- **Dependencias aún no construidas**: HU-02, HU-05 y HU-07 publican las consultas que esta historia
  necesita (`000-core`, contracts/modulos.md). Mientras no existan, HU-01 usa sus versiones
  provisionales de solo lectura (RF-014a), que se avisan a cada dueño (Saul, C y la propia HU-05) para
  que las sustituyan; HU-01 nunca escribe en sus datos.
- **Datos de demostración**: se usan los centros, pistas, tarifas y usuarios ficticios sembrados por
  `000-core`.
