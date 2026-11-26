# Diferencia de `spec.md` antes y después de clarify · 000-core

**Historia**: Base común (sin historia propia) · **Responsable**: Saul · **PR**: #5

Generado el 26/11/2026 con `git diff <commit>^ <commit> -- specs/000-core/spec.md` sobre los commits de clarify: es la salida real de git, no una reconstrucción. Las preguntas y respuestas registradas están en la sección «Clarifications» del propio `spec.md`.

En esta spec, clarify se ejecutó **después de specify y antes del plan**, no sobre la primera versión funcional (enunciado, 5.1). 000-core no tiene historia propia: no se repite clarify sobre la versión funcional; sus decisiones se revisan en las de 002 y 006.

## Commit `84c3512`: 000-core: clarify (sesión 2026-10-28)

> 84c3512 · 2026-10-28 · saulperezz
>
> 000-core: clarify (sesión 2026-10-28)

**Cambios**: 1 file changed, 106 insertions(+), 74 deletions(-)

```diff
diff --git a/specs/000-core/spec.md b/specs/000-core/spec.md
index 1bbf61a..1b18efd 100644
--- a/specs/000-core/spec.md
+++ b/specs/000-core/spec.md
@@ -17,20 +17,30 @@ cada cambio de estado. Las specs 001 a 008 lo referencian y no lo redefinen; si
 
 ### Historias del sistema que se apoyan en esta base
 
-| ID | Spec | Historia |
-|----|------|----------|
-| HU-01 | spec-001 | Buscar pistas libres por deporte, centro y fecha, con el precio de mi tarifa |
-| HU-02 | spec-002 | Reservar una pista y pagarla en el momento |
-| HU-03 | spec-003 | Cancelar una reserva (devolución si faltan más de 24 h) |
-| HU-04 | spec-004 | Inscribirse en un curso trimestral con lista de espera |
+| ID    | Spec     | Historia                                                                                |
+| ----- | -------- | --------------------------------------------------------------------------------------- |
+| HU-01 | spec-001 | Buscar pistas libres por deporte, centro y fecha, con el precio de mi tarifa            |
+| HU-02 | spec-002 | Reservar una pista y pagarla en el momento                                              |
+| HU-03 | spec-003 | Cancelar una reserva (devolución si faltan más de 24 h)                                 |
+| HU-04 | spec-004 | Inscribirse en un curso trimestral con lista de espera                                  |
 | HU-05 | spec-005 | Solicitar franjas fijas de temporada para un club y aprobarlas/rechazarlas (concejalía) |
-| HU-06 | spec-006 | Validar el acceso con el código QR |
-| HU-07 | spec-007 | Cerrar una instalación por mantenimiento (cancelación masiva con devolución) |
-| HU-08 | spec-008 | Informe mensual de ocupación e ingresos |
+| HU-06 | spec-006 | Validar el acceso con el código QR                                                      |
+| HU-07 | spec-007 | Cerrar una instalación por mantenimiento (cancelación masiva con devolución)            |
+| HU-08 | spec-008 | Informe mensual de ocupación e ingresos                                                 |
 
 El reparto de historias entre personas del grupo se recoge en `docs/agrupamiento.md` (constitución,
 «Acuerdos de colaboración»), no en esta spec.
 
+## Clarifications
+
+### Session 2026-10-28
+
+- Q: ¿Qué ocurre con una reserva pendiente de pago cuando HU-07 cierra la instalación por mantenimiento, si RF-015 no permite pasar de pendiente de pago a cancelada? → A: Pasa a caducada, sin cobro ni devolución (su pago pendiente queda fallido); HU-07 lo hace reutilizando el mecanismo de caducidad de HU-02.
+- Q: ¿La entidad Curso debe tener el atributo «precio» si D10 dice que los cursos son gratuitos? → A: No; se elimina el atributo «precio» de Curso.
+- Q: ¿Se puede reintentar un pago fallido? → A: No. El pago queda fallido y la reserva caducada; el usuario debe crear una reserva nueva si la franja sigue libre. Si la reserva caduca por tiempo, su pago pendiente pasa a fallido.
+- Q: ¿Qué ocurre con las franjas fijas de club aprobadas que coinciden con un cierre por mantenimiento? → A: Quedan suspendidas solo durante el cierre (la solicitud sigue aprobada; no hay cobro ni devolución porque no se cobran, D9) y el representante del club recibe un aviso en «Mis avisos» (D6).
+- Q: ¿Puede el usuario cancelar una reserva confirmada que ya ha empezado? → A: No; solo puede cancelarse antes de su hora de inicio (principio VIII, la opción más simple); después se rechaza con el motivo.
+
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
 Los «usuarios» de estas historias son las cuatro personas del equipo que desarrollan las specs 001 a
@@ -142,8 +152,9 @@ declaran la dependencia en su spec.
 1. **Dado** una reserva confirmada, **cuando** HU-03 la cancela, **entonces** el QR deja de valer en
    HU-06, la franja vuelve a aparecer libre en HU-01 y HU-08 tiene en cuenta la devolución si la hubo.
 2. **Dado** una instalación con reservas confirmadas, **cuando** HU-07 la cierra por mantenimiento,
-   **entonces** esas reservas pasan a canceladas con devolución íntegra, cada titular recibe un aviso
-   en «Mis avisos» y la instalación deja de ofrecer franjas en HU-01 y HU-02 durante el cierre.
+   **entonces** esas reservas pasan a canceladas con devolución íntegra, las pendientes de pago del
+   periodo pasan a caducadas sin cobro ni devolución, cada titular afectado recibe un aviso en «Mis
+   avisos» y la instalación deja de ofrecer franjas en HU-01 y HU-02 durante el cierre.
 3. **Dado** una reserva cancelada o caducada, **cuando** cualquier historia intenta cambiar su estado,
    **entonces** el cambio se rechaza.
 4. **Dado** una solicitud de franjas fijas que choca con reservas confirmadas o pendientes de pago
@@ -157,7 +168,12 @@ declaran la dependencia en su spec.
 - **Antelación**: una franja de hoy + 7 días naturales es reservable; una de hoy + 8 días no. Una
   franja ya empezada (aunque sea por un minuto) no es reservable.
 - **Bloqueo del pago**: una reserva pendiente de pago creada hace exactamente 10 minutos deja de
-  bloquear la franja y pasa a caducada; si el pago se completa antes, pasa a confirmada.
+  bloquear la franja y pasa a caducada (y su pago pendiente, a fallido); si el pago se completa antes,
+  pasa a confirmada.
+- **Pago fallido**: no se puede reintentar; la reserva queda caducada y, para volver a intentarlo, el
+  usuario crea una reserva nueva si la franja sigue libre.
+- **Cancelar una reserva ya empezada**: se rechaza indicando el motivo; solo se cancela antes del
+  inicio.
 - **Plazo de devolución**: con más de 24 h hasta el inicio, devolución íntegra; con exactamente 24 h o
   menos, sin devolución.
 - **Ventana del QR**: válido exactamente 15 minutos antes del inicio y exactamente en la hora de fin;
@@ -170,8 +186,11 @@ declaran la dependencia en su spec.
 - **QR de otro centro**: el personal del centro A lee un QR válido de una reserva del centro B; se
   rechaza indicando que pertenece a otro centro.
 - **Código QR inexistente o adivinado**: se rechaza y la validación queda registrada.
-- **Cierre que solapa con una reserva pendiente de pago**: la reserva no puede confirmarse para una
-  franja cerrada; las reservas confirmadas en el periodo se cancelan con devolución íntegra.
+- **Cierre que solapa con una reserva pendiente de pago**: la reserva pasa a caducada sin cobro ni
+  devolución (su pago pendiente, a fallido) y ya no puede confirmarse; las reservas confirmadas en el
+  periodo se cancelan con devolución íntegra.
+- **Cierre que solapa con una franja fija de club aprobada**: la franja fija queda suspendida solo
+  durante el cierre (la solicitud sigue aprobada) y el representante del club recibe un aviso.
 - **Franja fija que se solapa con otra franja fija ya aprobada**: no se puede aprobar.
 - **Representante de club empadronado**: aplica siempre la tarifa club.
 - **Instalación no reservable por franjas** (sala, piscina): nunca aparece en la búsqueda de HU-01.
@@ -221,19 +240,26 @@ declaran la dependencia en su spec.
 **Reservas, pagos, devoluciones y acceso**
 
 - **RF-015**: Las reservas DEBEN seguir exclusivamente las transiciones: pendiente de pago → confirmada;
-  pendiente de pago → caducada; confirmada → cancelada. Cancelada y caducada son estados finales.
-- **RF-016**: Una reserva pendiente de pago DEBE pasar a caducada si el pago falla o no se completa en
-  10 minutos desde su creación.
+  pendiente de pago → caducada; confirmada → cancelada. Cancelada y caducada son estados finales. No
+  existe la transición pendiente de pago → cancelada.
+- **RF-016**: Una reserva pendiente de pago DEBE pasar a caducada si el pago falla, si no se completa
+  en 10 minutos desde su creación o si su franja queda dentro de un cierre por mantenimiento. Al
+  caducar, su pago pendiente DEBE pasar a fallido, sin cobro ni devolución. Un pago fallido NO DEBE
+  poder reintentarse: para volver a intentarlo, el usuario DEBE crear una reserva nueva si la franja
+  sigue libre.
 - **RF-017**: Al confirmarse, la reserva DEBE recibir un código de acceso (QR) opaco y aleatorio,
   distinto de su identificador y no deducible.
 - **RF-018**: El QR DEBE ser válido solo para reservas confirmadas, desde 15 minutos antes del inicio
   hasta el fin de la reserva, ambos incluidos; cada intento de validación DEBE registrarse con su
   resultado y, si se rechaza, con el motivo.
-- **RF-019**: La cancelación por el usuario DEBE generar devolución íntegra si faltan más de 24 h para
-  el inicio y ninguna devolución con 24 h exactas o menos.
+- **RF-019**: La cancelación por el usuario solo DEBE permitirse antes de la hora de inicio de la
+  reserva; DEBE generar devolución íntegra si faltan más de 24 h para el inicio y ninguna devolución
+  con 24 h exactas o menos.
 - **RF-020**: El cierre por mantenimiento DEBE reutilizar el mismo mecanismo de cancelación y
-  devolución, con motivo «cierre por mantenimiento» y devolución íntegra siempre, y DEBE avisar a cada
-  titular afectado.
+  devolución para las reservas confirmadas, con motivo «cierre por mantenimiento» y devolución íntegra
+  siempre; DEBE pasar a caducadas, sin cobro ni devolución, las reservas pendientes de pago del
+  periodo (RF-016); DEBE suspender durante el cierre las franjas fijas de club aprobadas, sin cambiar
+  el estado de la solicitud; y DEBE avisar a cada titular y a cada representante de club afectados.
 - **RF-021**: El pago y la devolución DEBEN realizarse a través de una pasarela simulada; las
   devoluciones se registran directamente como completadas.
 
@@ -261,37 +287,39 @@ declaran la dependencia en su spec.
 
 ### Roles y permisos
 
-| Rol | Quién es | Qué puede hacer |
-|-----|----------|-----------------|
-| **Ciudadano** | Persona usuaria del servicio | HU-01, HU-02, HU-03, HU-04 |
+| Rol                       | Quién es                                 | Qué puede hacer                                                      |
+| ------------------------- | ---------------------------------------- | -------------------------------------------------------------------- |
+| **Ciudadano**             | Persona usuaria del servicio             | HU-01, HU-02, HU-03, HU-04                                           |
 | **Representante de club** | Ciudadano que actúa en nombre de un club | Lo del ciudadano (con tarifa club) + solicitar franjas fijas (HU-05) |
-| **Personal de centro** | Trabajador asignado a **un** centro | HU-06 y HU-07, solo en su centro |
-| **Técnico de concejalía** | Personal de la concejalía | HU-08 y aprobar/rechazar solicitudes de HU-05 |
+| **Personal de centro**    | Trabajador asignado a **un** centro      | HU-06 y HU-07, solo en su centro                                     |
+| **Técnico de concejalía** | Personal de la concejalía                | HU-08 y aprobar/rechazar solicitudes de HU-05                        |
 
 ### Valores de negocio (única fuente de verdad)
 
-| Valor | Cifra | Se aplica en |
-|-------|-------|--------------|
-| Duración de una franja | 1 hora, en punto | HU-01, HU-02, HU-05, HU-08 |
-| Horario de apertura de los centros | 08:00–22:00 todos los días; 14 franjas (última 21:00–22:00) | HU-01, HU-05, HU-08 |
-| Antelación máxima de reserva | Fecha de la franja entre hoy y hoy + 7 días naturales, ambos incluidos (ej.: el lunes 21 a las 18:00 se puede reservar hasta la franja de 21:00 del lunes 28) | HU-01, HU-02 |
-| Reserva de franjas ya empezadas | No se permite | HU-01, HU-02 |
-| Tiempo de bloqueo mientras se paga | 10 minutos desde que se crea la reserva | HU-02 (lo respeta HU-01) |
-| Plazo para devolución al cancelar | Íntegra si faltan **más de 24 h** para el inicio; con 24 h exactas o menos, sin devolución | HU-03, HU-08 |
-| Devolución por cierre de mantenimiento | Siempre íntegra, sea cual sea la antelación | HU-07, HU-08 |
-| Ventana de validez del QR | Desde 15 minutos antes del inicio hasta el fin de la reserva, ambos incluidos | HU-06 |
-| Tarifas (€ por hora) | Empadronado 6,00 €; no empadronado 10,00 €; club 4,00 € (iguales para tenis y pádel) | HU-01, HU-02, HU-08 |
-| Duración de la temporada | De septiembre a junio | HU-05 |
-| Zona horaria | `Europe/Madrid` (fechas almacenadas en UTC) | Todas |
+| Valor                                  | Cifra                                                                                                                                                         | Se aplica en               |
+| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------- |
+| Duración de una franja                 | 1 hora, en punto                                                                                                                                              | HU-01, HU-02, HU-05, HU-08 |
+| Horario de apertura de los centros     | 08:00–22:00 todos los días; 14 franjas (última 21:00–22:00)                                                                                                   | HU-01, HU-05, HU-08        |
+| Antelación máxima de reserva           | Fecha de la franja entre hoy y hoy + 7 días naturales, ambos incluidos (ej.: el lunes 21 a las 18:00 se puede reservar hasta la franja de 21:00 del lunes 28) | HU-01, HU-02               |
+| Reserva de franjas ya empezadas        | No se permite                                                                                                                                                 | HU-01, HU-02               |
+| Tiempo de bloqueo mientras se paga     | 10 minutos desde que se crea la reserva                                                                                                                       | HU-02 (lo respeta HU-01)   |
+| Plazo para devolución al cancelar      | Íntegra si faltan **más de 24 h** para el inicio; con 24 h exactas o menos, sin devolución                                                                    | HU-03, HU-08               |
+| Devolución por cierre de mantenimiento | Siempre íntegra, sea cual sea la antelación                                                                                                                   | HU-07, HU-08               |
+| Ventana de validez del QR              | Desde 15 minutos antes del inicio hasta el fin de la reserva, ambos incluidos                                                                                 | HU-06                      |
+| Tarifas (€ por hora)                   | Empadronado 6,00 €; no empadronado 10,00 €; club 4,00 € (iguales para tenis y pádel)                                                                          | HU-01, HU-02, HU-08        |
+| Duración de la temporada               | De septiembre a junio                                                                                                                                         | HU-05                      |
+| Zona horaria                           | `Europe/Madrid` (fechas almacenadas en UTC)                                                                                                                   | Todas                      |
 
 ### Estados
 
 - **Reserva**: pendiente de pago (recién creada; bloquea la franja durante el tiempo de bloqueo) ·
-  confirmada (pago completado; tiene QR válido dentro de su ventana) · caducada (pago fallido o tiempo
-  agotado; la franja vuelve a estar libre) · cancelada (por el usuario o por cierre de mantenimiento;
-  el QR deja de valer). Transiciones permitidas: pendiente de pago → confirmada | caducada;
+  confirmada (pago completado; tiene QR válido dentro de su ventana) · caducada (pago fallido, tiempo
+  agotado o cierre por mantenimiento; la franja vuelve a estar libre salvo que esté cerrada) ·
+  cancelada (confirmada que cancela el usuario antes del inicio o el cierre de mantenimiento; el QR
+  deja de valer). Transiciones permitidas: pendiente de pago → confirmada | caducada;
   confirmada → cancelada. Cancelada y caducada son finales.
-- **Pago**: pendiente → completado | fallido.
+- **Pago**: pendiente → completado | fallido. Completado y fallido son finales; un pago fallido no se
+  reintenta. Si la reserva caduca (por tiempo o por cierre), su pago pendiente pasa a fallido.
 - **Devolución**: se registra completada (pasarela simulada).
 - **Instalación**: operativa ↔ cerrada por mantenimiento (durante un periodo).
 - **Solicitud de franjas fijas**: pendiente → aprobada | rechazada.
@@ -299,17 +327,19 @@ declaran la dependencia en su spec.
 
 ### Responsables de los cambios de estado
 
-| Cambio | Responsable | Afecta a |
-|--------|-------------|----------|
-| Reserva nueva → pendiente de pago (la franja deja de estar libre) | HU-02 | HU-01 |
-| Reserva pendiente de pago → confirmada (se genera el QR) | HU-02 | HU-01, HU-06, HU-08 |
-| Reserva pendiente de pago → caducada (pago fallido o tiempo agotado) | HU-02 | HU-01 |
-| Reserva confirmada → cancelada por el usuario (+ devolución si procede) | HU-03 | HU-01, HU-06, HU-08 |
-| Instalación → cerrada por mantenimiento; reservas afectadas → canceladas con devolución íntegra y aviso | HU-07 | HU-01, HU-02, HU-06, HU-08 |
-| Instalación → operativa (reapertura) | HU-07 | HU-01, HU-02 |
-| Solicitud de franjas fijas → pendiente / aprobada / rechazada (+ aviso al club) | HU-05 | HU-01, HU-02, HU-08 |
-| Inscripción → confirmada / en lista de espera / baja; paso de la lista a confirmada (+ aviso) | HU-04 | — |
-| Validación de acceso registrada | HU-06 | — |
+| Cambio                                                                                                  | Responsable | Afecta a                   |
+| ------------------------------------------------------------------------------------------------------- | ----------- | -------------------------- |
+| Reserva nueva → pendiente de pago (la franja deja de estar libre)                                       | HU-02       | HU-01                      |
+| Reserva pendiente de pago → confirmada (se genera el QR)                                                | HU-02       | HU-01, HU-06, HU-08        |
+| Reserva pendiente de pago → caducada (pago fallido o tiempo agotado; pago → fallido)                    | HU-02       | HU-01                      |
+| Reserva confirmada → cancelada por el usuario antes del inicio (+ devolución si procede)                | HU-03       | HU-01, HU-06, HU-08        |
+| Instalación → cerrada por mantenimiento; reservas confirmadas → canceladas con devolución íntegra y aviso | HU-07     | HU-01, HU-02, HU-06, HU-08 |
+| Reserva pendiente de pago → caducada por cierre (pago → fallido, sin cobro; mecanismo de HU-02) y aviso | HU-07       | HU-01, HU-02               |
+| Franja fija aprobada → suspendida durante el cierre (la solicitud sigue aprobada) y aviso al club       | HU-07       | HU-01, HU-08               |
+| Instalación → operativa (reapertura)                                                                    | HU-07       | HU-01, HU-02               |
+| Solicitud de franjas fijas → pendiente / aprobada / rechazada (+ aviso al club)                         | HU-05       | HU-01, HU-02, HU-08        |
+| Inscripción → confirmada / en lista de espera / baja; paso de la lista a confirmada (+ aviso)           | HU-04       | —                          |
+| Validación de acceso registrada                                                                         | HU-06       | —                          |
 
 ### Reglas que cruzan historias
 
@@ -321,7 +351,9 @@ declaran la dependencia en su spec.
   servidor.
 - **El personal solo actúa en su centro**: un QR de otro centro se rechaza indicando el motivo.
 - **Cierre por mantenimiento** reutiliza el mecanismo de cancelación y devolución de HU-03 con motivo
-  «cierre por mantenimiento» y devolución íntegra siempre.
+  «cierre por mantenimiento» y devolución íntegra siempre para las reservas confirmadas, y el
+  mecanismo de caducidad de HU-02 (sin cobro ni devolución) para las pendientes de pago. Las franjas
+  fijas de club aprobadas quedan suspendidas durante el cierre y su club recibe un aviso.
 - **Aprobación de franjas fijas con reservas en conflicto** (HU-05): no se aprueba mientras haya
   reservas confirmadas o pendientes de pago en conflicto dentro de la temporada; la aprobación lista
   esas reservas y la acción «aprobar» queda bloqueada. El técnico puede rechazar o esperar a que esas
@@ -342,25 +374,25 @@ declaran la dependencia en su spec.
 
 ### Entidades clave
 
-| Entidad | Qué representa y atributos | Propietaria (crea/modifica) | Lectoras |
-|---------|----------------------------|------------------------------|----------|
-| **Centro** | Polideportivo del municipio (4). Nombre, dirección, horario de apertura. | Precargado | Todas |
-| **Deporte** | Tenis, pádel, natación, yoga, pilates… | Precargado | Todas |
-| **Instalación** | Pista, pabellón, sala o piscina de un centro. Tipo, deporte(s) que admite, centro, reservable por franjas (sí/no), estado (operativa / cerrada por mantenimiento). | HU-07 (estado); resto precargado | HU-01, HU-02, HU-05, HU-06, HU-08 |
-| **Tarifa** | Precio por hora de una instalación (o tipo de instalación) para cada tipo de tarifa; importe en euros con decimales exactos. | Precargada | HU-01, HU-02 |
-| **Franja** | Hueco de 1 h de una instalación en una fecha y hora de inicio. **No se almacena**: se calcula a partir del horario del centro. | HU-01 (cálculo) | HU-02 |
-| **Usuario** | Nombre, correo, contraseña (protegida), rol, empadronado (sí/no), club (si es representante), centro de trabajo (si es personal). | Precargado | Todas |
-| **Club** | Nombre del club deportivo. | Precargado | HU-02, HU-05 |
-| **Reserva** | Usuario, instalación, fecha y hora de inicio y fin, tipo de tarifa aplicada, importe, estado, fecha de creación, código de acceso (QR), fecha y motivo de cancelación (si aplica). | HU-02 (crea); HU-03 y HU-07 (cancelan) | HU-01, HU-06, HU-08 |
-| **Pago** | Reserva, importe, estado, fecha. Mediante pasarela simulada. | HU-02 | HU-03, HU-07, HU-08 |
-| **Devolución** | Pago, importe, motivo (cancelación del usuario / cierre por mantenimiento), fecha. | HU-03, HU-07 | HU-08 |
-| **Cierre por mantenimiento** | Instalación, fecha y hora de inicio y fin, motivo, persona que lo registra. | HU-07 | HU-01, HU-02, HU-08 |
-| **Temporada** | Nombre, fecha de inicio y fin (septiembre a junio). | Precargada | HU-05 |
-| **Solicitud de franjas fijas** | Club, instalación, días de la semana, hora de inicio y fin, temporada, estado, fecha de resolución. | HU-05 | HU-01, HU-02, HU-08 |
-| **Curso** | Nombre, deporte, instalación, trimestre, días y horario, plazas máximas, precio. | Precargado; gestionado por HU-04 | HU-04 |
-| **Inscripción** | Usuario, curso, estado, fecha y hora de llegada (ordena la lista de espera; la posición se calcula). | HU-04 | — |
-| **Notificación** | Usuario, texto, fecha, leída (sí/no). Se muestra en «Mis avisos». | HU-04, HU-05, HU-07 | Usuario destinatario |
-| **Validación de acceso** | Reserva (si existe), código leído, resultado (aceptado / motivo del rechazo), fecha y hora, persona del personal. | HU-06 | Evidencia y demo |
+| Entidad                        | Qué representa y atributos                                                                                                                                                         | Propietaria (crea/modifica)            | Lectoras                          |
+| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- | --------------------------------- |
+| **Centro**                     | Polideportivo del municipio (4). Nombre, dirección, horario de apertura.                                                                                                           | Precargado                             | Todas                             |
+| **Deporte**                    | Tenis, pádel, natación, yoga, pilates…                                                                                                                                             | Precargado                             | Todas                             |
+| **Instalación**                | Pista, pabellón, sala o piscina de un centro. Tipo, deporte(s) que admite, centro, reservable por franjas (sí/no), estado (operativa / cerrada por mantenimiento).                 | HU-07 (estado); resto precargado       | HU-01, HU-02, HU-05, HU-06, HU-08 |
+| **Tarifa**                     | Precio por hora de una instalación (o tipo de instalación) para cada tipo de tarifa; importe en euros con decimales exactos.                                                       | Precargada                             | HU-01, HU-02                      |
+| **Franja**                     | Hueco de 1 h de una instalación en una fecha y hora de inicio. **No se almacena**: se calcula a partir del horario del centro.                                                     | HU-01 (cálculo)                        | HU-02                             |
+| **Usuario**                    | Nombre, correo, contraseña (protegida), rol, empadronado (sí/no), club (si es representante), centro de trabajo (si es personal).                                                  | Precargado                             | Todas                             |
+| **Club**                       | Nombre del club deportivo.                                                                                                                                                         | Precargado                             | HU-02, HU-05                      |
+| **Reserva**                    | Usuario, instalación, fecha y hora de inicio y fin, tipo de tarifa aplicada, importe, estado, fecha de creación, código de acceso (QR), fecha y motivo de cancelación (si aplica). | HU-02 (crea y caduca); HU-03 y HU-07 (cancelan); HU-07 (caduca por cierre, con el mecanismo de HU-02) | HU-01, HU-06, HU-08               |
+| **Pago**                       | Reserva, importe, estado, fecha. Mediante pasarela simulada. Un único pago por reserva; un pago fallido no se reintenta.                                                           | HU-02                                  | HU-03, HU-07, HU-08               |
+| **Devolución**                 | Pago, importe, motivo (cancelación del usuario / cierre por mantenimiento), fecha.                                                                                                 | HU-03, HU-07                           | HU-08                             |
+| **Cierre por mantenimiento**   | Instalación, fecha y hora de inicio y fin, motivo, persona que lo registra.                                                                                                        | HU-07                                  | HU-01, HU-02, HU-08               |
+| **Temporada**                  | Nombre, fecha de inicio y fin (septiembre a junio).                                                                                                                                | Precargada                             | HU-05                             |
+| **Solicitud de franjas fijas** | Club, instalación, días de la semana, hora de inicio y fin, temporada, estado, fecha de resolución.                                                                                | HU-05                                  | HU-01, HU-02, HU-08               |
+| **Curso**                      | Nombre, deporte, instalación, trimestre, días y horario, plazas máximas (sin precio: D10).                                                                                         | Precargado; gestionado por HU-04       | HU-04                             |
+| **Inscripción**                | Usuario, curso, estado, fecha y hora de llegada (ordena la lista de espera; la posición se calcula).                                                                               | HU-04                                  | —                                 |
+| **Notificación**               | Usuario, texto, fecha, leída (sí/no). Se muestra en «Mis avisos».                                                                                                                  | HU-04, HU-05, HU-07                    | Usuario destinatario              |
+| **Validación de acceso**       | Reserva (si existe), código leído, resultado (aceptado / motivo del rechazo), fecha y hora, persona del personal.                                                                  | HU-06                                  | Evidencia y demo                  |
 
 ## Criterios de éxito *(obligatorio)*
 
```
