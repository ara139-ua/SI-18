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
