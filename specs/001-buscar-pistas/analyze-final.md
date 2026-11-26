# `/speckit-analyze` final · 001-buscar-pistas

**Fecha**: 2026-11-26 · **Ejecutado por**: Artur (con Claude Code) · **Rama**: `feature/001-005-evidencias`
sobre `develop` en `ef5e9be` · **Constitución**: 1.2.0 (y la propuesta 1.2.1 en `feature/constitucion-1.2.1`)

**Momento**: después de implementar la spec (PR #23 y #24), de su converge (Fase 8, T026–T027) y del cierre de
las tareas T021–T025. El analyze inicial se pasó el 2026-11-25, antes de implementar; sus hallazgos y lo que se
decidió con cada uno están en [analyze-registro.md](./analyze-registro.md).

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` frente a la constitución.

## Informe de análisis de la especificación

| ID | Categoría | Gravedad | Ubicación | Resumen | Recomendación |
|----|-----------|----------|-----------|---------|---------------|
| C1 | Constitución / colaboración | MEDIA | constitución 1.2.0, «Acuerdos de colaboración» · rama `feature/001-buscar-pistas` | Sigue del analyze inicial: la constitución pide ramas `spec-001` … `spec-008` y el grupo usa `feature/NNN-…`. | Decidido en grupo: parche 1.2.1 de la constitución (rama `feature/constitucion-1.2.1`). Se cierra al fusionarlo. |
| I1 | Incoherencia | MEDIA | spec.md RF-014a y «Supuestos» · contracts/modulos.md · `Provisional/ConsultaCierresProvisional.cs` (comentario) | La spec y el código dicen que `ConsultaCierresProvisional` la sustituirá spec-007 (C), pero desde la constitución 1.2.0 HU-07 queda fuera del alcance comprometido: la provisional pasa a ser la implementación efectiva de `IConsultaCierres`. | Añadir una nota de alcance en «Supuestos» de spec.md y en el comentario de la clase (se conserva como base para trabajo futuro, como en las demás specs). |
| I2 | Incoherencia | BAJA | plan.md «Código fuente» · tasks.md T003, T005 | El plan lista `ConsultaReservasProvisional` y `ConsultaFranjasFijasProvisional`, que ya no existen: se retiraron en el #31 (spec-002) y en el #33 (spec-005), como preveía RF-014a. | Nota en plan.md con la retirada; las tareas hechas no se reescriben. |
| I3 | Incoherencia | BAJA | plan.md «Comprobación de la constitución» (Colaboración) y «Dependencias y riesgos» (D6) | El plan dice que la convención de `Program.cs` se propone en el PR de HU-01 y la revisa Saul (T072); en realidad entró antes, en el #18 de 000-core (hallazgo G1 del analyze inicial). tasks.md T024 ya lo anota. | Nota en plan.md que remita al #18. |
| E1 | Cobertura | BAJA | spec.md CE-001 | CE-001 («encontrar una franja en menos de 1 minuto») no tiene ninguna tarea que lo mida; T022 mide CE-002 (< 2 s) y T021 el recorrido con teclado. | Anotar la medición con cronómetro en la revisión manual (quickstart §5) o dejar constancia de que se da por cumplido con el recorrido de T021. |
| U1 | Infraespecificación | BAJA | spec.md, cabecera | La spec sigue en «Estado: Borrador» aunque está implementada y fusionada. | Cambiar a «Implementada». |

**Tabla de cobertura**

| Requisito | ¿Tiene tarea? | Tareas | Notas |
|-----------|---------------|--------|-------|
| RF-001 | Sí | T010, T011 | 401 sin sesión, 403 con otros roles |
| RF-002 | Sí | T013 | Menú solo para ciudadano y representante |
| RF-003 | Sí | T010, T012 | Tres filtros obligatorios |
| RF-004 | Sí | T012 | Filtros desde los catálogos de 000-core |
| RF-005 | Sí | T010, T011, T012 | `FUERA_DE_ANTELACION`, límites de 7 días |
| RF-006 | Sí | T009, T011 | Solo pistas del deporte |
| RF-007 | Sí | T008, T009, T011 | Libres según RF-011 de 000-core |
| RF-008 | Sí | T009, T017 | Horas de Madrid y precio en euros |
| RF-009 | Sí | T009, T011 | Orden por pista y hora |
| RF-010 | Sí | T009, T011 | Pistas sin franjas omitidas |
| RF-011 | Sí | T018, T019 | Dos mensajes distintos |
| RF-012 | Sí | T014, T015 | Mismo cálculo que HU-02 |
| RF-013 | Sí | T015, T016, T027 | `IConsultaDisponibilidad`; T027 de converge |
| RF-014 | Sí | T003–T006, T008 | Lectura solo por las interfaces |
| RF-014a | Sí | T003–T007 | Dos de las tres provisionales ya retiradas (I2) |
| RF-015 | Sí | T012, T019, T020, T021, T026 | WCAG 2.1 AA; T026 de converge |
| RF-016 | Sí | T012, T017, T020 | Español, Madrid, euros |
| CE-001 | **No** | — | E1 |
| CE-002 | Sí | T022 | 30–44 ms en 10 peticiones |
| CE-003 | Sí | T011, T018 | Pruebas de límites |
| CE-004 | Sí | T016 | `ConsultaDisponibilidadTests` |
| CE-005 | Sí | T014 | `PrecioPorTarifaTests` |
| CE-006 | Sí | T020, T021 | Lighthouse 100 y axe sin graves |
| CE-007 | Sí | T018, T019 | |

**Problemas de alineación con la constitución**: C1 (ramas), con decisión tomada. Ninguna infracción de los
principios I–IX: las reglas están en el backend (III), solo se usan códigos del catálogo (IV), no hay entidades
ni valores nuevos fuera de 000-core (VII) y las provisionales están justificadas por RF-014a (VIII).

**Tareas sin requisito**: ninguna. T001–T002 (entrada del módulo y DTO) y T022–T025 (cierre) son de soporte.

**Métricas**

- Requisitos: 24 (17 RF y 7 CE)
- Tareas: 27 (25 iniciales y 2 de converge), todas hechas
- Cobertura: 23 de 24 (96 %)
- Ambigüedades: 0
- Duplicados: 0
- Problemas críticos: 0

## Siguientes pasos

No hay problemas críticos ni altos. Los seis hallazgos son de documentación y ninguno cambia el código:

- C1 se cierra al fusionar el parche 1.2.1 de la constitución.
- I1, I2, I3, E1 y U1 se pueden corregir a mano en spec.md y plan.md (notas, sin reescribir tareas hechas).
