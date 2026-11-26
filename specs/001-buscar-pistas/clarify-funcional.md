# Clarify sobre la versión funcional · 001-buscar-pistas (HU-01)

**Responsable**: Artur. **Revisor**: Saul (revisión cruzada). **Comando**: `/speckit-clarify` (Spec Kit 1.1.2.dev0)
sobre `specs/001-buscar-pistas`, después de probar la aplicación ya implementada (enunciado, 5.1). La diferencia de
la spec antes y después de esta pasada está en el apartado 5; la de la primera pasada, en [clarify-diff.md](./clarify-diff.md).

## 1. Tres situaciones probadas

**Cómo se probó**: contra la API local (`https://localhost:7180`, `dotnet run` con la siembra de demo en una base
propia, `si18_demo`, y PostgreSQL de `docker-compose`), con sesiones reales de los usuarios ficticios de la siembra,
el 26/11/2026, sobre `develop` en `027ab28`. Las peticiones las hizo un script de Artur fuera del repositorio
(`situaciones.mjs`, que reinicia los datos antes de empezar); aquí se copian la petición, los datos, el resultado
esperado según la spec antes de esta pasada y el observado. Las horas son de Madrid; «hoy» es el 26/11/2026.

Centros: 1 = Polideportivo Norte, 2 = Polideportivo Sur. Deportes: 2 = pádel, 5 = natación.

| Situación | Hora (Madrid) | Usuario | Petición | Datos | Esperado | Observado |
|-----------|---------------|---------|----------|-------|----------|-----------|
| S1 habitual | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 200: pistas de pádel del Sur con franjas libres a 6,00 € | **200** · 2 pistas, 28 franjas; primera: Pista de pádel 1 08:00–09:00 6 € |
| S1 habitual | 26/11/2026 22:33:45 | `ciudadano.noempadronado@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 200: las mismas franjas a 10,00 € | **200** · 2 pistas, 28 franjas; primera: Pista de pádel 1 08:00–09:00 10 € |
| S1 habitual | 26/11/2026 22:33:45 | `representante.padel@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 200: las mismas franjas a 4,00 € (tarifa club) | **200** · 2 pistas, 28 franjas; primera: Pista de pádel 1 08:00–09:00 4 € |
| S2 límite | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-12-03` | — | 200 (hoy + 7 entra) | **200** · 2 pistas, 28 franjas; primera: Pista de pádel 1 08:00–09:00 6 € |
| S2 límite | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-12-04` | — | 422 FUERA_DE_ANTELACION | **422** · `codigo`: FUERA_DE_ANTELACION; `detail`: Solo puedes reservar desde hoy hasta dentro de 7 días. |
| S2 límite | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-25` | — | 422 FUERA_DE_ANTELACION | **422** · `codigo`: FUERA_DE_ANTELACION; `detail`: Solo puedes reservar desde hoy hasta dentro de 7 días. |
| S2 límite | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-26` | — | 200 solo con franjas que aún no han empezado; a las 22:33 no queda ninguna (pregunta 2) | **200** · 0 pistas, 0 franjas |
| S2 entrada inválida | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=31/02/2026` | — | 400 PETICION_INVALIDA con el campo fecha marcado | **400** · `codigo`: PETICION_INVALIDA; `detail`: Revisa los datos marcados.; `errors`: {"fecha":["Elige una fecha válida."]} |
| S2 entrada inválida | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=5&centroId=1&fecha=2026-11-27` | — | Sin decidir en la spec (pregunta 1) | **200** · 0 pistas, 0 franjas |
| S2 entrada inválida | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=99&fecha=2026-11-27` | — | 404 NO_ENCONTRADO (centro inexistente) | **404** · `codigo`: NO_ENCONTRADO; `detail`: No se ha encontrado lo que buscas. |
| S3 permiso | 26/11/2026 22:33:45 | `(sin sesión)` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 401 | **401** · `codigo`: NO_AUTENTICADO; `detail`: Tienes que iniciar sesión para continuar. |
| S3 permiso | 26/11/2026 22:33:45 | `personal.norte@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `detail`: Tu perfil no permite realizar esta acción. |
| S3 permiso | 26/11/2026 22:33:45 | `tecnico@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=2&fecha=2026-11-27` | — | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `detail`: Tu perfil no permite realizar esta acción. |

