# Especificación: 000-core · Núcleo común de SI-18

**Rama de trabajo**: `develop` (la base común no tiene rama propia; constitución, «Acuerdos de colaboración»)

**Creada**: 2026-10-21

**Estado**: Borrador

**Entrada**: Descripción del usuario: "Crea la spec 000-core en specs/000-core (número 000, nombre corto "core"). Es el núcleo común de SI-18: no añade pantallas propias, define el vocabulario, las entidades, los estados, los valores de negocio, los roles y quién es responsable de cada cambio de estado, para que las specs 001 a 008 lo referencien en lugar de redefinirlo (constitución, principio VII). Usa como contenido el documento "Proyecto SDD/propuesta-000-core.md", secciones 2 a 9, incluidas las decisiones D1–D10 tomadas por el grupo (regístralas como Assumptions con su motivo). Las historias de usuario de 000-core son: (1) un único modelo de datos para las cuatro personas, (2) valores de negocio definidos una sola vez, (3) identidad, rol, tarifa y centro de trabajo comunes, (4) eventos entre historias con responsable claro. Todo en español."

## Alcance de esta spec

`000-core` no aporta pantallas propias (salvo la identificación común, ver HU-C3). Su producto es un
**contrato compartido**: vocabulario, entidades, estados, valores de negocio, roles y responsables de
cada cambio de estado. Las specs 001 a 008 lo referencian y no lo redefinen; si una spec contradice a
`000-core`, manda `000-core` (constitución, principio VII).

### Historias del sistema que se apoyan en esta base

| ID | Spec | Historia |
|----|------|----------|
| HU-01 | spec-001 | Buscar pistas libres por deporte, centro y fecha, con el precio de mi tarifa |
| HU-02 | spec-002 | Reservar una pista y pagarla en el momento |
| HU-03 | spec-003 | Cancelar una reserva (devolución si faltan más de 24 h) |
| HU-04 | spec-004 | Inscribirse en un curso trimestral con lista de espera |
| HU-05 | spec-005 | Solicitar franjas fijas de temporada para un club y aprobarlas/rechazarlas (concejalía) |
| HU-06 | spec-006 | Validar el acceso con el código QR |
| HU-07 | spec-007 | Cerrar una instalación por mantenimiento (cancelación masiva con devolución) |
| HU-08 | spec-008 | Informe mensual de ocupación e ingresos |

El reparto de historias entre personas del grupo se recoge en `docs/agrupamiento.md` (constitución,
«Acuerdos de colaboración»), no en esta spec.

## Escenarios de usuario y pruebas *(obligatorio)*

Los «usuarios» de estas historias son las cuatro personas del equipo que desarrollan las specs 001 a
008, y, a través de ellas, los usuarios finales del servicio.

### Historia de usuario 1 - Un único modelo de datos para las cuatro personas (Prioridad: P1)

Como miembro del equipo que desarrolla una de las historias HU-01 a HU-08, quiero que todas las
entidades del dominio (centro, instalación, reserva, pago, curso…) estén definidas una sola vez, con
su historia propietaria y sus lectoras, para poder trabajar en paralelo sin redefinirlas y para que
la integración de las ocho historias no falle.

**Por qué esta prioridad**: sin un modelo común, cuatro personas trabajando a la vez sobre reservas,
pagos e instalaciones producen definiciones incompatibles; es el bloqueo principal de la integración.

**Prueba independiente**: se revisa cada spec 001–008 y se comprueba que toda entidad que usa aparece
en la sección «Entidades clave» de esta spec, con el mismo nombre y los mismos atributos, y que solo
la historia propietaria la crea o la modifica.

**Escenarios de aceptación**:

1. **Dado** que la spec-002 necesita la entidad Reserva, **cuando** su autor la describe, **entonces**
   referencia la definición de `000-core` y no añade ni cambia atributos sin haberlos propuesto antes
   aquí.
2. **Dado** que la entidad Reserva tiene como propietaria a HU-02 (creación) y a HU-03/HU-07
   (cancelación), **cuando** otra historia (por ejemplo HU-08) necesita reservas, **entonces** solo
   las lee.
3. **Dado** que una spec necesita una entidad o atributo nuevo, **cuando** se detecta, **entonces** el
   cambio se propone primero en `000-core` mediante pull request revisado y solo después se usa.

---

