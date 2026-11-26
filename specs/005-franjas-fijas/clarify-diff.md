# Diferencia de `spec.md` antes y después de clarify · 005-franjas-fijas

**Historia**: HU-05 · **Responsable**: Artur · **PR**: #33

Generado el 26/11/2026 con `git diff <commit>^ <commit> -- specs/005-franjas-fijas/spec.md` sobre los commits de clarify: es la salida real de git, no una reconstrucción. Las preguntas y respuestas registradas están en la sección «Clarifications» del propio `spec.md`.

En esta spec, clarify se ejecutó **después de specify y antes del plan**, no sobre la primera versión funcional (enunciado, 5.1). La pasada sobre la versión funcional se ha pedido a Artur; si llega, se guardará en `clarify-funcional.md`. Si no, consta que se aclaró antes del plan.

## Commit `d8af902`: spec-005: clarify (sesión 2026-11-25)

> d8af902 · 2026-11-25 · Artur Ramazanov Anichkin
>
> spec-005: clarify (sesión 2026-11-25)
>
> Cinco aclaraciones: rechazo sin motivo escrito; tres códigos de error nuevos que se proponen antes
> en 000-core (SOLICITUD_CON_CONFLICTOS, FRANJA_FIJA_SOLAPADA y SOLICITUD_YA_RESUELTA, los tres 409);
> conflictos sin datos personales del titular; el representante no puede retirar una solicitud; las
> reservas del propio club también cuentan como conflicto.

**Cambios**: 1 file changed, 45 insertions(+), 16 deletions(-)

