# Contrato de errores (Problem Details, RFC 9457)

**Ámbito**: todas las respuestas de error de la API de SI-18 (principio IV, RF-028). Cada spec
001–008 añade sus códigos aquí, mediante un pull request a 000-core, antes de usarlos (RF-003).

## Formato

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://si18.test/errores/franja-ocupada",
  "title": "Franja no disponible",
  "status": 409,
  "detail": "Otra persona acaba de reservar esta franja. Elige otra hora.",
  "codigo": "FRANJA_OCUPADA",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

- `codigo` es estable y se puede leer por máquina: el cliente decide **solo** por `codigo`.
- `title` y `detail` están en español de España y el cliente los muestra al usuario tal cual.
- `type` es `https://si18.test/errores/{codigo-en-kebab-case}` (es un identificador, no hace falta
  que se pueda abrir).
- `traceId` sirve para buscar el error en los registros. Nunca se incluyen trazas ni detalles
  internos.
- Los errores de validación de formato añaden `errors: { "campo": ["mensaje"] }`.

## Estado HTTP por categoría

| Estado | Cuándo                                                              |
| ------ | ------------------------------------------------------------------- |
| 400    | Petición mal formada (JSON inválido, campo obligatorio ausente, tipo incorrecto) |
| 401    | Sin sesión, o credenciales incorrectas                              |
| 403    | Sesión válida pero rol o centro no permitidos                       |
| 404    | Recurso inexistente, o que no pertenece al usuario (no se revela cuál de los dos) |
| 409    | Conflicto de estado o de concurrencia                               |
| 422    | Regla de negocio incumplida con una petición bien formada           |
| 500    | Error no controlado (mensaje genérico)                              |

## Catálogo de códigos de 000-core

| Código                     | HTTP | `detail` (texto para el usuario)                                                   | Origen         |
| -------------------------- | ---- | ---------------------------------------------------------------------------------- | -------------- |
| `PETICION_INVALIDA`        | 400  | «Revisa los datos marcados.»                                                       | Comun          |
| `NO_AUTENTICADO`           | 401  | «Tienes que iniciar sesión para continuar.»                                        | Comun          |
| `CREDENCIALES_INVALIDAS`   | 401  | «Correo o contraseña incorrectos.» (no revela si el correo existe)                 | Comun, HU-C3 esc. 5 |
| `ROL_NO_PERMITIDO`         | 403  | «Tu perfil no permite realizar esta acción.»                                       | RF-009         |
| `OTRO_CENTRO`              | 403  | «Esta instalación pertenece a otro centro; solo puedes actuar en el tuyo.»         | RF-009, D7     |
| `NO_ENCONTRADO`            | 404  | «No se ha encontrado lo que buscas.»                                               | Comun          |
| `TRANSICION_NO_PERMITIDA`  | 409  | «Esta reserva ya no se puede modificar en su estado actual.»                       | RF-015, HU-C4 esc. 3 |
| `FRANJA_OCUPADA`           | 409  | «Otra persona acaba de reservar esta franja. Elige otra hora.»                     | RF-014         |
| `RESERVA_CADUCADA`         | 409  | «Han pasado más de 10 minutos y la reserva ha caducado. Si la franja sigue libre, haz una reserva nueva.» | RF-016 |
| `PAGO_RECHAZADO`           | 422  | «El pago no se ha completado y la reserva ha caducado. Si la franja sigue libre, haz una reserva nueva.» | RF-016 |
| `FUERA_DE_ANTELACION`      | 422  | «Solo puedes reservar desde hoy hasta dentro de 7 días.»                           | RF-011, D3     |
| `FRANJA_YA_EMPEZADA`       | 422  | «Esa franja ya ha empezado y no se puede reservar.»                                | RF-011         |
| `INSTALACION_NO_RESERVABLE`| 422  | «Esta instalación no se reserva por franjas.»                                      | RF-012         |
| `INSTALACION_CERRADA`      | 422  | «La instalación está cerrada por mantenimiento en esa franja.»                     | RF-011         |
| `FRANJA_BLOQUEADA_CLUB`    | 422  | «La franja está reservada para un club durante la temporada.»                      | RF-011         |
| `PRECIO_CAMBIADO`          | 409  | «El precio de la franja ha cambiado. Revisa el nuevo importe.»                     | RF-013         |
| `RESERVA_YA_EMPEZADA`      | 422  | «La reserva ya ha empezado y no se puede cancelar.»                                | RF-019 (aclaración) |
| `QR_DESCONOCIDO`           | 422  | «Código no válido.»                                                                | RF-018         |
| `QR_RESERVA_NO_CONFIRMADA` | 422  | «La reserva de este código no está confirmada (cancelada o caducada).»             | RF-018         |
| `QR_FUERA_DE_VENTANA`      | 422  | «El código solo es válido desde 15 minutos antes del inicio hasta el final de la reserva.» | RF-018 |
| `QR_OTRO_CENTRO`           | 403  | «Este código pertenece a una reserva de otro centro.»                              | D7             |
| `ERROR_INTERNO`            | 500  | «Ha ocurrido un error inesperado. Inténtalo de nuevo más tarde.»                   | Comun          |

El catálogo vive en el código como constantes en `SI18.Dominio/Comun/CodigosError.cs`, y la tabla
código → estado HTTP vive en `SI18.Api/Infraestructura/ManejadorErrores.cs`. Ninguna otra parte del
código escribe el texto de un código.