### Historia de usuario 2 - Valores de negocio definidos una sola vez (Prioridad: P1)

Como miembro del equipo, quiero que las cifras que gobiernan las reglas (duración de franja, horario,
antelación, tiempo de bloqueo, plazo de devolución, ventana del QR, tarifas, zona horaria) estén
escritas en un único sitio, para que ninguna historia use un valor distinto al de otra.

**Por qué esta prioridad**: HU-01 y HU-02 deben calcular igual la disponibilidad y el precio; HU-03 y
HU-08 deben aplicar igual el plazo de 24 h. Un valor duplicado y divergente produce cobros o
devoluciones incorrectos.

**Prueba independiente**: se buscan en las specs 001–008 las cifras de negocio y se comprueba que
todas remiten a la tabla «Valores de negocio» de esta spec y que no hay ninguna cifra que la
contradiga.

**Escenarios de aceptación**:

1. **Dado** un ciudadano empadronado que busca (HU-01) una franja de pádel libre, **cuando** la reserva
   (HU-02), **entonces** el importe cobrado es el mismo que se le mostró en la búsqueda (6,00 €).
2. **Dado** que hoy es lunes 21 a las 18:00, **cuando** un ciudadano consulta o reserva, **entonces**
   puede elegir cualquier franja aún no empezada hasta la de 21:00–22:00 del lunes 28, y ninguna del
   martes 29.
3. **Dado** que el grupo decide cambiar una cifra (por ejemplo, el tiempo de bloqueo), **cuando** se
   cambia, **entonces** se modifica solo en esta spec y todas las historias pasan a aplicar el nuevo
   valor sin editarlas una a una.

---

### Historia de usuario 3 - Identidad, rol, tarifa y centro de trabajo comunes (Prioridad: P1)

Como persona que usa el servicio (ciudadano, representante de club, personal de centro o técnico de
concejalía), quiero identificarme una vez con mi correo y contraseña y que todas las funciones sepan
quién soy, qué rol tengo, qué tarifa me corresponde y, si soy personal, a qué centro estoy asignado.

**Por qué esta prioridad**: todas las historias dependen de saber el rol (permisos), la tarifa
(precio) o el centro (ámbito del personal). Sin esta base ninguna historia puede probarse de extremo
a extremo.

**Prueba independiente**: con los usuarios precargados de cada rol se inicia sesión y se comprueba
que el sistema conoce su rol, su tipo de tarifa y, para el personal, su centro; y que se rechaza una
contraseña incorrecta.

**Escenarios de aceptación**:

1. **Dado** un usuario precargado con rol ciudadano y empadronado = sí, **cuando** inicia sesión con
   su correo y contraseña correctos, **entonces** queda identificado como ciudadano con tarifa
   empadronado.
2. **Dado** un ciudadano con empadronado = no, **cuando** se identifica, **entonces** su tarifa es no
   empadronado.
3. **Dado** un representante de club, **cuando** se identifica, **entonces** su tarifa es club,
   independientemente de su dato de empadronamiento, y puede además solicitar franjas fijas para su
   club (HU-05).
4. **Dado** un empleado del personal asignado al centro A, **cuando** intenta validar un QR o cerrar
   una instalación del centro B, **entonces** la acción se rechaza indicando el motivo.
5. **Dado** cualquier usuario, **cuando** introduce una contraseña incorrecta, **entonces** no queda
   identificado y recibe un mensaje en español que no revela si el correo existe.
6. **Dado** un usuario de cualquier rol, **cuando** intenta una acción reservada a otro rol, **entonces**
   se rechaza.

---

### Historia de usuario 4 - Eventos entre historias con responsable claro (Prioridad: P2)

Como miembro del equipo, quiero saber qué historia es la única responsable de cada cambio de estado
(reserva, instalación, solicitud de franjas fijas, inscripción) y a qué otras historias afecta, para
coordinar las dependencias (por ejemplo, que una cancelación invalide el QR) sin solapamientos ni
huecos.

**Por qué esta prioridad**: los estados ya quedan definidos en las historias 1 y 2; esta historia fija
la responsabilidad y el impacto, imprescindible para integrar pero no para empezar a trabajar.

**Prueba independiente**: para cada transición de la tabla «Responsables de los cambios de estado»
se comprueba que existe exactamente una historia que la ejecuta y que las historias afectadas
declaran la dependencia en su spec.

**Escenarios de aceptación**:

