# Plan de implementación: 002 · Reservar y pagar una pista (HU-02)

**Rama**: `feature/002-reservar-pagar-plan` | **Fecha**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/002-reservar-pagar/spec.md` (con las aclaraciones del
2026-11-25), la base técnica común de [`000-core/plan.md`](../000-core/plan.md) y lo que spec-001 ya
dejó en `develop` ([`001-buscar-pistas/plan.md`](../001-buscar-pistas/plan.md)).

## Resumen

HU-02 permite **reservar una franja libre y pagarla en el momento**. No crea tablas ni migraciones:
`reservas`, `pagos`, el índice `ux_reservas_activa`, la máquina de estados, la pasarela simulada, el
generador del código de acceso y el barrido ya están en 000-core. HU-02 los compone:

- **Backend** (módulo `Reservas`, dueño: Saul):
  - cinco endpoints en `/api/reservas`: resumen, crear, pagar, ver una y «Mis reservas» (E1–E4, E6);
  - un `ServicioReservar` que sigue el «Flujo de una reserva» de 000-core: disponibilidad y precio de
    HU-01 (`IConsultaDisponibilidad`), caducidad perezosa, índice único → `FRANJA_OCUPADA`, pasarela y
    código de acceso (E3, E4);
  - bloqueo de fila (`FOR UPDATE`) en toda escritura sobre una reserva ya guardada, para que nunca se
    cobre dos veces ni se cruce un pago con el barrido o un cierre (E5);
  - `ConsultaReservas` (definitiva de `IConsultaReservas`) y **retirada de `ConsultaReservasProvisional`
    y de su línea en `RegistroDisponibilidad`** (E7);
  - `ServicioReservas` (`IServicioReservas`): `CancelarPorUsuario` y `AplicarCierre`, para HU-03 y
    HU-07 de C, dentro de la transacción del llamador (E8).
- **Cliente**: resumen, pago con hora límite fija, resultado con QR, «Mis reservas» y el componente
  `EnlaceReservar`, que pone el botón «Reservar» en `/buscar-pistas` con un cambio de una línea en la
  pantalla de Artur (E10, E11).

Las decisiones y sus motivos están en [research.md](./research.md) (E1–E14).

## Contexto técnico

Todo es igual que en [000-core/plan.md](../000-core/plan.md#contexto-técnico); no hay dependencias nuevas.

- **Lenguaje y versión**: C# 14 sobre .NET 10 (`net10.0`).
- **Dependencias principales**: ASP.NET Core Minimal API, EF Core 10 con Npgsql, Blazor WebAssembly,
  MudBlazor 8.x y QRCoder (ya referenciado en el cliente, R14). Ningún paquete nuevo.
- **Almacenamiento**: PostgreSQL 17. **Sin migraciones nuevas** ([data-model.md](./data-model.md)).
- **Pruebas**: xUnit v3, `FakeTimeProvider` y `FabricaApi` con Testcontainers (000-core, R15).
- **Plataforma**: navegadores actuales (WebAssembly) y una API en Kestrel.
- **Tipo de proyecto**: aplicación web (SPA servida por la API).
- **Objetivos de rendimiento**: < 500 ms p95 por petición (000-core); CE-001 (< 2 min para reservar y
  pagar) depende sobre todo de la persona (E14).
- **Restricciones**:
  - reglas en el backend; exclusión mutua garantizada por la base de datos (principio III);
  - hora de `TimeProvider`, leída una vez por operación, y `HorarioMadrid` (principios II y IX);
  - importes `decimal`; valores de `ValoresNegocio` (`TiempoBloqueoPago`, `DuracionFranja`), nunca
    literales;
  - datos de tarjeta: ni se guardan ni se registran (R9);
  - WCAG 2.1 AA;
  - ninguna escritura en tablas de otros módulos.
- **Escala y alcance**: 4 centros, 16 pistas reservables, 14 franjas por pista y día, hasta 8 días
  reservables. Cuatro pantallas, un componente y cinco endpoints.

## Comprobación de la constitución

*Puerta: debe superarse antes de la Fase 0 y volver a comprobarse tras la Fase 1.*

| Principio | Cómo lo cumple el diseño | Antes de F0 | Tras F1 |
| --------- | ------------------------ | ----------- | ------- |
| I. WCAG 2.1 AA | Botón «Reservar» con nombre accesible completo; campo de tarjeta con etiqueta visible y error asociado; resultado anunciado con `MensajeAccesible` y foco a su encabezado; estados escritos en texto; QR con descripción y código en texto; sin cuenta atrás; comprobación de quickstart §5 en el PR (E10, E11) | ✅ | ✅ |
| II. Lógica probada | La máquina de estados ya está probada en 000-core. HU-02 añade pruebas de integración de todos los escenarios y de los límites (7/8 días, inicio exacto, 9:59/10:00, solape del cierre, cambio de hora), de la concurrencia (20 a la vez, doble pago) y de `IServicioReservas`. Los límites de 10 min se preparan sin adelantar el reloj para no disparar el barrido (E13) | ✅ | ✅ |
| III. Reglas en el backend | Disponibilidad y precio los decide HU-01 (`IConsultaDisponibilidad`); transiciones, la entidad `Reserva`; la unicidad, `ux_reservas_activa`; el doble cobro, `FOR UPDATE`. El cliente no calcula ni la hora límite ni el orden de la lista (E3–E6) | ✅ | ✅ |
| IV. Errores coherentes | Solo códigos del catálogo de 000-core; **ninguno nuevo**. `23505` se traduce a `FRANJA_OCUPADA` dentro de Reservas. Los rechazos guardan antes el cambio de estado y explican lo ocurrido (E3, E4) | ✅ | ✅ |
| V. Sin secretos | Nada nuevo que configurar; el número de tarjeta no se registra (E4) | ✅ | ✅ |
| VI. Datos ficticios | Usa la siembra de 000-core y tarjetas ficticias de R9; las pruebas insertan sus reservas | ✅ | ✅ |
| VII. Base común en 000-core | Sin entidades, estados, valores, índices ni códigos nuevos. Implementa `IConsultaReservas` e `IServicioReservas` con las firmas de 000-core, sin cambiarlas ([contracts/modulos.md](./contracts/modulos.md)) | ✅ | ✅ |
| VIII. Cero alcance fantasma | Solo lo que pide la spec: sin acción de abandonar el pago, sin límite por persona, sin paginación, sin cuenta atrás, sin cancelar (HU-03). El resumen previo (E2) es la forma más simple de cumplir RF-003 sin duplicar cálculos. Se borra la provisional | ✅ | ✅ |
| IX. Idioma, moneda y zona | Textos en español; fechas, horas y hora límite calculadas en Madrid por la API; importes con `FormatoEs.Importe`; cambio de hora probado | ✅ | ✅ |
| Pila y seguridad | La pila de 000-core, sin cambios; política `EsCiudadanoORepresentante`; recurso ajeno → 404 sin revelar que existe | ✅ | ✅ |
| Colaboración | Escribe en `*/Reservas` y en `BarridoCaducidades` (de Saul). Toca código de Artur **solo** en lo pactado: la retirada de la provisional (D5 de spec-001) y una línea en `BuscarPistas.razor` (aclaración de spec-001), con su revisión. `Program.cs`: las dos líneas de la convención. A C se le avisa de `IServicioReservas` (ver «Puntos a coordinar») | ✅ | ✅ |

**Resultado**: se supera sin infracciones. «Seguimiento de la complejidad» queda vacío.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/002-reservar-pagar/
├── spec.md
├── plan.md              # este fichero
├── research.md          # Fase 0: decisiones E1–E14
├── data-model.md        # Fase 1: qué lee y escribe HU-02, uso de la máquina de estados y DTO (sin tablas nuevas)
├── quickstart.md        # Fase 1: validación de principio a fin
├── contracts/
│   ├── api-reservas.openapi.yaml   # /api/reservas: resumen, crear, pagar, ver una, mis reservas
│   └── modulos.md                  # qué interfaces de 000-core implementa, retira y consume
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks; no la crea este comando)
```

