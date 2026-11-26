# Verificación integrada (enunciado 5.4)

> Ejecutada el 26/11/2026 por Saul sobre el commit `25681ee` (rama `feature/verificacion-integrada`, sobre
> `develop` en `a87cf97`). Los recorridos R1 y R2 son **pruebas automáticas** por HTTP:
> [`fuentes/tests/SI18.Api.Tests/Comun/RecorridosIntegradosTests.cs`](../fuentes/tests/SI18.Api.Tests/Comun/RecorridosIntegradosTests.cs).

## Por qué son pruebas automáticas y no un recorrido a mano

El enunciado (5.4) pide al menos un recorrido satisfactorio y otro de error, permiso o estado que crucen
historias, ejecutados contra el sistema. Se iban a hacer a mano contra la API local, pero **la ventana
horaria real no lo permitió antes de la entrega**:

- Los centros abren de 08:00 a 22:00 y la última franja es la de 21:00–22:00 (`ValoresNegocio.HoraCierre`).
  La entrega es el 26/11/2026 antes de las 00:00 y la ejecución empezó a las 21:37, así que no existía
  ninguna franja que empezara en los minutos siguientes.
- El QR solo vale desde 15 minutos antes del inicio de la franja (`MargenQrAntesDelInicio`). Validar con
  éxito en R1 exige una reserva **pagada** de una franja que empiece en menos de 15 minutos.
- No se falseó el reloj de la API local para hacerlo a mano, porque el recorrido dejaría de ser una
  observación del sistema real.

Por eso R1 y R2 se automatizaron con la misma API (`FabricaApi`, el mismo `Program`, PostgreSQL real en
Testcontainers) y un **reloj propio** (`FakeTimeProvider`) en una fábrica derivada con `WithWebHostBuilder`,
que comparte la base de datos con el resto de pruebas. No se tocó ningún código de producción. La
comprobación manual de la pantalla queda fuera de este documento (véase `specs/002-reservar-pagar/tasks.md`,
T058).

Qué cubre y qué no: las pruebas ejercen la API (endpoints, reglas, base de datos y mensajes de error). **No
ejercen el cliente Blazor**: lo que muestra cada pantalla no se verificó aquí.

## Datos comunes

| Dato | Valor |
| ---- | ----- |
| Base de datos | PostgreSQL 17 (Testcontainers), con la siembra de `000-core`; la comparten el resto de pruebas de la ejecución |
| Clave de los usuarios | La de pruebas de `FabricaApi` (`ClaveDemo`); no es la de demostración |
| Usuarios | `ciudadana2.empadronada` (R1), `personal.este`, `representante.norte`, `tecnico`, `ciudadana.empadronada`, `personal.oeste` (R2) |
| Reloj | `FakeTimeProvider` propio; solo avanza |

Fechas y pistas elegidas con `git grep` para que ninguna otra prueba las use (las franjas fijas aprobadas
valen toda la temporada, así que se eligió una combinación de pista, día de la semana y hora que ninguna
otra prueba busca ni reserva).

## R1 · satisfactorio · HU-01 → HU-02 → HU-06

Prueba: `RecorridosIntegradosTests.R1_buscar_en_Este_reservar_y_pagar_ver_el_codigo_en_Mis_reservas_y_validarlo_dos_veces`.

**Precondiciones**: jueves 11/03/2027, 09:50 en Madrid (08:50 UTC); la franja de las 10:00 empieza en 10
minutos, dentro del margen de 15 minutos del QR. «Polideportivo Este», «Pista de tenis 1» (sin franja fija
ese día)..

| # | Paso | Resultado esperado | Hist. |
| - | ---- | ------------------ | ----- |
| 1 | `GET /api/disponibilidad/franjas` (Tenis, Este, 11/03/2027) como Carla | «Pista de tenis 1» ofrece la franja de las 10:00 | HU-01 |
| 2 | `GET /api/reservas/resumen` | El importe del resumen es el precio de la búsqueda | HU-02 |
| 3 | `POST /api/reservas` con ese importe | 201, estado `pendiente_pago`, sin código de acceso | HU-02 |
| 4 | `POST /api/reservas/{id}/pago` con la tarjeta aceptada | 200, estado `confirmada`, con código de acceso | HU-02 |
| 5 | `GET /api/reservas` («Mis reservas») | La reserva sale `confirmada` con el mismo código | HU-02 |
| 6 | A las 09:52, `POST /api/validaciones-acceso` con `personal.este` y el código | 201: titular «Carla Vecina», «Pista de tenis 1», «Polideportivo Este», 10:00; **sin** aviso de validación anterior | HU-06 |
| 7 | A las 09:55, la misma petición | 201 con el aviso de la anterior: `validacionAnteriorUtc` = 09:52 (08:52 UTC) y `validacionAnteriorHora` = «09:52» | HU-06 |
| 8 | Lectura de la base | Dos filas de `validaciones_acceso` de esa reserva, las dos aceptadas; la reserva sigue `confirmada` | HU-06 |