1. **Dado** una reserva confirmada, **cuando** HU-03 la cancela, **entonces** el QR deja de valer en
   HU-06, la franja vuelve a aparecer libre en HU-01 y HU-08 tiene en cuenta la devolución si la hubo.
2. **Dado** una instalación con reservas confirmadas, **cuando** HU-07 la cierra por mantenimiento,
   **entonces** esas reservas pasan a canceladas con devolución íntegra, cada titular recibe un aviso
   en «Mis avisos» y la instalación deja de ofrecer franjas en HU-01 y HU-02 durante el cierre.
3. **Dado** una reserva cancelada o caducada, **cuando** cualquier historia intenta cambiar su estado,
   **entonces** el cambio se rechaza.
4. **Dado** una solicitud de franjas fijas que choca con reservas confirmadas o pendientes de pago
   dentro de la temporada, **cuando** el técnico intenta aprobarla, **entonces** la aprobación queda
   bloqueada y se le listan las reservas en conflicto.

---

### Casos límite

- **Antelación**: una franja de hoy + 7 días naturales es reservable; una de hoy + 8 días no. Una
  franja ya empezada (aunque sea por un minuto) no es reservable.
- **Bloqueo del pago**: una reserva pendiente de pago creada hace exactamente 10 minutos deja de
  bloquear la franja y pasa a caducada; si el pago se completa antes, pasa a confirmada.
- **Plazo de devolución**: con más de 24 h hasta el inicio, devolución íntegra; con exactamente 24 h o
  menos, sin devolución.
- **Ventana del QR**: válido exactamente 15 minutos antes del inicio y exactamente en la hora de fin;
  rechazado un minuto antes de la ventana o un minuto después del fin.
- **Cambio de horario de verano**: el plazo de 24 h y la ventana del QR se evalúan en hora de
  `Europe/Madrid`, de modo que una reserva situada justo después del cambio de hora calcula
  correctamente si faltan más de 24 h.
- **Concurrencia**: dos usuarios intentan reservar la misma franja a la vez; solo uno lo consigue y el
  otro recibe un conflicto explicado.
- **QR de otro centro**: el personal del centro A lee un QR válido de una reserva del centro B; se
  rechaza indicando que pertenece a otro centro.
- **Código QR inexistente o adivinado**: se rechaza y la validación queda registrada.
- **Cierre que solapa con una reserva pendiente de pago**: la reserva no puede confirmarse para una
  franja cerrada; las reservas confirmadas en el periodo se cancelan con devolución íntegra.
- **Franja fija que se solapa con otra franja fija ya aprobada**: no se puede aprobar.
- **Representante de club empadronado**: aplica siempre la tarifa club.
- **Instalación no reservable por franjas** (sala, piscina): nunca aparece en la búsqueda de HU-01.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Modelo y gobierno de la base común**

- **RF-001**: El sistema DEBE usar un único modelo de datos con las entidades definidas en «Entidades
  clave»; ninguna spec 001–008 DEBE redefinirlas.
- **RF-002**: Cada entidad DEBE tener una única historia propietaria, que es la única que la crea o
  modifica; las demás historias solo la leen.
- **RF-003**: Toda entidad, estado o valor de negocio nuevo o distinto DEBE proponerse primero en esta
  spec, mediante pull request revisado por al menos otra persona del grupo, antes de usarse en otra
  spec.

**Identidad, roles y tarifas**

- **RF-004**: Los usuarios DEBEN venir precargados con datos ficticios; no existe registro de usuarios.
- **RF-005**: El sistema DEBE permitir identificarse con correo y contraseña y DEBE guardar la
  contraseña de forma que no pueda recuperarse en claro.
- **RF-006**: Cada usuario DEBE tener exactamente un rol: ciudadano, representante de club, personal de
  centro o técnico de concejalía.
- **RF-007**: El tipo de tarifa DEBE determinarse así: ciudadano empadronado → empadronado; ciudadano no
  empadronado → no empadronado; representante de club → club.
- **RF-008**: Cada representante de club DEBE estar asociado a un club; cada miembro del personal de
  centro DEBE estar asignado a exactamente un centro.
- **RF-009**: El sistema DEBE restringir las acciones por rol según la tabla «Roles y permisos»; el
  personal de centro solo DEBE poder validar QR y cerrar instalaciones de su propio centro.

**Disponibilidad, franjas y precio**

