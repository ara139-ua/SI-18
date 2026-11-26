# Informe de `/speckit-converge` (pasada final) · 002-reservar-pagar · 2026-11-26

**Rama**: `feature/refinamiento-002-006`. **Ejecutado por**: Saul. **Parte de**:
[converge-2026-11-26-2.md](./converge-2026-11-26-2.md) (que generó T055–T060, implementadas en el #54),
[clarify-funcional.md](./clarify-funcional.md) y el [analyze final](./analyze-final-2026-11-26.md).
`FEATURE_DIR` fijado a mano. No hay `.specify/extensions.yml`: sin hooks.

**Resultado**: `tasks_appended`. Hay 1 hallazgo. Saul aprobó la tarea y se escribió en `tasks.md`: la tarea
T061 (Phase 13: Convergence).

## Convergence Findings

| ID | Gap Type | Severity | Source | Evidencia | Trabajo pendiente |
|----|----------|----------|--------|-----------|-------------------|
| F1 | partial | LOW | RF-009a; plan: contrato; T060 | `contracts/api-reservas.openapi.yaml`, operación `/api/reservas/{id}/pago`: el `422` solo describe `PAGO_RECHAZADO` y el `409` describe `TRANSICION_NO_PERMITIDA` sin la caducada por `FRANJA_YA_EMPEZADA`. El código (`ServicioReservar.Pagar`, `ManejadorErrores.cs:31`) responde 422 `FRANJA_YA_EMPEZADA`, y T060 documentó research, plan y quickstart pero no el contrato | Añadir `FRANJA_YA_EMPEZADA` al `422` de `/pago`, ampliar `TRANSICION_NO_PERMITIDA` y el texto de la operación |

## Tarea añadida a tasks.md

```markdown
## Phase 13: Convergence

- [ ] T061 [Saul · revisa Artur] En `contracts/api-reservas.openapi.yaml`, operación `/api/reservas/{id}/pago`: añadir 422 `FRANJA_YA_EMPEZADA` (vigente pero con la franja ya empezada: queda caducada y el pago fallido, sin cobro), incluir «o por una franja ya empezada» en `TRANSICION_NO_PERMITIDA` y en la descripción de la operación; solo documentación per RF-009a, T060 (partial)
```

## Revisado y conforme

- RF-001 a RF-022 y RF-028 (con RF-008a, RF-009a, RF-014a, RF-015a y RF-019a), HU-1 a HU-6 y los casos límite.
- RF-008a: `ServicioReservar.ADto` toma el más temprano entre creación + 10 min e inicio; pruebas `CrearReservaTests.RF008a_…` (2).
- RF-009a: `Reserva.YaEmpezada` y `Confirmar`; `Pagar` caduca y responde `FRANJA_YA_EMPEZADA` sin pasarela; pruebas en `PagarReservaTests` (`RF009a_…`) y `ReservaTests`.
- RF-017: `MisReservas.razor` no ofrece «Pagar» pasado el límite; `PagarReserva.razor` trata `FRANJA_YA_EMPEZADA` como resultado final.
- HU-4 esc. 3 con el representante del club dueño: `MotivosRechazoTests` (T059).
- research E4, «Flujo» del plan y quickstart R19 (T060).
- Constitución 1.2.1, principios I–IX.

**Pendiente que no es de código**: la comprobación manual de accesibilidad de T058 (quickstart §5) sigue sin hacerse; R19 no se ha ejecutado a mano. Ambas constan en `tasks.md`.

**Hallazgos por tipo**: partial 1 · missing 0 · contradicts 0 · unrequested 0. **Por severidad**: LOW 1.
