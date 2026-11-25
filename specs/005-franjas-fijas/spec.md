# Especificación: 005 · Franjas fijas de temporada para clubes (HU-05)

**Rama de trabajo**: `feature/005-franjas-fijas`

**Creada**: 2026-11-25

**Estado**: Borrador

**Responsable**: Artur (módulo `Disponibilidad`, `docs/agrupamiento.md`)

**Entrada**: Descripción del usuario: "Crea la spec 005 en specs/005-franjas-fijas (número 005, nombre corto "franjas-fijas"). Es la HU-05 de SI-18, de Artur (módulo Disponibilidad): "Como representante de un club deportivo, quiero solicitar franjas fijas para toda la temporada (por ejemplo, pabellón 2, martes y jueves de 19 a 21 h), para organizar los entrenamientos con horarios estables". Detalles del README: la solicitud indica instalación, días de la semana, horario y temporada; queda pendiente hasta que la concejalía la revisa; la concejalía (técnico) puede aprobarla o rechazarla; si se aprueba, las franjas se bloquean para el resto de usuarios durante toda la temporada. Criterios de aceptación (COS): una solicitud nueva aparece como pendiente y no bloquea nada todavía; al aprobarla, las franjas dejan de estar disponibles en las búsquedas de otros usuarios; al rechazarla, las franjas siguen disponibles y el club recibe el resultado; no se puede aprobar una solicitud que choque con reservas ya existentes sin tratarlas antes. Esta spec NO redefine nada de 000-core (constitución, principio VII): referencia specs/000-core para la entidad SolicitudFranjasFijas (data-model.md: club, instalación pista o pabellón según D2, días ISO 1–7 sin repetidos, horas en punto entre 08:00 y 22:00, temporada, estado pendiente/aprobada/rechazada con fecha de resolución, transiciones Aprobar/Rechazar ya implementadas en el dominio), RF-022 (no se aprueba con reservas confirmadas o pendientes de pago en conflicto dentro de la temporada, ni si se solapa con otra franja fija aprobada), D8 (la aprobación queda bloqueada y se listan las reservas en conflicto; el técnico puede rechazar o esperar), la temporada de septiembre a junio, los avisos en «Mis avisos» (RF-024, D6: el club recibe el resultado), la suspensión durante los cierres por mantenimiento (aclaración de 000-core: la franja fija queda suspendida solo durante el cierre y el representante recibe un aviso, que crea HU-07), los roles (representante solicita; técnico de concejalía aprueba o rechaza), el prefijo /api/solicitudes-franjas-fijas y los mensajes del catálogo de errores. HU-05 es dueña de implementar de forma definitiva IConsultaFranjasFijas (contracts/modulos.md): FranjasBloqueadas, que excluye los periodos de cierre y que hoy usa HU-01 con una versión provisional en specs/001-buscar-pistas (RF-014a) que esta spec sustituye y borra, y RepresentantesAfectados, que consume HU-07. Para detectar reservas en conflicto usa IConsultaReservas (HU-02). Todo en español de España."

## Alcance de esta spec

HU-05 permite a un **representante de club** pedir franjas fijas semanales para toda una temporada y a un
**técnico de concejalía** aprobarlas o rechazarlas. Una solicitud aprobada bloquea esas horas para el
resto de usuarios en la búsqueda (HU-01) y en la reserva (HU-02).

Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa tal cual:

| Concepto                                                        | Dónde está definido en `000-core`                              |
| --------------------------------------------------------------- | -------------------------------------------------------------- |
| Solicitud de franjas fijas: campos, reglas y estados            | data-model.md § SolicitudFranjasFijas; spec.md «Estados»       |
| Qué instalaciones admiten franjas fijas (pistas y pabellones)   | spec.md D2                                                     |
| No aprobar con reservas en conflicto ni solapada con otra fija  | spec.md RF-022, D8 y «Reglas que cruzan historias»             |
| Temporada (septiembre a junio) y la temporada «2026-2027»       | spec.md «Valores de negocio»; data-model.md § Temporada        |
| Avisos en «Mis avisos»                                          | spec.md RF-024, D6                                             |
| Suspensión de una franja fija durante un cierre                 | spec.md, aclaración de la sesión 2026-10-28; RF-020            |
| Quién puede hacer qué                                           | spec.md «Roles y permisos»                                     |
| Horario 08:00–22:00, franjas de 1 h en punto, zona Madrid        | spec.md «Valores de negocio»; constitución, principio IX       |
| Interfaz `IConsultaFranjasFijas`                                | contracts/modulos.md                                           |
| Prefijo de ruta                                                 | contracts/README.md: `/api/solicitudes-franjas-fijas`          |