### Código fuente

```text
fuentes/
├── src/
│   ├── SI18.Contratos/Reservas/                        # E12 (nuevo)
│   │   ├── EstadoReserva.cs
│   │   ├── PeticionReserva.cs
│   │   ├── PeticionPago.cs
│   │   ├── ResumenReservaDto.cs
│   │   └── ReservaDto.cs
│   ├── SI18.Api/
│   │   ├── Program.cs                                  # + AddReservas() y MapReservas() (E9)
│   │   ├── Infraestructura/BarridoCaducidades.cs       # FOR UPDATE SKIP LOCKED (E5; dueño: Saul)
│   │   └── Modulos/
│   │       ├── Reservas/
│   │       │   ├── RegistroReservas.cs                 # AddReservas / MapReservas (E9)
│   │       │   ├── EndpointsReservas.cs                # /api/reservas (E1)
│   │       │   ├── ServicioReservar.cs                 # resumen, crear, pagar, mis reservas (E2–E4, E6)
│   │       │   ├── CargaReservas.cs                    # CargarParaModificar: FOR UPDATE + Pago (E5)
│   │       │   ├── ConsultaReservas.cs                 # IConsultaReservas definitiva (E7)
│   │       │   └── ServicioReservas.cs                 # IServicioReservas para HU-03 y HU-07 (E8)
│   │       └── Disponibilidad/                         # de Artur, solo lo pactado en D5 de spec-001
│   │           ├── RegistroDisponibilidad.cs           # − la línea TryAddScoped<IConsultaReservas, …Provisional>
│   │           └── Provisional/ConsultaReservasProvisional.cs   # se BORRA
│   └── SI18.Cliente/
│       ├── Layout/MainLayout.razor                     # + «Mis reservas» en el hueco de Reservas
│       ├── Disponibilidad/BuscarPistas.razor           # de Artur: + 1 línea <EnlaceReservar …/> y su @using (E10) ⚠ coordinar
│       └── Reservas/                                   # E10, E11 (nuevo)
│           ├── EnlaceReservar.razor
│           ├── NuevaReserva.razor                      # /reservas/nueva
│           ├── PagarReserva.razor                      # /reservas/{id}/pago
│           ├── MisReservas.razor                       # /mis-reservas
│           └── CodigoAcceso.razor                      # QR (QRCoder, SVG) + código en texto
└── tests/SI18.Api.Tests/
    ├── Reservas/                                       # E13 (nuevo)
    │   ├── DatosReservas.cs
    │   ├── CrearReservaTests.cs
    │   ├── ReservaConcurrenteTests.cs
    │   ├── PagarReservaTests.cs
    │   ├── MisReservasTests.cs
    │   ├── ConsultaReservasTests.cs                    # las dos pruebas movidas desde ConsultasProvisionalesTests
    │   ├── ServicioReservasTests.cs
    │   ├── ReservarComoEnLaBusquedaTests.cs
    │   └── EstadoReservaEspejoTests.cs
    └── Disponibilidad/ConsultasProvisionalesTests.cs   # − las dos pruebas de IConsultaReservas (E7)
```

