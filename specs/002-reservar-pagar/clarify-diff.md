# Diferencia de `spec.md` antes y después de clarify · 002-reservar-pagar

**Historia**: HU-02 · **Responsable**: Saul · **PR**: #25 (y #29 para la aclaración del plan)

Generado el 26/11/2026 con `git diff <commit>^ <commit> -- specs/002-reservar-pagar/spec.md` sobre los commits de clarify: es la salida real de git, no una reconstrucción. Las preguntas y respuestas registradas están en la sección «Clarifications» del propio `spec.md`.

En esta spec, clarify se ejecutó **después de specify y antes del plan**, no sobre la primera versión funcional (enunciado, 5.1). La pasada sobre la versión funcional (enunciado, 5.1) está en `clarify-funcional.md`. El segundo commit añade una aclaración surgida durante el plan.

## Commit `7a4e799`: spec-002: clarify (sesión 2026-11-25)

> 7a4e799 · 2026-11-25 · saulperezz
>
> spec-002: clarify (sesión 2026-11-25)
>
> Cinco aclaraciones con Saul:
> - IServicioReservas (cancelar por el usuario y aplicar cierre) lo implementa spec-002 (RF-019a).
> - No hay acción para abandonar el pago; la franja se libera sola a los 10 min.
> - Sin límite de reservas por persona.
> - «Mis reservas» muestra todas, próximas primero por inicio y luego las terminadas (RF-015a).
> - La pantalla de pago muestra la hora límite fija, sin cuenta atrás (RF-008a).

**Cambios**: 1 file changed, 61 insertions(+), 13 deletions(-)

