# T058 · revisión del marcado de los dos estados nuevos del cliente (spec-002)

> **Excepción justificada, no comprobación manual.** El 26/11/2026 se revisó **leyendo el código** del
> cliente (commit `a87cf97`). **No se arrancó la aplicación** ni se usó NVDA, Lighthouse ni axe sobre estos
> dos estados. Motivo: la comprobación necesita una franja que empiece en los minutos siguientes y los
> estados solo existen entre el inicio de la franja y «creación + 10 min»; los centros cierran a las 22:00
> (última franja 21:00–22:00) y la entrega es antes de las 00:00 del 26/11/2026, así que no hubo ventana
> horaria (véase [verificacion-integrada.md](../verificacion-integrada.md)). La comprobación manual de
> quickstart §5 (principio I) sobre estos dos estados **no se hizo** y queda como trabajo pendiente.

El **comportamiento** (la hora límite, el 422 `FRANJA_YA_EMPEZADA`, la caducidad sin cobro y el 409 del
segundo intento) lo cubren las pruebas de T056 y T057 (`PagarReservaTests.RF009a_…`, `ReservaTests`,
`CrearReservaTests.RF008a_…`) y, de extremo a extremo por HTTP, `RecorridosIntegradosTests`. Lo que se
revisó aquí es solo el **marcado**.

## Estado A · «Mis reservas» con una pendiente pasada de plazo (`MisReservas.razor`)

| Qué se miró | Lo que dice el código | Resultado de la revisión |
| ----------- | --------------------- | ------------------------ |
| Sin «Pagar» | La rama `PendientePago && PlazoTerminado(reserva)` solo escribe `<p>El plazo para pagar terminó a las …</p>`; no hay `MudButton` | Cumple |
| Estado por texto | Chip con `TextosReservas.Estado(…)` («Pendiente de pago»); el color solo lo refuerza | Cumple |
| Orden y lista | `<ul role="list">` con `<li role="listitem">`, título `h2` por reserva | Cumple |
| 320 px | No hay anchos fijos en esa rama | No verificado a la vista |

## Estado B · `/reservas/{id}/pago` con `FRANJA_YA_EMPEZADA` (`PagarReserva.razor`)

| Qué se miró | Lo que dice el código | Resultado de la revisión |
| ----------- | --------------------- | ------------------------ |
| Sin formulario | El `<form>` solo está en `Resultado.Pendiente`; con `YaEmpezada` se pinta la sección de resultado | Cumple |
| Foco al encabezado | `MostrarResultado` pone `_enfocarResultado`; `OnAfterRenderAsync` llama a `_encabezado.FocusAsync()`; el `h2` tiene `tabindex="-1"` y la sección `aria-labelledby` | Cumple |
| Encabezado | «La franja ya ha empezado» (`Encabezado(Resultado.YaEmpezada)`) | Cumple |
| `role` y `aria-live` | `MensajeAccesible` es un `div role="status" aria-live="polite" aria-atomic="true"` siempre en el DOM | Cumple |
| Mensaje anunciado una vez | El texto del catálogo (`error.Detalle`) se escribe una sola vez, en `MensajeAccesible`; el encabezado lleva otro texto | Cumple en el marcado. **No verificado con lector de pantalla**: `MudAlert` puede llevar su propio `role="alert"` dentro de la región `status`, lo que podría anunciarlo dos veces; es el mismo patrón que los demás resultados (confirmada, caducada, rechazada) |
| Texto e icono | `MudAlert` con `Severity.Warning`: icono de la severidad y texto | Cumple |
| Enlace alcanzable | `MudButton Href="/buscar-pistas"` («Volver a buscar pistas») se pinta como `<a>`, tabulable | Cumple |
| Segundo intento | No hay formulario desde el que reintentar | Cumple |

## Lo que queda sin hacer

- NVDA sobre los dos estados (anuncio una sola vez del mensaje y del encabezado).
- Lighthouse (Accesibilidad ≥ 95) y axe sobre `/mis-reservas` y `/reservas/{id}/pago` en esos estados.
- Teclado, foco visible y 320 px de estos dos estados.
- Escenario R19 de quickstart §3 a mano.
