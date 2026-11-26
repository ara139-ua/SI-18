# Registro de `/speckit-analyze` · 005-franjas-fijas (reconstrucción)

> **Reconstrucción** hecha por Saul el 26/11/2026 a partir del commit `13fc6dd` (PR #33, de Artur).
> **El informe original de `/speckit-analyze` no se conservó.** Este registro solo recoge los
> identificadores y las decisiones que constan en esa fuente; lo que no consta se indica así. Artur,
> responsable de la spec, no ha revisado esta reconstrucción.

**Responsable de la spec**: Artur. **Fecha de la pasada**: 25/11/2026.

El commit no indica la gravedad de ningún hallazgo.

| ID | Historia / criterio | Gravedad | Evidencia (lo que consta) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| T1 | Temporada terminada | No consta | Insertar una temporada rompería `InicializadorDatosTests` (Temporadas: 1) | La prueba usa un reloj propio (nueva ayuda `ConServicioSolicitudes`) | Artur | Sin revisión registrada en GitHub (#33 fusionado por Artur) | Resuelto | tasks.md |
| T2 | `RevisarFranjasFijasTests` | No consta | Una aprobación podía bloquear las demás pruebas | Se reparten pistas y días (aprobar, rechazar y ya resuelta) | Artur | Ídem | Resuelto | tasks.md |
| U1 | Caso límite «solicitud de otro club» | No consta | Prometía un `NO_ENCONTRADO` sin endpoint | Ya no lo promete | Artur | Ídem | Resuelto | spec.md |
| U2 | «Todos los representantes» | No consta | No consta el detalle | Se comprueba contando los representantes del club, sin añadir usuarios | Artur | Ídem | Resuelto | tasks.md |
| I1 | T023 | No consta | No consta el detalle | T023 explica por qué las pruebas movidas conservan sus pistas de Oeste | Artur | Ídem | Resuelto | tasks.md T023 |
| C1 | Nombre de la rama | No consta | «Sigue pendiente de decidirlo en grupo» | No consta ninguna decisión. Hecho comprobable: la rama real se llamó `feature/005-franjas-fijas`, y la constitución dice `spec-005` | Artur | — | Abierto (no consta) | — |

## Lo que no consta

- El texto del informe, la gravedad de cada hallazgo, la tabla de cobertura y las métricas.
- Si se repitió analyze después de las correcciones (no hay informe final).
- La decisión sobre C1.