```diff
diff --git a/specs/002-reservar-pagar/spec.md b/specs/002-reservar-pagar/spec.md
index 4e34029..0c8ff45 100644
--- a/specs/002-reservar-pagar/spec.md
+++ b/specs/002-reservar-pagar/spec.md
@@ -34,9 +34,28 @@ Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa
 | Quién puede reservar                                         | spec.md «Roles y permisos»: ciudadano y representante de club       |
 | Zona horaria `Europe/Madrid`, euros, español                 | constitución, principio IX                                          |
 
-**Fuera de alcance** (lo hacen otras specs): buscar franjas libres (spec-001), cancelar una reserva
-confirmada y su devolución (spec-003), validar el QR en el acceso (spec-006) y caducar o cancelar
-reservas por un cierre por mantenimiento (spec-007).
+**Fuera de alcance** (lo hacen otras specs): buscar franjas libres (spec-001); la pantalla para
+cancelar, la decisión de devolver y la devolución (spec-003); validar el QR en el acceso (spec-006); y
+registrar un cierre por mantenimiento, con sus devoluciones y avisos (spec-007). HU-02 solo aporta a
+spec-003 y spec-007 los cambios de estado de las reservas (RF-019a).
+
+## Clarifications
+
+### Session 2026-11-25
+
+- Q: ¿Quién implementa las operaciones de cancelar una reserva por el usuario (HU-03) y aplicar un
+  cierre (HU-07), que escriben en reservas y pagos? → A: Las implementa la spec-002 (dueña de esas
+  tablas), con sus pruebas, reutilizando la máquina de estados de `000-core`; HU-03 y HU-07 solo las
+  usan.
+- Q: ¿Puede el usuario soltar a propósito una reserva pendiente de pago antes de los 10 minutos? → A:
+  No; no hay ninguna acción para abandonar el pago y la franja se libera sola a los 10 minutos.
+- Q: ¿Se limita cuántas reservas (pendientes o confirmadas) puede tener una persona a la vez? → A: No;
+  sin límite, porque `000-core` no fija ninguno.
+- Q: ¿Qué reservas muestra «Mis reservas»? → A: Todas las del usuario, en cualquier estado: primero las
+  que aún no han terminado, por inicio ascendente; después las terminadas, de la más reciente a la más
+  antigua; sin paginación.
+- Q: ¿La pantalla de pago muestra cuánto queda para que caduque la reserva? → A: Sí, como hora límite
+  fija en hora de Madrid («Paga antes de las 18:42; después la reserva caduca»), sin cuenta atrás.
 
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
@@ -60,7 +79,9 @@ importe de su tarifa y con un código QR.
    **entonces** ve un resumen con la pista, el centro, la fecha, la hora de inicio y de fin en hora de
    Madrid y el importe de su tarifa, igual al mostrado en la búsqueda.
 2. **Dado** el resumen, **cuando** Ana confirma que quiere reservar, **entonces** la reserva se crea
-   pendiente de pago y la franja deja de estar libre para cualquier otra persona.
+   pendiente de pago, la franja deja de estar libre para cualquier otra persona y Ana ve la pantalla de
+   pago con la hora límite («Paga antes de las 18:42; después la reserva caduca» si la creó a las
+   18:32).
 3. **Dado** una reserva pendiente de pago creada hace menos de 10 minutos, **cuando** Ana paga con una
    tarjeta que la pasarela acepta, **entonces** el pago se completa, la reserva pasa a confirmada y Ana
    ve un mensaje de confirmación con su código QR.
@@ -187,6 +208,9 @@ que solo ve las suyas, con el estado correcto, y el QR solo en las confirmadas.
    existe).
 5. **Dado** una reserva pendiente que ya ha superado los 10 minutos, **cuando** Ana abre «Mis
    reservas», **entonces** la ve como caducada aunque el barrido aún no la haya guardado así.
+6. **Dado** que Ana tiene reservas para el sábado y el domingo y otra del mes pasado, **cuando** abre
+   «Mis reservas», **entonces** ve primero la del sábado, después la del domingo y al final la del mes
+   pasado.
 
 ---
 
@@ -280,6 +304,9 @@ al reservar y pagar:
 - **RF-008**: El usuario DEBE poder pagar su reserva pendiente introduciendo los datos de una tarjeta
   ficticia, que se envían a la pasarela simulada de `000-core` (RF-021). Los datos de la tarjeta NO
   DEBEN guardarse ni registrarse.
+- **RF-008a**: La pantalla de pago DEBE mostrar, como texto fijo, la hora límite para pagar en hora de
+  Madrid (creación de la reserva + el tiempo de bloqueo de `000-core`), por ejemplo «Paga antes de las
+  18:42; después la reserva caduca». No hay cuenta atrás que se actualice en pantalla.
 - **RF-009**: Si al pagar la reserva ya no está vigente (10 minutos o más desde su creación), el
   sistema DEBE caducarla, dejar su pago fallido, no llamar a la pasarela y responder
   `RESERVA_CADUCADA`.
@@ -303,9 +330,12 @@ al reservar y pagar:
 
 **Mis reservas**
 
-- **RF-015**: El usuario DEBE poder ver una lista de sus propias reservas con la pista, el centro, la
-  fecha y las horas en hora de Madrid, el importe en euros con formato español y el estado. Una
-  reserva pendiente no vigente DEBE mostrarse como caducada.
+- **RF-015**: El usuario DEBE poder ver una lista de **todas** sus reservas, en cualquier estado, con la
+  pista, el centro, la fecha y las horas en hora de Madrid, el importe en euros con formato español y
+  el estado. Una reserva pendiente no vigente DEBE mostrarse como caducada.
+- **RF-015a**: La lista DEBE ordenarse así: primero las reservas que aún no han terminado (fin
+  posterior al momento actual), por hora de inicio ascendente; después las terminadas, de la más
+  reciente a la más antigua. No tiene paginación.
 - **RF-016**: Las reservas confirmadas DEBEN mostrar su código de acceso como QR y como texto; las que
   no están confirmadas NO DEBEN mostrar ningún código.
 - **RF-017**: Desde una reserva pendiente de pago vigente, el usuario DEBE poder ir a pagarla.
@@ -318,6 +348,21 @@ al reservar y pagar:
 - **RF-019**: Al publicar esa consulta, la versión **provisional** que aportó HU-01 (spec-001,
   RF-014a) DEBE retirarse, y la búsqueda de HU-01 DEBE seguir dando exactamente el mismo resultado
   (spec-001, CE-003).
+- **RF-019a**: Esta historia DEBE ofrecer a HU-03 y a HU-07 las dos operaciones de escritura sobre
+  reservas que define `000-core` (contracts/modulos.md), sin que esas historias escriban en las tablas
+  de reservas y pagos:
+  - **cancelar por el usuario** una reserva confirmada: si no es del usuario, `NO_ENCONTRADO`; si ya ha
+    empezado, `RESERVA_YA_EMPEZADA`; si no está confirmada, `TRANSICION_NO_PERMITIDA`. Devuelve los
+    datos que HU-03 necesita para decidir y registrar la devolución (reserva, titular, inicio, pago e
+    importe). La devolución y su política son de HU-03;
+  - **aplicar un cierre** a una instalación en un periodo: cancela, con motivo «cierre por
+    mantenimiento», las reservas confirmadas que se solapan con el periodo y caduca (pago → fallido)
+    las pendientes de pago que se solapan, y devuelve las dos listas para que HU-07 registre las
+    devoluciones y los avisos (RF-020 de `000-core`). Las devoluciones, los avisos y el registro del
+    cierre son de HU-07.
+
+  Los cambios se guardan en la misma operación que los de quien llama (devolución, aviso), para que se
+  guarden juntos o no se guarde nada.
 
 **Mensajes, accesibilidad e idioma**
 
@@ -377,16 +422,19 @@ HU-02 no crea entidades nuevas. Usa las de `000-core` (spec.md, «Entidades clav
 - **Datos de la tarjeta**: se pide solo lo que necesita la pasarela simulada (el número de 16 dígitos;
   las tarjetas que terminan en `0002` se rechazan, R9 de `000-core`). Un error de formato no cuenta como
   pago fallido.
-- **Sin límite de reservas por persona**: `000-core` no fija ninguno; un usuario puede tener varias
-  reservas en distintas franjas (principio VIII).
+- **Sin límite de reservas por persona** (confirmado en «Clarifications»): `000-core` no fija ninguno;
+  un usuario puede tener a la vez varias reservas, pendientes o confirmadas, en franjas distintas
+  (principio VIII). El abuso queda acotado por el bloqueo de 10 minutos.
 - **«Mis reservas» en esta historia**: el prefijo `/api/reservas` de `000-core` incluye «listar las
   mías» para HU-02. La cancelación desde esa lista la añade HU-03.
-- **Abandonar el pago**: no hay acción para soltar la reserva pendiente antes de tiempo (`000-core` no
-  permite pendiente de pago → cancelada); la franja se libera sola a los 10 minutos.
+- **Abandonar el pago** (confirmado en «Clarifications»): no hay ninguna acción para soltar la
+  reserva pendiente antes de tiempo (`000-core` no permite pendiente de pago → cancelada); salir de la
+  pantalla de pago no cambia nada y la franja se libera sola a los 10 minutos (RF-014).
 - **Consulta para otras historias**: HU-02 es dueña de las tablas `reservas` y `pagos`
   (`docs/agrupamiento.md`) y publica la consulta de reservas activas de `000-core`
-  (contracts/modulos.md). Las operaciones de cancelar por el usuario y aplicar un cierre, que también
-  escriben en reservas, las piden HU-03 y HU-07; quién y cuándo las implementa se decide aparte.
+  (contracts/modulos.md). También implementa las operaciones de cancelar por el usuario y aplicar un
+  cierre que piden HU-03 y HU-07 (confirmado en «Clarifications», RF-019a), para que nadie más escriba
+  en esas tablas; C (HU-03, HU-07) solo las usa y se le avisa de la dependencia.
 - **Dependencias**: necesita de HU-01 la comprobación de disponibilidad y precio y el resultado de la
   búsqueda donde va el botón «Reservar»; la consulta provisional que hay que retirar está en el código
   de HU-01. Esta historia no escribe en datos de otros módulos.
```

