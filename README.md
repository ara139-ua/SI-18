# SI-18 · Reserva de Instalaciones Deportivas Municipales

**Pila propuesta:** Blazor WebAssembly · ASP.NET Core Minimal API · PostgreSQL · MudBlazor

---

## Resumen

El documento describe el **SI-18**, un sistema para que un ayuntamiento gestione la **reserva de instalaciones deportivas** (pistas de tenis y pádel, pabellones, salas y piscinas de cuatro polideportivos) y la **inscripción en cursos**. Hasta ahora todo se hacía en mostrador o por teléfono, con colas enormes en septiembre.

## De qué va

Hay cinco tipos de usuario, cada uno con necesidades distintas:

- **Ciudadanos**: buscan pistas libres, las reservan y pagan, cancelan si no pueden ir y se inscriben en cursos trimestrales.
- **Clubes**: piden horarios fijos para toda la temporada. La concejalía los revisa y aprueba, y una vez aprobados bloquean esas franjas para los demás.
- **Personal del centro**: valida los códigos QR en el acceso y marca instalaciones como cerradas por mantenimiento.
- **Técnicos de la concejalía**: sacan informes mensuales de ocupación e ingresos para justificar el presupuesto.
- **Sistema**: aplica tarifas distintas (empadronado, no empadronado, club), cobra al reservar y gestiona devoluciones.

### Reglas de negocio clave

- Las pistas se reservan en franjas de 1 hora y como máximo con 7 días de antelación.
- La reserva solo queda confirmada tras el pago.
- Si se cancela con más de 24 h de antelación hay devolución; si no, se pierde el dinero.
- Los cursos tienen plazas limitadas y lista de espera.
- El QR solo vale desde 15 minutos antes del inicio y mientras dura la reserva.
- Si una instalación cierra por mantenimiento, las reservas afectadas se cancelan solas, se avisa a los usuarios y se devuelve el importe.

### Requisitos no funcionales

- Accesibilidad **WCAG AA** (administración pública).

---

## Historias de usuario

### HU1. Buscar pistas libres

**Historia de usuario**
- Como ciudadano
- Quiero buscar pistas libres filtrando por deporte, centro y fecha
- Para encontrar rápidamente dónde jugar un partido

**Detalles**
- Los filtros son deporte, centro y fecha.
- El resultado muestra las franjas libres de 1 hora.
- Cada franja muestra el precio que le corresponde al usuario según su tarifa: empadronado, no empadronado o club.
- Las franjas ocupadas, bloqueadas por clubes o cerradas por mantenimiento no aparecen como disponibles.

**COS**
- Dado un deporte, un centro y una fecha, se listan solo las franjas libres.
- Un usuario empadronado y uno no empadronado ven precios distintos para la misma franja.
- Si no hay disponibilidad, se muestra un mensaje claro.
- Los filtros y resultados cumplen WCAG AA.

---

### HU2. Reservar y pagar una pista

**Historia de usuario**
- Como ciudadano
- Quiero reservar una pista y pagarla en el mismo momento
- Para tener la franja asegurada sin hacer cola

**Detalles**
- La reserva se confirma solo tras el pago correcto.
- No se permite reservar con más de 7 días de antelación.
- Mientras el pago está pendiente, la franja no debe poder reservarla otro usuario.
- Al confirmar, se genera el código QR asociado a la reserva.

**COS**
- Una reserva sin pago completado no queda confirmada ni bloquea la pista de forma permanente.
- Un intento de reservar a más de 7 días es rechazado con un mensaje explicativo.
- Dos usuarios no pueden confirmar la misma franja.
- Tras el pago, la reserva aparece como confirmada con su QR.

---

### HU3. Cancelar una reserva

**Historia de usuario**
- Como ciudadano
- Quiero cancelar una reserva que no voy a usar
- Para liberar la pista y recuperar mi dinero cuando corresponda

**Detalles**
- Con más de 24 h de antelación respecto al inicio, se devuelve el importe.
- Con menos de 24 h, la cancelación libera la pista pero no hay devolución.
- Antes de confirmar, se informa al usuario de si recibirá reembolso o no.
- La franja liberada vuelve a aparecer como disponible.

**COS**
- Cancelar con más de 24 h genera una devolución del importe completo.
- Cancelar con 24 h o menos no genera devolución.
- Tras cancelar, el QR deja de ser válido.
- La franja vuelve a estar reservable.

---

### HU4. Inscribirse en un curso con lista de espera

**Historia de usuario**
- Como ciudadano
- Quiero inscribirme en un curso trimestral (natación, yoga, pilates, iniciación al tenis)
- Para practicar deporte de forma regular

**Detalles**
- Cada curso tiene un número de plazas limitado.
- Si hay plazas, la inscripción se confirma.
- Si está lleno, el usuario entra en una lista de espera ordenada por orden de llegada.
- Cuando alguien se da de baja, se asigna la plaza al primero de la lista y se le avisa.

