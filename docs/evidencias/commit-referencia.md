# Commit de referencia y versión de la herramienta

> **Identificado a posteriori** por Saul el 26/11/2026. El enunciado (5.1, «Preparación común») pide
> guardar un commit de referencia antes del refinamiento; no se marcó en su momento (no hay etiquetas
> en el repositorio), así que aquí se identifica después a partir del historial.

## Commit de referencia del refinamiento sobre la versión funcional

| Dato | Valor |
| ---- | ----- |
| Commit | `36a3166` (`develop`, «Merge pull request #48 from ara139-ua/feature/cierre-tareas») |
| Fecha | 26/11/2026 18:25 (hora de Madrid) |
| Qué contiene | La versión funcional de 000-core, 001, 002, 005 y 006, con todas las casillas de `specs/*/tasks.md` marcadas |
| Último cambio de código antes de él | `f384073` (spec-006 T032) |
| Comprobación | `git diff --stat 36a3166 <commit de la memoria> -- fuentes` permite ver qué código cambió después |

El refinamiento sobre la versión funcional (clarify en 002 y 006, analyze final en 000-core y 002 y
converge en 002) parte de este commit. Los cambios posteriores hasta empezarlo son solo de documentos: la
constitución 1.2.0 (#49), el alcance de dos personas (#50) y estas evidencias.

## Estados de partida que constan en las pasadas anteriores

| Pasada | Estado de partida | Fuente |
| ------ | ----------------- | ------ |
| 000-core, converge (26/11/2026) | `develop` en `f4c7558`, rama `feature/000-core-cierre-final` | `specs/000-core/converge-2026-11-26.md` |
| 002, converge (26/11/2026) | `develop` en `ba60464`, rama `feature/002-reservar-pagar-cierre` | PR #39 (`specs/002-reservar-pagar/converge-2026-11-26.md`) |
| 006, converge (26/11/2026) | `develop` con #45 y #46, rama `feature/006-validar-qr-cierre` | `specs/006-validar-qr/converge-2026-11-26.md` |
| 001 y 005, converge (25/11/2026) | No consta | Commits `3f3022e` y `11d4b36` |
| 001 y 005, analyze y converge finales (26/11/2026) | `develop` en `ef5e9be` | `analyze-final.md` y `converge-2026-11-26.md` de cada spec |
| Analyze inicial de 001 y 005 (25/11/2026) | El padre de cada commit de correcciones (`8c1c27f`, `13fc6dd`); el informe original se recuperó de la sesión | `analyze-registro.md` de cada spec |
| Analyze de 000-core y 002 | No consta (el padre de cada commit de correcciones es el estado analizado) | `analyze-registro.md` de cada spec |

## Versión de la herramienta

| Herramienta | Versión | Fuente |
| ----------- | ------- | ------ |
| Spec Kit | `1.1.2.dev0`, integración `claude`, scripts de PowerShell | `.specify/init-options.json` y `.specify/integration.json` |
| Comandos | `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement`, `/speckit-converge` (nombres de la integración instalada, equivalentes a `/speckit.clarify`, etc.) | `.claude/skills/` |
| Asistente | Claude Code (los commits generados con él llevan `Co-Authored-By: Claude`) | Historial de git |