- **RF-010**: Las franjas DEBEN ser de 1 hora en punto, dentro del horario de apertura del centro, y
  DEBEN calcularse, no almacenarse.
- **RF-011**: Una franja DEBE considerarse libre solo si: no tiene reserva confirmada ni pendiente de
  pago vigente; no está bloqueada por una franja fija de club aprobada; la instalación no está cerrada
  por mantenimiento en ese momento; no ha empezado; y su fecha está entre hoy y hoy + 7 días naturales.
- **RF-012**: Solo las pistas de tenis y pádel DEBEN ser reservables por franjas por ciudadanos y
  representantes; los pabellones solo por franjas fijas de club; salas y piscinas solo para cursos.
- **RF-013**: La búsqueda (HU-01) y la reserva (HU-02) DEBEN aplicar exactamente el mismo cálculo de
  disponibilidad y de precio; la reserva DEBE volver a comprobar ambos en el momento de reservar.
- **RF-014**: El sistema DEBE garantizar como máximo una reserva activa (pendiente de pago vigente o
  confirmada) por instalación y franja, también ante solicitudes simultáneas.

**Reservas, pagos, devoluciones y acceso**

- **RF-015**: Las reservas DEBEN seguir exclusivamente las transiciones: pendiente de pago → confirmada;
  pendiente de pago → caducada; confirmada → cancelada. Cancelada y caducada son estados finales.
- **RF-016**: Una reserva pendiente de pago DEBE pasar a caducada si el pago falla o no se completa en
  10 minutos desde su creación.
- **RF-017**: Al confirmarse, la reserva DEBE recibir un código de acceso (QR) opaco y aleatorio,
  distinto de su identificador y no deducible.
- **RF-018**: El QR DEBE ser válido solo para reservas confirmadas, desde 15 minutos antes del inicio
  hasta el fin de la reserva, ambos incluidos; cada intento de validación DEBE registrarse con su
  resultado y, si se rechaza, con el motivo.
- **RF-019**: La cancelación por el usuario DEBE generar devolución íntegra si faltan más de 24 h para
  el inicio y ninguna devolución con 24 h exactas o menos.
- **RF-020**: El cierre por mantenimiento DEBE reutilizar el mismo mecanismo de cancelación y
  devolución, con motivo «cierre por mantenimiento» y devolución íntegra siempre, y DEBE avisar a cada
  titular afectado.
- **RF-021**: El pago y la devolución DEBEN realizarse a través de una pasarela simulada; las
  devoluciones se registran directamente como completadas.

**Franjas fijas, cursos y avisos**

- **RF-022**: Una solicitud de franjas fijas DEBE pasar de pendiente a aprobada o rechazada; no DEBE
  poder aprobarse mientras existan reservas confirmadas o pendientes de pago en conflicto dentro de la
  temporada, ni si se solapa con otra franja fija aprobada.
- **RF-023**: La inscripción en cursos DEBE ser gratuita; sus estados son confirmada, en lista de
  espera y baja; la posición en la lista de espera DEBE calcularse por fecha y hora de llegada; la baja
  no genera devolución.
- **RF-024**: Los avisos al usuario DEBEN mostrarse solo dentro de la aplicación, en «Mis avisos»;
  pueden crearlos HU-04, HU-05 y HU-07.

**Informe y transversales**

- **RF-025**: El informe mensual (HU-08) DEBE calcularse según las reglas de cómputo de la sección
  «Reglas que cruzan historias».
- **RF-026**: Todas las reglas que dependen de la hora DEBEN evaluarse en la zona horaria
  `Europe/Madrid`, teniendo en cuenta el cambio de horario de verano (constitución, principio IX).
- **RF-027**: Los importes DEBEN expresarse en euros con dos decimales exactos y formato español
  (por ejemplo, «6,00 €»).
- **RF-028**: Todo rechazo por regla de negocio o permisos DEBE explicar el motivo al usuario en
  español.

### Roles y permisos

| Rol | Quién es | Qué puede hacer |
|-----|----------|-----------------|
| **Ciudadano** | Persona usuaria del servicio | HU-01, HU-02, HU-03, HU-04 |
| **Representante de club** | Ciudadano que actúa en nombre de un club | Lo del ciudadano (con tarifa club) + solicitar franjas fijas (HU-05) |
| **Personal de centro** | Trabajador asignado a **un** centro | HU-06 y HU-07, solo en su centro |
| **Técnico de concejalía** | Personal de la concejalía | HU-08 y aprobar/rechazar solicitudes de HU-05 |

