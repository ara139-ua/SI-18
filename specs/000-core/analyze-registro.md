# Registro de `/speckit-analyze` · 000-core (reconstrucción)

> **Reconstrucción** hecha por Saul el 26/11/2026 a partir del commit `bba07fb` y del PR #8 (primera
> pasada) y del commit `013a370` del PR #10 (segunda pasada). **Los informes originales de
> `/speckit-analyze` no se conservaron.** Este registro solo recoge los identificadores, las gravedades y
> las decisiones que constan en esas fuentes; lo que no consta se indica así. El informe final de
> analyze sobre la versión actual está en `analyze-final-2026-11-26.md`.

**Responsable de la spec**: Saul (base común, con Artur). **Fecha de las pasadas**: 28/10/2026.

## Primera pasada (correcciones en `bba07fb`, PR #8)

El PR #8 agrupa los hallazgos por gravedad («Críticos (constitución)», «Altos», «Medios») y dice que
«los hallazgos bajos (L1–L7) quedan fuera para que el PR sea pequeño».

| ID | Historia / criterio | Gravedad | Evidencia (lo que consta) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| C1 | Constitución I (WCAG) | CRITICAL | Faltaba la comprobación WCAG en los PR con cambios en `SI18.Cliente` | Todo PR con cambios de cliente anota la comprobación WCAG (quickstart §6); T073 queda como revisión final | Saul | Fusionado por Artur; sin revisión registrada en GitHub | Resuelto | tasks.md T024, T055, T067 y regla de colaboración |
| C2 | Constitución II (lógica probada) | CRITICAL | Faltaban pruebas de las transiciones de `SolicitudFranjasFijas` y del solape `[inicio, fin)` de `CierreMantenimiento` | Se añaden, con los dos lados del límite | Saul | Ídem | Resuelto | tasks.md T029 y T031 |
| H1 | Pruebas con sesión | HIGH | Con `http`, la cookie `Secure` no se envía y fallarían las pruebas con sesión | `FabricaApi` usa `https://localhost` | Saul | Ídem | Resuelto | tasks.md |
| H2 | T057 y T071 | HIGH | Dependían de «endpoints de prueba», que no se registran limpiamente con Minimal API | Sin endpoints de prueba (`IAuthorizationService`, BD caída, `DefaultHttpContext`) | Saul | Ídem | Resuelto | tasks.md T057, T071 |
| M1 | Siembra | MEDIUM | Desglose de las 26 instalaciones | Desglose corregido | Saul | Ídem | Resuelto | tasks.md |
| M2 | Modelo de datos | MEDIUM | Se decía 18 tablas | Son 17 | Saul | Ídem | Resuelto | tasks.md |
| M3 | Principio VIII | MEDIUM | El índice único de inscripciones entraba en la migración | Fuera de `NucleoComun`; lo propondría la spec-004 | Saul | Ídem | Resuelto | tasks.md |
| M4 | `FormatoEs` | MEDIUM | No consta el detalle | Pasa a `SI18.Contratos`, con pruebas | Saul | Ídem | Resuelto | plan.md, tasks.md |
| M5 | Nomenclatura | MEDIUM | No consta el detalle | `AvisoAccesible` → `MensajeAccesible` | Saul | Ídem | Resuelto | plan.md, research.md, tasks.md |
| M6 | Nomenclatura | MEDIUM | `HU-3`/`HU-4` de 000-core se confundían con HU-03/HU-04 | Pasan a `HU-C3`/`HU-C4` | Saul | Ídem | Resuelto | contracts/errores.md, quickstart.md, research.md, tasks.md |
| L1–L7 | No consta | LOW | No consta (el PR #8 solo dice que existían) | No se trataron en el #8 | No consta | — | No consta | — |

Ficheros cambiados por `bba07fb` (`git show --stat`): `contracts/errores.md`, `data-model.md`, `plan.md`,
`quickstart.md`, `research.md` y `tasks.md`. La columna «Dónde se resuelve» sale de buscar cada cambio en
ese diff.

## Segunda pasada (corrección en `013a370`, PR #10)

El PR #10 dice: «`tasks.md`: corrección N1 del segundo `/speckit-analyze` (T071 usa `FabricaApi` sin
siembra)».

| ID | Historia / criterio | Gravedad | Evidencia (lo que consta) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| N1 | T071 | No consta | T071 necesitaba `FabricaApi` sin siembra | T071 usa `FabricaApi` sin siembra | Saul | Fusionado por Saul; sin revisión registrada | Resuelto | tasks.md T071 |

No consta si la segunda pasada encontró más hallazgos.

## Lo que no consta

- El texto de los informes, la tabla de cobertura y las métricas de las dos pasadas.
- Los hallazgos L1–L7 y su contenido.
- La gravedad de N1.

## Informe final (26/11/2026)

Registro de los hallazgos de [analyze-final-2026-11-26.md](./analyze-final-2026-11-26.md), una pasada real
sobre la versión actual (no una reconstrucción).

| ID | Historia / criterio | Gravedad | Evidencia (analyze final) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| D1 | RF-016, tabla de responsables, data-model `Confirmar` | CRITICAL | spec-002 RF-009a (aclaración del 26/11/2026) añade una causa de caducidad que 000-core no tiene (principio VII) | Añadir la causa en 000-core por PR revisado por Artur, antes de implementar RF-009a | Saul | Artur (revisión pedida en el PR) | Resuelto: spec-002 T055 (`7cd5517`) | spec.md RF-016, data-model.md § Reserva |
| I1 | spec.md L46, L49, L56, CE-007; tasks.md fase 3 y T011 | MEDIUM | «Las cuatro personas» y «persona C/D» frente a la constitución 1.2.x (dos personas) | Nota de alcance junto a CE-007, sin reescribir lo decidido | Saul | — | Abierto (documentación) | spec.md |
| I2 | tasks.md T076 | LOW | «falta la 006», ya fusionada | Actualizar la nota | Saul | — | Abierto (documentación) | tasks.md T076 |
| I3 | research.md L44–L45 | LOW | «Persona C/D» sin nota de alcance | Nota o referencia a plan.md | Saul | — | Abierto (documentación) | research.md |
| U1 | tasks.md | LOW | Pocas tareas citan su RF; la cobertura es inferida | Sin cambios; la matriz de la memoria dirá que es inferida | Saul | — | Aceptado | — |
| C1 | RF-025 | LOW | Sin tarea en 000-core (el plan lo deja a spec-008) | Base para trabajo futuro (HU-08 fuera del alcance comprometido) | Saul | — | Aceptado | — |
