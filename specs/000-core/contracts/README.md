# Contratos de 000-core

| Fichero                                              | Qué fija                                                                        |
| ---------------------------------------------------- | ------------------------------------------------------------------------------- |
| [api-comun.openapi.yaml](./api-comun.openapi.yaml)   | Endpoints HTTP comunes: sesión, catálogo precargado y «Mis avisos»               |
| [errores.md](./errores.md)                           | Formato Problem Details, estados HTTP y catálogo de códigos de error            |
| [modulos.md](./modulos.md)                           | Interfaces C# públicas entre módulos y el grafo de dependencias permitido       |

## Convenciones HTTP comunes

- **Rutas**: todas bajo `/api`, con sustantivos en español, en plural y en kebab-case.
- **JSON**: propiedades en camelCase y enumerados como texto en `snake_case` (igual que en la base de
  datos).
- **Instantes**: ISO 8601 en UTC (`2026-11-02T17:00:00Z`). El cliente los muestra en hora de Madrid.
- **Fechas y horas de pared**: `yyyy-MM-dd` y `HH:mm`.
- **Importes**: número JSON con dos decimales (`6.00`). El cliente los formatea como `6,00 €`
  (`es-ES`, RF-027).
- **Sesión**: cookie `si18.sesion`. Sin sesión → 401 `NO_AUTENTICADO`, nunca una redirección.
- **Contratos C#**: los DTO están en `SI18.Contratos/<Modulo>/` y los comparten la API y el cliente.

## Prefijos de ruta reservados por módulo

Cada spec define sus endpoints dentro de su prefijo; ningún módulo publica rutas en el prefijo de otro.

| Prefijo                                 | Módulo (dueño)  | Historia |
| --------------------------------------- | --------------- | -------- |
| `/api/sesion`, `/api/centros`, `/api/deportes`, `/api/instalaciones`, `/api/avisos` | Comun | 000-core |
| `/api/disponibilidad`                   | Disponibilidad  | HU-01    |
| `/api/solicitudes-franjas-fijas`        | Disponibilidad  | HU-05    |
| `/api/reservas` (crear, pagar, listar las mías) | Reservas | HU-02    |
| `/api/validaciones-acceso`              | Reservas        | HU-06    |
| `/api/reservas/{id}/cancelacion`        | Cancelaciones   | HU-03    |
| `/api/cierres-mantenimiento`            | Cancelaciones   | HU-07    |
| `/api/cursos`, `/api/inscripciones`     | CursosInformes  | HU-04    |
| `/api/informes/mensual`                 | CursosInformes  | HU-08    |

`/api/reservas/{id}/cancelacion` está dentro de `/api/reservas` pero pertenece a Cancelaciones. Se
registra en `Modulos/Cancelaciones/EndpointsCancelacion.cs` y llama a
`IServicioReservas.CancelarPorUsuario`.