### Valores de negocio (única fuente de verdad)

| Valor | Cifra | Se aplica en |
|-------|-------|--------------|
| Duración de una franja | 1 hora, en punto | HU-01, HU-02, HU-05, HU-08 |
| Horario de apertura de los centros | 08:00–22:00 todos los días; 14 franjas (última 21:00–22:00) | HU-01, HU-05, HU-08 |
| Antelación máxima de reserva | Fecha de la franja entre hoy y hoy + 7 días naturales, ambos incluidos (ej.: el lunes 21 a las 18:00 se puede reservar hasta la franja de 21:00 del lunes 28) | HU-01, HU-02 |
| Reserva de franjas ya empezadas | No se permite | HU-01, HU-02 |
| Tiempo de bloqueo mientras se paga | 10 minutos desde que se crea la reserva | HU-02 (lo respeta HU-01) |
| Plazo para devolución al cancelar | Íntegra si faltan **más de 24 h** para el inicio; con 24 h exactas o menos, sin devolución | HU-03, HU-08 |
| Devolución por cierre de mantenimiento | Siempre íntegra, sea cual sea la antelación | HU-07, HU-08 |
| Ventana de validez del QR | Desde 15 minutos antes del inicio hasta el fin de la reserva, ambos incluidos | HU-06 |
| Tarifas (€ por hora) | Empadronado 6,00 €; no empadronado 10,00 €; club 4,00 € (iguales para tenis y pádel) | HU-01, HU-02, HU-08 |
| Duración de la temporada | De septiembre a junio | HU-05 |
| Zona horaria | `Europe/Madrid` (fechas almacenadas en UTC) | Todas |

### Estados

- **Reserva**: pendiente de pago (recién creada; bloquea la franja durante el tiempo de bloqueo) ·
  confirmada (pago completado; tiene QR válido dentro de su ventana) · caducada (pago fallido o tiempo
  agotado; la franja vuelve a estar libre) · cancelada (por el usuario o por cierre de mantenimiento;
  el QR deja de valer). Transiciones permitidas: pendiente de pago → confirmada | caducada;
  confirmada → cancelada. Cancelada y caducada son finales.
- **Pago**: pendiente → completado | fallido.
- **Devolución**: se registra completada (pasarela simulada).
- **Instalación**: operativa ↔ cerrada por mantenimiento (durante un periodo).
- **Solicitud de franjas fijas**: pendiente → aprobada | rechazada.
- **Inscripción**: confirmada · en lista de espera · baja.

### Responsables de los cambios de estado

| Cambio | Responsable | Afecta a |
|--------|-------------|----------|
| Reserva nueva → pendiente de pago (la franja deja de estar libre) | HU-02 | HU-01 |
| Reserva pendiente de pago → confirmada (se genera el QR) | HU-02 | HU-01, HU-06, HU-08 |
| Reserva pendiente de pago → caducada (pago fallido o tiempo agotado) | HU-02 | HU-01 |
| Reserva confirmada → cancelada por el usuario (+ devolución si procede) | HU-03 | HU-01, HU-06, HU-08 |
| Instalación → cerrada por mantenimiento; reservas afectadas → canceladas con devolución íntegra y aviso | HU-07 | HU-01, HU-02, HU-06, HU-08 |
| Instalación → operativa (reapertura) | HU-07 | HU-01, HU-02 |
| Solicitud de franjas fijas → pendiente / aprobada / rechazada (+ aviso al club) | HU-05 | HU-01, HU-02, HU-08 |
| Inscripción → confirmada / en lista de espera / baja; paso de la lista a confirmada (+ aviso) | HU-04 | — |
| Validación de acceso registrada | HU-06 | — |

### Reglas que cruzan historias

- **Disponibilidad y precio idénticos en HU-01 y HU-02**: una franja que aparece libre en la búsqueda
  tiene que poder reservarse al precio mostrado; HU-02 lo vuelve a comprobar al reservar.
- **Una sola reserva activa por franja** (pendiente de pago vigente o confirmada), garantizada también
  a nivel de almacenamiento de datos y no solo en la lógica (constitución, principio III).
- **El QR es un código opaco y aleatorio**, no el identificador de la reserva; lo valida siempre el
  servidor.
