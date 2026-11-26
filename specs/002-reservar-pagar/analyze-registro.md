# Registro de `/speckit-analyze` · 002-reservar-pagar (reconstrucción)

> **Reconstrucción** hecha por Saul el 26/11/2026 a partir del commit `0ef218c` y del PR #30. **El
> informe original de `/speckit-analyze` no se conservó.** Este registro solo recoge los
> identificadores y las decisiones que constan en esas fuentes; lo que no consta se indica así. El
> informe final de analyze sobre la versión actual está en `analyze-final-2026-11-26.md`.

**Responsable de la spec**: Saul. **Fecha de la pasada**: 25/11/2026.

Ni el commit ni el PR indican la gravedad de los hallazgos.

| ID | Historia / criterio | Gravedad | Evidencia (lo que consta) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| I1 | HU-4 (motivos de rechazo) | No consta | `FRANJA_BLOQUEADA_CLUB` no tenía el estado del catálogo | Es 422, como en el catálogo de 000-core | Saul | Sin revisión registrada en GitHub (#30 fusionado por Saul) | Resuelto | tasks.md T036 |
| I2 | Concurrencia (`ReservaConcurrenteTests`) | No consta | La siembra no tiene 20 personas que puedan reservar | Las 20 peticiones simultáneas se reparten entre las sesiones sembradas | Saul | Ídem | Resuelto | research.md E13 |
| I3 | Plan | No consta | El plan no listaba todas las pruebas | plan.md lista `MotivosRechazoTests` y el caso nuevo de `BarridoCaducidadesTests` | Saul | Ídem | Resuelto | plan.md |
| I4 | Dependencias entre historias de la spec | No consta | T031 (US2) dependía de una tarea de US3 | `Confirmada → TRANSICION_NO_PERMITIDA` pasa a T027 | Saul | Ídem | Resuelto | tasks.md T027, T031 |
| C1 | Retirada de la provisional (D5 de spec-001) | No consta | `DatosDisponibilidad.ConServicio` (de Artur) construía la provisional | La retirada también toca `DatosDisponibilidad.cs`; se añade a plan, research y contracts | Saul | Ídem | Resuelto | plan.md, research.md, contracts/modulos.md |
| U1 | CE-001 | No consta | Faltaba cronometrar el recorrido de reserva y pago | T049 lo cronometra | Saul | Ídem | Resuelto | tasks.md T049 |

## Lo que no consta

- El texto del informe, la gravedad de cada hallazgo, la tabla de cobertura y las métricas.
- Si hubo hallazgos que no se corrigieron.

## Informe final (26/11/2026)

Registro de los hallazgos de [analyze-final-2026-11-26.md](./analyze-final-2026-11-26.md), que es una
pasada real sobre la versión actual (no una reconstrucción), hecha después de la pasada de clarify sobre la
versión funcional ([clarify-funcional.md](./clarify-funcional.md)).

| ID | Historia / criterio | Gravedad | Evidencia (analyze final) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| D1 | RF-009a; 000-core RF-016 y `Confirmar` | CRITICAL | La aclaración 1 añade una causa de caducidad que 000-core no tiene (principio VII) | Proponer primero el cambio en 000-core, por PR revisado por Artur | Saul | Artur (revisión pedida en el PR) | Resuelto: T055 (`7cd5517`) | 000-core spec.md RF-016 y data-model.md § Reserva |
| C1 | RF-009a, RF-008a, RF-011, RF-017, HU-2 esc. 6, HU-5 esc. 3 | HIGH | Criterios nuevos sin tarea ni prueba; research.md E4 sin el paso nuevo | Convertirlo en tareas con `/speckit-converge` y documentar el paso | Saul | Artur (revisión pedida en el PR) | Resuelto: T056 (`51dd22c`), T057 (`01127dd`), T058 (`b7b8a14`; comprobación manual de accesibilidad pendiente) y T060 | converge-2026-11-26-2.md F2–F4, F6 |
| C2 | HU-4 esc. 3 (representante del club dueño) | MEDIUM | La prueba existente usa una ciudadana | Añadir la prueba (el código ya lo cumple) | Saul | Artur (revisión pedida en el PR) | Resuelto: T059 (`84a67ee`) | converge-2026-11-26-2.md F5 |
| C3 | RF-013 (reservas de tarifa club) | LOW | Sin prueba con dos representantes del mismo club | Aceptado: exigiría añadir usuarios a la siembra (principio VIII); la propiedad por usuario ya está probada | Saul | — | Aceptado (justificado) | — |
| I1 | RF-009a, RF-020 | LOW | El texto de `FRANJA_YA_EMPEZADA` dice «no se puede reservar» | Aceptado: no se crea un código ni un texto nuevo (principio VII) | Saul | — | Aceptado (justificado) | — |

## Revisión entre compañeros (enunciado 5.2)

| Criterio revisado | Revisor | Fecha | Qué comprobó | Resultado |
|-------------------|---------|-------|--------------|-----------|
| CE-005 (el importe cobrado coincide con el precio de la búsqueda) | Artur | 26/11/2026 | La prueba `ReservarComoEnLaBusquedaTests.El_precio_de_la_busqueda_es_el_del_resumen_el_de_la_reserva_y_el_del_pago` y que `ServicioReservar` usa `disponibilidad.Precio` (`ServicioReservar.cs:42` y `:65`) sin recalcular el precio | ✅ Cubierto; sin incidencias |

## Matriz de trazabilidad de HU-02

Decisiones del plan: research.md E1–E14. Las pruebas son de `fuentes/tests/SI18.Api.Tests` salvo que se
diga otra cosa. Los criterios de la aclaración del 26/11/2026 se cubren con la fase 12 de tasks.md
(T055–T060).

| Criterio | Qué exige (resumen) | Decisión del plan | Tareas | Prueba |
|----------|---------------------|-------------------|--------|--------|
| RF-001 | Solo ciudadano y representante; sin sesión, 401 | E1, E9 | T017, T018, T039 | `CrearReservaTests.Sin_sesion_da_401`, `Personal_y_tecnico_reciben_403`; `MisReservasTests.El_personal_de_centro_recibe_403` |
| RF-002 | «Mis reservas» en el menú solo para quien puede usarla | E11 | T041 | Manual (quickstart §4 paso 8) |
| RF-003 | «Reservar» en cada franja y resumen | E2, E10, E11 | T013, T021, T022, T024 | `CrearReservaTests.HU1_esc1_el_resumen_de_una_franja_libre_trae_pista_centro_fecha_horas_e_importe` |
| RF-004 | Volver a comprobar libre y precio con la regla de HU-01 | E3 | T014, T026, T036 | `MotivosRechazoTests` (HU4 esc. 1–5) |
| RF-005 | Crear pendiente con tarifa e importe | E3 | T014, T018 | `CrearReservaTests.HU1_esc2_…` |
| RF-006 | Caducar la pendiente no vigente al reservar | E3 | T026, T028 | `CrearReservaTests.HU2_esc4_una_pendiente_ajena_de_10_minutos_exactos_se_caduca_al_reservar` y `…_9_minutos_59_segundos_sigue_bloqueando` |
| RF-007 | Dos peticiones simultáneas: una sola activa | E3, E5 | T026, T029 | `ReservaConcurrenteTests.De_20_peticiones_simultaneas_…`; `CrearReservaTests.Doble_envio_de_la_misma_reserva_crea_una_sola` |
| RF-008 | Pagar sin guardar la tarjeta | E4 | T015, T048 | `PagarReservaTests.El_numero_de_tarjeta_no_aparece_en_ningun_registro` |
| RF-008a | Hora límite fija (la más temprana entre creación + 10 min e inicio) | E4, E11 | T025, T057 | `CrearReservaTests.RF008a_creada_5_min_antes_del_inicio_la_hora_limite_es_la_del_inicio` y `RF008a_creada_con_mas_antelacion_la_hora_limite_es_la_creacion_mas_10_minutos` |
| RF-009 | Caducada por tiempo → `RESERVA_CADUCADA` sin cobro | E4 | T027, T030 | `PagarReservaTests.HU2_esc3_a_los_10_minutos_exactos_caduca_sin_cobrar`, `A_los_9_59_se_cobra_y_queda_confirmada`, `Caducada_por_el_barrido_…` |
| RF-009a | Pagar con la franja empezada → `FRANJA_YA_EMPEZADA`, caducada, sin cobro | E4 paso 4b | T055, T056, T060 | `PagarReservaTests.RF009a_…` (4 casos) y `ReservaTests.Confirmar_en_el_inicio_de_la_franja_…`, `Confirmar_1_s_antes_del_inicio_confirma` (rojo antes y verde después: clarify-funcional.md §2) |
| RF-010 | Confirmar con código | E4 | T015, T019 | `PagarReservaTests.HU1_esc3_…` |
| RF-011 | Rechazo sin reintento | E4 | T033, T034 | `PagarReservaTests.HU3_esc1_…`, `HU3_esc2_…`, `Pagar_dos_veces_es_TRANSICION_NO_PERMITIDA_y_conserva_el_codigo` |
| RF-012 | Tarjeta mal formada → 400 sin cambiar nada | E4 | T017, T034 | `PagarReservaTests.HU3_esc4_una_tarjeta_con_formato_no_valido_da_400_y_no_cambia_nada` |
| RF-013 | Solo el titular (en las de club, el representante) | E6 | T015, T016, T039 | `PagarReservaTests.HU5_esc4_…`; `MisReservasTests.HU5_esc4_…` (C3) |
| RF-014 | La pendiente deja de bloquear a los 10 min | E3, E7 | T005, T006, T030 | `BarridoCaducidadesTests`; `ConsultaReservasTests.Una_pendiente_bloquea_hasta_los_10_minutos_y_no_despues` |
| RF-015 | Lista de todas las reservas con estado efectivo | E6 | T038, T039 | `MisReservasTests.HU5_esc5_…` |
| RF-015a | Orden de «Mis reservas» | E6 | T038, T039 | `MisReservasTests.HU5_esc6_…` |
| RF-016 | Código solo en confirmadas | E6, E11 | T023, T039 | `MisReservasTests.HU5_esc1_a_3_…` |
| RF-017 | Ir a pagar mientras esté vigente y no haya empezado | E11 | T040, T058 | Manual (quickstart §4 paso 7; comprobación de T058 pendiente) |
| RF-018 | Consulta de reservas activas para HU-01 | E7 | T007, T008 | `ConsultaReservasTests` |
| RF-019 | Retirar la provisional sin cambiar la búsqueda | E7 | T009, T010 | `git grep` sin resultados (T010); pruebas de HU-01 en verde |
| RF-019a | Operaciones para HU-03 y HU-07 (base para trabajo futuro) | E8 | T044–T046 | `ServicioReservasTests` |
| RF-020 | Textos del catálogo | E11 | T032, T035, T037 | Manual; `CatalogoErroresTests` (000-core) |
| RF-021 | WCAG 2.1 AA | E11 | T042, T043 | Lighthouse 100 y axe sin infracciones (`docs/evidencias/auditorias/002-pantallas/`) |
| RF-022 | Español, Madrid, euros | E11, E12 | T024, T025, T040 | `MotivosRechazoTests` (cambios de hora); `FormatoEs` |
| CE-001 | Reservar y pagar en < 2 min | E14 | T049 | 58 s cronometrados (#39) |
| CE-002 | Simultáneas: como mucho una activa | E5, E13 | T029 | `ReservaConcurrenteTests` |
| CE-003 | Dejan de bloquear a los 10 min exactos | E3 | T028, T030 | `PagarReservaTests.HU2_esc3_…`; `CrearReservaTests.HU2_esc4_…` |
| CE-004 | Confirmadas con pago y código distinto | E4 | T019 | `PagarReservaTests.HU1_esc3_…` |
| CE-005 | Importe cobrado = precio de la búsqueda | E2, E3 | T020 | `ReservarComoEnLaBusquedaTests.El_precio_de_la_busqueda_es_el_del_resumen_el_de_la_reserva_y_el_del_pago` (revisado por Artur ✅) |
| CE-006 | Rechazos explicados | E11 | T036, T037 | `MotivosRechazoTests`; manual |
| CE-007 | Retirada de la provisional sin romper HU-01 | E7 | T008, T010 | `ConsultaReservasTests`; pruebas de HU-01 |
| CE-008 | Teclado, Lighthouse ≥ 95 y axe | E11 | T042, T043 | Auditoría de 002 (Lighthouse 100) |
| HU-4 esc. 3 (club propio) | El representante del club dueño tampoco reserva su franja fija | — | T059 | `MotivosRechazoTests.HU4_esc3_el_representante_del_club_duenio_de_la_franja_fija_tampoco_puede_reservarla` |
