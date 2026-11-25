# Checklist de calidad de la especificación: 002 · Reservar y pagar una pista

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

- Las referencias a `000-core` (RF-011, RF-013 a RF-017, RF-021, R8, R9 y los códigos del catálogo de
  errores, como `FRANJA_OCUPADA` o `PAGO_RECHAZADO`) no son detalles de implementación: son el contrato
  común que la constitución (principio VII) obliga a citar en lugar de repetir.
- «Barrido» y «pasarela simulada» son piezas ya definidas en `000-core`; la spec solo exige su efecto
  funcional (la reserva acaba caducada sin intervención; el pago no es real).
- RF-018 y RF-019 recogen en términos funcionales la responsabilidad de HU-02 sobre la consulta de
  reservas activas de `000-core` (contracts/modulos.md) y la retirada de la versión provisional de
  spec-001 (RF-014a). El detalle técnico queda para `/speckit-plan`.
- CE-008 nombra Lighthouse y axe porque son la comprobación de accesibilidad acordada en
  quickstart.md §6 de `000-core` (principio I), no una elección técnica de esta spec.
- Sin marcas de aclaración: las decisiones abiertas («Mis reservas» en esta historia, abandonar el
  pago, datos de la tarjeta, límite de reservas por persona y quién implementa cancelar y aplicar
  cierre) están resueltas con supuestos documentados en «Supuestos» y se revisan en `/speckit-clarify`.
