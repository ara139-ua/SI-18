# Diferencia de `spec.md` antes y después de clarify · 006-validar-qr

**Historia**: HU-06 · **Responsable**: Saul · **PR**: #45

Generado el 26/11/2026 con `git diff <commit>^ <commit> -- specs/006-validar-qr/spec.md` sobre los commits de clarify: es la salida real de git, no una reconstrucción. Las preguntas y respuestas registradas están en la sección «Clarifications» del propio `spec.md`.

En esta spec, clarify se ejecutó **después de specify y antes del plan**, no sobre la primera versión funcional (enunciado, 5.1). La pasada sobre la versión funcional (enunciado, 5.1) está en `clarify-funcional.md`.

## Commit `da3c09e`: spec-006: clarify (cámara, reutilización del código y datos en los rechazos)

> da3c09e · 2026-11-26 · saulperezz
>
> spec-006: clarify (cámara, reutilización del código y datos en los rechazos)

**Cambios**: 1 file changed, 77 insertions(+), 24 deletions(-)

```diff
diff --git a/specs/006-validar-qr/spec.md b/specs/006-validar-qr/spec.md
index 84c06d9..12e0401 100644
--- a/specs/006-validar-qr/spec.md
+++ b/specs/006-validar-qr/spec.md
@@ -41,7 +41,36 @@ Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa
 
 **Fuera de alcance**: control de aforo; validar inscripciones a cursos; generar o mostrar el QR (lo
 hace spec-002); cancelar reservas (spec-003) o cerrar instalaciones (spec-007); pantallas para consultar
-o exportar el historial de validaciones; cambiar el centro asignado a una persona del personal.
+o exportar el historial de validaciones; cambiar el centro asignado a una persona del personal; leer
+el QR con la cámara (aclaración 2026-11-26).
+
+## Clarifications
+
+### Session 2026-11-26
+
+- Q: ¿La lectura con la cámara del móvil entra en esta spec, o se deja fuera y solo se ofrece la
+  introducción manual del código? → A: Solo introducción manual; la cámara queda fuera de alcance.
+  - **Justificación**: la descripción del sistema solo dice que el personal «valida» el QR que el
+    usuario muestra en el móvil, sin mencionar la cámara; la entrada de la historia la daba como
+    alternativa («o» introduce el código a mano); spec-002 ya muestra el código en texto bajo el QR; y
+    es la opción más simple (principio VIII).
+  - **Historia afectada**: HU-1 (escenario 5), «Fuera de alcance», RF-003 y «Supuestos».
+- Q: ¿Un código ya aceptado se vuelve a aceptar en cada validación dentro de su ventana, o solo la
+  primera vez? → A: Se acepta siempre dentro de la ventana, pero si esa reserva ya tiene una validación
+  aceptada, el resultado muestra además un aviso con la hora de la anterior.
+  - **Justificación**: la descripción dice que el código es válido «durante el tiempo que dura» la
+    reserva, así que no se puede rechazar una segunda lectura; el aviso ayuda a detectar una captura
+    compartida sin bloquear el acceso y sin crear un motivo nuevo en `000-core`, porque se calcula con
+    las validaciones ya registradas.
+  - **Historia afectada**: HU-1 (escenarios 6 y 7), RF-006a, «Casos límite» y «Supuestos».
+- Q: Cuando se rechaza un código de una reserva del mismo centro porque está cancelada o porque se lee
+  fuera de su ventana, ¿se muestran al personal la instalación y la franja de esa reserva? → A: Sí, en
+  los dos casos se muestran la instalación y la franja (fecha y horas), nunca el titular.
+  - **Justificación**: con la franja el personal puede indicar cuándo volver (fuera de la ventana) y
+    explicar una cancelación que el usuario quizá no conoce, por ejemplo por un cierre por
+    mantenimiento de HU-07; nunca se muestra el titular y solo ocurre con reservas del mismo centro,
+    así que solo se expone lo que ya sabe quien enseña el código.
+  - **Historia afectada**: HU-2 (escenarios 1, 2 y 3), RF-007 y CE-003.
 
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
@@ -70,6 +99,16 @@ titular, la instalación y la franja.
    pantalla lo anuncia.
 4. **Dado** que se ha mostrado un resultado, **cuando** el personal quiere validar al siguiente,
    **entonces** puede introducir otro código sin salir de la pantalla.
+5. **Dado** una persona del personal en la pantalla de validación, **cuando** la abre, **entonces**
+   encuentra un campo para escribir el código y un botón para validarlo, y la aplicación no le pide
+   permiso para usar la cámara (aclaración 2026-11-26).
+6. **Dado** una reserva confirmada cuyo código ya se aceptó a las 17:50, **cuando** se vuelve a validar
+   a las 18:20, dentro de su ventana, **entonces** se acepta otra vez y el resultado muestra además el
+   aviso «Este código ya se validó hoy a las 17:50», con texto e icono, que el lector de pantalla
+   anuncia junto con el resultado.
+7. **Dado** una reserva cuyo código se validó antes solo con rechazos (por ejemplo, fuera de la
+   ventana), **cuando** se acepta por primera vez, **entonces** no se muestra ningún aviso de validación
+   anterior.
 
 ---
 
@@ -86,14 +125,17 @@ rechaza con el motivo de `000-core` correspondiente.
 
 **Escenarios de aceptación**:
 
-1. **Dado** una reserva confirmada que empieza a las 18:00, **cuando** se introduce su código a las
-   17:44, **entonces** se rechaza con el motivo «El código solo es válido desde 15 minutos antes del
-   inicio hasta el final de la reserva».
+1. **Dado** una reserva confirmada del centro del personal que empieza a las 18:00, **cuando** se
+   introduce su código a las 17:44, **entonces** se rechaza con el motivo «El código solo es válido
+   desde 15 minutos antes del inicio hasta el final de la reserva» y se muestran la instalación y la
+   franja «18:00–19:00» con su fecha, pero no el titular (aclaración 2026-11-26).
 2. **Dado** la misma reserva (fin a las 19:00), **cuando** se introduce su código a las 19:01,
-   **entonces** se rechaza con el mismo motivo.
-3. **Dado** una reserva que estaba confirmada y se ha cancelado (por su titular o por un cierre por
-   mantenimiento), **cuando** se introduce su código dentro de lo que era su ventana, **entonces** se
-   rechaza indicando que la reserva no está confirmada.
+   **entonces** se rechaza con el mismo motivo y se muestran la instalación y la franja, pero no el
+   titular.
+3. **Dado** una reserva del centro del personal que estaba confirmada y se ha cancelado (por su titular
+   o por un cierre por mantenimiento), **cuando** se introduce su código dentro de lo que era su
+   ventana, **entonces** se rechaza indicando que la reserva no está confirmada y se muestran la
+   instalación y la franja, pero no el titular (aclaración 2026-11-26).
 4. **Dado** un código que no corresponde a ninguna reserva, **cuando** se introduce, **entonces** se
    rechaza con «Código no válido» y no se muestra ningún dato de ninguna reserva.
 5. **Dado** una reserva confirmada de una pista de otro centro, **cuando** el personal la valida dentro
@@ -198,9 +240,10 @@ lector de pantalla y con la pantalla a 320 px de ancho.
 - **Campo vacío**: no se envía; se pide escribir el código y no se registra ninguna validación.
 - **Código muy largo o con caracteres extraños**: se rechaza como `QR_DESCONOCIDO`; se registra lo
   leído recortado a la longitud máxima de `000-core` (64 caracteres).
-- **Mismo código validado varias veces**: [NEEDS CLARIFICATION: ¿un código ya aceptado se vuelve a
-  aceptar en cada validación dentro de su ventana (por ejemplo, alguien sale y vuelve a entrar), o solo
-  la primera vez, para que una captura del QR no sirva a una segunda persona?]
+- **Mismo código validado varias veces** (aclaración 2026-11-26): dentro de la ventana se acepta cada
+  vez (por ejemplo, alguien sale y vuelve a entrar). Si la reserva ya tiene una validación aceptada, el
+  resultado avisa de la hora de la más reciente, aunque la hiciera otra persona del personal. Las
+  validaciones rechazadas no cuentan para el aviso.
 - **Código adivinado o probado al azar**: con 128 bits aleatorios no se puede adivinar (R14); cada
   intento queda registrado como rechazado.
 - **Personal sin centro asignado**: no puede darse; `000-core` exige el centro para ese rol.
@@ -219,9 +262,8 @@ lector de pantalla y con la pantalla a 320 px de ancho.
 
 **Validar un código**
 
-- **RF-003**: La pantalla de validación DEBE permitir **introducir el código a mano** (obligatorio) y
-  validarlo. [NEEDS CLARIFICATION: ¿la lectura con la cámara del móvil entra en esta spec (y con qué
-  alcance), o se deja fuera y solo se ofrece la introducción manual?]
+- **RF-003**: La pantalla de validación DEBE permitir **introducir el código a mano** y validarlo. Es
+  la única forma de lectura: la cámara queda fuera de alcance (aclaración 2026-11-26).
 - **RF-004**: Antes de validar, se DEBEN quitar los espacios del principio y del final del código. Un
   código vacío NO DEBE enviarse a validar.
 - **RF-005**: La decisión DEBE tomarse siempre en el servidor, a partir del código recibido y de la hora
@@ -237,10 +279,16 @@ lector de pantalla y con la pantalla a 320 px de ancho.
   añade la 2 (D7). No se redefinen la ventana ni los motivos.
 - **RF-006**: Si el código se acepta, se DEBEN mostrar el nombre del titular de la reserva, la
   instalación (con su centro) y la franja (fecha y horas de inicio y fin en hora de Madrid).
+- **RF-006a**: Si el código se acepta y la reserva ya tenía al menos una validación **aceptada**
+  registrada, el resultado DEBE mostrar además un aviso con la fecha y hora (de Madrid) de la más
+  reciente. El aviso no cambia el resultado (sigue aceptado), se distingue con texto e icono y se
+  anuncia junto con el resultado. Se calcula solo con las validaciones registradas; no hay estado ni
+  motivo nuevo en `000-core` (aclaración 2026-11-26).
 - **RF-007**: Si el código se rechaza, se DEBE mostrar «Acceso denegado» con el texto del motivo del
   catálogo de `000-core`. Con `QR_DESCONOCIDO` y `QR_OTRO_CENTRO` NO DEBE mostrarse ningún dato de la
-  reserva. Con `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA` se PUEDEN mostrar la instalación y la
-  franja (no el titular), para que el personal pueda explicar el motivo.
+  reserva. Con `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA` (que solo pueden darse con reservas
+  del centro del personal, RF-005) DEBEN mostrarse la instalación y la franja (fecha y horas), y NO DEBE
+  mostrarse el titular, para que el personal pueda explicar el motivo (aclaración 2026-11-26).
 
 **Registro**
 
@@ -295,7 +343,9 @@ HU-06 no crea entidades nuevas ni cambia las de `000-core` (spec.md, «Entidades
   1 segundo antes de la ventana, justo al fin, 1 segundo y 1 minuto después, y los dos días de cambio
   de hora), el resultado es el esperado.
 - **CE-003**: El 100 % de los códigos de reservas canceladas, inexistentes o de otro centro se rechazan
-  con su motivo, y ninguno muestra datos de la reserva que no permite RF-007.
+  con su motivo; ningún rechazo muestra el titular, los de códigos inexistentes o de otro centro no
+  muestran ningún dato de la reserva, y los de reservas canceladas o fuera de la ventana muestran
+  siempre la instalación y la franja.
 - **CE-004**: El 100 % de los intentos de validación del personal quedan registrados con su resultado,
   y ningún intento de otro rol o sin sesión queda registrado ni recibe datos de una reserva.
 - **CE-005**: Una persona del personal valida un código introducido a mano en menos de 15 segundos,
@@ -303,22 +353,25 @@ HU-06 no crea entidades nuevas ni cambia las de `000-core` (spec.md, «Entidades
 - **CE-006**: La validación se completa entera solo con el teclado y a 320 px de ancho, la revisión
   automática de accesibilidad (Lighthouse) da 95 o más, la revisión con axe no encuentra infracciones
   graves ni críticas y un lector de pantalla anuncia cada resultado.
+- **CE-007**: En el 100 % de los casos de prueba, una validación aceptada de una reserva que ya tenía
+  otra aceptada muestra el aviso con la hora correcta de la más reciente, y una primera validación
+  aceptada no muestra ningún aviso (aclaración 2026-11-26).
 
 ## Supuestos
 
-- **Introducción manual primero**: el código se puede leer en voz alta o copiar, porque spec-002 lo
-  muestra también como texto bajo el QR. La introducción manual es obligatoria; la cámara queda
-  pendiente de la aclaración de RF-003.
+- **Solo introducción manual** (aclaración 2026-11-26): el código se puede leer en voz alta o copiar,
+  porque spec-002 lo muestra también como texto bajo el QR (22 caracteres). La cámara queda fuera de
+  alcance; si más adelante se quisiera, se propondría como cambio de esta spec.
 - **Orden de los motivos**: `000-core` fija los motivos pero no su orden cuando hay varios; se elige el
   de RF-005 para no revelar al personal de un centro datos de reservas de otro (spec.md de `000-core`,
   caso límite «QR de otro centro»).
 - **Sin historial en pantalla**: el enunciado pide que cada validación «quede registrada», no
   consultarla; no se construye una pantalla de historial (principio VIII). El registro sirve como
   evidencia en la base de datos y en la demo.
-- **Sin estado «usado»**: `000-core` no tiene un estado de reserva «utilizada». Si la aclaración de la
-  reutilización del código pide aceptar solo la primera vez, se decidirá a partir de las validaciones
-  ya registradas, sin estado nuevo; si exigiera cambiar el modelo, se propondría primero en `000-core`
-  (RF-003 de `000-core`).
+- **Sin estado «usado»** (aclaración 2026-11-26): `000-core` no tiene un estado de reserva
+  «utilizada» y no hace falta. Un código se acepta cada vez que se valida dentro de su ventana; el aviso
+  de validación anterior (RF-006a) se obtiene de las validaciones ya registradas, sin cambiar el modelo
+  ni el catálogo de motivos de `000-core`.
 - **Sin límite de intentos**: no se limita el número de validaciones por persona del personal; los
   códigos no se pueden adivinar (128 bits, R14) y cada intento queda registrado (principio VIII).
 - **Datos de demostración**: se usan los centros, pistas y usuarios ficticios de `000-core`, con una
```
