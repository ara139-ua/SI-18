# Informe de `/speckit-analyze` · 006-validar-qr · 2026-11-26 (tercer informe, sobre la versión funcional)

Pasada de solo lectura sobre `spec.md`, `plan.md` y `tasks.md` (con `research.md`, `contracts/`,
`quickstart.md` y las pruebas como apoyo) contra la constitución **1.2.1**, después de la pasada de clarify
sobre la versión funcional (spec.md, sesión «2026-11-26 (sobre la versión funcional)»;
[clarify-funcional.md](./clarify-funcional.md)) y de los dos converge del 26/11/2026. Ejecutado por Saul
en la rama `feature/refinamiento-002-006`. Los informes anteriores son [analyze-1.md](./analyze-1.md)
(inicial) y [analyze-2.md](./analyze-2.md) (después de sus ajustes); su registro, en
[analyze-registro.md](./analyze-registro.md).

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| G1 | Coverage | MEDIUM | spec.md RF-004 (frase nueva) y «Casos límite» («Mayúsculas y minúsculas»); tasks.md (ninguna tarea); `ValidarAccesoTests` | La aclaración 2 fija que el código se compara distinguiendo mayúsculas. El código ya compara exacto (`ServicioValidarAcceso.cs:31`) y se comprobó a mano contra la API (clarify-funcional.md), pero no hay ninguna prueba automática: la constitución (II) pide prueba para cada regla | Añadir una prueba: el código de la reserva con una letra en otra caja → 422 `QR_DESCONOCIDO`, sin datos de la reserva, registrada con `reserva_id` nulo. Es trabajo de código: lo convierte en tarea `/speckit-converge` |
| G2 | Inconsistency | LOW | research.md V10 (L175); spec.md HU-1 esc. 6; `TextosReservas.AvisoValidacionAnterior` (T032) | Sigue abierto F7 del segundo converge: tras T032 el aviso tiene dos textos («…ya se validó hoy a las HH:mm» y «…se validó por última vez el dd/MM/yyyy a las HH:mm»), pero research.md V10 solo recoge el primero | Documentar el segundo texto en research.md V10 |
| P1 | Constitution (proceso) | HIGH | Constitución, «Flujo de trabajo» («quien revisa DEBE confirmar…»); plan.md C1 y C2; F1 de los dos converge | La revisión del código común de C1 y C2 (#46) por @ara139-ua sigue pendiente. Artur ha hecho la revisión cruzada de RF-002 (analyze-registro.md §3), que no sustituye a la de C1 y C2 | Sin cambio en los documentos: sigue pedida en el #43 |

La aclaración 1 (franjas fijas de club fuera de alcance) no requiere tareas: solo amplía «Fuera de
alcance». La nota sobre el titular de las reservas de tarifa club (spec-002) no cambia nada en HU-06.
Sin duplicaciones, sin términos vagos sin cuantificar y sin marcadores pendientes. Los hallazgos de
analyze-1 y analyze-2 (D1, C1, C2, U1, U2, A1, A2, F1, F2, U3, E1 y L1) no vuelven a aparecer.

**Coverage Summary Table** (solo cambios respecto a la matriz de analyze-registro.md §2):

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| RF-004 (mayúsculas) | No | — | **G1** |
| «Fuera de alcance» (franjas fijas de club) | No aplica | — | Sin trabajo |
| Resto (RF-001–RF-017, RF-006a, CE-001–CE-007) | Sí | Según la matriz de §2 | Sin cambios |

**Constitution Alignment Issues**: P1 (proceso de revisión, no de los documentos).

**Unmapped Tasks**: ninguna.

**Metrics**:

- Total Requirements: 25 (18 RF, contando RF-006a, y 7 CE)
- Total Tasks: 33 (T001–T033; T005 y T021 retiradas en su día con su número)
- Coverage %: 100 % de los requisitos con tarea; el criterio nuevo de RF-004 sin prueba (G1)
- Ambiguity Count: 0
- Duplication Count: 0
- Critical Issues Count: 0

## Next Actions

1. `/speckit-converge` para que convierta G1 y G2 en tareas de la fase 10.
2. P1: sigue pendiente de @ara139-ua (#43).