**Resultado observado**: `correcto` (614 ms). Salida de `dotnet test`:

```
correcto SI18.Api.Tests.Comun.RecorridosIntegradosTests.R1_buscar_en_Este_reservar_y_pagar_ver_el_codigo_en_Mis_reservas_y_validarlo_dos_veces (614ms)
```

## R2 · error, permiso y estado · HU-05 → HU-01 → HU-02 → HU-06

Prueba: `RecorridosIntegradosTests.R2_franja_fija_aprobada_bloquea_la_reserva_el_personal_de_otro_centro_y_una_ciudadana_no_validan`.

**Precondiciones**: una reserva confirmada de Este auxiliar (viernes 12/03/2027, 10:00, «Pista de tenis 2»),
creada y pagada por HTTP al principio de la prueba para tener un código real de Este; lunes 15/03/2027, 07:00
en Madrid. «Polideportivo Norte», «Pista de pádel 2», sin franja fija los lunes de 08:00 a 09:00.

| # | Paso | Resultado esperado | Hist. |
| - | ---- | ------------------ | ----- |
| 1 | Búsqueda (Pádel, Norte, 15/03/2027) como `ciudadana.empadronada` | La franja de las 08:00 de «Pista de pádel 2» está libre | HU-01 |
| 2 | `POST /api/solicitudes-franjas-fijas` como `representante.norte` (lunes, 08:00–09:00, temporada 2026-2027) | 201, estado `pendiente` | HU-05 |
| 3 | `POST /api/solicitudes-franjas-fijas/{id}/aprobacion` como `tecnico` | 200, estado `aprobada` | HU-05 |
| 4 | Búsqueda otra vez | La franja de las 08:00 ya no sale; la de las 09:00 sí | HU-01 |
| 5 | `POST /api/reservas` de esa franja como `ciudadana.empadronada` | 422 `FRANJA_BLOQUEADA_CLUB`; no se crea ninguna reserva de esa franja | HU-02 |
| 6 | `POST /api/validaciones-acceso` con `personal.oeste` y el código de Este | 403 `QR_OTRO_CENTRO`, **sin** la extensión `reserva`, con `validacionId` | HU-06 |
| 7 | Lectura de la base | Un intento más de esa reserva: `aceptada = false`, `motivo_rechazo = QR_OTRO_CENTRO`, `reserva_id` de la reserva y `personal_id` de `personal.oeste` | HU-06 |
| 8 | `POST /api/validaciones-acceso` como `ciudadana.empadronada` | 403 `ROL_NO_PERMITIDO`; no se registra ningún intento | HU-06 |

**Resultado observado**: `correcto` (6 s 999 ms), en la misma ejecución:

```
correcto SI18.Api.Tests.Comun.RecorridosIntegradosTests.R2_franja_fija_aprobada_bloquea_la_reserva_el_personal_de_otro_centro_y_una_ciudadana_no_validan (6s 999ms)
```

**Nota sobre un error de la propia prueba, corregido durante la ejecución**: la primera versión esperaba 422
en el paso 6. La API responde 403 para `QR_OTRO_CENTRO` (`ManejadorErrores.cs:40`, como el catálogo de
`000-core`); se corrigió la prueba, no el código.

## Regresión

Sobre el commit `25681ee` (los cambios posteriores de esta rama son solo de documentos):

| Comando | Resultado |
| ------- | --------- |
| `dotnet build SI18.slnx` | Correcta: 0 advertencias, 0 errores |
| `dotnet test SI18.slnx --no-build` | 408 de 408 correctas (406 anteriores + R1 y R2), 0 errores, 0 omitidas, 39 s |
| `dotnet test SI18.slnx --no-build --filter-trait "Categoria=Limites"` | 167 de 167 correctas, 0 errores, 0 omitidas |
| `dotnet test SI18.slnx --no-build --filter-class "*RecorridosIntegradosTests"` | 2 de 2 correctas |

Con la selección de la tercera fila, `SI18.Dominio.Tests` no ejecuta ninguna prueba y la herramienta termina
con el código 8 («ninguna prueba»); no es un fallo.

## Limitaciones

- Las pruebas no ejercen el cliente Blazor (véase arriba).
- La franja fija aprobada en R2 vale toda la temporada 2026-2027 para los lunes de 08:00 a 09:00 de
  «Pista de pádel 2» en Norte. Con `SI18_TEST_CONNECTION` apuntando a una base reutilizada, una segunda
  ejecución chocaría con esa franja ya aprobada; con Testcontainers (lo habitual) cada ejecución parte de
  una base nueva.