**Decisión de estructura**: la de 000-core, sin proyectos nuevos. El código nuevo va en las carpetas
`Reservas` de cada proyecto (dueño: Saul). Fuera de ellas se toca solo:

- `Program.cs`: las dos líneas de la convención de 000-core;
- `MainLayout.razor`: la entrada en el hueco reservado para Reservas (T056 de 000-core);
- `BarridoCaducidades.cs`: es de Saul (T068 de 000-core);
- tres ficheros de Artur, solo con lo ya pactado y con su revisión (ver «Puntos a coordinar»).

No se añaden pruebas en `SI18.Dominio.Tests`: las reglas de dominio que usa HU-02 ya están probadas.

## Flujo de una reserva y su pago

Concreta el flujo de 000-core (plan.md, «Flujo de una reserva y su caducidad») con las rutas y
cuerpos de esta spec.

```text
/buscar-pistas ──[Reservar] (EnlaceReservar)──► /reservas/nueva?instalacionId&inicioUtc
GET /api/reservas/resumen?instalacionId&inicioUtc                          [EsCiudadanoORepresentante]
   ├─ IConsultaDisponibilidad.MotivoNoDisponible → lanza 400/404 · motivo → 409/422     (E2 = E3 pasos 1–2)
   └─ 200 ResumenReservaDto (importe = IConsultaDisponibilidad.Precio)
POST /api/reservas { instalacionId, inicioUtc, importeMostrado }
   ├─ 400 PETICION_INVALIDA · MotivoNoDisponible → 400/404/409/422
   ├─ Precio ≠ importeMostrado                              → 409 PRECIO_CAMBIADO
   ├─ BEGIN · FOR UPDATE pendientes NO vigentes de la franja → Caducar (pago → fallido)   (R8.2)
   ├─ INSERT reserva(pendiente_pago) + pago(pendiente)
   └─ COMMIT ── 23505 ux_reservas_activa ──► 409 FRANJA_OCUPADA                         (R7)
   → 201 ReservaDto (horaLimitePago) ──► /reservas/{id}/pago
POST /api/reservas/{id}/pago { numeroTarjeta }
   ├─ ≠ 16 dígitos                                          → 400 (sin pasarela, sin cambios)
   ├─ BEGIN · FOR UPDATE reserva del usuario + pago          → 404 si no es suya
   ├─ estado ≠ pendiente_pago                               → 409 TRANSICION_NO_PERMITIDA
   ├─ !EstaVigente(ahora): Caducar · COMMIT                 → 409 RESERVA_CADUCADA     (R8.3)
   ├─ IPasarelaPago.Cobrar
   │    ok → Confirmar(GeneradorCodigoAcceso.Nuevo()) · COMMIT → 200 ReservaDto(codigoAcceso)
   │    ko → Caducar · COMMIT                               → 422 PAGO_RECHAZADO
BarridoCaducidades (60 s): FOR UPDATE SKIP LOCKED · Caducar las pendientes no vigentes  (R8.4)

HU-03 / HU-07 (C) ──► BEGIN ──► IServicioReservas.CancelarPorUsuario / AplicarCierre (FOR UPDATE, sin SaveChanges)
                      ──► devolución + aviso ──► SaveChanges ──► COMMIT
HU-01 (Artur) ──► IConsultaReservas.Activas ──► ConsultaReservas (antes: ConsultaReservasProvisional)
```