**Fuera de alcance**: cerrar instalaciones y avisar de la suspensión (HU-07, persona C), cancelar las
reservas que chocan con una solicitud (HU-03 la cancela su titular), retirar o modificar una solicitud
ya enviada o aprobada, cobrar las franjas fijas (no se cobran, D9 de 000-core) y el informe mensual
(HU-08).

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Solicitar franjas fijas para mi club (Prioridad: P1)

Diego, representante del «Club Deportivo Ficticio Norte», pide para su club el pabellón del
Polideportivo Norte los martes y jueves de 19:00 a 21:00 durante la temporada 2026-2027. La solicitud
queda **pendiente** y, mientras tanto, esas horas siguen libres para todo el mundo. Diego ve la lista
de solicitudes de su club con su estado.

**Por qué esta prioridad**: sin solicitud no hay nada que revisar; es el inicio de la historia.

**Prueba independiente**: como representante, se envía una solicitud válida y se comprueba que aparece
como pendiente en la lista del club y que las franjas siguen apareciendo libres en la búsqueda de HU-01.

**Escenarios de aceptación**:

1. **Dado** Diego con sesión iniciada, **cuando** pide el pabellón del Polideportivo Norte los martes y
   jueves de 19:00 a 21:00 en la temporada 2026-2027, **entonces** la solicitud queda guardada como
   **pendiente**, a nombre de su club, y aparece en la lista de solicitudes de su club.
2. **Dado** esa solicitud pendiente, **cuando** cualquier persona busca esa instalación un martes de la
   temporada, **entonces** las franjas de 19:00 y 20:00 siguen apareciendo libres (si nada más las
   ocupa).
3. **Dado** que Diego elige una sala o una piscina, **cuando** envía la solicitud, **entonces** se
   rechaza explicando que esa instalación no admite franjas fijas.
4. **Dado** que Diego no elige ningún día, repite un día, pone una hora que no es en punto, una hora
   de fin anterior o igual a la de inicio, o una hora fuera de 08:00–22:00, **cuando** envía la
   solicitud, **entonces** se rechaza marcando el dato incorrecto.
5. **Dado** que Diego elige una temporada que ya ha terminado, **cuando** envía la solicitud,
   **entonces** se rechaza explicando el motivo.
6. **Dado** otro representante de otro club, **cuando** consulta sus solicitudes, **entonces** no ve
   las del club de Diego.

---

### Historia de usuario 2 - Aprobar una solicitud sin conflictos (Prioridad: P1)

Javier, técnico de concejalía, ve las solicitudes pendientes de todos los clubes y aprueba la de
Diego. Desde ese momento, los martes y jueves de 19:00 a 21:00 de la temporada desaparecen de la
búsqueda y no se pueden reservar. Los representantes del club reciben un aviso con el resultado.

**Por qué esta prioridad**: es lo que hace que la franja fija sirva para algo: el bloqueo efectivo.

**Prueba independiente**: con una solicitud pendiente sin conflictos, el técnico la aprueba y se
comprueba que la búsqueda de HU-01 ya no muestra esas franjas en ninguna fecha de la temporada y que
el representante tiene un aviso en «Mis avisos».

**Escenarios de aceptación**:

1. **Dado** una solicitud pendiente sin reservas ni franjas fijas en conflicto, **cuando** Javier la
   aprueba, **entonces** pasa a **aprobada** con su fecha de resolución.
2. **Dado** esa solicitud aprobada, **cuando** cualquier persona busca esa instalación un martes o un
   jueves de la temporada, **entonces** las franjas de 19:00 y 20:00 no aparecen; los demás días y
   horas siguen igual.
3. **Dado** esa solicitud aprobada, **cuando** alguien intenta reservar una de esas franjas (HU-02),
   **entonces** se rechaza con el motivo «La franja está reservada para un club durante la
   temporada.».
4. **Dado** la aprobación, **cuando** termina, **entonces** cada representante del club recibe en «Mis
   avisos» un aviso que dice que la solicitud ha sido aprobada, con la instalación, los días y el
   horario.
