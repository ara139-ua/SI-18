# `/speckit-analyze` tras el clarify funcional · 005-franjas-fijas

**Fecha**: 2026-11-26 · **Ejecutado por**: Artur (con Claude Code) · **Rama**: `feature/001-005-clarify-funcional`
sobre `develop` en `027ab28` · **Constitución**: 1.2.1

**Momento**: después del clarify sobre la versión funcional (enunciado 5.1, [clarify-funcional.md](./clarify-funcional.md)),
que añadió dos aclaraciones (sesión 2026-11-26): un club puede pedir una instalación de cualquier centro, y una
solicitud idéntica a otra suya pendiente se acepta. El análisis anterior es [analyze-final.md](./analyze-final.md).

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` frente a la constitución.

## Informe de análisis de la especificación

| ID | Categoría | Gravedad | Ubicación | Resumen | Recomendación |
|----|-----------|----------|-----------|---------|---------------|
| E3 | Cobertura | BAJA | spec.md «Casos límite» (Representante con varias solicitudes) · tasks.md T008, T018 | El caso límite ahora dice que una solicitud idéntica a otra pendiente del mismo club se acepta, y que al aprobar una la otra se rechaza con `FRANJA_FIJA_SOLAPADA`. T018 (esc. 6) prueba dos pendientes solapadas, pero ninguna prueba envía el duplicado exacto. | Prueba nueva: el mismo club envía dos veces la misma solicitud (201 las dos), se aprueba la primera y la segunda da 409 `FRANJA_FIJA_SOLAPADA` (converge). |

Sin hallazgos sobre el resto de lo aclarado:

- **Instalación de otro centro (RF-001)**: ya lo prueba T008, esc. 1 (`SolicitarFranjasFijasTests`): el
  representante del club Norte pide el pabellón del Polideportivo Sur y queda pendiente. No hace falta tarea.
- **Sala o piscina (esc. 3)**: la prueba manual dio 400 `PETICION_INVALIDA` con el campo `instalacionId`
  marcado («Elige una pista o un pabellón.»), que es lo que dicen RF-005 y el contrato. La previsión de 422 del
  probador era errónea, no un fallo.

**Tabla de cobertura**: igual que en analyze-final.md. CE-001 sigue sin tarea (E1, aceptado en el registro).

**Problemas de alineación con la constitución**: ninguno. Las dos respuestas evitan códigos de error y campos
nuevos (IV, VII): un club sigue sin centro en `000-core`.

**Tareas sin requisito**: ninguna.

**Métricas**

- Requisitos: 25 (19 RF y 6 CE)
- Tareas: 30, todas hechas
- Cobertura: 24 de 25 (96 %)
- Ambigüedades: 0
- Duplicados: 0
- Problemas críticos: 0

## Siguientes pasos

No hay problemas críticos ni altos. E3 se resuelve con converge (una tarea de prueba).