Todas dieron el resultado esperado salvo dos puntos que la spec no decidía: natación (un deporte que existe
pero no se reserva por franjas) y la búsqueda de hoy cuando ya no queda ninguna franja. Son las dos preguntas
de abajo. La pantalla solo ofrece tenis y pádel, así que el caso de natación solo se alcanza escribiendo la
petición a mano.

## 2. Preguntas, respuestas y justificación

Integradas en spec.md, «Clarifications», sesión «2026-11-26 (sobre la versión funcional, enunciado 5.1)».

| # | Pregunta | Respuesta | Justificación (resumen) | Historia y criterios afectados |
|---|----------|-----------|-------------------------|--------------------------------|
| 1 | Si se pide por la API un deporte que existe pero no se reserva por franjas (natación, yoga…), ¿qué responde la búsqueda? | 200 con resultado vacío y `totalPistas = 0`; la pantalla dice que el centro no tiene pistas del deporte | El README solo habla de pistas reservables por franjas; para quien busca, «este centro no tiene pistas de natación» es cierto y le orienta mejor que un error. Un 400 o un 422 exigiría un código nuevo en 000-core sin que la pantalla lo pueda provocar | «Casos límite» (Deporte sin pistas reservables), RF-011 |
| 2 | Al buscar para hoy cuando ya no queda ninguna franja por empezar (después de las 21:00), ¿qué se muestra? | El mensaje general de RF-011 («No hay pistas libres… Prueba otra fecha u otro centro»); la fecha propuesta sigue siendo hoy | El mensaje ya sugiere otra fecha, que es lo útil; un mensaje propio de «hoy» o cambiar la fecha por defecto según la hora añadiría reglas sin cambiar lo que puede hacer el usuario | «Casos límite» (Hoy sin franjas por empezar), RF-005, RF-011 |

## 3. Criterios afectados en «Dado… Cuando… Entonces…»

- **RF-011 (deporte sin pistas reservables)**: **Dado** el Polideportivo Norte, donde la natación solo está en
  la piscina, **cuando** Ana pide por la API la búsqueda de natación en ese centro para mañana, **entonces**
  recibe 200 con `totalPistas = 0` y ninguna pista, y la pantalla mostraría «Polideportivo Norte no tiene pistas
  de Natación.».
- **RF-011 (hoy sin franjas)**: **Dado** que son las 21:00:00 o más tarde en Madrid, **cuando** Ana busca pádel
  en el Polideportivo Norte para hoy, **entonces** no se muestra ninguna franja, aparece el mensaje general «No
  hay pistas libres… Prueba otra fecha u otro centro» (con `totalPistas = 2`) y la fecha propuesta sigue siendo hoy.

## 4. Qué queda

- Las dos respuestas ya se cumplían en el código (tabla de arriba). La 1 tenía prueba solo con «Baloncesto»;
  se añadió «Natación» en T028 (`BuscarSinResultadosTests.Un_deporte_sin_pistas_reservables_da_200_con_total_cero`).
  La 2 ya la probaba T018 (`Hoy_a_las_21_ya_no_quedan_franjas_pero_si_pistas`).
- `plan.md` no cambia: RF-011 ya se apoyaba en `TotalPistas`. `tasks.md` solo gana T028.
- Analyze repetido: [analyze-funcional.md](./analyze-funcional.md) (E3, resuelto con T028; I2 e I3 siguen como
  notas pendientes de plan.md). Converge: [converge-2026-11-26-2.md](./converge-2026-11-26-2.md).
  Batería completa en verde después del cambio (410 pruebas).

## 5. Diferencia de spec.md