5. **Dado** una fecha fuera de la temporada de la solicitud (por ejemplo, julio), **cuando** se busca,
   **entonces** esas horas siguen libres.

---

### Historia de usuario 3 - Ver los conflictos que impiden aprobar (Prioridad: P1)

Si en la temporada ya hay reservas confirmadas o pendientes de pago en esos días y horas, o si la
solicitud se solapa con otra franja fija ya aprobada, Javier no puede aprobarla: ve la lista de lo que
choca y puede rechazarla o esperar a que esas reservas pasen o se cancelen.

**Por qué esta prioridad**: es un criterio de aceptación explícito y protege a quien ya pagó (D8 de
000-core): nadie pierde una reserva por una franja fija.

**Prueba independiente**: con una reserva confirmada un martes a las 19:00 en la instalación de una
solicitud pendiente, el técnico intenta aprobar y se comprueba que no se aprueba y que la reserva
aparece en la lista de conflictos.

**Escenarios de aceptación**:

1. **Dado** una solicitud pendiente y una reserva **confirmada** en esa instalación un martes de la
   temporada a las 19:00, **cuando** Javier consulta la solicitud, **entonces** ve esa reserva en la
   lista de conflictos (fecha, hora e instalación) y la acción «Aprobar» aparece bloqueada.
2. **Dado** ese conflicto, **cuando** Javier intenta aprobar igualmente, **entonces** se rechaza con un
   motivo que explica que hay reservas en conflicto, y la solicitud sigue pendiente.
3. **Dado** una reserva **pendiente de pago vigente** en una de esas franjas, **cuando** Javier
   consulta la solicitud, **entonces** también cuenta como conflicto; si caduca, deja de contar.
4. **Dado** que la reserva en conflicto se cancela o ya ha pasado, **cuando** Javier vuelve a
   consultar la solicitud, **entonces** ya no aparece en la lista y puede aprobarla.
5. **Dado** otra solicitud ya **aprobada** para la misma instalación y temporada que comparte al menos
   un día y una hora, **cuando** Javier intenta aprobar la nueva, **entonces** se rechaza con un motivo
   que explica que se solapa con otra franja fija aprobada.
6. **Dado** dos solicitudes **pendientes** que se solapan, **cuando** Javier aprueba la primera,
   **entonces** la segunda pasa a mostrar el solape y ya no se puede aprobar.

---

### Historia de usuario 4 - Rechazar una solicitud (Prioridad: P2)

Javier rechaza una solicitud. Las horas siguen libres y los representantes del club reciben un aviso
con el resultado.

**Por qué esta prioridad**: completa el ciclo de la solicitud y es un criterio de aceptación, pero
aporta valor una vez que existen solicitudes y aprobaciones.

**Prueba independiente**: el técnico rechaza una solicitud pendiente y se comprueba su estado, que las
franjas siguen libres en la búsqueda y el aviso del representante.

**Escenarios de aceptación**:

1. **Dado** una solicitud pendiente (tenga o no conflictos), **cuando** Javier la rechaza, **entonces**
   pasa a **rechazada** con su fecha de resolución.
2. **Dado** la solicitud rechazada, **cuando** se busca esa instalación en esos días y horas,
   **entonces** las franjas siguen libres.
3. **Dado** el rechazo, **cuando** termina, **entonces** cada representante del club recibe en «Mis
   avisos» un aviso que dice que la solicitud ha sido rechazada, con la instalación, los días y el
   horario.
4. **Dado** una solicitud ya aprobada o rechazada, **cuando** Javier intenta aprobarla o rechazarla otra
   vez, **entonces** se rechaza explicando que ya está resuelta.

---

### Historia de usuario 5 - Las franjas fijas bloquean de verdad, también con cierres (Prioridad: P2)

HU-01 y HU-02 consultan a esta historia qué franjas bloquean los clubes; HU-07 le pregunta qué clubes
se ven afectados por un cierre. Durante un cierre por mantenimiento, la franja fija queda suspendida y
la hora cuenta como cerrada, no como reservada para el club.

**Por qué esta prioridad**: sustituye la consulta provisional que dejó la spec-001 (RF-014a de
spec-001) y da a HU-07 lo que necesita; sin ella, las aprobaciones de la historia 2 no bloquearían
nada de forma definitiva.