```diff
diff --git a/specs/005-franjas-fijas/spec.md b/specs/005-franjas-fijas/spec.md
index f818af1..60dd094 100644
--- a/specs/005-franjas-fijas/spec.md
+++ b/specs/005-franjas-fijas/spec.md
@@ -36,6 +36,26 @@ reservas que chocan con una solicitud (HU-03 la cancela su titular), retirar o m
 ya enviada o aprobada, cobrar las franjas fijas (no se cobran, D9 de 000-core) y el informe mensual
 (HU-08).
 
+## Clarifications
+
+### Session 2026-11-25
+
+- Q: Cuando el técnico rechaza una solicitud de franjas fijas, ¿escribe un motivo que le llega al club en
+  el aviso? → A: No. El rechazo no lleva motivo; el aviso solo dice que se ha rechazado, con la
+  instalación, los días y el horario.
+- Q: ¿Qué códigos de error se usan para «solicitud con conflictos», «solapada con otra franja fija» y «ya
+  resuelta», que no existen en el catálogo de `000-core`? → A: Tres códigos nuevos, propuestos primero
+  en `000-core` por PR: `SOLICITUD_CON_CONFLICTOS` (409), `FRANJA_FIJA_SOLAPADA` (409) y
+  `SOLICITUD_YA_RESUELTA` (409). Las solicitudes dejan de usar `TRANSICION_NO_PERMITIDA`.
+- Q: En la lista de reservas que impiden aprobar una solicitud, ¿el técnico ve quién hizo cada reserva?
+  → A: No. Ve solo la fecha, la hora, la instalación y el estado de cada reserva, sin datos personales
+  del titular.
+- Q: ¿Puede el representante retirar una solicitud de franjas fijas que todavía está pendiente? → A:
+  No. Una vez enviada, solo la resuelve el técnico (aprobándola o rechazándola).
+- Q: Si el propio club tiene una reserva suelta en una de las horas que pide como franja fija, ¿cuenta
+  como conflicto que impide aprobar? → A: Sí. Cualquier reserva confirmada o pendiente de pago vigente
+  es un conflicto, sea de quien sea.
+
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
 ### Historia de usuario 1 - Solicitar franjas fijas para mi club (Prioridad: P1)
@@ -226,6 +246,8 @@ pantalla, y se pasa la comprobación de quickstart.md §6 de `000-core`.
   aprobadas a la vez, siempre que las aprobadas no se solapen entre sí.
 - **Dos representantes del mismo club**: ven las mismas solicitudes (son del club) y los dos reciben
   los avisos.
+- **Reserva del propio club**: si un representante del club tiene una reserva suelta en una de esas
+  horas, también es un conflicto; el club la cancela (HU-03) o el técnico espera.
 - **Cambio de hora**: los días del cambio de horario de verano, una franja fija de 19:00 a 21:00 sigue
   bloqueando las franjas de 19:00 y 20:00 en hora de Madrid.
 - **Cierre que cubre toda la franja fija**: ese día no se bloquea nada por el club; las horas cuentan
@@ -259,20 +281,21 @@ pantalla, y se pasa la comprobación de quickstart.md §6 de `000-core`.
 - **RF-007**: Solo un técnico de concejalía DEBE poder aprobar o rechazar solicitudes, y DEBE poder ver
   las solicitudes de todos los clubes, empezando por las pendientes.
 - **RF-008**: Para cada solicitud pendiente, el técnico DEBE poder ver sus **conflictos**:
-  - las reservas confirmadas o pendientes de pago vigentes de esa instalación que empiezan entre ahora
-    y el fin de la temporada, un día de la semana de la solicitud y a una hora de la solicitud (fecha,
+  - las reservas confirmadas o pendientes de pago vigentes de esa instalación, de cualquier usuario
+    (también del propio club), que empiezan entre ahora y el fin de la temporada, un día de la semana de la solicitud y a una hora de la solicitud (fecha,
     hora, instalación y estado de cada una, sin datos personales del titular);
   - las solicitudes **aprobadas** de la misma instalación y temporada que comparten al menos un día y
     una franja horaria.
-- **RF-009**: Una solicitud con algún conflicto NO DEBE poder aprobarse (RF-022 y D8 de `000-core`): la
-  acción se rechaza con un motivo claro y la solicitud sigue pendiente. Los conflictos DEBEN volver a
+- **RF-009**: Una solicitud con algún conflicto NO DEBE poder aprobarse (RF-022 y D8 de `000-core`), y
+  sigue pendiente. La acción se rechaza con `SOLICITUD_CON_CONFLICTOS` si hay reservas en conflicto, o
+  con `FRANJA_FIJA_SOLAPADA` si solo se solapa con otra solicitud aprobada. Los conflictos DEBEN volver a
   comprobarse en el momento de aprobar, no solo al mostrarlos.
 - **RF-010**: Una solicitud sin conflictos DEBE poder aprobarse; pasa a **aprobada** con su fecha de
   resolución.
 - **RF-011**: Una solicitud pendiente DEBE poder rechazarse, tenga o no conflictos; pasa a
   **rechazada** con su fecha de resolución.
-- **RF-012**: Una solicitud ya aprobada o rechazada NO DEBE poder volver a resolverse; se rechaza
-  explicando que ya está resuelta.
+- **RF-012**: Una solicitud ya aprobada o rechazada NO DEBE poder volver a resolverse; se rechaza con
+  `SOLICITUD_YA_RESUELTA`.
 - **RF-013**: Al aprobar o rechazar, cada representante del club DEBE recibir en «Mis avisos» un aviso
   con el resultado, la instalación, los días y el horario (RF-024 de `000-core`). El aviso DEBE
   guardarse junto con el cambio de estado: o se guardan los dos o ninguno.
@@ -338,18 +361,24 @@ consultar y al aprobar.
 
 ## Supuestos
 
-- **Sin motivo de rechazo escrito**: el técnico rechaza sin escribir un motivo, porque la entidad de
-  `000-core` no tiene ese campo. Añadirlo exigiría cambiar primero `000-core` (principio VII).
-- **Códigos de error nuevos**: «solicitud con conflictos», «solapada con otra franja fija» y «solicitud
-  ya resuelta» no tienen un código adecuado en el catálogo de `000-core` (el que usa hoy el dominio,
-  `TRANSICION_NO_PERMITIDA`, habla de «reserva»). Se proponen primero en `000-core` por PR (RF-003 de
-  `000-core`) y solo después se usan aquí.
-- **Conflictos sin datos personales**: el técnico ve fecha, hora, instalación y estado de cada reserva
-  en conflicto, no el nombre ni el correo de su titular (principio VI y mínima exposición).
+- **Sin motivo de rechazo escrito** (confirmado en «Clarifications»): el técnico rechaza sin escribir un
+  motivo, porque la entidad de `000-core` no tiene ese campo. Añadirlo exigiría cambiar primero
+  `000-core` (principio VII).
+- **Códigos de error nuevos** (confirmado en «Clarifications»): `SOLICITUD_CON_CONFLICTOS`,
+  `FRANJA_FIJA_SOLAPADA` y `SOLICITUD_YA_RESUELTA`, los tres 409, con su texto en español. Se proponen
+  primero en `000-core` por PR (RF-003 de `000-core`: catálogo de contracts/errores.md, `CodigosError` y
+  la tabla de estados de `ManejadorErrores`) y solo después se usan aquí. En ese mismo PR,
+  `SolicitudFranjasFijas` deja de lanzar `TRANSICION_NO_PERMITIDA` (cuyo texto habla de «reserva») y
+  pasa a lanzar `SOLICITUD_YA_RESUELTA`.
+- **Conflictos sin datos personales** (confirmado en «Clarifications»): el técnico ve fecha, hora,
+  instalación y estado de cada reserva en conflicto, no el nombre ni el correo de su titular (mínima
+  exposición de datos de terceros).
 - **El técnico no cancela reservas**: «tratar» los conflictos significa rechazar la solicitud o esperar
   a que las reservas pasen o las cancelen sus titulares (HU-03). Esta historia no cancela reservas.
-- **Sin retirar ni modificar**: un representante no puede retirar ni cambiar una solicitud enviada, y
-  una aprobada dura toda la temporada. Si hiciera falta, se propondría en otra spec.
+- **Sin retirar ni modificar** (confirmado en «Clarifications»): un representante no puede retirar,
+  borrar ni cambiar una solicitud enviada, ni siquiera pendiente, y una aprobada dura toda la
+  temporada. Si hiciera falta, se propondría en otra spec (y un estado «retirada» exigiría cambiar
+  `000-core`).
 - **Temporada en curso**: con la siembra solo existe «2026-2027»; el formulario ofrece las temporadas
   no terminadas.
 - **Dependencias**: `IConsultaReservas` la publica HU-02 (spec-002, Saul, que también retira la
```