```diff
diff --git a/specs/001-buscar-pistas/spec.md b/specs/001-buscar-pistas/spec.md
index 2143564..0f36f9a 100644
--- a/specs/001-buscar-pistas/spec.md
+++ b/specs/001-buscar-pistas/spec.md
@@ -51,6 +51,15 @@ de club (spec-005) y los cierres por mantenimiento (spec-007). HU-01 solo **lee*
 - Q: ¿El centro es obligatorio al buscar, o se puede buscar en todos los centros a la vez? → A:
   Obligatorio; no existe la opción «Todos los centros».
 
+### Session 2026-11-26 (sobre la versión funcional, enunciado 5.1)
+
+- Q: Si se pide por la API un deporte que existe pero no se reserva por franjas (natación, yoga…), ¿qué
+  responde la búsqueda? → A: Un resultado vacío con `totalPistas = 0`, que la pantalla muestra con el
+  mensaje «el centro no tiene pistas del deporte» de RF-011; no es una petición inválida.
+- Q: Al buscar para hoy cuando ya no queda ninguna franja por empezar (después de las 21:00), ¿qué se
+  muestra? → A: El mensaje general de RF-011 («No hay pistas libres… Prueba otra fecha u otro centro»);
+  no hay un mensaje propio para «hoy» y la fecha propuesta sigue siendo hoy.
+
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
 ### Historia de usuario 1 - Ver las franjas libres de un deporte en un centro y una fecha (Prioridad: P1)
@@ -171,6 +180,9 @@ ven en la búsqueda:
   el día 2 aunque en UTC aún sea el día 1).
 - **Franja empezando**: a las 18:00:00 en punto la franja de las 18:00 ya no aparece; a las 17:59:59
   sí.
+- **Hoy sin franjas por empezar**: a partir de las 21:00:00 de Madrid, buscar la fecha de hoy no da
+  ninguna franja y se muestra el mensaje general de RF-011, que sugiere otra fecha u otro centro. La
+  fecha propuesta por defecto sigue siendo hoy (RF-005).
 - **Cambio de hora**: los días 25/10/2026 y 28/03/2027 la búsqueda muestra igualmente 14 franjas de
   08:00 a 21:00 en hora de Madrid, cada una de una hora real.
 - **Reserva pendiente que caduca**: una reserva pendiente de pago creada hace exactamente 10 minutos
@@ -182,7 +194,9 @@ ven en la búsqueda:
 - **Franja fija suspendida por un cierre**: durante el cierre la franja aparece como cerrada (no
   libre), no como bloqueada por el club; el resultado para quien busca es el mismo: no aparece.
 - **Deporte sin pistas reservables**: baloncesto, fútbol sala, natación, yoga y pilates no tienen
-  pistas reservables por franjas (RF-012 de `000-core`), así que no se ofrecen como filtro.
+  pistas reservables por franjas (RF-012 de `000-core`), así que no se ofrecen como filtro. Si se piden
+  igualmente (por ejemplo, escribiendo la petición a mano), el resultado es vacío con `totalPistas = 0`
+  y se muestra el mensaje de RF-011 «el centro no tiene pistas del deporte», no un error.
 - **Instalación no reservable**: pabellones, salas y piscinas no aparecen nunca en el resultado,
   aunque admitan el deporte elegido.
 - **Dos personas a la vez**: la búsqueda muestra la situación en el momento de buscar; si otra
@@ -227,8 +241,9 @@ ven en la búsqueda:
   inicio.
 - **RF-010**: Si una instalación no tiene ninguna franja libre, NO DEBE aparecer en el resultado.
 - **RF-011**: Si no hay ninguna franja libre en total, DEBE mostrarse un mensaje que lo diga y sugiera
-  cambiar la fecha o el centro; si el centro no tiene ninguna pista del deporte elegido, el mensaje
-  DEBE decirlo así.
+  cambiar la fecha o el centro; si el centro no tiene ninguna pista reservable por franjas del deporte
+  elegido (también cuando el deporte no se reserva por franjas), el mensaje DEBE decirlo así. Para
+  distinguir los dos casos, el resultado DEBE indicar cuántas pistas del deporte tiene el centro.
 
 **Precio y coherencia con la reserva**
```
