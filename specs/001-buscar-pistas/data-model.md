# Modelo de datos: 001 · Buscar pistas libres

**HU-01 no añade tablas, columnas, índices ni migraciones.** Todo lo que lee está en el modelo común
de [`000-core/data-model.md`](../000-core/data-model.md) (principio VII). Este documento recoge solo:

1. qué lee HU-01 y por qué vía;
2. el modelo de lectura que devuelve la búsqueda (no se almacena).

## 1. Qué lee y por qué vía

| Dato (tabla de 000-core)                 | Dueño            | Vía de lectura en HU-01                                                       | Para qué                                     |
| ---------------------------------------- | ---------------- | ----------------------------------------------------------------------------- | -------------------------------------------- |
| `centros`, `deportes`                    | Comun            | `Si18DbContext`, `AsNoTracking`, y en el cliente `GET /api/centros` y `/api/deportes` | Validar filtros (404) y rellenarlos          |
| `instalaciones`, `instalacion_deportes`  | Comun            | `Si18DbContext`, `AsNoTracking`                                               | Pistas candidatas (RF-006)                   |
| `tarifas`                                | Comun            | `Si18DbContext`, `AsNoTracking`, una vez por petición                         | Precio (RF-012)                              |
| `reservas`                               | Reservas (HU-02) | `IConsultaReservas.Activas` (provisional, D5)                                 | Franjas ocupadas                             |
| `cierres_mantenimiento`                  | Cancelaciones (HU-07) | `IConsultaCierres.Cierres` (provisional, D5)                             | Franjas cerradas                             |
| `solicitudes_franjas_fijas`, `temporadas`| Disponibilidad (HU-05) | `IConsultaFranjasFijas.FranjasBloqueadas` (provisional, D5)            | Franjas bloqueadas por club                  |
| Tarifa del usuario                       | Comun (sesión)   | `IUsuarioActual.TipoTarifa` (claim calculado al iniciar sesión, RF-007 de 000-core) | Precio (RF-012)                         |

HU-01 **no escribe** en ninguna tabla.

## 2. Pistas candidatas

Una instalación entra en la búsqueda si y solo si:

- `instalaciones.centro_id = centroId`;
- `instalaciones.reservable_por_franjas = true` (solo pistas de tenis y pádel, RF-012 de 000-core);
- existe `instalacion_deportes (instalacion_id, deporte_id = deporteId)`.

`totalPistas` = número de instalaciones candidatas. Una candidata sin ninguna franja libre no
aparece en `pistas` (RF-010 de la spec).

## 3. Franja libre

Es la `Franja` de 000-core (data-model.md § Franja), calculada con `CalculadoraFranjas` para
`(pista, fecha)`, para la que `ReglaDisponibilidad.MotivoNoDisponible(franja, ahoraUtc, …)` devuelve
`null`. Las entradas de la regla salen de:

| Parámetro de `ReglaDisponibilidad` | Valor en HU-01                                                                            |
| ---------------------------------- | ----------------------------------------------------------------------------------------- |
| `ahoraUtc`                         | `TimeProvider.GetUtcNow()` en el momento de la petición                                   |
| `reservablePorFranjas`             | `true` (solo hay candidatas reservables; se pasa el valor de la instalación igualmente)    |
| `cierres`                          | `IConsultaCierres.Cierres(pista, inicioDíaUtc, inicioDíaSiguienteUtc)`                    |
| `bloqueosClub`                     | `IConsultaFranjasFijas.FranjasBloqueadas(pista, fecha, fecha)`                            |
| `iniciosConReservaActiva`          | `IConsultaReservas.Activas(pista, inicioDíaUtc, inicioDíaSiguienteUtc)` → `InicioUtc`      |

`inicioDíaUtc = HorarioMadrid.AUtc(fecha, 00:00)` e `inicioDíaSiguienteUtc = HorarioMadrid.AUtc(fecha + 1, 00:00)`
(un día de Madrid, también en los días de cambio de hora).

Precio de cada franja: `CalculadoraPrecio.Precio(pista.Tipo, usuario.TipoTarifa, tarifas)`.

## 4. Modelo de lectura de la búsqueda (no se almacena)

DTO en `SI18.Contratos/Disponibilidad/`, compartidos por la API y el cliente. JSON según las
convenciones de 000-core (contracts/README.md): camelCase, enumerados en `snake_case`, instantes en UTC
ISO 8601, horas de pared `HH:mm` y fechas `yyyy-MM-dd`.

### `ResultadoBusquedaDto`

| Campo         | Tipo                      | Regla                                                                   |
| ------------- | ------------------------- | ----------------------------------------------------------------------- |
| `fecha`       | fecha (`yyyy-MM-dd`)      | La buscada (fecha de Madrid)                                            |
| `centroId`    | entero                    | El buscado                                                              |
| `deporteId`   | entero                    | El buscado                                                              |
| `totalPistas` | entero ≥ 0                | Pistas candidatas (§ 2), tengan o no franjas libres                     |
| `pistas`      | lista de `PistaLibreDto`  | Solo pistas con ≥ 1 franja libre, ordenadas por `nombre` (RF-009)       |

### `PistaLibreDto`

| Campo           | Tipo                       | Regla                                                    |
| --------------- | -------------------------- | -------------------------------------------------------- |
| `instalacionId` | entero                     | Identifica la pista (la usará HU-02 para reservar)       |
| `nombre`        | texto                      | «Pista de pádel 1»                                        |
| `tipo`          | `TipoInstalacion`          | `pista_tenis` o `pista_padel`                             |
| `franjas`       | lista de `FranjaLibreDto`  | ≥ 1, ordenadas por `inicioUtc`                            |

### `FranjaLibreDto`

| Campo        | Tipo              | Regla                                                                   |
| ------------ | ----------------- | ----------------------------------------------------------------------- |
| `inicioUtc`  | instante UTC      | Identifica la franja junto con `instalacionId` (la usará HU-02)         |
| `finUtc`     | instante UTC      | `inicioUtc` + 1 h                                                        |
| `horaInicio` | `HH:mm`           | Hora de pared de Madrid («18:00»)                                       |
| `horaFin`    | `HH:mm`           | Hora de pared de Madrid («19:00»)                                       |
| `precio`     | decimal (2 dec.)  | Precio para la tarifa de quien busca; el cliente lo muestra con `FormatoEs.Importe` |

`horaInicio` y `horaFin` los calcula la API con `HorarioMadrid`. Así el cliente no tiene que convertir
zonas horarias para mostrar la hora (principio III).