**COS**
- Con plazas libres, la inscripción queda confirmada y se descuenta una plaza.
- Con el curso lleno, el usuario queda en lista de espera y ve su posición.
- Al producirse una baja, la plaza pasa al siguiente de la lista.
- Nunca se superan las plazas máximas del curso.

---

### HU5. Solicitar franjas fijas para un club

**Historia de usuario**
- Como representante de un club deportivo
- Quiero solicitar franjas fijas para toda la temporada (por ejemplo, pabellón 2, martes y jueves de 19 a 21 h)
- Para organizar los entrenamientos con horarios estables

**Detalles**
- La solicitud indica instalación, días de la semana, horario y temporada.
- Queda en estado pendiente hasta que la concejalía la revisa.
- La concejalía puede aprobarla o rechazarla.
- Si se aprueba, las franjas se bloquean para el resto de usuarios durante toda la temporada.

**COS**
- Una solicitud nueva aparece como pendiente y no bloquea nada todavía.
- Al aprobarla, las franjas dejan de estar disponibles en las búsquedas de otros usuarios.
- Al rechazarla, las franjas siguen disponibles y el club recibe el resultado.
- No se puede aprobar una solicitud que choque con reservas ya existentes sin tratarlas antes.

---

### HU6. Validar el acceso con código QR

**Historia de usuario**
- Como personal del centro
- Quiero validar el código QR que muestra el usuario
- Para dejar pasar solo a quien tiene una reserva válida

**Detalles**
- El QR está asociado a una reserva concreta.
- Es válido desde 15 minutos antes del inicio y hasta el final de la reserva.
- Fuera de ese intervalo, o si la reserva está cancelada, se rechaza.
- El resultado de la validación debe ser claro y rápido para el personal.

**COS**
- Un QR leído dentro de la ventana de validez se acepta.
- Un QR leído más de 15 minutos antes del inicio, o después del fin, se rechaza.
- Un QR de una reserva cancelada se rechaza.
- La pantalla de validación es accesible y legible desde el móvil.

---

### HU7. Cerrar una instalación por mantenimiento

**Historia de usuario**
- Como personal del centro
- Quiero marcar una instalación como cerrada por mantenimiento
- Para que nadie pueda reservarla mientras no esté operativa

**Detalles**
- Al cerrar, la instalación deja de aceptar nuevas reservas.
- Las reservas existentes afectadas se cancelan automáticamente.
- Se avisa a los usuarios afectados y se les devuelve el importe, sea cual sea la antelación.
- El personal indica el motivo y el periodo de cierre.

**COS**
- Una instalación cerrada no aparece como disponible en las búsquedas.
- Todas las reservas afectadas pasan a estado cancelado.
- Cada usuario afectado recibe una notificación y la devolución íntegra.
- Al reabrir la instalación, vuelve a admitir reservas.

---

### HU8. Consultar el informe mensual de ocupación e ingresos

**Historia de usuario**
- Como técnico de la concejalía
- Quiero ver un informe mensual de ocupación e ingresos por centro e instalación
- Para planificar las inversiones del año siguiente y justificar el presupuesto

**Detalles**
- El informe se genera por mes y se desglosa por centro y por instalación.
- Muestra el porcentaje de ocupación.
- Muestra lo recaudado según el tipo de tarifa: empadronado, no empadronado y club.
- Conviene dejar claro cómo cuentan las reservas canceladas y las devoluciones.

**COS**
- Al elegir un mes, se muestran ocupación e ingresos de cada centro y cada instalación.
- La suma de los ingresos por tarifa coincide con el total del centro.
- Las reservas canceladas con devolución no se cuentan como ingreso.
- El informe cumple WCAG AA.

---

## Notas

- El documento da pie a una novena historia: la **aprobación de franjas por la concejalía**. Aquí está incluida dentro de HU5, pero puede separarse.
- Ambigüedad a decidir: el texto no dice cómo cuentan en la ocupación las franjas bloqueadas por clubes ni las instalaciones cerradas. Conviene fijarlo en los COS de HU8.

---

Reparto propuesto
Persona	Historias	Tema:
- Artur	HU1 (buscar pistas) + HU5 (franjas fijas de clubes)	Disponibilidad: qué franjas están libres, a qué precio y cuáles bloquea un club
- B	    HU2 (reservar y pagar) + HU6 (validar QR)	Reserva y acceso: se crea la reserva, se genera el QR y se valida en la puerta
- Saul	HU3 (cancelar reserva) + HU7 (cierre por mantenimiento)	Cancelaciones y devoluciones: el cierre por mantenimiento es una cancelación masiva con reembolso
- D	    HU4 (cursos y lista de espera) + HU8 (informe mensual)	Cursos e informes: la parte más independiente del sistema
