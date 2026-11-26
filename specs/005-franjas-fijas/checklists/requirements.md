# Checklist de calidad de la especificación: 005 · Franjas fijas de temporada para clubes

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

- Las referencias a `000-core` (RF-022, D2, D8, contracts/modulos.md con `IConsultaFranjasFijas`,
  `IConsultaReservas` e `IConsultaCierres`, y los códigos del catálogo de errores) son el contrato común
  que la constitución (principio VII) obliga a citar, no detalles de implementación. Lo mismo vale para
  la consulta provisional de spec-001 que se retira (RF-015).
- CE-006 nombra Lighthouse y axe porque son la comprobación de accesibilidad acordada en
  quickstart.md §6 de `000-core` (principio I).
- **Dependencia de 000-core**: harán falta tres códigos de error nuevos (solicitud con conflictos,
  solapada con otra franja fija, ya resuelta). Según RF-003 de `000-core`, se proponen allí antes de
  usarlos (véase «Supuestos»). El plan debe recogerlo.
- Sin marcas de aclaración: las decisiones abiertas se han resuelto con supuestos razonables (sin
  motivo de rechazo escrito, conflictos sin datos personales, el técnico no cancela reservas, sin
  retirar ni modificar solicitudes). `/speckit-clarify` puede revisarlas.
