# Informe de `/speckit-analyze` · 006-validar-qr · 2026-11-26 (informe inicial)

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` (con `research.md`, `data-model.md`,
`contracts/` y `quickstart.md` como apoyo) contra la constitución 1.1.0. Es el informe **inicial**, antes
de cualquier ajuste: no se ha corregido nada.

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| D1 | Constitution | CRITICAL | tasks.md T030 (L380); constitución, «Flujo de trabajo» | La constitución exige revisar que no se suben secretos **antes de fusionar** (principio V, «con herramienta si está disponible»). Ninguna tarea lo hace: T030 abre los PR con la lista I–IX, pero sin el `git grep` de quickstart §7 de 000-core. | Añadir a T030 (o a una tarea propia antes de T030) el `git grep` de 000-core quickstart §7 y `git ls-files fuentes/.env`, con el resultado en la descripción de los dos PR. |
| C1 | Underspecification | HIGH | tasks.md L42 (tabla de datos), T007 (L146–L154), T013 (L223–L232), T018 (L277–L280) | Varias pruebas HTTP insertan cada una una reserva **confirmada** en la misma pista (Este «Pista de pádel 2») y en la misma hora (la hora en punto actual de `fabrica.Reloj`): aceptada, aceptada con espacios, «otro centro», «la reserva no cambia». La segunda inserción choca con `ux_reservas_activa` (`23505`): es el mismo tipo de fallo que el #37, pero entre pruebas de esta misma spec. | Fijar en tasks.md una sola reserva confirmada compartida por la clase (creada una vez en el constructor o en un `IAsyncLifetime`, o «si no existe»), o pasar a `ConServicio` todo lo que no sea comprobar la forma HTTP. La cancelada de T013 no choca (no es activa). |
| C2 | Underspecification | HIGH | tasks.md T008 (L156–L165), T014 (L234–L242) | Las pruebas con reloj propio también reutilizan reserva: T008 tiene cinco casos y T014 seis, sobre «la reserva de Carla el 19/01/2027 de 10:00 a 11:00» y «el 21/01/2027 de 10:00 a 11:00». Si cada `[Fact]` inserta su propia confirmada a esa hora, chocan con `ux_reservas_activa`; si la comparten, el aviso de T008 depende del orden de ejecución (validaciones de otros casos de la misma reserva). | Asignar en la tabla de datos una hora distinta a cada caso de T008 (o una fecha distinta, dentro del 16–31/01/2027) y una reserva compartida de solo lectura para T014 (sus casos no dependen del historial). Ampliar la franja horaria permitida si 10:00–13:00 no basta. |
| U1 | Inconsistency | MEDIUM | tasks.md T008 (L162–L164); spec.md «Casos límite» (L245) | La spec dice que el aviso cuenta «aunque la hiciera otra persona del personal», pero la siembra solo tiene **una** persona del personal por centro, y T008 propone validar «con `personal.este` en un segundo ámbito», que es la misma persona. El caso no se prueba de verdad. | Probarlo insertando directamente una `ValidacionAcceso` aceptada con `personal_id` de otra persona (por ejemplo, `personal.oeste`, aunque sea de otro centro: la consulta del aviso no filtra por persona ni por centro), y validar después con `personal.este`. |
| U2 | Inconsistency | MEDIUM | spec.md CE-001 (L340); tasks.md T028 (L363–L367) | CE-001 pide < 1 s en el **95 %** de los intentos; T028 lo mide con 10 intentos y acepta «al menos 9 de 10» (90 %). Con 10 muestras no se puede afirmar un 95 %. | Medir 20 intentos y exigir que 19 de 20 bajen de 1 s (o todos), igual que la medición de < 300 ms p95 de la misma tarea. |
| A1 | Ambiguity | MEDIUM | tasks.md T005 (L115–L118), T021 (L301–L302) | Dos tareas no tienen entregable fijo: T005 «comprueba» y «no se cambia», y remite a T021; T021 solo actúa «si T020 no pasa». No se sabe cuándo se marcan ni qué queda hecho. | Quitar T005 (lo cubre T020) y convertir T021 en «aplicar `Politicas.EsPersonal` al grupo» dentro de T004, o marcarla explícitamente como verificación que se cierra cuando T020 está en verde. |
| A2 | Ambiguity | MEDIUM | tasks.md T019 (L281–L284); spec.md RF-009 | RF-009 («si no se puede registrar, el resultado no se da por aceptado») solo tiene una tarea de **revisión** (T019), sin prueba: la constitución (II) pide pruebas para las reglas. | Añadir una prueba con un `SaveChanges` que falle (por ejemplo, un `ValidacionAcceso` con `personal_id` inexistente vía un `IUsuarioActual` de prueba, que viola la FK) y comprobar 500 `ERROR_INTERNO` sin cuerpo de resultado. |
| F1 | Inconsistency | LOW | tasks.md «Datos de las pruebas» (reglas) vs. T020 (L296–L300) | La regla general dice que «no se ha registrado nada» se comprueba con el máximo `id` filtrando por `personal_id`; T020 lo hace con un `codigo_leido` único porque esas peticiones no tienen `personal_id`. Las dos formas son válidas, pero la regla no recoge la segunda. | Añadir a la regla: «o, si la petición no es del personal, por un `codigo_leido` único de la prueba». |
| F2 | Inconsistency | LOW | plan.md «Código fuente» (árbol de `tests/`); tasks.md T016 | El plan dice que C2 lleva «su prueba» pero el árbol de ficheros no incluye `tests/SI18.Api.Tests/Comun/ClienteApiExtensionesTests.cs`, que sí crea T016. | Añadir el fichero al árbol del plan. |
| U3 | Underspecification | LOW | tasks.md T016 | `ClienteApi` recibe `NavigationManager` en su constructor; la prueba de T016 necesita una instancia de prueba y no se dice cómo. | Indicar en T016 una subclase mínima de `NavigationManager` para la prueba (sin navegar). |
| E1 | Coverage | LOW | spec.md HU-4 esc. 3, RF-016; tasks.md T022, T024, T026 | Que el menú **no** muestre «Validar acceso» a los demás roles no se comprueba en ninguna tarea: T022 cubre el acceso por URL y T026 solo mira el menú con `personal.este`. | Añadir a T026 (o a T022) mirar el menú con una ciudadana y con el técnico. |

**Coverage Summary Table:**

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| RF-001 solo personal de centro | Sí | T004, T020, T021 | Ver A1 |
| RF-002 centro del personal | Sí | T009, T013 | Por `IUsuarioActual.CentroId` |
| RF-003 solo introducción manual | Sí | T012 | |
| RF-004 espacios y campo vacío | Sí | T004, T007, T012, T018 | |
| RF-005 orden de motivos | Sí | T009, T013, T014 | |
| RF-006 datos si se acepta | Sí | T007, T009, T012 | |
| RF-006a aviso de validación anterior | Sí | T008, T010, T012 | Ver U1, C2 |
| RF-007 datos en los rechazos | Sí | T013, T015, T017 | |
| RF-008 registro de cada intento | Sí | T007, T009, T013 | |
| RF-009 misma operación | Parcial | T009, T019 | Sin prueba (A2) |
| RF-010 la reserva no cambia | Sí | T009, T018 | |
| RF-011 código de HU-02 | Sí | T007 (usa `DatosReservas.Confirmada`) | |
| RF-012 cancelada invalida el código | Sí | T013, T029 | |
| RF-013 texto, icono y anuncio único | Sí | T012, T017, T025, T026 | |
| RF-014 teclado y foco al campo | Sí | T012, T025, T026 | |
| RF-015 320 px | Sí | T025, T026 | |
| RF-016 menú solo para el personal | Parcial | T024 | Falta comprobar los demás roles (E1) |
| RF-017 español y hora de Madrid | Sí | T003, T012, T027 | |
| CE-001 < 1 s en el 95 % | Sí | T028 | Ver U2 |
| CE-002 límites y cambios de hora | Sí | T014 | Ver C2 |
| CE-003 rechazos sin datos indebidos | Sí | T001, T013, T015 | |
| CE-004 todo registrado, nada de otros roles | Sí | T007, T013, T018, T020 | |
| CE-005 < 15 s a mano | Sí | T028 | |
| CE-006 accesibilidad | Sí | T025, T026 | |
| CE-007 aviso correcto | Sí | T008, T010 | Ver U1, C2 |

**Constitution Alignment Issues:**

- **V. Sin secretos** (D1): falta la revisión de secretos antes de fusionar que exige «Flujo de trabajo y
  controles de calidad». El resto de principios (I–IV, VI–IX) está cubierto por tareas concretas: T026
  (I), T007–T020 con `Categoria=Limites` en T014 (II), T009 (III), T004/T015 (IV), datos ficticios de la
  siembra (VI), sin entidades ni códigos nuevos (VII), sin cámara ni historial (VIII), T003/T027 (IX).

**Unmapped Tasks:**

- T005: sin entregable propio (A1).
- T023: coordinación sin código (C1 del plan); se mantiene, pero no cubre ningún requisito por sí sola.

**Metrics:**

- Total Requirements: 25 (18 RF, incluido RF-006a, y 7 CE)
- Total Tasks: 31
- Coverage % (requirements with >=1 task): 100 % (25/25); 2 parciales (RF-009, RF-016)
- Ambiguity Count: 2 (A1, A2)
- Duplication Count: 0
- Critical Issues Count: 1 (D1)

## Next Actions

- Hay **un CRITICAL** (D1): resolverlo antes de `/speckit-implement`. Es un cambio de una línea en T030.
- Los dos **HIGH** (C1, C2) también conviene resolverlos antes de escribir las pruebas: son la misma
  causa que el #37 y harían fallar la suite de forma intermitente o según el orden.
- Los MEDIUM y LOW se pueden resolver editando tasks.md (y una línea de plan.md para F2); no requieren
  volver a `/speckit-specify` ni a `/speckit-plan`.
- Sugerencia: editar a mano tasks.md (D1, C1, C2, U1, U2, A1, A2, F1, U3, E1) y plan.md (F2), y volver a
  ejecutar `/speckit-analyze` para guardar el informe tras los ajustes.

¿Quieres que proponga las ediciones concretas para estos hallazgos? (No se aplica nada automáticamente.)