**Prueba independiente**: con una franja fija aprobada y un cierre que la cubre en parte, se comprueba
que la consulta de franjas bloqueadas no devuelve las horas cerradas y que la de representantes
afectados devuelve a los representantes del club.

**Escenarios de aceptación**:

1. **Dado** una franja fija aprobada martes de 19:00 a 21:00, **cuando** se piden las franjas
   bloqueadas de esa instalación para un martes de la temporada, **entonces** se devuelven las de 19:00
   y 20:00.
2. **Dado** además un cierre esa misma tarde de 19:00 a 20:00, **cuando** se piden las franjas
   bloqueadas, **entonces** solo se devuelve la de 20:00 (la de 19:00 está suspendida por el cierre).
3. **Dado** un cierre de una instalación que coincide con franjas fijas aprobadas de uno o varios
   clubes, **cuando** HU-07 pregunta por los representantes afectados, **entonces** recibe los
   representantes de esos clubes, sin repetir y sin los de clubes no afectados.
4. **Dado** que esta historia publica su consulta definitiva, **cuando** se fusiona, **entonces** la
   versión provisional de spec-001 deja de existir y la búsqueda de HU-01 da el mismo resultado que
   antes para las franjas fijas.

---

### Historia de usuario 6 - Solicitar y revisar sin barreras de accesibilidad (Prioridad: P3)

Las pantallas de solicitud y de revisión se usan enteras con teclado y lector de pantalla
(constitución, principio I).

**Por qué esta prioridad**: principio no negociable; se comprueba sobre las pantallas ya construidas.

**Prueba independiente**: se completa una solicitud y una revisión solo con el teclado y con lector de
pantalla, y se pasa la comprobación de quickstart.md §6 de `000-core`.

**Escenarios de aceptación**:

1. **Dado** el formulario de solicitud, **cuando** se recorre con el tabulador, **entonces** todos los
   campos (instalación, días, hora de inicio, hora de fin, temporada) tienen etiqueta visible, los días
   se eligen con casillas que se activan con el teclado y el foco siempre se ve.
2. **Dado** un envío, una aprobación o un rechazo, **cuando** termina bien o mal, **entonces** el
   resultado se anuncia a los lectores de pantalla.
3. **Dado** la lista de solicitudes y la de conflictos, **cuando** se recorren, **entonces** el estado
   («Pendiente», «Aprobada», «Rechazada») y cada conflicto se leen en texto, no solo con color.

---

### Casos límite

- **Temporada en curso**: la temporada 2026-2027 empezó en septiembre. Una franja fija aprobada en
  noviembre bloquea desde la aprobación hasta el fin de la temporada (30/06/2027); los días ya pasados
  no importan.
- **Conflictos solo hacia delante**: una reserva que ya ha terminado no es un conflicto; una que
  empieza más tarde que ahora, dentro de la temporada, sí.
- **Pendiente que caduca**: una reserva pendiente de pago cuenta como conflicto mientras está vigente
  (10 minutos, 000-core); al caducar deja de contar.
- **Solape parcial**: martes 19:00–21:00 y martes 20:00–22:00 en la misma instalación se solapan (en la
  franja de 20:00). Martes 19:00–20:00 y martes 20:00–21:00 no se solapan.
- **Mismo horario, otro día**: martes 19:00–21:00 y miércoles 19:00–21:00 no se solapan.
- **Otra instalación u otra temporada**: no hay solape.
- **Representante con varias solicitudes**: un club puede tener varias solicitudes pendientes o
  aprobadas a la vez, siempre que las aprobadas no se solapen entre sí.
- **Dos representantes del mismo club**: ven las mismas solicitudes (son del club) y los dos reciben
  los avisos.
- **Cambio de hora**: los días del cambio de horario de verano, una franja fija de 19:00 a 21:00 sigue
  bloqueando las franjas de 19:00 y 20:00 en hora de Madrid.
- **Cierre que cubre toda la franja fija**: ese día no se bloquea nada por el club; las horas cuentan
  como cerradas.
- **Roles**: un ciudadano, un miembro del personal o un técnico no puede enviar solicitudes; un
  representante no puede aprobar ni rechazar. Se rechaza con `ROL_NO_PERMITIDO`.
- **Solicitud de otro club**: un representante que pide ver una solicitud de otro club recibe
  `NO_ENCONTRADO`, sin revelar que existe.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Solicitar (representante de club)**