## Puntos a coordinar

| # | Con quién | Qué | Propuesta | Cuándo |
| - | --------- | --- | --------- | ------ |
| C1 | **Artur** (dueño de `/buscar-pistas`) | Botón «Reservar» en cada franja (aclaración de spec-001, RF-003) | Componente `EnlaceReservar` de Reservas; en `BuscarPistas.razor` solo `@using SI18.Cliente.Reservas` y `<EnlaceReservar Pista="pista" Franja="franja" Fecha="_resultado.Fecha" />` dentro del `<li>`. Sin cambios en DTO, endpoint ni pruebas de HU-01. Lo puede hacer Saul en el PR de implementación de 002 o Artur en el suyo; en los dos casos Artur revisa (E10) | Antes de implementar la pantalla de resumen |
| C2 | **Artur** (dueño de `Disponibilidad`) | Retirada de la provisional | Borrar `ConsultaReservasProvisional.cs`, su línea en `RegistroDisponibilidad.cs` y mover sus dos pruebas a `Reservas/ConsultaReservasTests.cs`, como pactó D5 de spec-001. Artur revisa el PR (E7) | En el mismo PR que publica `ConsultaReservas` |
| C3 | **C** (HU-03, HU-07) | Uso de `CancelarPorUsuario` y `AplicarCierre` | Abrir la transacción antes de llamar, no esperar `SaveChanges` de Reservas, y guardar devolución y aviso en la misma transacción. `AplicarCierre` caduca también las pendientes guardadas ya no vigentes; C decide a quién avisa. Contrato en [contracts/modulos.md](./contracts/modulos.md) (E8) | Al publicar este plan (aviso al grupo) |
| C4 | **C** (HU-03) | Botón «Cancelar» en «Mis reservas» | `MisReservas.razor` deja marcado el hueco; HU-03 lo rellena llamando a `/api/reservas/{id}/cancelacion` | Cuando se planifique spec-003 |

## Dependencias y riesgos

| Dependencia o riesgo | Mitigación |
| -------------------- | ---------- |
| El cambio de `BuscarPistas.razor` (C1) se retrasa o choca con trabajo de Artur en spec-005 | Es una línea dentro del `<li>` y no depende de nada más. Hasta entonces, la reserva se prueba abriendo `/reservas/nueva?…` (quickstart §4) |
| Al quitar la provisional cambia el resultado de la búsqueda | `ConsultaReservas` usa la misma consulta; las dos pruebas que la fijaban se mueven sin cambios y las de búsqueda de spec-001 no se tocan (E7; CE-007) |
| Doble cobro o choque entre pago, barrido y cierre | `FOR UPDATE` en toda escritura sobre una reserva guardada; `SKIP LOCKED` en el barrido; prueba de dos pagos simultáneos con una pasarela que cuenta llamadas (E5, E13) |
| C llama a `IServicioReservas` sin transacción o guarda en dos pasos | La implementación lo exige (`InvalidOperationException`) y el contrato lo explica con un ejemplo (C3) |
| Las pruebas de 10 minutos disparan el barrido al adelantar el `FakeTimeProvider` | Se preparan con `creada_en_utc` en el pasado, sin adelantar el reloj (E13) |
| Divergencia entre lo que se muestra y lo que se cobra | El importe sale siempre de `IConsultaDisponibilidad.Precio` y se compara con el mostrado; prueba de extremo a extremo contra la búsqueda (CE-005) |
| El número de tarjeta acaba en un registro | No se registra el cuerpo de la petición; `PeticionPago.ToString()` lo oculta; revisión en el PR (E4) |
| Cambio de hora | Horas y hora límite con `HorarioMadrid`; prueba del 28/03/2027 (E13) |

## Seguimiento de la complejidad

No aplica: no hay infracciones de la constitución que justificar.
