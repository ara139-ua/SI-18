# Agrupamiento · Reparto de historias de SI-18 (G18)

Reparto de las historias de usuario entre las personas del grupo y dueños de cada módulo. Según la
constitución («Acuerdos de colaboración»), cada persona es responsable de dos historias y este reparto
**no cambia sin acuerdo del grupo**.

## Alcance comprometido

El grupo son **dos personas**, Saul y Artur. El alcance está **acordado con el profesor**: cada
persona es responsable de dos historias, así que el alcance comprometido son **cuatro historias**:
HU-01, HU-02, HU-05 y HU-06.

Las otras cuatro historias identificadas en la descripción del sistema (HU-03, HU-04, HU-07 y HU-08)
quedan **fuera del alcance comprometido**. No están asignadas a nadie y no son trabajo pendiente.
`000-core` ya deja preparada la base común que necesitarían (modelo de datos, valores de negocio,
estados e interfaces entre módulos). Esa base se explica en la memoria como punto de partida para
trabajo futuro.

> Hasta el 26/11/2026 este documento repartía esas cuatro historias a dos personas «C» y «D» que no
> llegaron a formar parte del grupo (versión anterior en el historial de git, #9).

## Reparto por persona

| Persona | Historias    | Módulo           | Qué cubre |
| ------- | ------------ | ---------------- | --------- |
| Artur   | HU-01, HU-05 | `Disponibilidad` | HU-01 buscar pistas · HU-05 franjas fijas de clubes |
| Saul    | HU-02, HU-06 | `Reservas`       | HU-02 reservar y pagar · HU-06 validar QR en el acceso |

| Persona | HU-01 | HU-02 | HU-05 | HU-06 |
| ------- | :---: | :---: | :---: | :---: |
| Artur   | X     |       | X     |       |
| Saul    |       | X     |       | X     |

## Historias fuera del alcance comprometido

| Historia | Qué cubriría | Módulo previsto | Base que ya existe (000-core y specs comprometidas) |
| -------- | ------------ | --------------- | --------------------------------------------------- |
| HU-03 | Cancelar una reserva (devolución si faltan más de 24 h) | `Cancelaciones` | `Devolucion`, `PoliticaDevolucion` y `ValoresNegocio.PlazoDevolucion`; transición confirmada → cancelada; `IServicioReservas.CancelarPorUsuario` (spec-002) |
| HU-04 | Inscribirse en un curso trimestral con lista de espera | `CursosInformes` | Entidades `Curso` e `Inscripcion`, sus configuraciones y la siembra de cursos |
| HU-07 | Cerrar una instalación por mantenimiento | `Cancelaciones` | `CierreMantenimiento`, `IConsultaCierres`, `IServicioReservas.AplicarCierre` (spec-002), `IConsultaFranjasFijas.RepresentantesAfectados` (spec-005) e `IServicioAvisos` |
| HU-08 | Informe mensual de ocupación e ingresos | `CursosInformes` | Reglas de cómputo de 000-core (RF-025), estados de reserva, pago y devolución, y el rol de técnico |

## Dueños de módulo

Cada módulo tiene un único dueño, que es el único que escribe en sus tablas; los demás usan su
interfaz pública (ver `specs/000-core/plan.md`, «Dueños de módulo»).

| Módulo           | Historias    | Tablas que escribe                                   | Dueño |
| ---------------- | ------------ | ---------------------------------------------------- | ----- |
| `Comun`          | 000-core     | `centros`, `deportes`, `instalaciones`, `instalacion_deportes`, `tarifas`, `usuarios`, `clubes`, `temporadas`, `notificaciones` | Saul y Artur |
| `Disponibilidad` | HU-01, HU-05 | `solicitudes_franjas_fijas`                          | Artur |
| `Reservas`       | HU-02, HU-06 | `reservas`, `pagos`, `validaciones_acceso`           | Saul  |
| `Cancelaciones`  | HU-03, HU-07 | `devoluciones`, `cierres_mantenimiento`              | Sin dueño: fuera del alcance comprometido; solo existe la base de 000-core |
| `CursosInformes` | HU-04, HU-08 | `cursos` (solo la siembra), `inscripciones`          | Sin dueño: fuera del alcance comprometido; solo existe la base de 000-core |

## Reglas

- Los cambios en el modelo de datos compartido (`Comun`) o en los valores de negocio van por pull
  request revisado por la otra persona del grupo.
- Cuando una historia dependa de la otra persona (por ejemplo, HU-01 consulta las reservas activas de
  HU-02), la dependencia se anota en las dos specs y se avisa a la otra persona.
- Todo commit indica la spec o tarea a la que pertenece (por ejemplo, `spec-002 T014: ...`).