- **RF-001**: Solo un representante de club DEBE poder enviar solicitudes, siempre a nombre de su
  propio club (el de su sesión, no uno elegido).
- **RF-002**: La solicitud DEBE indicar instalación, días de la semana, hora de inicio, hora de fin y
  temporada, con las reglas de `000-core` (data-model.md § SolicitudFranjasFijas): instalación pista o
  pabellón (D2); al menos un día, sin repetir, de lunes a domingo; horas en punto, inicio ≥ 08:00, fin ≤
  22:00 y fin posterior al inicio.
- **RF-003**: Solo DEBEN poder elegirse temporadas que no hayan terminado (fecha de fin ≥ hoy en Madrid).
- **RF-004**: Una solicitud nueva DEBE quedar **pendiente** y NO DEBE bloquear ninguna franja.
- **RF-005**: Un dato incorrecto DEBE rechazarse marcando el campo, con un texto en español; una
  instalación que no es pista ni pabellón, o una temporada terminada, DEBEN rechazarse con su motivo.
- **RF-006**: Un representante DEBE poder ver la lista de solicitudes de su club con instalación, días,
  horario, temporada, estado y fecha de resolución; no DEBE ver las de otros clubes.

**Revisar (técnico de concejalía)**

- **RF-007**: Solo un técnico de concejalía DEBE poder aprobar o rechazar solicitudes, y DEBE poder ver
  las solicitudes de todos los clubes, empezando por las pendientes.
- **RF-008**: Para cada solicitud pendiente, el técnico DEBE poder ver sus **conflictos**:
  - las reservas confirmadas o pendientes de pago vigentes de esa instalación que empiezan entre ahora
    y el fin de la temporada, un día de la semana de la solicitud y a una hora de la solicitud (fecha,
    hora, instalación y estado de cada una, sin datos personales del titular);
  - las solicitudes **aprobadas** de la misma instalación y temporada que comparten al menos un día y
    una franja horaria.
- **RF-009**: Una solicitud con algún conflicto NO DEBE poder aprobarse (RF-022 y D8 de `000-core`): la
  acción se rechaza con un motivo claro y la solicitud sigue pendiente. Los conflictos DEBEN volver a
  comprobarse en el momento de aprobar, no solo al mostrarlos.
- **RF-010**: Una solicitud sin conflictos DEBE poder aprobarse; pasa a **aprobada** con su fecha de
  resolución.
- **RF-011**: Una solicitud pendiente DEBE poder rechazarse, tenga o no conflictos; pasa a
  **rechazada** con su fecha de resolución.
- **RF-012**: Una solicitud ya aprobada o rechazada NO DEBE poder volver a resolverse; se rechaza
  explicando que ya está resuelta.
- **RF-013**: Al aprobar o rechazar, cada representante del club DEBE recibir en «Mis avisos» un aviso
  con el resultado, la instalación, los días y el horario (RF-024 de `000-core`). El aviso DEBE
  guardarse junto con el cambio de estado: o se guardan los dos o ninguno.

**Bloqueo y consultas para otras historias**

- **RF-014**: Una solicitud aprobada DEBE bloquear, para todos los usuarios, las franjas de su
  instalación en sus días de la semana y su horario, en todas las fechas de su temporada, salvo las que
  coincidan con un cierre por mantenimiento (aclaración de `000-core`). Las pendientes y rechazadas no
  bloquean nada.
- **RF-015**: Esta historia DEBE publicar de forma definitiva la consulta de franjas bloqueadas que
  usan HU-01 y HU-02 (`IConsultaFranjasFijas.FranjasBloqueadas` de `000-core`), con la regla de RF-014,
  y retirar la versión provisional de spec-001 sin cambiar el resultado de la búsqueda.
- **RF-016**: Esta historia DEBE publicar la consulta que usa HU-07 para saber a quién avisar de un
  cierre (`IConsultaFranjasFijas.RepresentantesAfectados`): los representantes de los clubes con franjas
  fijas aprobadas que se solapan con el periodo del cierre en esa instalación, sin repetir.
- **RF-017**: Para conocer las reservas en conflicto, esta historia DEBE leerlas solo a través de la
  consulta que publica HU-02 (`IConsultaReservas`), y los cierres a través de la de HU-07
  (`IConsultaCierres`). No DEBE escribir en sus datos.

