# Agrupamiento · Reparto de historias de SI-18 (G18)

Reparto definitivo de las historias de usuario entre las cuatro personas del grupo y dueños de cada
módulo. Según la constitución («Acuerdos de colaboración»), cada persona es responsable de dos
historias y este reparto **no cambia sin acuerdo del grupo**.

## Reparto por persona

| Persona | Historias      | Módulo           | Qué cubre |
| ------- | -------------- | ---------------- | --------- |
| Artur   | HU-01, HU-05   | `Disponibilidad` | HU-01 buscar pistas · HU-05 franjas fijas de clubes |
| Saul    | HU-02, HU-06   | `Reservas`       | HU-02 reservar y pagar · HU-06 validar QR en el acceso |
| C       | HU-03, HU-07   | `Cancelaciones`  | HU-03 cancelar reserva · HU-07 cierre por mantenimiento |
| D       | HU-04, HU-08   | `CursosInformes` | HU-04 cursos y lista de espera · HU-08 informe mensual |

| Persona | HU-01 | HU-02 | HU-03 | HU-04 | HU-05 | HU-06 | HU-07 | HU-08 |
| ------- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Artur   | X     |       |       |       | X     |       |       |       |
| Saul    |       | X     |       |       |       | X     |       |       |
| C       |       |       | X     |       |       |       | X     |       |
| D       |       |       |       | X     |       |       |       | X     |

## Dueños de módulo

Cada módulo tiene un único dueño, que es el único que escribe en sus tablas; los demás usan su
interfaz pública (ver `specs/000-core/plan.md`, «Dueños de módulo»).

| Módulo           | Historias    | Tablas que escribe                                   | Dueño |
| ---------------- | ------------ | ---------------------------------------------------- | ----- |
| `Comun`          | 000-core     | `centros`, `deportes`, `instalaciones`, `instalacion_deportes`, `tarifas`, `usuarios`, `clubes`, `temporadas`, `notificaciones` | Todo el grupo |
| `Disponibilidad` | HU-01, HU-05 | `solicitudes_franjas_fijas`                          | Artur |
| `Reservas`       | HU-02, HU-06 | `reservas`, `pagos`, `validaciones_acceso`           | Saul  |
| `Cancelaciones`  | HU-03, HU-07 | `devoluciones`, `cierres_mantenimiento`              | C     |
| `CursosInformes` | HU-04, HU-08 | `cursos` (solo la siembra), `inscripciones`          | D     |

## Reglas

- Los cambios en el modelo de datos compartido (`Comun`) o en los valores de negocio van por pull
  request revisado por al menos otra persona del grupo.
- Cuando una historia dependa de otra persona (por ejemplo, la cancelación de HU-03 invalida el QR
  de HU-06), la dependencia se anota en las dos specs y se avisa al grupo.
- Todo commit indica la spec o tarea a la que pertenece (por ejemplo, `spec-002 T014: ...`).
