# `/speckit-analyze` tras el clarify funcional · 001-buscar-pistas

**Fecha**: 2026-11-26 · **Ejecutado por**: Artur (con Claude Code) · **Rama**: `feature/001-005-clarify-funcional`
sobre `develop` en `027ab28` · **Constitución**: 1.2.1

**Momento**: después del clarify sobre la versión funcional (enunciado 5.1, [clarify-funcional.md](./clarify-funcional.md)),
que añadió dos aclaraciones (sesión 2026-11-26), amplió el caso límite «Deporte sin pistas reservables», añadió
«Hoy sin franjas por empezar» y precisó RF-011. El análisis anterior es [analyze-final.md](./analyze-final.md).

Análisis de solo lectura de `spec.md`, `plan.md` y `tasks.md` frente a la constitución.

## Informe de análisis de la especificación

| ID | Categoría | Gravedad | Ubicación | Resumen | Recomendación |
|----|-----------|----------|-----------|---------|---------------|
| E3 | Cobertura | BAJA | spec.md «Casos límite» (Deporte sin pistas reservables), RF-011 · tasks.md T018 | La aclaración pone como ejemplo un deporte que no se reserva por franjas (natación, solo en piscina). T018 prueba «Baloncesto» (en el Norte solo está en el pabellón, que tampoco se reserva por franjas): es la misma regla, pero el ejemplo de la spec no tiene prueba propia. | Añadir «Natación» al mismo caso de `BuscarSinResultadosTests` (converge). |
| I2 | Incoherencia | BAJA | plan.md «Código fuente» | Sigue del analyze final: el plan lista provisionales ya retiradas (#31, #33). | Nota en plan.md (pendiente de documentación, fuera de esta pasada). |
| I3 | Incoherencia | BAJA | plan.md «Comprobación de la constitución» y «Dependencias y riesgos» | Sigue del analyze final: la convención de `Program.cs` entró en el #18, no en el PR de HU-01. | Nota en plan.md (pendiente de documentación, fuera de esta pasada). |

Sin hallazgos nuevos sobre el resto de lo aclarado:

- **«Hoy sin franjas por empezar»**: ya lo prueba T018 («hoy a las 21:00:00» → 200 con `TotalPistas = 2` y
  `Pistas` vacía) y la pantalla muestra el mensaje general de RF-011 (T019).
- **RF-011 (indicar cuántas pistas tiene el centro)**: es el `TotalPistas` del contrato (T002, T009); la
  redacción nueva no cambia el plan ni el contrato.

**Tabla de cobertura**: igual que en analyze-final.md, salvo RF-011, que ahora también cubre el caso
«deporte que no se reserva por franjas» (T018; E3 pide su ejemplo literal). CE-001 sigue sin tarea (E1,
aceptado en el registro).

**Problemas de alineación con la constitución**: ninguno. La aclaración no crea códigos de error ni valores
nuevos (IV, VII): el caso es un resultado vacío, no un error.

**Tareas sin requisito**: ninguna.

**Métricas**

- Requisitos: 24 (17 RF y 7 CE)
- Tareas: 27, todas hechas
- Cobertura: 23 de 24 (96 %)
- Ambigüedades: 0
- Duplicados: 0
- Problemas críticos: 0

## Siguientes pasos

No hay problemas críticos ni altos. E3 se resuelve con converge (una tarea de prueba); I2 e I3 siguen pendientes
como notas de plan.md.
