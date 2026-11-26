# Registro de `/speckit-analyze` y trazabilidad · 005-franjas-fijas

**Responsable**: Artur (módulo `Disponibilidad`). **Informes**: analyze inicial (2026-11-25, antes de implementar;
sus hallazgos están copiados en la tabla 1 del informe que se dio en la sesión de Claude Code, que no se guardó como fichero) y
[analyze-final.md](./analyze-final.md) (2026-11-26, después de implementar y converger). Converge:
[converge-2026-11-26.md](./converge-2026-11-26.md) (dos pasadas).

> Este registro sustituye a la reconstrucción que hizo Saul el 26/11/2026 (#51) a partir de los commits de correcciones, porque el informe original sí se ha recuperado. Los identificadores y las decisiones coinciden con los de esa reconstrucción; aquí se añaden la gravedad, la evidencia de cada hallazgo, la decisión sobre C1 y el analyze final. Los PR de las correcciones (#33) los fusionó Artur sin revisión registrada en GitHub.

## 1. Registro de hallazgos del analyze inicial (2026-11-25)

Estados: **Resuelto** (corregido en el commit indicado) · **Verificado** (el analyze final confirma que ya no
aparece).

| ID | Historia / criterio | Gravedad | Evidencia (analyze inicial) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|-----------------------------|----------|-------------|---------|--------|-------------------|
| T1 | T008 · `InicializadorDatosTests` (000-core) | ALTA | T008 insertaba una temporada terminada en la base compartida, y `InicializadorDatosTests` exige `Temporadas: 1` | No insertar nada: «temporada terminada» con el servicio y un reloj propio en julio de 2027 (`DatosDisponibilidad.ConServicioSolicitudes`) | Artur | Sin revisión registrada (#33) | Verificado (analyze final) | `13fc6dd` · tasks.md T006, T008 · `SolicitarFranjasFijasTests.Esc5_una_temporada_terminada_no_se_puede_elegir` |
| T2 | T012 y T019 (`RevisarFranjasFijasTests`) | MEDIA | El rechazo de T019 usaba la misma pista y día que la aprobada de T012, que seguiría bloqueando | Rechazar en otra pista y otro día de Este, escrito en la tarea | Artur | Sin revisión registrada (#33) | Verificado (analyze final) | `13fc6dd` · tasks.md T019 · `RevisarFranjasFijasTests.Rechazar_deja_las_franjas_libres_y_avisa_al_club` |
| U1 | «Casos límite» (solicitud de otro club) | MEDIA | El caso límite hablaba de `NO_ENCONTRADO` al ver una solicitud ajena, pero no hay endpoint de detalle | Caso límite reescrito: «un representante solo ve en su lista las de su club; no hay forma de consultarlas una a una» | Artur | Sin revisión registrada (#33) | Verificado (analyze final) | `13fc6dd` · spec.md «Casos límite» · `SolicitarFranjasFijasTests.Esc6_un_representante_no_ve_las_solicitudes_de_otro_club` |
| U2 | RF-013, CE-004 («cada representante») | BAJA | La siembra tiene un representante por club y no se pueden añadir usuarios (`Usuarios: 10`) | Comprobar que hay tantos avisos nuevos como representantes del club, leídos de la base | Artur | Sin revisión registrada (#33) | Verificado (analyze final) | `13fc6dd` · tasks.md T012 · `RevisarFranjasFijasTests.Aprobar_sin_conflictos_bloquea_las_franjas_de_la_temporada_y_avisa_al_club` |
| I1 | tasks.md «Datos de las pruebas» · T023 | BAJA | Las dos pruebas movidas de spec-001 usan Oeste, no Norte, contra la regla «un centro por clase» | Se conservan sus pistas de Oeste y la tarea explica por qué no chocan | Artur | Sin revisión registrada (#33) | Verificado (analyze final) | `13fc6dd` · tasks.md «Datos de las pruebas», T023 |
| C1 | Constitución, «Acuerdos de colaboración» | BAJA | Mismo punto que C1 de la spec 001: ramas `feature/…` frente a `spec-NNN` | Parche 1.2.1 de la constitución con la práctica real. Decidido por Artur y Saul el 26/11/2026 | Artur (propuesta), Saul (aprobación) | Saul (aprueba el PR de la 1.2.1) | Resuelto al fusionar `feature/constitucion-1.2.1` | `.specify/memory/constitution.md` 1.2.1 |

### Hallazgos nuevos del analyze final (2026-11-26)

| ID | Historia / criterio | Gravedad | Evidencia (analyze final) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| I1 | RF-016, RF-017 · contracts/modulos.md | MEDIA | `RepresentantesAfectados` no tiene consumidor y los cierres se leen con la provisional: HU-07 está fuera del alcance comprometido (constitución 1.2.0) | Nota de alcance: se conserva como base para trabajo futuro | Saul | Artur (revisa el cambio de Saul) | Resuelto (por Saul, #50) | contracts/modulos.md «Coordinación», nota de alcance |
| E1 | CE-001 | BAJA | Sin tarea que mida «menos de 2 minutos» | Se da por cumplido con el recorrido de T025 (solicitud completa con teclado en menos de un minuto) | Artur | — | Aceptado | tasks.md T025 |
| E2 | Historia 3, escenario 1 | BAJA | «Aprobar» desactivado no se comprobó en pantalla; la regla sí está probada en el servidor | Repetir la revisión con una solicitud con conflictos | Artur | — | Verificado (26/11/2026, grabación de la demo) | Demo de HU-05, escenario «La de Elena choca con una reserva confirmada…»: con la reserva del viernes en conflicto, «Aprobar» sale desactivado con el texto «No se puede aprobar: hay reservas o franjas fijas en conflicto.» y la solicitud se rechaza |
| U1 | Cabecera de spec.md | BAJA | «Estado: Borrador» | Cambiar a «Implementada» | Saul | Artur (revisa el cambio de Saul) | Resuelto (por Saul, #51) | spec.md «Estado: Implementada» |

### Hallazgo del converge

| ID | Origen | Gravedad | Evidencia | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|--------|----------|-----------|----------|-------------|---------|--------|-------------------|
| F1 | RF-008, CE-003 | MEDIA | Los conflictos empezaban en «ahora» aunque la temporada no hubiera empezado | Empezar en `max(ahora, inicio de la temporada)` | Artur | Sin revisión registrada (#34) | Verificado (converge, pasada 2; antes: 1 de 1 en rojo con el código anterior, después: en verde, [t030-antes-despues.md](../../docs/evidencias/t030-antes-despues.md)) | T030 · PR #34 · `ConflictosFranjasFijasTests.Antes_de_empezar_la_temporada_solo_cuentan_las_reservas_de_dentro_de_ella` |

## 2. Matriz de trazabilidad de HU-05

| Criterio | Qué exige (resumen) | Decisión del plan | Tareas | Prueba |
|----------|---------------------|-------------------|--------|--------|
| RF-001 | Solo el representante, a nombre de su club (de la sesión) | F1, F2 | T007, T008 | `SolicitarFranjasFijasTests`: `Esc1_la_solicitud_queda_pendiente_a_nombre_del_club_de_la_sesion`, `Solo_un_representante_puede_solicitar`, `Sin_sesion_da_401` |
| RF-002 | Pista o pabellón; días ISO sin repetir; horas en punto 08–22, fin > inicio | F2, F3 | T003, T005, T007, T008 | `SolicitarFranjasFijasTests`: `Esc3_una_sala_o_una_piscina_no_admiten_franjas_fijas`, `Esc4_un_dato_incorrecto_marca_su_campo` |
| RF-003 | Solo temporadas no terminadas | F2 | T007, T008 | `SolicitarFranjasFijasTests.Esc5_una_temporada_terminada_no_se_puede_elegir` (reloj propio) |
| RF-004 | Nueva = pendiente, sin bloqueo | F2 | T007, T008 | `SolicitarFranjasFijasTests.Esc2_una_solicitud_pendiente_no_bloquea_nada`; `ConsultaFranjasFijasTests.Una_solicitud_no_aprobada_no_bloquea_nada` |
| RF-005 | Error por campo en español | F2 | T007, T008, T009 | `SolicitarFranjasFijasTests.Esc4_un_dato_incorrecto_marca_su_campo`; manual (T025) |
| RF-006 | Lista del club, sin las de otros | F1 | T007, T008, T009 | `SolicitarFranjasFijasTests.Esc6_un_representante_no_ve_las_solicitudes_de_otro_club`, `Un_ciudadano_no_puede_ver_el_listado` |
| RF-007 | Solo el técnico resuelve; ve todas, pendientes primero | F1 | T011, T012, T013 | `RevisarFranjasFijasTests`: `El_tecnico_ve_primero_las_pendientes`, `Un_representante_no_puede_aprobar_y_un_id_inexistente_da_404` |
| RF-008 | Conflictos: reservas activas (sin datos personales) y aprobadas solapadas | F3, F4 | T015, T016, T017, T030 | `ConflictosFranjasFijasTests`: Esc1–Esc6, `Una_reserva_del_propio_club_tambien_es_un_conflicto`, `Antes_de_empezar_la_temporada_…` |
| RF-009 | No se aprueba con conflictos; se comprueba al aprobar | F4 | T011, T016 | `ConflictosFranjasFijasTests`: `Esc1_2_…_impide_aprobar`, `Esc5_si_se_solapa_con_otra_aprobada_no_se_puede_aprobar`, `Con_reservas_y_solapes_a_la_vez_el_error_es_el_de_las_reservas` |
| RF-010 | Sin conflictos se aprueba con fecha de resolución | F4 | T011, T012 | `RevisarFranjasFijasTests.Aprobar_sin_conflictos_bloquea_las_franjas_de_la_temporada_y_avisa_al_club` |
| RF-011 | Se rechaza tenga o no conflictos | F5 | T018, T019 | `RevisarFranjasFijasTests`: `Rechazar_deja_las_franjas_libres_y_avisa_al_club`, `Se_puede_rechazar_una_solicitud_con_conflictos` |
| RF-012 | Ya resuelta → `SOLICITUD_YA_RESUELTA` | F5 | T011, T018, T019 | `RevisarFranjasFijasTests.Una_solicitud_ya_resuelta_no_se_vuelve_a_resolver_ni_avisa_otra_vez` |
| RF-013 | Aviso a cada representante, guardado con el cambio de estado | F5 | T011, T012, T018, T019 | `RevisarFranjasFijasTests` (aprobar y rechazar; número de avisos = número de representantes) |
| RF-014 | Aprobada bloquea su temporada, salvo cierres | F3, F6 | T003, T021, T023 | `ConsultaFranjasFijasTests`: `Una_franja_fija_aprobada_bloquea_sus_dias_y_horas_dentro_de_la_temporada`, `Un_cierre_suspende_solo_las_franjas_fijas_que_cubre`; `ReglaFranjasFijasTests` |
| RF-015 | Consulta definitiva y retirada de la provisional | F6 | T021, T022, T023 | `ConsultaFranjasFijasTests.Con_franja_fija_y_cierre_la_busqueda_da_cerrada_y_bloqueada_como_antes`; pruebas de HU-01 en verde |
| RF-016 | `RepresentantesAfectados` para HU-07 | F6 | T021, T023 | `ConsultaFranjasFijasTests.Los_representantes_afectados_son_los_de_los_clubes_cuyas_franjas_fijas_cubre_el_cierre` |
| RF-017 | Leer reservas y cierres solo por sus interfaces | F4, F6 | T005, T021 | Revisión de código; `ServicioSolicitudesFranjasFijas` usa `IConsultaReservas` |
| RF-018 | WCAG 2.1 AA | F7, F8 | T009, T013, T017, T020, T024, T025 | Manual (T025, 26/11/2026): teclado, anuncios, estados en texto; Lighthouse ≥ 95 y axe sin graves |
| RF-019 | Español y hora de Madrid | F3, F5 | T005, T009, T013 | `ReglaFranjasFijasTests.El_dia_del_cambio_de_hora_las_franjas_siguen_en_hora_de_Madrid`; texto del aviso («martes y jueves») |
| CE-001 | Solicitud en < 2 min | — | T025 | Manual (E1 del analyze final) |
| CE-002 | Aprobada no sale en la búsqueda ni se reserva; pendiente o rechazada no bloquea | F6 | T012, T019, T023 | `RevisarFranjasFijasTests`; `ConsultaFranjasFijasTests`; `MotivosRechazoTests` (spec-002, `FRANJA_BLOQUEADA_CLUB`) |
| CE-003 | Ningún conflicto omitido ni de más | F3, F4 | T016, T030 | `ConflictosFranjasFijasTests` (`Categoria=Limites`) |
| CE-004 | Todos los representantes reciben aviso; si falla, no cambia el estado | F5 | T012, T019 | `RevisarFranjasFijasTests`; un único `SaveChanges` (revisión de código) |
| CE-005 | Búsqueda igual con la definitiva que con la provisional | F6 | T022, T023 | `ConsultaFranjasFijasTests` (pruebas movidas de spec-001); todas las de HU-01 en verde |
| CE-006 | Teclado, Lighthouse ≥ 95, axe sin graves | F7, F8 | T024, T025 | Manual (T025, 26/11/2026) |

## 3. Revisión entre compañeros (enunciado 5.2)

| Criterio revisado | Revisor | Fecha | Qué comprobó | Resultado |
|-------------------|---------|-------|--------------|-----------|
| RF-009 (una solicitud con conflictos no se aprueba y sigue pendiente; los conflictos se vuelven a comprobar al aprobar) | Saul | 26/11/2026 | Las pruebas `ConflictosFranjasFijasTests.Esc1_2_una_reserva_confirmada_sale_sin_datos_personales_e_impide_aprobar` (409 `SOLICITUD_CON_CONFLICTOS` y sigue `Pendiente`), `Esc5_si_se_solapa_con_otra_aprobada_no_se_puede_aprobar` y `Con_reservas_y_solapes_a_la_vez_el_error_es_el_de_las_reservas`, y que `ServicioSolicitudesFranjasFijas.Aprobar` (`ServicioSolicitudesFranjasFijas.cs:141–157`) vuelve a llamar a `Conflictos()` antes de aprobar, leyendo las reservas por `IConsultaReservas` dentro de la temporada | ✅ Cubierto; sin incidencias |
