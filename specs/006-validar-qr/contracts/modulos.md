# Contratos entre módulos de HU-06

HU-06 **no define interfaces nuevas ni cambia ninguna firma** de
[`000-core/contracts/modulos.md`](../../000-core/contracts/modulos.md). Todo lo que necesita está dentro
del módulo `Reservas` (dueño: Saul), que es el dueño de `reservas` y de `validaciones_acceso`.

## Lo que usa de 000-core (sin cambiarlo)

| Pieza                                 | Para qué                                                                   |
| ------------------------------------- | -------------------------------------------------------------------------- |
| `ReglaAcceso.MotivoRechazo(reserva, ahora)` | Desconocido, no confirmada y fuera de ventana, en ese orden (research.md V3) |
| `ValidacionAcceso` (entidad y tabla)  | Registrar cada intento (V4)                                                |
| `CodigosError.Qr*` y `ManejadorErrores.Crear` | Motivos, textos y estado HTTP de cada rechazo (V2)                   |
| `ValoresNegocio.MargenQrAntesDelInicio` | Solo a través de `ReglaAcceso`; HU-06 no lo lee directamente             |
| `HorarioMadrid.ALocal`                | Fechas y horas de Madrid de la respuesta (V8)                              |
| `IUsuarioActual` (`Id`, `CentroId`)   | Quién valida y cuál es su centro (V7)                                      |
| `Politicas.EsPersonal`                | Solo el personal de centro (V7)                                            |

## Lo que **no** usa, y por qué

| Pieza                                  | Motivo                                                                    |
| -------------------------------------- | ------------------------------------------------------------------------- |
| `PermisosCentro.ExigirMismoCentro`     | Lanza `OTRO_CENTRO` y corta la petición sin registrar; HU-06 necesita `QR_OTRO_CENTRO` y el registro (V3) |
| `IServicioReservas`, `IConsultaReservas` | Son para otros módulos; HU-06 está dentro de Reservas y lee con el `Si18DbContext` |

## Lo que ofrece a otras historias

Nada nuevo. HU-03 (cancelar) y HU-07 (cierre) **no tienen que hacer nada** para invalidar un código:
una reserva que pasa a `cancelada` por `IServicioReservas.CancelarPorUsuario` o `AplicarCierre`
(spec-002, RF-019a) se rechaza con `QR_RESERVA_NO_CONFIRMADA` en la siguiente validación (RF-012). Se
avisa a C de esta dependencia (constitución, «Acuerdos de colaboración»).

> **Alcance (26/11/2026)**: HU-03 y HU-07 quedaron **fuera del alcance comprometido** por acuerdo con el
> profesor (el grupo son dos personas; ver [docs/agrupamiento.md](../../../docs/agrupamiento.md)). El aviso a «C» del párrafo anterior ya no tiene destinatario. Se conserva
> como base para trabajo futuro: no hay nadie asignado y no es trabajo pendiente.

## Cambios en código común (puntos a coordinar, revisión de @ara139-ua)

| Fichero (común)                           | Cambio                                                          | Punto |
| ----------------------------------------- | --------------------------------------------------------------- | ----- |
| `SI18.Cliente/Layout/MainLayout.razor`    | «Validar acceso» → `/validar-acceso` en el hueco del personal de centro (una línea dentro de `<AuthorizeView Roles="personal_centro">`) | C1 |
| `SI18.Cliente/Comun/ErrorApi.cs` y `ClienteApi.cs` | `ErrorApi.Extensiones` (`IReadOnlyDictionary<string, JsonElement>`), rellenado al leer el Problem Details. Aditivo: no cambia nada de lo que ya usan las demás pantallas | C2 |

Propuesta opcional, **no incluida** en este plan (C3): llevar la comprobación de centro a
`ReglaAcceso` en 000-core. Cambiaría una firma de 000-core para un solo consumidor (principio VIII).

## Grafo de dependencias

Sin cambios respecto al permitido por 000-core: HU-06 está dentro de `Reservas` y solo depende de `Comun`.
