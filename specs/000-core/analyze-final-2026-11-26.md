# Informe de `/speckit-analyze` · 000-core · 2026-11-26 (informe final)

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` de `specs/000-core` (con `data-model.md`
y `research.md` como apoyo) contra la constitución **1.2.1**, sobre la versión actual: implementada,
con converge ejecutado (T077–T079) y después de la pasada de clarify sobre la versión funcional de 002 y
006 (26/11/2026). Ejecutado por Saul en la rama `feature/refinamiento-002-006`, desde `develop` en
`32931fb`. `FEATURE_DIR` fijado a mano en `specs/000-core` (el script de prerrequisitos detecta otra
feature).

El informe inicial no se conservó; su registro reconstruido está en
[analyze-registro.md](./analyze-registro.md).

Alcance comprometido (constitución 1.2.0/1.2.1): HU-01, HU-02, HU-05 y HU-06. Las piezas de 000-core
para HU-03, HU-04, HU-07 y HU-08 se tratan como **base para trabajo futuro**: que no tengan historia
que las use no es un hallazgo de cobertura.

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| D1 | Constitution | CRITICAL | spec.md RF-016 (L245–L249) y «Responsables de los cambios de estado» (fila «pendiente de pago → caducada»); data-model.md L170–L182 (`Confirmar` solo exige `EstaVigente`); frente a spec-002 RF-009a (aclaración 2026-11-26) | RF-016 enumera las causas por las que una reserva pendiente pasa a caducada (pago fallido, 10 minutos, cierre). La aclaración de spec-002 sobre la versión funcional añade otra: pagar cuando la franja ya ha empezado. Según el principio VII, una regla distinta sobre un estado de 000-core se propone **primero en 000-core**; si no, «manda 000-core» y spec-002 queda en contradicción | Añadir en 000-core, antes de implementar el cambio de spec-002: la causa nueva en RF-016, en la fila de la tabla de responsables (HU-02) y la precondición «`ahora < inicio_utc`» en `Confirmar` (data-model.md). Va por PR revisado por Artur (principio VII, «Acuerdos de colaboración») |
| I1 | Inconsistency | MEDIUM | spec.md L46, L49, L56 y CE-007 (L415); tasks.md fase 3 (L251) y T011 (L115) | La spec y las tareas hablan de «las cuatro personas del equipo» y de «persona C» y «persona D»; la constitución 1.2.0/1.2.1 dice que el equipo son dos personas | No reescribir lo decidido en su momento: añadir en spec.md (junto a CE-007) una nota de alcance igual a la de plan.md («Dueños de módulo», #50) |
| I2 | Inconsistency | LOW | tasks.md T076 (L743) | «Se fusiona cuando estén todas las specs (falta la 006)»: la 006 ya está en `develop` (#45–#47) | Actualizar la nota de T076 (la tarea sigue cerrada) |
| I3 | Inconsistency | LOW | research.md L44–L45 | La tabla de dueños dice «Persona C» y «Persona D» sin la nota de alcance que sí lleva plan.md | Añadir la misma nota o una referencia a plan.md |
| U1 | Underspecification | LOW | tasks.md (T001–T076) | Solo cuatro tareas citan su RF de forma explícita (T048 → RF-027, T049 → RF-007, T060 → RF-015 y RF-016, T041 → RF-014 en una línea de su descripción); CE-006 se cita en la cabecera y en la fase 4. La cobertura de la tabla siguiente se ha inferido por el contenido de cada tarea | Sin acción obligatoria: la matriz de trazabilidad de la memoria debe usar la correspondencia de abajo y decir que está inferida |
| C1 | Coverage | LOW | spec.md RF-025; plan.md L136 («cálculos del informe en spec-008») | RF-025 (reglas de cómputo del informe) no tiene ninguna tarea en 000-core: el plan lo deja a spec-008, que está fuera del alcance comprometido | Aceptado: base para trabajo futuro; las reglas quedan escritas en la spec y los datos que necesitan (estados de reserva, pago y devolución) existen |

No hay ambigüedades sin cuantificar (todas las cifras están en «Valores de negocio»), ni marcadores
`TODO`/`[NEEDS CLARIFICATION]`, ni requisitos duplicados.

**Coverage Summary Table** (correspondencia inferida por el contenido de las tareas, salvo donde la tarea
cita el requisito):

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| RF-001 modelo de datos único | Sí | T027–T037, T039 | |
| RF-002 historia propietaria por entidad | Sí | T011, T035 | Más la regla de colaboración de tasks.md |
| RF-003 cambios primero en 000-core | Sí (proceso) | T076 | Regla de colaboración; ver D1 |
| RF-004 usuarios precargados ficticios | Sí | T038, T040 | |
| RF-005 correo y contraseña | Sí | T052, T053, T055, T057 | |
| RF-006 un rol por usuario | Sí | T012, T028 | |
| RF-007 tarifa | Sí | T049, T051 | Citado en T049 |
| RF-008 club y centro | Sí | T028, T050 | |
| RF-009 permisos por rol | Sí | T052, T057 | |
| RF-010 franjas de 1 h | Sí | T042, T016 | |
| RF-011 franja libre y orden de motivos | Sí | T044 | |
| RF-012 solo tenis y pádel por franjas | Sí | T027, T044 | |
| RF-013 mismo cálculo en búsqueda y reserva | Sí | T043, T044 | Probado de punta a punta en spec-002 (`ReservarComoEnLaBusquedaTests`) |
| RF-014 una reserva activa por franja | Sí | T041 | Citado en T041 |
| RF-015 transiciones de reserva | Sí | T060, T062 | Citado en T060 |
| RF-016 caducidad | Sí | T060, T068, T070 | Citado en T060; ver D1 |
| RF-017 QR opaco | Sí | T061, T062 | |
| RF-018 ventana del QR | Sí | T046, T047 | |
| RF-019 cancelación antes del inicio | Sí (base) | T045, T047, T060 | HU-03 fuera del alcance comprometido |
| RF-020 cierre por mantenimiento | Sí (base) | T031, T060 | HU-07 fuera del alcance; `IServicioReservas.AplicarCierre` en spec-002 |
| RF-021 pasarela simulada | Sí | T063, T064 | |
| RF-022 solicitud de franjas fijas | Sí | T029 | |
| RF-023 inscripción en cursos | Sí (base) | T032 | HU-04 fuera del alcance |
| RF-024 avisos en «Mis avisos» | Sí | T066, T067, T069 | |
| RF-025 informe mensual | No | — | C1: base para trabajo futuro |
| RF-026 zona horaria | Sí | T015, T016, T078 | |
| RF-027 euros | Sí | T048 | Citado en T048 |
| RF-028 motivo del rechazo | Sí | T018–T020, T071 | |
| CE-001 entidades en 000-core | Sí (proceso) | T076 | Revisión del PR final |
| CE-002 cifras una sola vez | Sí | T013, T078 | |
| CE-003 una historia por transición | Sí | T060, T062 | Ver D1 |
| CE-004 usuario por rol | Sí | T038, T040 | |
| CE-005 precio igual en búsqueda y reserva | Sí | T043 | Y spec-002 |
| CE-006 límites probados | Sí | T047, T070 | Citado en la fase 4 |
| CE-007 specs en paralelo | Sí (proceso) | T065 | Ver I1 |

**Constitution Alignment Issues**: D1 (principio VII). El resto de principios se cumplen en los tres
documentos (I: T073 y regla de accesibilidad en cada PR; II: T016, T047, T062, T070; III: R6 y T078;
IV: T018–T020, T071; V: T075, T077; VI: T038; VIII: plan.md; IX: T015, T048).

**Unmapped Tasks**: ninguna de producto. T001–T010 (esqueleto), T072–T076 (revisión y cierre) y
T077–T079 (converge) son transversales y se justifican por el plan.

**Metrics**:

- Total Requirements: 35 (28 RF y 7 CE)
- Total Tasks: 79 (todas cerradas)
- Coverage %: 97 % (34 de 35 con al menos una tarea; RF-025 queda como base para trabajo futuro)
- Ambiguity Count: 0
- Duplication Count: 0
- Critical Issues Count: 1 (D1)

## Next Actions

- **D1 se resuelve antes de implementar el cambio de spec-002** (RF-009a): proponer la causa nueva en
  RF-016 de 000-core y en `data-model.md`, por PR revisado por Artur. No requiere volver a clarify: la
  decisión funcional ya está tomada en spec-002; falta llevarla primero a la base común.
- I1–I3 son notas de documentación (LOW/MEDIUM) que se pueden hacer en el mismo PR.
- U1 y C1 no requieren cambios.

Este informe no modifica ningún fichero; las correcciones las decide Saul y se registran en
[analyze-registro.md](./analyze-registro.md).
