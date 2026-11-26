# Diferencia de `spec.md` antes y después de clarify · 001-buscar-pistas

**Historia**: HU-01 · **Responsable**: Artur · **PR**: #19

Generado el 26/11/2026 con `git diff <commit>^ <commit> -- specs/001-buscar-pistas/spec.md` sobre los commits de clarify: es la salida real de git, no una reconstrucción. Las preguntas y respuestas registradas están en la sección «Clarifications» del propio `spec.md`.

En esta spec, clarify se ejecutó **después de specify y antes del plan**, no sobre la primera versión funcional (enunciado, 5.1). La pasada sobre la versión funcional se ha pedido a Artur; si llega, se guardará en `clarify-funcional.md`. Si no, consta que se aclaró antes del plan.

## Commit `bccc3a0`: 001: clarify (sesión 2026-11-25)

> bccc3a0 · 2026-11-25 · Artur Ramazanov Anichkin
>
> 001: clarify (sesión 2026-11-25)
>
> Cinco aclaraciones: consultas provisionales de solo lectura mientras HU-02, HU-05 y HU-07 no
> publiquen las suyas (nuevo RF-014a); búsqueda solo con sesión; solo se muestran las franjas
> libres; el botón «Reservar» lo añade la spec-002; el centro es obligatorio.

**Cambios**: 1 file changed, 40 insertions(+), 13 deletions(-)

```diff
diff --git a/specs/001-buscar-pistas/spec.md b/specs/001-buscar-pistas/spec.md
index 1aa4459..db01f79 100644
--- a/specs/001-buscar-pistas/spec.md
+++ b/specs/001-buscar-pistas/spec.md
@@ -33,6 +33,24 @@ Esta spec **no redefine** nada de `000-core` (constitución, principio VII). Usa
 **Fuera de alcance** (lo hacen otras specs): reservar y pagar la franja (spec-002), las franjas fijas
 de club (spec-005) y los cierres por mantenimiento (spec-007). HU-01 solo **lee** su resultado.
 
+## Clarifications
+
+### Session 2026-11-25
+
+- Q: ¿Qué hace HU-01 mientras HU-02, HU-05 y HU-07 no publican sus consultas de reservas, franjas
+  fijas y cierres? → A: HU-01 incluye versiones provisionales de solo lectura de esas consultas, con
+  las firmas de `000-core`, marcadas como provisionales; cada una se sustituye por la de su dueño
+  cuando exista.
+- Q: ¿Se puede buscar pistas sin haber iniciado sesión? → A: No; solo con sesión, y se ve el precio
+  de la tarifa propia. Sin sesión se lleva a «Iniciar sesión».
+- Q: En el resultado, ¿qué se hace con las franjas que no están libres? → A: No se muestran; solo
+  aparecen las franjas libres.
+- Q: ¿Qué spec pone el botón «Reservar» en cada franja del resultado? → A: La spec-002; la spec-001
+  entrega cada franja identificada (pista, fecha y hora de inicio) y no dibuja ningún botón de
+  reserva.
+- Q: ¿El centro es obligatorio al buscar, o se puede buscar en todos los centros a la vez? → A:
+  Obligatorio; no existe la opción «Todos los centros».
+
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
 ### Historia de usuario 1 - Ver las franjas libres de un deporte en un centro y una fecha (Prioridad: P1)
@@ -228,6 +246,11 @@ ven en la búsqueda:
   las reservas activas (de HU-02), los cierres por mantenimiento (de HU-07) y las franjas fijas
   aprobadas (de HU-05) solo a través de las consultas que esas historias publican (`000-core`,
   contracts/modulos.md). No DEBE escribir en sus datos.
+- **RF-014a**: Mientras la historia dueña de una de esas consultas no la haya publicado, HU-01 DEBE
+  aportar una versión **provisional de solo lectura** con la misma firma de `000-core`, que aplique las
+  mismas reglas (por ejemplo, la vigencia de 10 minutos de una reserva pendiente) y esté marcada como
+  provisional. Cuando el dueño publique la suya, la provisional DEBE retirarse sin cambiar el
+  comportamiento de la búsqueda.
 
 **Accesibilidad e idioma**
 
@@ -275,21 +298,25 @@ franjas libres con hora de inicio, hora de fin y precio.
 
 ## Supuestos
 
-- **Búsqueda con sesión**: buscar exige haber iniciado sesión, porque el precio depende de la tarifa
-  del usuario y la historia la plantea para ciudadanos y representantes. Una búsqueda anónima con
-  todos los precios queda fuera de alcance.
-- **Tres filtros obligatorios**: deporte, centro y fecha son obligatorios, como en el criterio de
-  aceptación («dado un deporte, un centro y una fecha»). Buscar en todos los centros a la vez o en un
-  rango de fechas queda fuera de alcance (constitución, principio VIII).
-- **Solo franjas libres**: el resultado lista solo las franjas libres; no muestra las no disponibles
-  con su motivo. Es lo que piden los criterios de aceptación y lo más simple.
-- **De la búsqueda a la reserva**: el acceso a «Reservar» desde una franja del resultado pertenece a
-  HU-02 (spec-002). HU-01 solo deja identificada cada franja (pista, fecha y hora de inicio) para que
-  HU-02 pueda partir de ella.
+- **Búsqueda con sesión** (confirmado en «Clarifications»): buscar exige haber iniciado sesión,
+  porque el precio depende de la tarifa del usuario y la historia la plantea para ciudadanos y
+  representantes. La búsqueda anónima, con o sin precios, queda fuera de alcance.
+- **Tres filtros obligatorios** (el centro, confirmado en «Clarifications»): deporte, centro y fecha
+  son obligatorios, como en el criterio de aceptación («dado un deporte, un centro y una fecha»).
+  Buscar en todos los centros a la vez o en un rango de fechas queda fuera de alcance (constitución,
+  principio VIII).
+- **Solo franjas libres** (confirmado en «Clarifications»): el resultado lista solo las franjas
+  libres; las no disponibles no se muestran, ni como «No disponible» ni con su motivo. Es lo que piden
+  los criterios de aceptación y lo más simple.
+- **De la búsqueda a la reserva** (confirmado en «Clarifications»): el botón «Reservar» de cada franja
+  del resultado lo añade HU-02 (spec-002). HU-01 no dibuja ningún botón de reserva, ni siquiera
+  inactivo: solo deja identificada cada franja (pista, fecha y hora de inicio) para que HU-02 pueda
+  partir de ella.
 - **Sin actualización automática**: el resultado refleja el momento de la búsqueda; para ver cambios
   se vuelve a buscar. HU-02 vuelve a comprobar disponibilidad y precio al reservar.
 - **Dependencias aún no construidas**: HU-02, HU-05 y HU-07 publican las consultas que esta historia
-  necesita (`000-core`, contracts/modulos.md). Si alguna no está implementada cuando se construya
-  HU-01, el plan acordará con su responsable cómo cubrir el hueco sin que HU-01 escriba en sus datos.
+  necesita (`000-core`, contracts/modulos.md). Mientras no existan, HU-01 usa sus versiones
+  provisionales de solo lectura (RF-014a), que se avisan a cada dueño (Saul, C y la propia HU-05) para
+  que las sustituyan; HU-01 nunca escribe en sus datos.
 - **Datos de demostración**: se usan los centros, pistas, tarifas y usuarios ficticios sembrados por
   `000-core`.
```
