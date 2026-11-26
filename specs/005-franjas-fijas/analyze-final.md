# `/speckit-analyze` final · 005-franjas-fijas

**Fecha**: 2026-11-26 · **Ejecutado por**: Artur (con Claude Code) · **Rama**: `feature/001-005-evidencias`
sobre `develop` en `ef5e9be` · **Constitución**: 1.2.0 (y la propuesta 1.2.1 en `feature/constitucion-1.2.1`)

**Momento**: después de implementar la spec (PR #33), de su converge (Fase 10, T030, PR #34) y del cierre de
las tareas T025, T027 y T028. El analyze inicial se pasó el 2026-11-25, antes de implementar; sus hallazgos y lo
que se decidió con cada uno están en [analyze-registro.md](./analyze-registro.md).

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` frente a la constitución.

## Informe de análisis de la especificación

| ID | Categoría | Gravedad | Ubicación | Resumen | Recomendación |
|----|-----------|----------|-----------|---------|---------------|
| C1 | Constitución / colaboración | BAJA | constitución 1.2.0, «Acuerdos de colaboración» · rama `feature/005-franjas-fijas` | Sigue del analyze inicial: la constitución pide ramas `spec-001` … `spec-008` y el grupo usa `feature/NNN-…`. | Decidido en grupo: parche 1.2.1 de la constitución (rama `feature/constitucion-1.2.1`). Se cierra al fusionarlo. |
| I1 | Incoherencia | MEDIA | spec.md RF-016, RF-017, «Supuestos» (dependencias) · contracts/modulos.md | La spec da por hecho que HU-07 (C) consumirá `RepresentantesAfectados` y publicará `IConsultaCierres`. Desde la constitución 1.2.0, HU-07 queda fuera del alcance comprometido: `RepresentantesAfectados` está hecha y probada pero no tiene consumidor, y los cierres se siguen leyendo con la provisional de spec-001. | Añadir una nota de alcance en «Supuestos» y en contracts/modulos.md: se conserva como base para trabajo futuro, como en las demás specs. |
| E1 | Cobertura | BAJA | spec.md CE-001 | CE-001 («enviar una solicitud en menos de 2 minutos») no tiene ninguna tarea que lo mida. | Anotar la medición con cronómetro en la revisión manual o dejar constancia de que se da por cumplido con el recorrido de T025. |
| E2 | Cobertura | BAJA | spec.md historia 3, escenario 1 · tasks.md T017, T025 | El botón «Aprobar» desactivado con su explicación no se ha comprobado en pantalla (T025 lo deja anotado como «sin probar»). La regla sí está probada en el servidor (T016, `ConflictosFranjasFijasTests`). | Repetir la revisión de `/franjas-fijas/revision` con una solicitud con conflictos. |
| U1 | Infraespecificación | BAJA | spec.md, cabecera | La spec sigue en «Estado: Borrador» aunque está implementada y fusionada. | Cambiar a «Implementada». |

Hallazgos que ya **no** aparecen respecto al analyze inicial y al converge:

- La dependencia de HU-02 para el rechazo al reservar (historia 2, escenario 3; CE-002) está cubierta:
  `ReglaDisponibilidad` devuelve `FRANJA_BLOQUEADA_CLUB` y lo comprueban `RevisarFranjasFijasTests` (spec-005) y
  `MotivosRechazoTests` (spec-002).
- El rango de conflictos antes del inicio de la temporada (F1 del converge) se corrigió en T030.

**Tabla de cobertura**

| Requisito | ¿Tiene tarea? | Tareas | Notas |
|-----------|---------------|--------|-------|
| RF-001 | Sí | T007, T008 | El club sale de la sesión |
| RF-002 | Sí | T003, T005, T007, T008 | Validación previa al constructor |
| RF-003 | Sí | T007, T008 | Temporadas no terminadas |
| RF-004 | Sí | T007, T008 | Pendiente sin bloqueo |
| RF-005 | Sí | T007, T008, T009 | Error por campo |
| RF-006 | Sí | T007, T008, T009 | Solo las del club |
| RF-007 | Sí | T011, T012, T013 | Pendientes primero |
| RF-008 | Sí | T015, T016, T017, T030 | Sin datos personales; T030 de converge |
| RF-009 | Sí | T011, T016 | Se vuelven a comprobar al aprobar |
| RF-010 | Sí | T011, T012 | |
| RF-011 | Sí | T018, T019 | Con o sin conflictos |
| RF-012 | Sí | T011, T018, T019 | `SOLICITUD_YA_RESUELTA` |
| RF-013 | Sí | T011, T012, T018, T019 | Aviso en el mismo `SaveChanges` |
| RF-014 | Sí | T003, T021, T023 | Sin las franjas de los cierres |
| RF-015 | Sí | T021, T022, T023 | Provisional de spec-001 retirada |
| RF-016 | Sí | T021, T023 | Sin consumidor por ahora (I1) |
| RF-017 | Sí | T005, T021 | Solo por las interfaces |
| RF-018 | Sí | T009, T013, T017, T020, T024, T025 | WCAG 2.1 AA |
| RF-019 | Sí | T005, T009, T013 | «martes y jueves», hora de Madrid |
| CE-001 | **No** | — | E1 |
| CE-002 | Sí | T012, T019, T023 | También `MotivosRechazoTests` de spec-002 |
| CE-003 | Sí | T016 | Ninguno omitido, ninguno de más |
| CE-004 | Sí | T012, T019 | Todos los representantes |
| CE-005 | Sí | T022, T023 | Búsqueda igual que con la provisional |
| CE-006 | Sí | T024, T025 | Lighthouse ≥ 95 y axe sin graves |

**Problemas de alineación con la constitución**: C1 (ramas), con decisión tomada. Ninguna infracción de los
principios I–IX: sin entidades, estados ni columnas nuevas (VII); los tres códigos de error se propusieron
antes en 000-core (#32, IV); las reglas y los conflictos se calculan en el servidor (III); el riesgo de
concurrencia aceptado en research.md F4 está justificado (VIII).

**Tareas sin requisito**: ninguna. T001–T002 (contratos), T006 (ayudas de prueba) y T026–T029 (cierre) son de
soporte.

**Métricas**

- Requisitos: 25 (19 RF y 6 CE)
- Tareas: 30 (29 iniciales y 1 de converge), todas hechas
- Cobertura: 24 de 25 (96 %)
- Ambigüedades: 0
- Duplicados: 0
- Problemas críticos: 0

## Siguientes pasos

No hay problemas críticos ni altos. Los cinco hallazgos son de documentación o de comprobación manual:

- C1 se cierra al fusionar el parche 1.2.1 de la constitución.
- I1, E1 y U1 se pueden corregir a mano en spec.md y contracts/modulos.md.
- E2 pide repetir una comprobación manual; no cambia el código.
