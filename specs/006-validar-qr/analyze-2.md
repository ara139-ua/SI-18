# Informe de `/speckit-analyze` · 006-validar-qr · 2026-11-26 (informe final)

Segunda pasada de solo lectura, después del commit «spec-006: correcciones de analyze» (`6ce36b8`),
sobre `spec.md`, `plan.md` y `tasks.md` (con `research.md`, `data-model.md`, `contracts/` y
`quickstart.md` como apoyo) contra la constitución 1.1.0. El registro de los hallazgos de la primera
pasada está en [analyze-registro.md](./analyze-registro.md).

## Comprobación de los hallazgos de analyze-1

| ID | Gravedad | ¿Sigue? | Dónde se comprueba |
|----|----------|---------|--------------------|
| D1 | CRITICAL | No | T030: revisión de secretos (`git grep` de quickstart §7 de 000-core y `git ls-files fuentes/.env`) antes de abrir cada PR |
| C1 | HIGH | No | «Datos de las pruebas»: una sola reserva HTTP compartida e idempotente (T006 `ConfirmadaCompartida`, T007 con `IAsyncLifetime`); T013 la reutiliza y la cancelada no es activa |
| C2 | HIGH | No | «Asignación de fechas»: T008 → 18, 20 y 22/01/2027; T014 → 21/01/2027 de solo lectura; T018b → 25/01/2027 |
| U1 | MEDIUM | No | T008 (22/01/2027): aceptada anterior con `personal_id` de `personal.oeste` |
| U2 | MEDIUM | No | T028: 20 mediciones, al menos 19 < 1 s |
| A1 | MEDIUM | No | T005 y T021 retiradas; la política en T004 y la prueba en T020 |
| A2 | MEDIUM | No | T018b: fallo de `SaveChanges` por FK dentro de la prueba, sin filas; T019 depende de T018b |
| F1 | LOW | No | Regla con las dos formas y `HayValidacionConCodigo` (T006, T020) |
| F2 | LOW | No | plan.md: `Comun/ClienteApiExtensionesTests.cs` en el árbol |
| U3 | LOW | No | T016: `NavegacionDePrueba : NavigationManager` |
| E1 | LOW | No | T026: menú con una ciudadana y con el técnico |

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| L1 | Coverage | LOW | tasks.md T007 (L174–L188); spec.md HU-1 esc. 1 | Al compartir la reserva HTTP (C1), T007 ya no puede exigir `validacionAnteriorHora` nulo en el primer 201: otras pruebas de la clase pueden haberla validado antes. «Sin aviso en la primera aceptada» queda probado solo a nivel de servicio (T008, 18/01/2027), no por HTTP. | Aceptable: la forma HTTP del campo se comprueba en T007 y su valor en T008. Si se quiere también por HTTP, comprobar en T007 que `validacionAnteriorHora` es nulo **o** igual a la hora de una validación aceptada anterior de esa reserva leída de la base. No bloquea. |

**Coverage Summary Table:**

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| RF-001 | Sí | T004, T020 | |
| RF-002 | Sí | T009, T013 | |
| RF-003 | Sí | T012 | |
| RF-004 | Sí | T004, T007, T012, T018 | |
| RF-005 | Sí | T009, T013, T014, T015 | |
| RF-006 | Sí | T007, T009, T012 | |
| RF-006a | Sí | T008, T010, T012 | Ver L1 |
| RF-007 | Sí | T001, T013, T015, T017 | |
| RF-008 | Sí | T007, T009, T013 | |
| RF-009 | Sí | T009, T018b, T019 | Con prueba (antes, parcial) |
| RF-010 | Sí | T009, T018 | |
| RF-011 | Sí | T007 | |
| RF-012 | Sí | T013, T029 | |
| RF-013 | Sí | T012, T017, T025, T026 | |
| RF-014 | Sí | T012, T025, T026 | |
| RF-015 | Sí | T025, T026 | |
| RF-016 | Sí | T024, T026 | Con los demás roles (antes, parcial) |
| RF-017 | Sí | T003, T012, T027 | |
| CE-001 | Sí | T028 | 19 de 20 |
| CE-002 | Sí | T014 | |
| CE-003 | Sí | T001, T013, T015 | |
| CE-004 | Sí | T007, T013, T018, T020 | |
| CE-005 | Sí | T028 | |
| CE-006 | Sí | T025, T026 | |
| CE-007 | Sí | T008, T010 | |

**Constitution Alignment Issues:** ninguno. I (T025, T026), II (pruebas antes de la implementación,
`Categoria=Limites` en T014, prueba de RF-009 en T018b), III (T009), IV (T004, T015; ningún código
nuevo), V (T030), VI (siembra ficticia), VII (sin entidades, estados ni códigos nuevos), VIII (sin cámara,
historial, estado «utilizada» ni límite de intentos) y IX (T003, T027).

**Unmapped Tasks:** T023 (coordinación sin código del enlace del menú; prepara T024). No es un problema.

**Metrics:**

- Total Requirements: 25 (18 RF, incluido RF-006a, y 7 CE)
- Total Tasks: 30 activas (T001–T031 sin T005 ni T021, retiradas, más T018b)
- Coverage % (requirements with >=1 task): 100 % (25/25); 0 parciales
- Ambiguity Count: 0
- Duplication Count: 0
- Critical Issues Count: 0

## Next Actions

- Sin CRITICAL ni HIGH. Se puede pasar a `/speckit-implement` empezando por la Fase 1 (T001 y T002).
- L1 es opcional y no bloquea; se puede ajustar al escribir T007.

¿Quieres que proponga la edición concreta para L1? (No se aplica nada automáticamente.)