**Accesibilidad e idioma**

- **RF-018**: Las pantallas de solicitud y de revisión DEBEN cumplir WCAG 2.1 AA (constitución,
  principio I): etiquetas visibles, uso completo con teclado, foco visible, resultados anunciados y
  estados en texto.
- **RF-019**: Todos los textos DEBEN estar en español de España; los días, las horas y las fechas, en
  hora de Madrid (constitución, principio IX).

### Entidades clave

HU-05 es propietaria de **Solicitud de franjas fijas** (`000-core`, «Entidades clave»): la crea, la
aprueba y la rechaza. No añade campos ni estados.

- **Solicitud de franjas fijas**: club, instalación, temporada, días de la semana, hora de inicio y de
  fin, estado y fecha de resolución.
- **Club, Temporada, Instalación, Usuario**: precargados; se leen. Los representantes de un club son
  los usuarios con ese club.
- **Reserva** (de HU-02) y **Cierre por mantenimiento** (de HU-07): se leen, a través de sus consultas,
  para conflictos y suspensiones.
- **Notificación**: se crea un aviso por representante al resolver una solicitud (a través del servicio
  de avisos de `000-core`).

**Conflicto** (no se almacena): reserva o solicitud aprobada que impide aprobar otra; se calcula al
consultar y al aprobar.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Un representante envía una solicitud completa en menos de 2 minutos.
- **CE-002**: En el 100 % de los casos de prueba, una solicitud aprobada hace que sus franjas dejen de
  aparecer en la búsqueda de HU-01 y no se puedan reservar en HU-02, en todas las fechas de la
  temporada fuera de los cierres, y una pendiente o rechazada no bloquea ninguna.
- **CE-003**: En el 100 % de los casos de prueba, una solicitud con reservas confirmadas o pendientes
  vigentes en conflicto, o solapada con otra aprobada, no se puede aprobar, y el técnico ve todos los
  conflictos (ninguno omitido, ninguno de más).
- **CE-004**: En el 100 % de las aprobaciones y rechazos, todos los representantes del club reciben su
  aviso, y si falla el aviso no cambia el estado.
- **CE-005**: Con la consulta definitiva, la búsqueda de HU-01 da exactamente el mismo resultado que con
  la provisional en todas sus pruebas.
- **CE-006**: La solicitud y la revisión se completan solo con el teclado; Lighthouse da 95 o más y axe
  no encuentra infracciones graves ni críticas.

## Supuestos

- **Sin motivo de rechazo escrito**: el técnico rechaza sin escribir un motivo, porque la entidad de
  `000-core` no tiene ese campo. Añadirlo exigiría cambiar primero `000-core` (principio VII).
- **Códigos de error nuevos**: «solicitud con conflictos», «solapada con otra franja fija» y «solicitud
  ya resuelta» no tienen un código adecuado en el catálogo de `000-core` (el que usa hoy el dominio,
  `TRANSICION_NO_PERMITIDA`, habla de «reserva»). Se proponen primero en `000-core` por PR (RF-003 de
  `000-core`) y solo después se usan aquí.
- **Conflictos sin datos personales**: el técnico ve fecha, hora, instalación y estado de cada reserva
  en conflicto, no el nombre ni el correo de su titular (principio VI y mínima exposición).
- **El técnico no cancela reservas**: «tratar» los conflictos significa rechazar la solicitud o esperar
  a que las reservas pasen o las cancelen sus titulares (HU-03). Esta historia no cancela reservas.
- **Sin retirar ni modificar**: un representante no puede retirar ni cambiar una solicitud enviada, y
  una aprobada dura toda la temporada. Si hiciera falta, se propondría en otra spec.
- **Temporada en curso**: con la siembra solo existe «2026-2027»; el formulario ofrece las temporadas
  no terminadas.
- **Dependencias**: `IConsultaReservas` la publica HU-02 (spec-002, Saul, que también retira la
  provisional de spec-001); `IConsultaCierres` la publica HU-07 (spec-007, C). Mientras no existan, se
  usan las provisionales de spec-001 (RF-014a de spec-001). La consulta provisional de franjas fijas de
  spec-001 la retira esta spec.
- **Datos de demostración**: los clubes, la temporada, los representantes y el técnico son los
  ficticios sembrados por `000-core`.