- **El personal solo actúa en su centro**: un QR de otro centro se rechaza indicando el motivo.
- **Cierre por mantenimiento** reutiliza el mecanismo de cancelación y devolución de HU-03 con motivo
  «cierre por mantenimiento» y devolución íntegra siempre.
- **Aprobación de franjas fijas con reservas en conflicto** (HU-05): no se aprueba mientras haya
  reservas confirmadas o pendientes de pago en conflicto dentro de la temporada; la aprobación lista
  esas reservas y la acción «aprobar» queda bloqueada. El técnico puede rechazar o esperar a que esas
  reservas pasen o se cancelen. Tampoco se aprueba si se solapa con otra franja fija aprobada.
- **Cómputo del informe (HU-08)**:
  - Instalaciones incluidas: las usadas por franjas (pistas y pabellones); salas y piscinas quedan
    fuera.
  - Horas disponibles del mes = horas de apertura de cada instalación − horas cerradas por
    mantenimiento.
  - Horas ocupadas = horas de reservas confirmadas (no canceladas) + horas bloqueadas por franjas fijas
    de club aprobadas (fuera de los periodos de cierre).
  - Ingresos = pagos completados − devoluciones. Una cancelación sin devolución sí cuenta como
    ingreso; una con devolución, no. Las reservas caducadas no generan ingreso.
  - Las franjas fijas de club no se cobran; el ingreso «club» sale de las reservas sueltas con tarifa
    club.
- **Cursos**: inscripción gratuita; el pago de cursos queda fuera de alcance; la baja no genera
  devolución.

### Entidades clave

| Entidad | Qué representa y atributos | Propietaria (crea/modifica) | Lectoras |
|---------|----------------------------|------------------------------|----------|
| **Centro** | Polideportivo del municipio (4). Nombre, dirección, horario de apertura. | Precargado | Todas |
| **Deporte** | Tenis, pádel, natación, yoga, pilates… | Precargado | Todas |
| **Instalación** | Pista, pabellón, sala o piscina de un centro. Tipo, deporte(s) que admite, centro, reservable por franjas (sí/no), estado (operativa / cerrada por mantenimiento). | HU-07 (estado); resto precargado | HU-01, HU-02, HU-05, HU-06, HU-08 |
| **Tarifa** | Precio por hora de una instalación (o tipo de instalación) para cada tipo de tarifa; importe en euros con decimales exactos. | Precargada | HU-01, HU-02 |
| **Franja** | Hueco de 1 h de una instalación en una fecha y hora de inicio. **No se almacena**: se calcula a partir del horario del centro. | HU-01 (cálculo) | HU-02 |
| **Usuario** | Nombre, correo, contraseña (protegida), rol, empadronado (sí/no), club (si es representante), centro de trabajo (si es personal). | Precargado | Todas |
| **Club** | Nombre del club deportivo. | Precargado | HU-02, HU-05 |
| **Reserva** | Usuario, instalación, fecha y hora de inicio y fin, tipo de tarifa aplicada, importe, estado, fecha de creación, código de acceso (QR), fecha y motivo de cancelación (si aplica). | HU-02 (crea); HU-03 y HU-07 (cancelan) | HU-01, HU-06, HU-08 |
| **Pago** | Reserva, importe, estado, fecha. Mediante pasarela simulada. | HU-02 | HU-03, HU-07, HU-08 |
| **Devolución** | Pago, importe, motivo (cancelación del usuario / cierre por mantenimiento), fecha. | HU-03, HU-07 | HU-08 |
| **Cierre por mantenimiento** | Instalación, fecha y hora de inicio y fin, motivo, persona que lo registra. | HU-07 | HU-01, HU-02, HU-08 |
| **Temporada** | Nombre, fecha de inicio y fin (septiembre a junio). | Precargada | HU-05 |
| **Solicitud de franjas fijas** | Club, instalación, días de la semana, hora de inicio y fin, temporada, estado, fecha de resolución. | HU-05 | HU-01, HU-02, HU-08 |
| **Curso** | Nombre, deporte, instalación, trimestre, días y horario, plazas máximas, precio. | Precargado; gestionado por HU-04 | HU-04 |
| **Inscripción** | Usuario, curso, estado, fecha y hora de llegada (ordena la lista de espera; la posición se calcula). | HU-04 | — |
| **Notificación** | Usuario, texto, fecha, leída (sí/no). Se muestra en «Mis avisos». | HU-04, HU-05, HU-07 | Usuario destinatario |
| **Validación de acceso** | Reserva (si existe), código leído, resultado (aceptado / motivo del rechazo), fecha y hora, persona del personal. | HU-06 | Evidencia y demo |

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El 100 % de las entidades, estados y valores de negocio usados en las specs 001–008
  remiten a esta spec; una revisión cruzada (por ejemplo, con `/speckit-analyze`) no encuentra ninguna
  redefinición ni contradicción.
