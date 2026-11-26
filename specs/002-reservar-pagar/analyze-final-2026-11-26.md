# Informe de `/speckit-analyze` · 002-reservar-pagar · 2026-11-26 (informe final)

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` de `specs/002-reservar-pagar` (con
`research.md`, `data-model.md`, `contracts/` y las pruebas como apoyo) contra la constitución **1.2.1**,
sobre la versión actual: implementada (T001–T054), con el primer converge hecho y **después de la pasada
de clarify sobre la versión funcional** (sesión «2026-11-26 (sobre la versión funcional)» de spec.md, tres
aclaraciones). Ejecutado por Saul en la rama `feature/refinamiento-002-006`, desde `develop` en
`32931fb`.

El informe inicial no se conservó; su registro reconstruido está en
[analyze-registro.md](./analyze-registro.md).

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| D1 | Constitution | CRITICAL | spec.md RF-009a, HU-2 esc. 6, «Casos límite» («Pagar cuando la franja ya ha empezado»); 000-core spec.md RF-016 y data-model.md (`Confirmar`) | La aclaración 1 añade una causa para que una reserva pendiente pase a caducada (pagar con la franja ya empezada). RF-016 de 000-core enumera las causas y no la incluye, y `Confirmar` solo exige `EstaVigente`. Principio VII: el cambio se propone primero en 000-core; mientras no esté allí, spec-002 contradice a 000-core | Llevar la causa a RF-016, a la tabla de responsables y a la precondición de `Confirmar` en 000-core (PR revisado por Artur) **antes** de implementar RF-009a. Mismo hallazgo que D1 de [000-core](../000-core/analyze-final-2026-11-26.md) |
| C1 | Coverage | HIGH | spec.md RF-009a, RF-008a (hora límite), RF-011, RF-017, HU-2 esc. 6, HU-5 esc. 3; plan.md «Flujo»; research.md E4; tasks.md (ninguna tarea) | Los criterios nuevos de la aclaración 1 no tienen tarea ni prueba, y el plan y research.md E4 siguen describiendo el pago sin comprobar el inicio. La versión funcional confirma una reserva pagada después del inicio (clarify-funcional.md) | Actualizar research.md E4 (paso de pago: después de la vigencia, comprobar `ahora < inicio`) y añadir las tareas con su prueba; es trabajo de código: lo debe convertir en tareas `/speckit-converge` |
| C2 | Coverage | MEDIUM | spec.md HU-4 esc. 3 (frase nueva), «Casos límite» («Franja fija del propio club»); `MotivosRechazoTests.HU4_esc3_una_franja_fija_aprobada_da_FRANJA_BLOQUEADA_CLUB` | La aclaración 2 fija que el representante del club dueño de la franja fija también recibe `FRANJA_BLOQUEADA_CLUB`. El comportamiento ya es ese (la regla de HU-01 no mira quién reserva), pero la prueba existente reserva con una ciudadana: el caso nuevo no está probado | Añadir una prueba con el representante del club dueño de la franja fija (sin cambiar código) |
| C3 | Coverage | LOW | spec.md RF-013, HU-1 esc. 4 (aclaración 3); `PagarReservaTests.HU5_esc4_…`, `MisReservasTests.HU5_esc4_…` | La aclaración 3 (las reservas de tarifa club son del representante) no tiene una prueba con «otro representante del mismo club»: la siembra tiene un representante por club. La regla general (otra persona → `NO_ENCONTRADO`) sí está probada | Aceptado sin tarea: probarlo exigiría añadir usuarios a la siembra (principio VIII); la propiedad es la del usuario, que ya cubren las pruebas citadas |
| I1 | Inconsistency | LOW | 000-core contracts/errores.md (`FRANJA_YA_EMPEZADA`: «Esa franja ya ha empezado y no se puede reservar.»); spec.md RF-009a, RF-020 | Al pagar, el texto del catálogo dice «no se puede reservar». Se entiende (la reserva no llega a completarse), y RF-020 exige el texto del catálogo | Aceptado: no se crea un código ni un texto nuevo (principio VII); se anota en la decisión de RF-009a |

Sin duplicaciones, sin términos vagos sin cuantificar y sin marcadores pendientes. Los seis hallazgos
del analyze inicial (I1–I4, C1, U1) no vuelven a aparecer.

**Coverage Summary Table** (tareas según `tasks.md`; pruebas según los ficheros de `tests/SI18.Api.Tests`):

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| RF-001 quién reserva | Sí | T017, T018, T039 | `CrearReservaTests.Sin_sesion_da_401`, `Personal_y_tecnico_reciben_403` |
| RF-002 menú | Sí | T041 | Manual |
| RF-003 «Reservar» y resumen | Sí | T013, T021, T022, T024 | `CrearReservaTests.HU1_esc1_…` |
| RF-004 volver a comprobar libre y precio | Sí | T014, T026, T036 | `MotivosRechazoTests` |
| RF-005 crear pendiente | Sí | T014, T018 | `CrearReservaTests.HU1_esc2_…` |
| RF-006 caducar al reservar | Sí | T026, T028 | `CrearReservaTests.HU2_esc4_…` |
| RF-007 concurrencia | Sí | T026, T029 | `ReservaConcurrenteTests` |
| RF-008 pagar sin guardar la tarjeta | Sí | T015, T048 | `PagarReservaTests.El_numero_de_tarjeta_no_aparece_en_ningun_registro` |
| RF-008a hora límite | Parcial | T025 | **C1**: falta «la más temprana entre creación + 10 min e inicio» |
| RF-009 caducada por tiempo | Sí | T027, T030 | `PagarReservaTests.HU2_esc3_…`, `Caducada_por_el_barrido_…` |
| RF-009a pagar con la franja empezada | **No** | — | **C1** (y D1) |
| RF-010 confirmar con código | Sí | T015, T019 | `PagarReservaTests.HU1_esc3_…` |
| RF-011 pago rechazado y no reintento | Parcial | T033, T034 | Lo existente está cubierto; la frase nueva remite a RF-009a (**C1**) |
| RF-012 tarjeta mal formada | Sí | T017, T034 | `PagarReservaTests.HU3_esc4_…` |
| RF-013 solo el titular | Sí | T015, T016, T039 | `HU5_esc4_…` (C3 para el caso de club) |
| RF-014 la pendiente no bloquea para siempre | Sí | T005, T006, T030 | `BarridoCaducidadesTests`, `ConsultaReservasTests` |
| RF-015 «Mis reservas» | Sí | T038, T039 | `MisReservasTests` |
| RF-015a orden | Sí | T038, T039 | `MisReservasTests.HU5_esc6_…` |
| RF-016 código solo en confirmadas | Sí | T023, T039 | `MisReservasTests.HU5_esc1_a_3_…` |
| RF-017 ir a pagar | Parcial | T040 | **C1**: no se oculta «Pagar» cuando la franja ya ha empezado |
| RF-018 consulta de reservas activas | Sí | T007, T008 | `ConsultaReservasTests` |
| RF-019 retirar la provisional | Sí | T009, T010 | `git grep` sin resultados (T010) |
| RF-019a operaciones para HU-03 y HU-07 | Sí | T044–T046 | `ServicioReservasTests` (base para trabajo futuro: HU-03 y HU-07 fuera del alcance) |
| RF-020 textos del catálogo | Sí | T032, T035, T037 | I1 |
| RF-021 WCAG 2.1 AA | Sí | T042, T043 | Auditoría en `docs/evidencias/auditorias/002-pantallas/` |
| RF-022 español, Madrid, euros | Sí | T024, T025, T040 | `FormatoEs` |
| CE-001 < 2 min | Sí | T049 | 58 s medidos (#39) |
| CE-002 concurrencia | Sí | T029 | `ReservaConcurrenteTests` |
| CE-003 10 minutos exactos | Sí | T028, T030 | |
| CE-004 confirmadas con código distinto | Sí | T019 | |
| CE-005 importe = precio de la búsqueda | Sí | T020 | `ReservarComoEnLaBusquedaTests` |
| CE-006 rechazos explicados | Sí | T036, T037 | |
| CE-007 retirada de la provisional | Sí | T008, T010 | |
| CE-008 teclado, Lighthouse y axe | Sí | T042, T043 | |

**Constitution Alignment Issues**: D1 (principio VII).

**Unmapped Tasks**: ninguna. T049–T052 son de cierre y T053–T054, del primer converge.

**Metrics**:

- Total Requirements: 34 (26 RF, contando RF-008a, RF-009a, RF-015a y RF-019a, y 8 CE)
- Total Tasks: 54 (todas cerradas)
- Coverage %: 97 % con al menos una tarea (33 de 34; RF-009a sin tarea), con 3 parciales (RF-008a, RF-011, RF-017)
- Ambiguity Count: 0
- Duplication Count: 0
- Critical Issues Count: 1 (D1)

## Next Actions

1. **D1**: proponer en 000-core la causa nueva de caducidad (RF-016, tabla de responsables y `Confirmar`),
   por PR revisado por Artur, antes de implementar.
2. **C1**: actualizar research.md E4 y el «Flujo» del plan con el paso nuevo, y ejecutar
   `/speckit-converge` para que convierta los criterios sin código en tareas.
3. **C2**: una prueba más (puede salir del mismo converge).
4. C3 e I1: aceptados con su justificación; sin cambios.

Este informe no modifica ningún fichero; las correcciones las decide Saul y se registran en
[analyze-registro.md](./analyze-registro.md).