## Commit `544358f`: spec-002: plan, ajuste de E4 (reserva ya caducada)

> 544358f · 2026-11-25 · saulperezz
>
> spec-002: plan, ajuste de E4 (reserva ya caducada)
>
> Pagar una reserva que el barrido (o la caducidad al reservar) ya guardó como caducada por
> tiempo responde 409 RESERVA_CADUCADA, igual que antes del barrido. Las caducadas por pago
> rechazado o por cierre, las confirmadas y las canceladas siguen dando
> TRANSICION_NO_PERMITIDA. Ajustados research.md E4 y E13 (caso límite), data-model.md,
> contrato OpenAPI, quickstart, plan.md y spec.md (aclaración, RF-009, RF-011, casos límite).

**Cambios**: 1 file changed, 12 insertions(+), 4 deletions(-)

```diff
diff --git a/specs/002-reservar-pagar/spec.md b/specs/002-reservar-pagar/spec.md
index 0236b7d..01f93a0 100644
--- a/specs/002-reservar-pagar/spec.md
+++ b/specs/002-reservar-pagar/spec.md
@@ -56,6 +56,10 @@ spec-003 y spec-007 los cambios de estado de las reservas (RF-019a).
   antigua; sin paginación.
 - Q: ¿La pantalla de pago muestra cuánto queda para que caduque la reserva? → A: Sí, como hora límite
   fija en hora de Madrid («Paga antes de las 18:42; después la reserva caduca»), sin cuenta atrás.
+- Q (plan, 2026-11-25): ¿Qué responde el pago de una reserva que el barrido ya ha guardado como
+  caducada por tiempo? → A: `RESERVA_CADUCADA`, igual que si aún no se hubiera guardado: el usuario no
+  ve un mensaje distinto según haya pasado o no el barrido. Las caducadas por un pago rechazado o por
+  un cierre siguen dando `TRANSICION_NO_PERMITIDA` (plan, research.md E4).
 
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
@@ -250,12 +254,14 @@ al reservar y pagar:
   17:59:59, sí.
 - **Pagar en el límite del bloqueo**: pagar 9 minutos y 59 segundos después de crear la reserva → se
   intenta el cobro; pagar a los 10 minutos exactos → `RESERVA_CADUCADA`, sin cobro.
+- **Caducada por el barrido**: pagar una reserva que el barrido ya ha guardado como caducada por tiempo
+  → `RESERVA_CADUCADA` (el mismo mensaje que antes del barrido), sin cobro.
 - **Reservar una franja con una pendiente ajena que acaba de caducar**: con la otra reserva creada hace
   exactamente 10 minutos, se puede reservar; la anterior queda caducada en el mismo momento.
 - **Doble clic**: si la misma persona envía dos veces la misma reserva, la segunda recibe
   `FRANJA_OCUPADA`; nunca hay dos reservas activas en la misma franja.
-- **Pagar dos veces**: pagar una reserva ya confirmada o ya caducada → `TRANSICION_NO_PERMITIDA`, sin
-  cobro.
+- **Pagar dos veces**: pagar una reserva ya confirmada, o ya caducada por un pago rechazado →
+  `TRANSICION_NO_PERMITIDA`, sin cobro.
 - **Cambio de hora**: los días 25/10/2026 y 28/03/2027 se pueden reservar las 14 franjas de 08:00 a
   21:00 en hora de Madrid, cada una de una hora real.
 - **Cierre posterior**: si se registra un cierre por mantenimiento que cubre una reserva pendiente
@@ -314,13 +320,15 @@ al reservar y pagar:
   18:42; después la reserva caduca». No hay cuenta atrás que se actualice en pantalla.
 - **RF-009**: Si al pagar la reserva ya no está vigente (10 minutos o más desde su creación), el
   sistema DEBE caducarla, dejar su pago fallido, no llamar a la pasarela y responder
-  `RESERVA_CADUCADA`.
+  `RESERVA_CADUCADA`. Lo mismo si la reserva ya está guardada como caducada por tiempo (por el barrido o
+  por la caducidad al reservar): la respuesta no depende de si el barrido ha pasado.
 - **RF-010**: Si la pasarela acepta el cobro, la reserva DEBE pasar a confirmada, su pago a completado
   y la reserva DEBE recibir un código de acceso opaco y aleatorio (RF-017 de `000-core`), que se
   muestra al usuario como QR y como texto.
 - **RF-011**: Si la pasarela rechaza el cobro, la reserva DEBE pasar a caducada y su pago a fallido, y
   el usuario DEBE ver el mensaje de `PAGO_RECHAZADO`. El pago NO DEBE poder reintentarse; cualquier
-  intento de pagar una reserva que no está pendiente DEBE rechazarse con `TRANSICION_NO_PERMITIDA`.
+  intento de pagar una reserva que no está pendiente DEBE rechazarse con `TRANSICION_NO_PERMITIDA`,
+  salvo una caducada por tiempo, que responde `RESERVA_CADUCADA` (RF-009).
 - **RF-012**: Un número de tarjeta con un formato no válido DEBE rechazarse con `PETICION_INVALIDA`
   sin llamar a la pasarela y sin cambiar el estado de la reserva.
 - **RF-013**: Solo el titular DEBE poder pagar o ver una reserva; para cualquier otra persona, la
```
