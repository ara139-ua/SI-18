# Guía de validación: 002 · Reservar y pagar una pista

Cómo comprobar de principio a fin que HU-02 funciona. El arranque del entorno (Docker, `.env`, base de
datos y siembra) es el de [`000-core/quickstart.md`](../000-core/quickstart.md) §1–§3; aquí no se
repite.

## 1. Requisitos previos

- El entorno de 000-core arrancado (PostgreSQL de docker-compose y la API con la siembra hecha).
- `develop` con spec-001 fusionada (`/buscar-pistas` e `IConsultaDisponibilidad`).
- La contraseña de demostración (`SI18_CLAVE_DEMO` de tu `.env`).
- Tarjetas ficticias (R9 de 000-core): `4000 0000 0000 0001` se acepta; `4000 0000 0000 0002` se
  rechaza; `4000 0000 0000 001` (15 dígitos) no tiene un formato válido.

## 2. Pruebas automáticas

```powershell
cd fuentes
dotnet test SI18.slnx                                         # todo en verde, también las de spec-001
dotnet test SI18.slnx --filter-trait "Categoria=Limites"      # 7 días, inicio, 9:59/10:00, solape del cierre
```

Las pruebas de HU-02 están en `tests/SI18.Api.Tests/Reservas/` (research.md E13) y necesitan Docker
(Testcontainers). Comprueba además que:

- `tests/SI18.Api.Tests/Disponibilidad/` sigue en verde sin cambios, salvo las dos pruebas de
  `IConsultaReservas` que se han movido a `Reservas/ConsultaReservasTests.cs` (CE-007);
- ya no existe ninguna referencia a `ConsultaReservasProvisional`:

  ```powershell
  git grep -n "ConsultaReservasProvisional" -- fuentes   # sin resultados
  ```

## 3. Escenarios manuales contra la API

Con un cliente HTTP con cookies (o desde el navegador con la sesión iniciada). `{pádel1}` es el
`instalacionId` de «Pista de pádel 1» de Polideportivo Norte (`GET /api/instalaciones`), y `{inicio}`,
un `inicioUtc` libre de `GET /api/disponibilidad/franjas` para mañana. Contrato:
[contracts/api-reservas.openapi.yaml](./contracts/api-reservas.openapi.yaml).

| #   | Acción                                                                                                  | Resultado esperado                                                                                     | Spec |
| --- | ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ---- |
| R1  | Sesión como `ciudadana.empadronada@si18.test`; `GET /api/reservas/resumen?instalacionId={pádel1}&inicioUtc={inicio}` | 200 con pista, centro, fecha, horas de Madrid e `importe = 6.00`                                       | HU1 esc. 1 |
| R2  | `POST /api/reservas` con `{ instalacionId, inicioUtc, importeMostrado: 6.00 }`                          | 201, `estado = pendiente_pago`, `horaLimitePago` = hora actual de Madrid + 10 min (truncada), sin `codigoAcceso` | HU1 esc. 2 |
| R3  | Repite la búsqueda de spec-001 (`GET /api/disponibilidad/franjas`) para ese día                         | La franja reservada ya no aparece                                                                      | HU2 |
| R4  | Sesión como `ciudadano.noempadronado@si18.test`; `POST /api/reservas` sobre la misma franja             | 409 `FRANJA_OCUPADA`                                                                                   | HU2 esc. 1 |
| R5  | Con la sesión de R2: `POST /api/reservas/{id}/pago` con `4000 0000 0000 0001`                           | 200, `estado = confirmada`, `codigoAcceso` de 22 caracteres                                            | HU1 esc. 3 |
| R6  | Repite R5                                                                                               | 409 `TRANSICION_NO_PERMITIDA`, sin cobro                                                               | Casos límite |
| R7  | Reserva otra franja y paga con `4000 0000 0000 0002`                                                    | 422 `PAGO_RECHAZADO`; `GET /api/reservas/{id}` → `caducada`, sin código; la franja vuelve a la búsqueda | HU3 esc. 1, 3 |
| R8  | Reserva otra franja y paga con `4000 0000 0000 001`                                                     | 400 `PETICION_INVALIDA` con `errors.numeroTarjeta`; la reserva sigue `pendiente_pago`                  | HU3 esc. 4 |
| R9  | Reserva otra franja, espera 10 minutos y paga                                                           | 409 `RESERVA_CADUCADA`, tanto si el barrido ya la ha guardado como caducada como si no (research.md E4); sin cobro | HU2 esc. 3 |
| R9b | Paga otra vez la reserva de R7 (caducada por pago rechazado)                                            | 409 `TRANSICION_NO_PERMITIDA`, sin cobro                                                               | HU3 esc. 2 |
| R10 | `POST /api/reservas` con `importeMostrado: 5.00`                                                        | 409 `PRECIO_CAMBIADO`; no se crea nada                                                                 | HU4 esc. 5 |
| R11 | `POST /api/reservas` para hoy + 8 días (un `inicioUtc` a las 10:00 de Madrid de ese día)                | 422 `FUERA_DE_ANTELACION`                                                                              | HU4 esc. 1 |
| R12 | `POST /api/reservas` con el `instalacionId` de la Sala polivalente                                     | 422 `INSTALACION_NO_RESERVABLE`                                                                        | HU4 esc. 4 |
| R13 | `POST /api/reservas` con un `inicioUtc` a las 10:30 de Madrid                                          | 400 `PETICION_INVALIDA`                                                                                | Casos límite |
| R14 | Sesión como `representante.padel@si18.test`; resumen y reserva de una franja libre                     | `importe = 4.00` (tarifa club)                                                                         | HU1 esc. 4 |
| R15 | `GET /api/reservas` con la sesión de R2                                                                 | Todas las suyas: primero las futuras por inicio ascendente, luego las pasadas; código solo en las confirmadas | HU5 |
| R16 | Con la sesión de R4: `GET /api/reservas/{id de R2}` y `POST /api/reservas/{id de R2}/pago`              | 404 `NO_ENCONTRADO` en los dos                                                                         | HU5 esc. 4 |
| R17 | Sesión como `personal.norte@si18.test` o `tecnico@si18.test`; cualquier endpoint de `/api/reservas`     | 403 `ROL_NO_PERMITIDO`                                                                                 | RF-001 |
| R18 | Sin sesión                                                                                              | 401 `NO_AUTENTICADO`                                                                                   | RF-001 |
| R19 | Reserva una franja que empiece en menos de 10 minutos (por ejemplo, a las 20:55 la de las 21:00) y págala cuando ya haya empezado | La `horaLimitePago` de la reserva es la de inicio de la franja; el pago da 422 `FRANJA_YA_EMPEZADA`, `GET /api/reservas/{id}` → `caducada`, sin código y sin cobro; un segundo pago, 409 `TRANSICION_NO_PERMITIDA` | RF-008a, RF-009a, HU2 esc. 6 |

