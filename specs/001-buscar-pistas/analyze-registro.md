# Registro de `/speckit-analyze` · 001-buscar-pistas (reconstrucción)

> **Reconstrucción** hecha por Saul el 26/11/2026 a partir del commit `8c1c27f` (PR #19, de Artur) y
> del PR #18. **El informe original de `/speckit-analyze` no se conservó.** Este registro solo recoge
> los identificadores y las decisiones que constan en esas fuentes; lo que no consta se indica así.
> Artur, responsable de la spec, no ha revisado esta reconstrucción.

**Responsable de la spec**: Artur. **Fecha de la pasada**: 25/11/2026.

El commit no indica la gravedad de ningún hallazgo.

| ID | Historia / criterio | Gravedad | Evidencia (lo que consta) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| U1, U2 | Acceso a `/buscar-pistas` | No consta | La pantalla no comprobaba la sesión ni el rol al cargarse | Sin sesión lleva a «Iniciar sesión»; con un rol sin permiso no muestra el formulario y explica para quién es | Artur | Sin revisión registrada en GitHub | Resuelto | tasks.md T012, research.md D7, quickstart.md §4 |
| I1 | RF-011 | No consta | `MotivoNoDisponible` mezclaba motivos de negocio con peticiones mal formadas | Solo devuelve los seis motivos de RF-011 o `null`; las peticiones mal formadas lanzan `ErrorNegocio` (`PETICION_INVALIDA`, `NO_ENCONTRADO`) | Artur | Ídem | Resuelto | research.md D4, contracts, tasks.md T015, T016 |
| I2 | Orden de validación | No consta | No consta el detalle | Orden 400 → 422 → 404 | Artur | Ídem | Resuelto | research.md D2, tasks.md T018 |
| E1 | CE-002 | No consta | Faltaba medir el tiempo de respuesta | Medida en T022 | Artur | Ídem | Resuelto | tasks.md T022 |
| T1 | T018 | No consta | No consta el detalle | T018 restaura el reloj | Artur | Ídem | Resuelto | tasks.md T018 |
| T2 | Franjas fijas y cierres | No consta | Faltaba el caso de franja fija con cierre | Caso añadido | Artur | Ídem | Resuelto | tasks.md T011, T016 |
| C2 | Dependencias | No consta | No consta el detalle | T023 pide anotar la dependencia al crear las specs 002, 005 y 007 | Artur | Ídem | Resuelto (T023 cerrada en el #48) | tasks.md T023 |
| G1 | Registro de módulos | No consta | «Dos líneas en Program.cs», que quedó «para decidir en grupo» | Decidido en 000-core: cada módulo entra con `Add<Modulo>()` y `Map<Modulo>()` en orden fijo (el PR #18 dice que lo detectó este analyze) | Artur | Fusionado por Artur; sin revisión registrada | Resuelto en el #18 | 000-core plan.md «Registro de los módulos en la API», tasks.md, contracts/README.md |
| C1 | Nombre de la rama | No consta | Quedó «para decidir en grupo» | No consta ninguna decisión. Hecho comprobable: las ramas reales se llamaron `feature/001-buscar-pistas…`, y la constitución dice `spec-001` | Artur | — | Abierto (no consta) | — |

## Lo que no consta

- El texto del informe, la gravedad de cada hallazgo, la tabla de cobertura y las métricas.
- Si se repitió analyze después de las correcciones (no hay informe final).
- La decisión sobre C1.
