# Checklist de calidad de la especificación: 000-core · Núcleo común de SI-18

**Propósito**: Validar que la especificación está completa y tiene calidad antes de pasar a la planificación
**Creada**: 2026-10-21
**Spec**: [spec.md](../spec.md)

## Calidad del contenido

- [x] Sin detalles de implementación (lenguajes, frameworks, APIs)
- [x] Centrada en el valor para el usuario y las necesidades del negocio
- [x] Escrita para personas no técnicas
- [x] Todas las secciones obligatorias están completas

## Completitud de los requisitos

- [x] No quedan marcadores [NEEDS CLARIFICATION]
- [x] Los requisitos se pueden probar y no son ambiguos
- [x] Los criterios de éxito se pueden medir
- [x] Los criterios de éxito no dependen de la tecnología (sin detalles de implementación)
- [x] Todos los escenarios de aceptación están definidos
- [x] Los casos límite están identificados
- [x] El alcance está bien delimitado
- [x] Las dependencias y los supuestos están identificados

## Preparación de la funcionalidad

- [x] Todos los requisitos funcionales tienen criterios de aceptación claros
- [x] Los escenarios de usuario cubren los flujos principales
- [x] La funcionalidad cumple los resultados medibles definidos en los criterios de éxito
- [x] No se filtran detalles de implementación en la especificación

## Notas

- Validación superada en la primera iteración.
- Referencias técnicas que se mantienen a propósito, porque las exige la constitución o la propuesta del
  grupo, no porque elijan una tecnología: «fechas almacenadas en UTC» y `Europe/Madrid` (principio IX),
  la unicidad de la reserva activa «también a nivel de almacenamiento» (principio III), los cambios por
  pull request (Acuerdos de colaboración) y `/speckit-analyze` como forma de verificar CE-001.
- Las decisiones D1–D10 están en «Supuestos» con su motivo; por eso no hace falta ningún marcador de
  aclaración.
- Siguiente paso: `/speckit-clarify` (opcional) o `/speckit-plan`, que debe generar `plan.md` y
  `data-model.md` compartidos para las specs 001–008.