Las operaciones de `IServicioReservas` (cancelar por el usuario, aplicar un cierre) no tienen endpoint
en esta spec: se validan con `ServicioReservasTests` y, de punta a punta, con las specs 003 y 007.

## 4. Escenarios manuales en la pantalla

1. Inicia sesión como `ciudadana.empadronada@si18.test`. El menú muestra «Buscar pistas» y «Mis
   reservas».
2. Busca «Pádel», «Polideportivo Norte», mañana. Cada franja tiene un botón «Reservar».
   *(Si el cambio de `BuscarPistas.razor` aún no está fusionado —punto a coordinar con Artur—, abre
   `/reservas/nueva?instalacionId={pádel1}&inicioUtc={inicio}` a mano.)*
3. Pulsa «Reservar» en las 18:00: el resumen muestra pista, centro, fecha, «de 18:00 a 19:00» y
   «6,00 €». Pulsa «Confirmar reserva».
4. La pantalla de pago muestra «Paga antes de las HH:mm; después la reserva caduca.». Paga con
   `4000 0000 0000 0001`: se anuncia «Reserva confirmada» y aparece el QR con el código en texto.
5. Abre «Mis reservas»: la reserva aparece «Confirmada» con su QR.
6. Repite con `4000 0000 0000 0002`: se anuncia el mensaje de `PAGO_RECHAZADO` y un enlace para volver a
   buscar; en «Mis reservas» aparece «Caducada», sin QR.
7. Reserva y no pagues: en «Mis reservas» aparece «Pendiente de pago» con «Pagar»; pasados 10 minutos,
   aparece «Caducada».
8. Como `personal.norte@si18.test`, el menú no muestra «Mis reservas», y `/mis-reservas` o
   `/reservas/nueva?…` escritas a mano muestran un aviso, sin formulario. Sin sesión, llevan a «Iniciar
   sesión».

## 5. Accesibilidad (quickstart.md §6 de 000-core, aplicado a las pantallas de HU-02)

Sobre el botón «Reservar» de `/buscar-pistas`, `/reservas/nueva`, `/reservas/{id}/pago` y `/mis-reservas`:

1. Completa reserva y pago solo con el teclado, con el foco siempre visible (CE-008).
2. Con un lector de pantalla:
   - cada «Reservar» se anuncia con pista, fecha y horas («Reservar Pista de pádel 1, sábado
     26/12/2026, de 18:00 a 19:00»);
   - el campo de la tarjeta se anuncia con su etiqueta y, si hay error, con su mensaje;
   - el resultado del pago (confirmada, rechazada o caducada) se anuncia y el foco va a su encabezado;
   - el QR se anuncia con su descripción y el código se puede leer en texto.
3. Ningún estado se entiende solo por el color: «Confirmada», «Pendiente de pago», «Caducada» y
   «Cancelada» están escritos.
4. Lighthouse → Accesibilidad ≥ 95 y axe DevTools sin infracciones graves ni críticas en las cuatro
   pantallas.
5. Anota el resultado en la descripción del PR.
