# Guía de validación: 005 · Franjas fijas de temporada para clubes

Cómo comprobar de principio a fin que HU-05 funciona. El arranque del entorno (Docker, `.env`, base de
datos y siembra) es el de [`000-core/quickstart.md`](../000-core/quickstart.md) §1–§3; aquí no se
repite.

## 1. Requisitos previos

- El entorno de 000-core arrancado (PostgreSQL de docker-compose y la API con la siembra hecha).
- La contraseña de demostración (`SI18_CLAVE_DEMO` de tu `.env`).
- Usuarios de la siembra: `representante.norte@si18.test` (Club Deportivo Ficticio Norte),
  `representante.padel@si18.test` (Club de Pádel Ejemplo) y `tecnico@si18.test`.

## 2. Pruebas automáticas

```powershell
cd fuentes
dotnet test SI18.slnx                                         # todo en verde
dotnet test SI18.slnx --filter-trait "Categoria=Limites"      # solapes, Cubre, temporada, cambio de hora
```

Las de HU-05 están en `tests/SI18.Dominio.Tests/Disponibilidad/ReglaFranjasFijasTests.cs` y en
`tests/SI18.Api.Tests/Disponibilidad/` (research.md F10). Las de integración necesitan Docker.

## 3. Escenarios manuales contra la API

`{pabellón}` es el pabellón del Polideportivo Norte (`GET /api/instalaciones?centroId={Norte}`) y
`{temporada}` la de `GET /api/solicitudes-franjas-fijas/temporadas`.

| #   | Acción                                                                                              | Resultado esperado                                                                       | Spec |
| --- | --------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ---- |
| S1  | Como `representante.norte`, `POST` con `{pabellón}`, martes y jueves (`[2,4]`), `19:00`–`21:00`, `{temporada}` | 201; `estado = pendiente`; `clubNombre = Club Deportivo Ficticio Norte`               | HU1 esc. 1 |
| S2  | `GET /api/solicitudes-franjas-fijas` como `representante.norte` y como `representante.padel`       | El primero la ve; el segundo no                                                          | HU1 esc. 6 |
| S3  | Repite S1 con «Pista de pádel 1» (los pabellones no salen en la búsqueda de HU-01) y busca pádel en Norte un martes | La franja de 19:00 de esa pista sigue libre mientras la solicitud está pendiente | HU1 esc. 2 |
| S4  | `POST` con días `[]`, con `19:30`, con una sala o con una temporada terminada                       | 400 `PETICION_INVALIDA` con el campo en `errors`                                         | HU1 esc. 3–5 |
| S5  | Como `ciudadana.empadronada`, `POST`; como `representante.norte`, `POST …/aprobacion`                | 403 `ROL_NO_PERMITIDO`                                                                   | RF-001, RF-007 |
| S6  | Como `tecnico`, `GET …/{id}/conflictos` de S1                                                       | 200; `puedeAprobarse = true`                                                             | HU2 |
| S7  | `POST …/{id}/aprobacion`                                                                            | 200; `estado = aprobada`; `resueltaEnUtc` informada                                      | HU2 esc. 1 |
| S8  | Búsqueda de pádel en Norte un martes (con la solicitud de «Pista de pádel 1» aprobada)              | No salen las franjas de 19:00 ni de 20:00 en esa pista                                   | HU2 esc. 2 |
| S9  | `GET /api/avisos` como `representante.norte`                                                        | Aviso «… ha sido aprobada.»                                                              | HU2 esc. 4 |
| S10 | Otra solicitud que se solapa (martes `20:00`–`22:00`, misma pista) y `POST …/aprobacion`             | 409 `FRANJA_FIJA_SOLAPADA`                                                               | HU3 esc. 5 |
| S11 | Inserta una reserva confirmada un jueves de la temporada a las 19:00 en otra pista, pide para esa pista jueves 19–21 y consulta conflictos | La reserva aparece (fecha, hora, estado, sin usuario) y aprobar da 409 `SOLICITUD_CON_CONFLICTOS` | HU3 esc. 1–2 |
| S12 | `POST …/{id}/rechazo` de S11                                                                        | 200; `estado = rechazada`; aviso «… ha sido rechazada.»                                  | HU4 |
| S13 | `POST …/{id}/rechazo` otra vez                                                                      | 409 `SOLICITUD_YA_RESUELTA`                                                              | HU4 esc. 4 |

## 4. Escenarios manuales en las pantallas

1. Como `representante.norte@si18.test`: el menú muestra «Franjas fijas». En `/franjas-fijas` se envía una
   solicitud con el teclado y aparece en la lista como «Pendiente».
2. Como `tecnico@si18.test`: el menú muestra «Revisar franjas fijas». En `/franjas-fijas/revision`, la
   solicitud sale la primera, «Ver conflictos» muestra «No hay conflictos» y «Aprobar» la deja
   «Aprobada», con el resultado anunciado.
3. Con una solicitud con conflictos, «Aprobar» está desactivado y al lado se explica por qué. «Rechazar»
   funciona.
4. Sin sesión, las dos direcciones llevan a «Iniciar sesión». Con otro rol, aparece el aviso sin
   formulario.

## 5. Accesibilidad (quickstart.md §6 de 000-core, en las dos pantallas)

1. Todo solo con teclado: desplegables, casillas de los días (espacio para marcar), botones y lista. El
   foco siempre visible, también tras elegir en un desplegable.
2. Lector de pantalla: el grupo de días se anuncia como «Días de la semana»; el resultado de enviar,
   aprobar o rechazar se anuncia; el estado de cada solicitud se lee en texto.
3. Lighthouse → Accesibilidad ≥ 95 y axe sin infracciones graves ni críticas.
4. Anota el resultado en la descripción del PR.