- **CE-002**: Cada cifra de negocio aparece escrita una sola vez en todo el conjunto de specs; cambiar
  una cifra requiere editar un único sitio.
- **CE-003**: El 100 % de las transiciones de estado tiene exactamente una historia responsable y
  ninguna queda sin responsable.
- **CE-004**: Para cada rol existe al menos un usuario precargado con el que se puede iniciar sesión, y
  el 100 % de los intentos de acción fuera del rol o del centro asignado se rechazan con un motivo.
- **CE-005**: En el 100 % de los casos, el precio que se muestra en la búsqueda coincide con el importe
  cobrado al reservar esa misma franja.
- **CE-006**: Todas las reglas con límite (7 días, 10 minutos, 24 h, 15 minutos) tienen casos de prueba
  a ambos lados del límite, incluido un caso en el cambio de horario de verano (constitución,
  principio II).
- **CE-007**: Las cuatro personas del equipo pueden empezar sus specs en paralelo sin tener que acordar
  de nuevo ningún término, estado o cifra recogido aquí.

## Supuestos

### Decisiones del grupo (21/10/2026)

- **D1 – Registro e identificación**: usuarios precargados (ficticios) e identificación común con
  correo y contraseña, sin registro; se implementa entre todos en `develop` antes de abrir las ramas.
  *Motivo*: ninguna historia pide el alta, pero todas necesitan rol y tarifa.
- **D2 – Qué se reserva por franjas**: solo pistas de tenis y pádel; pabellones para franjas fijas de
  clubes (un club puede pedirlas en pistas o pabellones); salas y piscinas para cursos.
  *Motivo*: la narrativa habla de «buscar pistas libres».
- **D3 – «7 días de antelación»**: fecha de la franja entre hoy y hoy + 7 días naturales, ambos
  incluidos. *Motivo*: fácil de explicar y de probar; no cambia cada hora.
- **D4 – Bloqueo durante el pago**: 10 minutos desde que se crea la reserva. *Motivo*: lo habitual en
  webs de reservas.
- **D5 – Horario y tarifas**: 08:00–22:00 (14 franjas); 6 € empadronado, 10 € no empadronado, 4 € club
  por hora. *Motivo*: valores de ejemplo; la narrativa no los da.
- **D6 – Canal de los avisos**: solo dentro de la app («Mis avisos»), sin correo. *Motivo*: simplicidad
  (principio VIII); fácil de mostrar en la demo.
- **D7 – Ámbito del personal**: solo actúa en su centro asignado. *Motivo*: da un caso de permisos
  verificable.
- **D8 – Franja fija que choca con reservas**: se bloquea la aprobación hasta que no haya conflicto.
  *Motivo*: nadie pierde una reserva pagada; más simple.
- **D9 – Cómputo del informe**: ocupadas = reservas confirmadas + franjas de club; disponibles =
  apertura − cierres; ingresos = pagos − devoluciones; clubes no se cobran; solo pistas y pabellones.
  *Motivo*: resuelve la ambigüedad señalada en el README.
- **D10 – Pago de cursos**: fuera de alcance; inscripción gratuita. *Motivo*: la narrativa no lo
  menciona.

### Otros supuestos

- La temporada va de septiembre a junio y está precargada (valor supuesto por el grupo).
- Los datos de centros (4), deportes, instalaciones, tarifas, usuarios, clubes, temporada y cursos son
  precargados y ficticios (constitución, principio VI); no hay pantallas para administrarlos.
- La pasarela de pago es simulada; no se procesan pagos reales.
- Cada persona tiene un único rol; un representante de club es un ciudadano que actúa en nombre de un
  único club.
- La pila técnica, el plan y el modelo de datos detallado (`plan.md`, `data-model.md`) se derivan de
  esta spec en `/speckit-plan`, conforme a la constitución.
