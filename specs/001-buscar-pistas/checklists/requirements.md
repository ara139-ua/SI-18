# Checklist de calidad de la especificación: 001 · Buscar pistas libres

**Propósito**: validar que la especificación está completa y tiene calidad antes de pasar a la planificación
**Creada**: 2026-11-25
**Spec**: [spec.md](../spec.md)

## Calidad del contenido

- [x] Sin detalles de implementación (lenguajes, frameworks, API)
- [x] Centrada en el valor para el usuario y en las necesidades del servicio
- [x] Escrita para personas no técnicas
- [x] Todas las secciones obligatorias completas

## Completitud de los requisitos

- [x] No quedan marcas [NEEDS CLARIFICATION]
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
- [x] La funcionalidad cumple los resultados medibles de los criterios de éxito
- [x] No se cuelan detalles de implementación en la especificación

## Notas

- Las referencias a `000-core` (RF-011, RF-013, contracts/modulos.md y los códigos del catálogo de
  errores, como `FUERA_DE_ANTELACION` o `ROL_NO_PERMITIDO`) no son detalles de implementación: son el
  contrato común que la constitución (principio VII) obliga a citar en lugar de repetir.
- CE-006 nombra Lighthouse y axe porque son la comprobación de accesibilidad acordada en
  quickstart.md §6 de `000-core` (constitución, principio I), no una elección técnica de esta spec.
- RF-013 (ofrecer a HU-02 disponibilidad y precio) recoge, en términos funcionales, la
  responsabilidad de HU-01 sobre `IConsultaDisponibilidad` definida en `000-core`. El detalle técnico
  queda para `/speckit-plan`.
- Sin marcas de aclaración: las decisiones abiertas (búsqueda con sesión, tres filtros obligatorios,
  solo franjas libres, acceso a «Reservar» en HU-02) se han resuelto con supuestos razonables
  documentados en «Supuestos». `/speckit-clarify` puede revisarlas si el grupo lo cree necesario.
