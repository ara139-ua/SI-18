---

description: "Lista de tareas de 002 · Reservar y pagar una pista (HU-02)"
---

# Tareas: 002 · Reservar y pagar una pista (HU-02)

**Entrada**: documentos de diseño de `specs/002-reservar-pagar/` ([plan.md](./plan.md),
[spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md),
[contracts/](./contracts/modulos.md), [quickstart.md](./quickstart.md)).

**Pruebas**: incluidas, porque las exige la constitución (principio II).

**Responsable**: Saul (módulo `Reservas`). Todas las tareas son suyas. Tocan código de Artur solo dos,
las dos pactadas y con su revisión en el PR:

- **T009** (retirada de `ConsultaReservasProvisional`, research.md E7; D5 de spec-001);
- **T022** (`EnlaceReservar` en `BuscarPistas.razor`, research.md E10; Artur ha confirmado que lo pone Saul).

**Organización**: por historia de usuario de la spec (US1–US6), más una fase para `IServicioReservas`
(RF-019a), que no tiene historia propia. Todas las rutas de código parten de `fuentes/`. Commits con el
formato de la constitución: `spec-002 T00X: …`.

## Formato: `- [ ] [ID] [P?] [Historia?] Descripción con ruta`

- **[P]**: se puede hacer en paralelo (ficheros distintos y sin dependencias pendientes).
- **[USn]**: historia de usuario de la spec a la que pertenece.

## Datos de las pruebas de integración

`FabricaApi` comparte una base de datos y un reloj entre todas las pruebas de la colección. El reloj
empieza el **lunes 21/12/2026 a las 18:00 de Madrid**, pero **otras pruebas lo adelantan**
(`BarridoCaducidadesTests` 10 min, `AvisosTests` 1 min) y el orden de las clases no está fijado. Por eso:

- toda prueba lee `ahora` de `fabrica.Reloj` y construye sus instantes a partir de él; nunca supone
  que son las 18:00 en punto;
- ninguna prueba de esta spec adelanta `fabrica.Reloj`: adelantarlo dispara el `BarridoCaducidades`
  registrado, que caduca las pendientes de **todas** las pruebas (research.md E13);
- los 10 minutos se preparan insertando la reserva con `CreadaEnUtc = ahora − 10 min` (o `− 9 min 59 s`);
- los límites que necesitan otra hora (inicio exacto de la franja, cambio de hora, rechazo a las 9:59 y
  nuevo intento a las 10:01) se prueban con `DatosReservas.ConServicio(ahoraUtc, …)` (T011), que usa un
  reloj propio, como `DatosDisponibilidad.ConServicio` de spec-001.

Para no pisarse con spec-001 (Norte pádel 21–28/12, Sur tenis 23/12, Este pádel 24/12, Oeste enero de
2027) ni con 000-core (15/01/2027 y 10/02/2027), cada clase de esta spec usa su propia pista y sus
fechas, anotadas en cada tarea. `representante.norte@si18.test` lo usa **solo** `MisReservasTests`,
para que su lista no dependa de otras pruebas.

Tarjetas ficticias (R9 de 000-core): `4000000000000001` se acepta, `4000000000000002` se rechaza.

---

## Fase 1: Setup (entrada del módulo y contratos)

**Propósito**: que el módulo `Reservas` exista en la API con su registro y sus DTO.

- [X] T001 Crear `src/SI18.Api/Modulos/Reservas/RegistroReservas.cs` con
  `AddReservas(this IServiceCollection)` y `MapReservas(this WebApplication)`, de momento vacíos, y
  añadir en `src/SI18.Api/Program.cs` **solo dos líneas** (research.md E9; convención de 000-core):
  `builder.Services.AddReservas();` justo después de `AddDisponibilidad()` y `app.MapReservas();` justo
  después de `MapDisponibilidad()`.
- [X] T002 [P] Crear los DTO en `src/SI18.Contratos/Reservas/`, uno por fichero, según
  [data-model.md § 3](./data-model.md) y [contracts/api-reservas.openapi.yaml](./contracts/api-reservas.openapi.yaml):
  - `EstadoReserva` (`PendientePago`, `Confirmada`, `Caducada`, `Cancelada`; JSON `pendiente_pago`,
    `confirmada`, `caducada`, `cancelada`), con los mismos nombres y orden que `SI18.Dominio.Comun.EstadoReserva`;
  - `PeticionReserva(int InstalacionId, DateTime InicioUtc, decimal ImporteMostrado)`: los tres
    «Obligatorio», `ImporteMostrado` «`> 0`» con «2 dec.»;
  - `PeticionPago(string NumeroTarjeta)`: «16 dígitos después de quitar los espacios. No se guarda ni
    se registra; `ToString()` no lo imprime» (redefinir `ToString()` para que devuelva
    `"PeticionPago { NumeroTarjeta = *** }"`);
  - `ResumenReservaDto(int InstalacionId, string Pista, string Centro, DateOnly Fecha, string HoraInicio, string HoraFin, DateTime InicioUtc, decimal Importe)`,
    con horas «`HH:mm`» de Madrid;
  - `ReservaDto(long Id, int InstalacionId, string Pista, string Centro, DateOnly Fecha, string HoraInicio, string HoraFin, DateTime InicioUtc, DateTime FinUtc, decimal Importe, EstadoReserva Estado, DateTime? LimitePagoUtc, string? HoraLimitePago, string? CodigoAcceso)`.
- [X] T003 [P] Crear `tests/SI18.Api.Tests/Reservas/EstadoReservaEspejoTests.cs` con el patrón de
  `Comun/EnumeracionesEspejoTests.cs`: mismos nombres y orden que el enumerado de dominio y mismo texto
  JSON con `OpcionesJson` (research.md E12). Depende de T002.

**Punto de control**: `dotnet build SI18.slnx` sin advertencias y `EstadoReservaEspejoTests` en verde.

---

## Fase 2: Fundacional (bloqueo de fila, consulta definitiva y datos de prueba)

**Propósito**: lo que necesitan todas las historias: cargar una reserva con bloqueo, la consulta
definitiva de reservas activas (y la retirada de la provisional) y los datos de prueba.

**⚠️ CRÍTICO**: ninguna historia puede empezar sin esta fase.

> **Mismo PR**: T007–T010 (consulta definitiva **y** retirada de la provisional) van **juntas en el
> mismo PR**, con **revisión de Artur**, como acordó D5 de spec-001. No se publica `ConsultaReservas`
> en un PR y se retira la provisional en otro: durante ese intervalo habría dos implementaciones de la
> misma regla registradas (research.md E7).

- [X] T004 Crear `src/SI18.Api/Modulos/Reservas/CargaReservas.cs` (clase `internal static`) con
  `Task<Reserva?> CargarParaModificar(Si18DbContext db, long reservaId, int? usuarioId, CancellationToken ct)`
  (research.md E5):
  - si `db.Database.CurrentTransaction` es nulo, lanza `InvalidOperationException` («Hay que abrir la
    transacción antes de cargar una reserva para modificarla»);
  - carga la reserva con `FromSql($"SELECT * FROM reservas WHERE id = {reservaId} FOR UPDATE")`
    (nombres de tabla y columnas de `ReservaConfig`), con `.Include(r => r.Pago)`, y filtra por
    `UsuarioId == usuarioId` si se pasa;
  - `AplicarCierre` (T045) y la caducidad al reservar (T026) cargan varias reservas: escriben su
    propia consulta `FOR UPDATE` con el mismo patrón y la misma comprobación de transacción.
- [X] T005 Cambiar `src/SI18.Api/Infraestructura/BarridoCaducidades.cs` (dueño: Saul, T068 de 000-core)
  a **`FOR UPDATE SKIP LOCKED`** (research.md E5; data-model.md § 2.3):
  - `EjecutarUnaVez` abre una transacción (`BeginTransactionAsync`) y carga las pendientes no vigentes
    con `FromSql($"SELECT * FROM reservas WHERE estado = 'pendiente_pago' AND creada_en_utc <= {limite} FOR UPDATE SKIP LOCKED")`
    + `.Include(r => r.Pago)`;
  - después `Caducar(ahora)`, `SaveChanges` y `Commit`; si no hay ninguna, `Commit` sin `SaveChanges`;
  - la regla no cambia (`<= ahora − TiempoBloqueoPago`) y el comentario de la clase explica por qué
    `SKIP LOCKED`: no espera a un pago en curso; lo que se salta lo coge en la siguiente vuelta.

  `Comun/BarridoCaducidadesTests.cs` debe seguir en verde sin cambios.
- [X] T006 Añadir a `tests/SI18.Api.Tests/Comun/BarridoCaducidadesTests.cs` el caso
  `El_barrido_salta_una_reserva_bloqueada_y_la_caduca_en_la_siguiente_vuelta`
  (`[Trait("Categoria","Limites")]`): con una pendiente creada hace 10 min (instante insertado, sin
  adelantar el reloj) y bloqueada por otra transacción abierta con `FOR UPDATE`, `EjecutarUnaVez`
  termina sin esperar y devuelve 0; tras liberar el bloqueo (`Rollback`), la siguiente vuelta la caduca.
  Usa el 11/02/2027 a las 12:00. Depende de T005.
- [X] T007 [P] Crear `src/SI18.Api/Modulos/Reservas/ConsultaReservas.cs`, que implementa
  `IConsultaReservas` con **la misma consulta** que la provisional (research.md E7; contracts/modulos.md):
  - `Activas(instalacionId, desdeUtc, hastaUtc, ct)` devuelve, con `AsNoTracking` y ordenadas por
    `InicioUtc`, las reservas de la instalación con `desdeUtc ≤ InicioUtc < hastaUtc` y
    `Estado == Confirmada || (Estado == PendientePago && CreadaEnUtc > ahora − ValoresNegocio.TiempoBloqueoPago)`,
    con `ahora` de `TimeProvider`, como `ReservaActiva`.

  Registrarla con `services.AddScoped<IConsultaReservas, ConsultaReservas>()` en `AddReservas()`.
  Depende de T001.
- [X] T008 Crear `tests/SI18.Api.Tests/Reservas/ConsultaReservasTests.cs` **moviendo sin cambiar sus
  casos** `Una_pendiente_bloquea_hasta_los_10_minutos_y_no_despues` y
  `Solo_cuentan_confirmadas_y_pendientes_vigentes_dentro_del_rango` desde
  `tests/SI18.Api.Tests/Disponibilidad/ConsultasProvisionalesTests.cs` (mismos datos: Polideportivo
  Oeste, enero de 2027; mismo `[Trait("Categoria","Limites")]`), con la `IConsultaReservas` resuelta de
  `fabrica.Services` y un caso más que comprueba que es `ConsultaReservas` (RF-019; CE-007). Depende de T007.
- [X] T009 **[código de Artur, revisión de Artur, mismo PR que T007–T008]** Retirar la provisional
  (research.md E7; contracts/modulos.md, «Lo que retira»):
  - borrar `src/SI18.Api/Modulos/Disponibilidad/Provisional/ConsultaReservasProvisional.cs`;
  - en `src/SI18.Api/Modulos/Disponibilidad/RegistroDisponibilidad.cs`, borrar **solo** la línea
    `services.TryAddScoped<IConsultaReservas, ConsultaReservasProvisional>();` (el comentario de las
    provisionales y las otras dos líneas no cambian; se quitan los `using` que dejen de usarse);
  - en `tests/SI18.Api.Tests/Disponibilidad/ConsultasProvisionalesTests.cs`, quitar los dos casos
    movidos en T008 (los de cierres y franjas fijas se quedan);
  - en `tests/SI18.Api.Tests/Disponibilidad/DatosDisponibilidad.cs`, `ConServicio` construye hoy
    `new ConsultaReservasProvisional(db, reloj)`: sustituirlo por `new ConsultaReservas(db, reloj)` (una
    línea más su `using`). **No lo recoge plan.md ni contracts/modulos.md**: avisar a Artur en el PR.

  Depende de T007 y T008.
- [X] T010 Comprobar la retirada (quickstart.md §2): `git grep -n "ConsultaReservasProvisional" -- fuentes`
  sin resultados y `dotnet test SI18.slnx` en verde con `tests/SI18.Api.Tests/Disponibilidad/` **sin más
  cambios** que los de T009 (`BuscarFranjasTests`, `BuscarSinResultadosTests`,
  `ConsultaDisponibilidadTests` y `PrecioPorTarifaTests` intactos; RF-019, CE-007). Anotarlo en el PR
  para la revisión de Artur. Depende de T009.
- [X] T011 [P] Crear `tests/SI18.Api.Tests/Reservas/DatosReservas.cs` (sobre `DatosDisponibilidad`, sin
  cambiarlo) con:
  - `Task<long> Pendiente(string correo, int pistaId, DateTime inicioUtc, DateTime creadaEnUtc)`;
  - `Task<long> Confirmada(…)`, `Task<long> CaducadaPorTiempo(…)` (pago resuelto en
    `creada + TiempoBloqueoPago`) y `Task<long> CaducadaPorRechazo(…)` (pago resuelto en
    `creada + 1 min`);
  - `Task<Reserva> Leer(long id)` con `AsNoTracking` e `Include(r => r.Pago)`;
  - `PasarelaContadora : IPasarelaPago`, que delega en `PasarelaSimulada` y cuenta las llamadas a
    `Cobrar` con `Interlocked`;
  - `Task<T> ConServicio<T>(DateTime ahoraUtc, string correo, Func<ServicioReservar, Task<T>> accion, IPasarelaPago? pasarela = null)`:
    construye un `ServicioReservar` (T012) con un `FakeTimeProvider` propio, un `ServicioDisponibilidad`
    con ese mismo reloj y `ConsultaReservas`, un `IUsuarioActual` de prueba con el `Id` y la
    `TipoTarifa` del usuario, y la pasarela indicada (`PasarelaSimulada` por defecto). Mismo motivo que
    en spec-001: `FakeTimeProvider` no retrocede y el reloj de la fábrica es compartido.

  Depende de T007 (y de T012 para `ConServicio`, que se completa entonces).

**Punto de control**: la búsqueda de spec-001 da lo mismo con `ConsultaReservas`, la provisional ya no
existe y el barrido usa `SKIP LOCKED`.

---

## Fase 3: Historia 1 · Reservar una franja libre y pagarla en el momento (P1) 🎯 MVP

**Objetivo**: desde una franja de `/buscar-pistas`, ver el resumen, crear la reserva pendiente, pagar con
una tarjeta aceptada y ver la reserva confirmada con su QR.

**Prueba independiente**: quickstart.md R1, R2, R5, R14 y los pasos 1–5 de §4.

- [X] T012 [P] [US1] Crear `src/SI18.Api/Modulos/Reservas/ServicioReservar.cs` (clase `sealed`,
  scoped; dependencias: `Si18DbContext`, `TimeProvider`, `IConsultaDisponibilidad`, `IPasarelaPago`,
  `IUsuarioActual`) con un método privado `ReservaDto ADto(Reserva r, Instalacion i, Centro c, DateTime ahora)`
  (data-model.md § 3):
  - `Fecha`, `HoraInicio`, `HoraFin` con `HorarioMadrid.ALocal` en «`HH:mm`»;
  - `Estado` **efectivo**: `PendientePago` con `!EstaVigente(ahora)` → `Caducada` (RF-015);
  - `LimitePagoUtc = CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago` y `HoraLimitePago` en Madrid
    **truncada al minuto**, solo si el estado efectivo es `PendientePago`;
  - `CodigoAcceso` solo si es `Confirmada` (RF-016).

  Registrarlo con `AddScoped<ServicioReservar>()` en `AddReservas()`. Depende de T002.
- [X] T013 [US1] Añadir a `ServicioReservar`
  `Task<ResumenReservaDto> Resumen(int instalacionId, DateTime inicioUtc, CancellationToken ct)` (research.md E2):
  - `MotivoNoDisponible(instalacionId, inicioUtc)`: sus excepciones (`PETICION_INVALIDA`,
    `NO_ENCONTRADO`) se dejan pasar; si devuelve un motivo, `throw CodigosError.Error(motivo)`;
  - lee con `AsNoTracking` la instalación y su centro;
  - `Importe = IConsultaDisponibilidad.Precio(instalacionId, IUsuarioActual.TipoTarifa)`.

  No escribe nada. Depende de T012.
- [X] T014 [US1] Añadir a `ServicioReservar`
  `Task<ReservaDto> Crear(PeticionReserva peticion, CancellationToken ct)`, pasos 1–3 y 5 de research.md E3:
  - `ahora` se lee **una sola vez** al principio;
  - mismas comprobaciones que `Resumen` y, si `Precio ≠ ImporteMostrado`, `PRECIO_CAMBIADO`;
  - en una transacción: `new Reserva(IUsuarioActual.Id, instalacionId, inicioUtc, TipoTarifa, importe, ahora)`
    (crea su pago pendiente), `SaveChanges` y `Commit`.

  La caducidad al reservar y el `23505` los añade T015 (US2). Depende de T013.
- [X] T015 [US1] Añadir a `ServicioReservar` `Task<ReservaDto> Pagar(long id, string numeroTarjeta, CancellationToken ct)`
  con el camino feliz de research.md E4 (pasos 2 y 5 con `true`):
  - transacción; `CargaReservas.CargarParaModificar(db, id, IUsuarioActual.Id)`; si es nula →
    `NO_ENCONTRADO` (RF-013);
  - si es `PendientePago` y `EstaVigente(ahora)`: `IPasarelaPago.Cobrar(reserva.Importe, numeroTarjeta)`
    y, con `true`, `Confirmar(GeneradorCodigoAcceso.Nuevo(), ahora)`, `SaveChanges`, `Commit` y el DTO
    con `CodigoAcceso`.

  El número de tarjeta no se pasa a ningún `ILogger` ni se guarda (R9). Los demás estados los añaden
  T027 (US2) y T033 (US3). Depende de T004 y T012.
- [X] T016 [US1] Añadir a `ServicioReservar` `Task<ReservaDto> Ver(long id, CancellationToken ct)`:
  la reserva **del usuario de la sesión** (`AsNoTracking`, con instalación y centro), o `NO_ENCONTRADO`
  (RF-013). Depende de T012.
- [X] T017 [US1] Crear `src/SI18.Api/Modulos/Reservas/EndpointsReservas.cs` con el grupo
  `/api/reservas` (`.RequireAuthorization(Politicas.EsCiudadanoORepresentante)`) y mapearlo en
  `MapReservas()` (contracts/api-reservas.openapi.yaml):
  - `GET /resumen?instalacionId&inicioUtc`: si falta alguno o `inicioUtc` no es un instante UTC ISO
    8601, 400 `PETICION_INVALIDA` con `ManejadorErrores.CrearValidacion` y `errors` por campo en camelCase
    («Indica la pista.», «Indica la hora de inicio.»);
  - `POST /`: cuerpo `PeticionReserva`; campo ausente o `importeMostrado ≤ 0` → 400 con `errors`;
    responde `Results.Created($"/api/reservas/{id}", dto)`;
  - `POST /{id}/pago`: cuerpo `PeticionPago`; quita los espacios y, si no quedan exactamente 16
    dígitos, 400 `PETICION_INVALIDA` con `errors.numeroTarjeta` («Escribe los 16 dígitos de la
    tarjeta.») **antes** de llamar al servicio (RF-012; research.md E4 paso 1);
  - `GET /{id}`: `Ver`.

  Ningún endpoint registra el cuerpo de la petición. Depende de T013–T016.
- [X] T018 [US1] Crear `tests/SI18.Api.Tests/Reservas/CrearReservaTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Sur, «Pista de pádel 1» y «Pista de pádel 2»,
  22–28/12/2026):
  - **HU1 esc. 1**: el resumen de una franja libre, con `ciudadana.empadronada@si18.test`, trae pista,
    centro, fecha, «18:00»–«19:00» e `importe = 6.00`;
  - **HU1 esc. 2**: `POST` → 201, `Location`, `estado = pendiente_pago`, `horaLimitePago` = `ahora` +
    10 min en Madrid truncada al minuto y sin `codigoAcceso`; en la base, pago `pendiente`; la búsqueda
    de spec-001 ya no devuelve esa franja;
  - **HU1 esc. 4**: con `representante.padel@si18.test`, resumen y reserva con `importe = 4.00` y
    `tipo_tarifa = club`;
  - **errores**: sin sesión → 401 `NO_AUTENTICADO`; `personal.sur@si18.test` y `tecnico@si18.test` →
    403 `ROL_NO_PERMITIDO`; sin `inicioUtc` → 400 con `errors.inicioUtc`; `importeMostrado = 0` → 400;
    `instalacionId = 999999` → 404 `NO_ENCONTRADO`; las 10:30 de Madrid → 400 `PETICION_INVALIDA`.

  Depende de T017 y T011.
- [X] T019 [P] [US1] Crear `tests/SI18.Api.Tests/Reservas/PagarReservaTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Este, «Pista de tenis 1» y «Pista de tenis 2»,
  22–28/12/2026) con el camino feliz (**HU1 esc. 3**): una pendiente creada hace 1 min, pagada con
  `4000 0000 0000 0001` (con espacios) → 200, `estado = confirmada`, `codigoAcceso` de 22 caracteres;
  en la base, reserva confirmada con ese código y pago `completado` con `resuelto_en_utc`; `GET /{id}`
  devuelve el mismo código. Esta clase crece en T030, T031, T034, T047 y T048. Depende de T017 y T011.
- [X] T020 [P] [US1] Crear `tests/SI18.Api.Tests/Reservas/ReservarComoEnLaBusquedaTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Norte, «Tenis», 26 y 27/12/2026; CE-005,
  HU1 esc. 5): para tres franjas de la búsqueda y con las tres tarifas (`ciudadana.empadronada`,
  `ciudadano.noempadronado`, `representante.padel`), `precio` de `GET /api/disponibilidad/franjas`
  = `importe` del resumen = `importe` de la reserva creada y pagada = `pagos.importe`. Depende de T017.
- [ ] T021 [P] [US1] Crear `src/SI18.Cliente/Reservas/EnlaceReservar.razor` (research.md E10;
  contracts/modulos.md, «Componente de cliente»):
  - parámetros `[EditorRequired]` `Pista` (`PistaLibreDto`), `Franja` (`FranjaLibreDto`) y `Fecha` (`DateOnly`);
  - un `MudButton` con `Href="/reservas/nueva?instalacionId={Pista.InstalacionId}&inicioUtc={Franja.InicioUtc:O}"`
    (URL codificada), texto visible «Reservar» y `aria-label` «Reservar {Pista.Nombre}, {día de la
    semana} {dd/MM/yyyy}, de {HoraInicio} a {HoraFin}» con cultura `es-ES` (HU6 esc. 1); el nombre
    accesible empieza por el texto visible (WCAG 2.5.3);
  - el estilo que separe el botón del texto de la franja va en el propio componente
    (`EnlaceReservar.razor.css`), no en `BuscarPistas.razor` ni en `app.css`.
- [ ] T022 [US1] **[código de Artur, revisión de Artur]** En `src/SI18.Cliente/Disponibilidad/BuscarPistas.razor`
  añadir **solo** (research.md E10; aclaración de spec-001; confirmado por Artur):
  - `@using SI18.Cliente.Reservas` junto a los otros `@using`;
  - `<EnlaceReservar Pista="pista" Franja="franja" Fecha="_resultado.Fecha" />` **dentro** del
    `<li role="listitem">` de cada franja, después de `@FormatoEs.Importe(franja.Precio)`.

  Nada más de la pantalla cambia: ni DTO, ni endpoint, ni pruebas de spec-001. Depende de T021.
- [ ] T023 [P] [US1] Crear `src/SI18.Cliente/Reservas/CodigoAcceso.razor` (research.md E11; RF-016):
  parámetros `Codigo` y `Descripcion`; dibuja el QR en SVG con QRCoder (`SvgQRCode`, ya referenciado
  en el cliente, R14) dentro de un `<figure role="img" aria-label="Código de acceso de {Descripcion}">`,
  con el SVG en `max-width: 100%` y, en un `<figcaption>`, el código en texto seleccionable.
- [ ] T024 [US1] Crear `src/SI18.Cliente/Reservas/NuevaReserva.razor` (ruta `/reservas/nueva`; research.md E11):
  - **acceso**, como `/buscar-pistas` (D7 de spec-001): sin sesión → `/iniciar-sesion`; con otro rol,
    aviso «Reservar pistas es para ciudadanos y representantes de club.» en `MensajeAccesible`
    (`Severity.Warning`), sin pedir nada a la API;
  - lee `instalacionId` e `inicioUtc` con `[SupplyParameterFromQuery]` y pide
    `GET /api/reservas/resumen`;
  - muestra el resumen en un `<dl>`: Pista, Centro, Fecha (dd/MM/yyyy), Hora («de 18:00 a 19:00») e
    Importe con `FormatoEs.Importe`;
  - botón «Confirmar reserva» (`ButtonType.Submit` dentro de un `<form>`), deshabilitado mientras se
    envía: `POST /api/reservas` con el importe del resumen y, con 201, navega a `/reservas/{id}/pago`.

  Los rechazos los completa T036 (US4). Depende de T017.
- [ ] T025 [US1] Crear `src/SI18.Cliente/Reservas/PagarReserva.razor` (ruta `/reservas/{Id:long}/pago`;
  research.md E11):
  - mismo control de acceso que T024;
  - `GET /api/reservas/{id}`; muestra el resumen y el texto fijo «Paga antes de las {HoraLimitePago};
    después la reserva caduca.» (RF-008a; sin cuenta atrás);
  - `MudTextField` «Número de tarjeta» con `Label` visible, `InputMode.numeric`,
    `UserAttributes` `autocomplete="cc-number"`, `HelperText` «16 dígitos; es una tarjeta ficticia» y
    el error de `errors.numeroTarjeta` en `Error`/`ErrorText` del propio campo;
  - botón «Pagar {importe}» (`ButtonType.Submit`), deshabilitado mientras se envía;
  - con 200: el formulario desaparece; `<h2 tabindex="-1">Reserva confirmada</h2>` recibe el foco,
    `MensajeAccesible` (`Severity.Success`) lo anuncia, `CodigoAcceso` y enlace «Ver mis reservas».

  Los demás resultados los añaden T032 (US2) y T035 (US3). Depende de T017 y T023.

**Punto de control**: una ciudadana reserva y paga de punta a punta desde `/buscar-pistas`.

---

## Fase 4: Historia 2 · Nadie más puede quedarse la franja mientras pago, pero el bloqueo no es eterno (P1)

**Objetivo**: exclusión mutua real (también con 20 peticiones a la vez y con dos pagos simultáneos),
caducidad al reservar y al pagar, y la misma respuesta antes y después del barrido.

**Prueba independiente**: quickstart.md R3, R4 y R9.

- [X] T026 [US2] Completar `ServicioReservar.Crear` (research.md E3 paso 4; data-model.md § 2.1):
  - dentro de la transacción y antes del `INSERT`, cargar con
    `FromSql(… WHERE instalacion_id = {…} AND inicio_utc = {…} AND estado = 'pendiente_pago' AND creada_en_utc <= {ahora − TiempoBloqueoPago} FOR UPDATE)`
    las pendientes **no vigentes** de esa franja y `Caducar(ahora)` cada una (R8.2; RF-006);
  - capturar `DbUpdateException` cuya `InnerException` sea `PostgresException` con `SqlState == "23505"`
    y `ConstraintName == "ux_reservas_activa"` → `CodigosError.Error(FranjaOcupada)` (R7; RF-007). La
    traducción vive aquí, no en `ManejadorErrores` (research.md E3).
- [X] T027 [US2] Completar `ServicioReservar.Pagar` con la caducidad (research.md E4 pasos 3 y 4;
  aclaración del plan del 2026-11-25):
  - `PendientePago` y `!EstaVigente(ahora)` → `Caducar(ahora)`, `SaveChanges`, `Commit` y **después**
    `throw CodigosError.Error(ReservaCaducada)`, sin llamar a la pasarela (RF-009; R8.3);
  - `Caducada` **por tiempo** (`Pago.ResueltoEnUtc ≥ CreadaEnUtc + ValoresNegocio.TiempoBloqueoPago`:
    barrido o caducidad al reservar) → `RESERVA_CADUCADA`, sin cambios ni pasarela;
  - `Confirmada` → `TRANSICION_NO_PERMITIDA`, sin pasarela (la necesita T031: el segundo de dos pagos
    simultáneos encuentra la reserva ya confirmada).

  Una caducada por rechazo o por cierre y una cancelada **no** entran aquí (T033). Depende de T015.
- [X] T028 [US2] Ampliar `tests/SI18.Api.Tests/Reservas/CrearReservaTests.cs` (Polideportivo Sur,
  «Pista de pádel 2»):
  - **HU2 esc. 1**: con una pendiente vigente de `ciudadana.empadronada`, `ciudadano.noempadronado`
    recibe 409 `FRANJA_OCUPADA` con el texto del catálogo;
  - **doble envío** (casos límite): la misma persona envía dos veces la misma reserva → 201 y 409
    `FRANJA_OCUPADA`; en la base, una sola activa;
  - **HU2 esc. 4** (`[Trait("Categoria","Limites")]`): con una pendiente ajena creada hace
    **exactamente** 10 min, la reserva se crea; la anterior queda `caducada` con pago `fallido` y
    `resuelto_en_utc = ahora`; con una creada hace 9 min 59 s → 409 `FRANJA_OCUPADA`.

  Depende de T026.
- [X] T029 [US2] Crear `tests/SI18.Api.Tests/Reservas/ReservaConcurrenteTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Oeste, «Pista de pádel 1», 22/12/2026 a las
  19:00; CE-002, HU2 esc. 2, RF-007): **20 peticiones `POST /api/reservas` simultáneas** sobre la misma
  franja (`Task.WhenAll`), repartidas entre las sesiones de `ciudadana.empadronada`,
  `ciudadano.noempadronado` y `representante.padel` (la siembra solo tiene esas personas que puedan
  reservar fuera de `MisReservasTests`) → exactamente una 201 y diecinueve 409 `FRANJA_OCUPADA`; en la
  base, una sola reserva activa y un solo pago. Depende de T026.
- [X] T030 [US2] Ampliar `tests/SI18.Api.Tests/Reservas/PagarReservaTests.cs` con los límites de los
  10 minutos (`[Trait("Categoria","Limites")]`; casos límite de la spec):
  - **9:59**: pendiente creada hace 9 min 59 s → se cobra y queda confirmada;
  - **10:00** (HU2 esc. 3): creada hace 10 min exactos → 409 `RESERVA_CADUCADA`; en la base,
    `caducada` y pago `fallido`; sin código;
  - **E4/E13 · caducada por el barrido → `RESERVA_CADUCADA`**: pendiente creada hace 10 min, caducada
    con `Barrido.EjecutarUnaVez` (el `BarridoCaducidades` registrado, como en `BarridoCaducidadesTests`)
    → el pago responde 409 **`RESERVA_CADUCADA`** (no `TRANSICION_NO_PERMITIDA`) y `pagos.resuelto_en_utc`
    no cambia (sin cobro ni cambios);
  - **E4 · caducada al reservar → `RESERVA_CADUCADA`**: la reserva de Ana caducada porque otra persona
    reservó su franja (T028) → 409 `RESERVA_CADUCADA`;
  - **frontera de «por tiempo»**: con `DatosReservas.CaducadaPorTiempo` (pago resuelto en
    `creada + 10:00` exactos) → `RESERVA_CADUCADA`.

  Depende de T027.
- [X] T031 [US2] Añadir a `PagarReservaTests` **dos pagos simultáneos de la misma reserva** (E5): con
  `DatosReservas.ConServicio` y una `PasarelaContadora`, dos llamadas a `Pagar` en paralelo (cada una
  con su ámbito y su `Si18DbContext`) → una confirma y la otra recibe `TRANSICION_NO_PERMITIDA`; la
  pasarela se llama **una sola vez**. Depende de T027.
- [ ] T032 [US2] En `src/SI18.Cliente/Reservas/PagarReserva.razor`, con 409 `RESERVA_CADUCADA`: el
  formulario desaparece, `<h2 tabindex="-1">La reserva ha caducado</h2>` recibe el foco y
  `MensajeAccesible` (`Severity.Warning`) anuncia el `Detalle` del catálogo, con el enlace «Volver a
  buscar pistas». Si `GET /api/reservas/{id}` ya la devuelve `caducada`, se muestra lo mismo sin
  formulario. Depende de T025.

**Punto de control**: CE-002 y CE-003 cumplidos; la respuesta al pagar no depende del barrido.

---

## Fase 5: Historia 3 · Saber qué pasa si el pago no se completa (P1)

**Objetivo**: pago rechazado → caducada sin reintento; formato de tarjeta no válido → sin pasarela y
sin cambios.

**Prueba independiente**: quickstart.md R7, R8 y R9b.

- [X] T033 [US3] Completar `ServicioReservar.Pagar` (research.md E4 pasos 3 y 5; data-model.md § 2.2):
  - `Cobrar` devuelve `false` → `Caducar(ahora)`, `SaveChanges`, `Commit` y **después**
    `throw CodigosError.Error(PagoRechazado)` (RF-011);
  - `Cancelada` o `Caducada` **no** por tiempo (`Pago.ResueltoEnUtc < CreadaEnUtc + TiempoBloqueoPago`:
    rechazo o cierre) → `TRANSICION_NO_PERMITIDA`, sin pasarela.

  El orden queda: no es suya → 404; caducada por tiempo → `RESERVA_CADUCADA`; otro estado no
  pendiente → `TRANSICION_NO_PERMITIDA`; pendiente no vigente → caducar y `RESERVA_CADUCADA`; cobro.
  Depende de T027.
- [X] T034 [US3] Ampliar `tests/SI18.Api.Tests/Reservas/PagarReservaTests.cs`:
  - **HU3 esc. 1**: `4000 0000 0000 0002` → 422 `PAGO_RECHAZADO`; `caducada`, pago `fallido`, sin código;
  - **E4/E13 · caducada por pago rechazado → `TRANSICION_NO_PERMITIDA`** (HU3 esc. 2;
    `[Trait("Categoria","Limites")]`): volver a pagar esa reserva con `…0001` → 409
    **`TRANSICION_NO_PERMITIDA`** (no `RESERVA_CADUCADA`), sin cobro; y, con `DatosReservas.ConServicio`,
    rechazo a los **9:59** de su creación y nuevo intento a los **10:01** → sigue siendo
    `TRANSICION_NO_PERMITIDA`, aunque ya hayan pasado los 10 minutos (el pago se resolvió antes del límite);
  - **frontera de «por tiempo»**: `DatosReservas.CaducadaPorRechazo` → `TRANSICION_NO_PERMITIDA`;
  - **pagar dos veces**: segundo pago de una confirmada → 409 `TRANSICION_NO_PERMITIDA`, mismo código
    de acceso;
  - **HU3 esc. 4**: `4000 0000 0000 001` (15 dígitos) y `4000-0000-0000-000A` → 400
    `PETICION_INVALIDA` con `errors.numeroTarjeta`; la reserva sigue `pendiente_pago` y el pago
    `pendiente`;
  - **HU3 esc. 3**: tras el rechazo, la franja vuelve a aparecer en `GET /api/disponibilidad/franjas` y
    se puede crear una reserva nueva;
  - **HU5 esc. 4**: pagar la reserva de otra persona → 404 `NO_ENCONTRADO` y la reserva no cambia.

  Depende de T033 y T017.
- [ ] T035 [US3] En `src/SI18.Cliente/Reservas/PagarReserva.razor`, completar los resultados (research.md E11):
  - 422 `PAGO_RECHAZADO` y 409 `TRANSICION_NO_PERMITIDA`: como T032, con `<h2 tabindex="-1">` «El pago no
    se ha completado» o «Esta reserva ya no se puede pagar», foco al encabezado, `MensajeAccesible`
    (`Severity.Error`) con el `Detalle` del catálogo y enlace «Volver a buscar pistas»;
  - 400 con `errors.numeroTarjeta`: el error en el propio campo, sin quitar el formulario ni mover el
    foco fuera del campo;
  - el resultado nunca se transmite solo con color: siempre hay encabezado y texto.

  Depende de T032.

**Punto de control**: ningún pago fallido deja una reserva confirmada ni se cobra dos veces (CE-004).

---

## Fase 6: Historia 4 · Entender por qué no puedo reservar una franja (P2)

**Objetivo**: cada motivo de RF-011 de 000-core y `PRECIO_CAMBIADO` llegan al usuario con su texto.

**Prueba independiente**: quickstart.md R10–R13.

- [X] T036 [P] [US4] Crear `tests/SI18.Api.Tests/Reservas/MotivosRechazoTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Sur, «Pista de tenis 1» y «Pista de tenis 2»,
  22–28/12/2026); en todos, además del código, **no se crea ninguna reserva** (HU4 esc. 6: `detail` en
  español y sin detalles técnicos):
  - **HU4 esc. 1** (`[Trait("Categoria","Limites")]`): 29/12 → 422 `FUERA_DE_ANTELACION` con «Solo
    puedes reservar desde hoy hasta dentro de 7 días.»; la de 21:00 del 28/12 → 201;
  - **HU4 esc. 2** (`[Trait("Categoria","Limites")]`, con `DatosReservas.ConServicio`): la franja de
    las 18:00 de hoy a las 18:00:00 → `FRANJA_YA_EMPEZADA`; a las 17:59:59 → se crea; a las 18:01 →
    `FRANJA_YA_EMPEZADA`;
  - **HU4 esc. 3**: un cierre (`DatosDisponibilidad.Cierre`) → 422 `INSTALACION_CERRADA`; una franja
    fija aprobada (`DatosDisponibilidad.FranjaFija`) → 422 `FRANJA_BLOQUEADA_CLUB`;
  - **HU4 esc. 4**: la «Sala polivalente» y el «Pabellón» → 422 `INSTALACION_NO_RESERVABLE`;
  - **HU4 esc. 5**: `importeMostrado = 5.00` → 409 `PRECIO_CAMBIADO`;
  - **cambio de hora** (`[Trait("Categoria","Limites")]`, con `DatosReservas.ConServicio` y reloj el
    25/03/2027): las 14 franjas de 08:00 a 21:00 del 28/03/2027 se pueden resumir, la de 08:00 es
    `2027-03-28T06:00:00Z`, y reservar y pagar la de 08:00 da `finUtc − inicioUtc = 1 h`.

  Los estados y códigos HTTP son los del catálogo de 000-core (`contracts/errores.md`). Depende de T026.
- [ ] T037 [US4] En `src/SI18.Cliente/Reservas/NuevaReserva.razor`, completar los rechazos (research.md E11):
  - si el resumen o la creación responden un error, `MensajeAccesible` (`Severity.Error`) con su
    `Detalle`, sin resumen ni botón, y enlace «Volver a buscar pistas»;
  - con 409 `PRECIO_CAMBIADO`: se vuelve a pedir el resumen, se muestra el importe nuevo, se anuncia
    el `Detalle` (`Severity.Warning`) y el botón «Confirmar reserva» queda disponible con el importe
    nuevo (HU4 esc. 5).

  Depende de T024.

**Punto de control**: CE-006 cumplido en la API y en la pantalla.

---

## Fase 7: Historia 5 · Ver mis reservas con su estado y su QR (P2)

**Objetivo**: «Mis reservas» con todas las reservas del usuario, en el orden de RF-015a, con el estado
efectivo y el QR solo en las confirmadas.

**Prueba independiente**: quickstart.md R15, R16 y los pasos 5–7 de §4.

- [X] T038 [US5] Añadir a `ServicioReservar` `Task<IReadOnlyList<ReservaDto>> Mias(CancellationToken ct)`
  y `GET /api/reservas` en `EndpointsReservas.cs` (research.md E6; data-model.md, «Orden de "Mis
  reservas"»):
  - todas las reservas de `IUsuarioActual.Id`, `AsNoTracking`, unidas a `instalaciones` y `centros`;
  - con `ahora` del servidor: primero las de `FinUtc > ahora` por `InicioUtc` ascendente; después las
    de `FinUtc ≤ ahora` por `InicioUtc` descendente; desempate por `Id`. Sin paginación;
  - cada elemento con `ADto` (estado efectivo; RF-015). No escribe nada.

  Depende de T016.
- [X] T039 [US5] Crear `tests/SI18.Api.Tests/Reservas/MisReservasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Oeste, «Pista de tenis 1» y «Pista de tenis 2»;
  **solo** con `representante.norte@si18.test`):
  - **HU5 esc. 6**: reservas confirmadas el 26/12 y el 27/12/2026 y otra del 20/11/2026 → orden 26/12,
    27/12, 20/11; una reserva que está en curso (`inicio ≤ ahora < fin`) va primera;
  - **HU5 esc. 1–3**: confirmada con `codigoAcceso`; caducada y cancelada sin código; pendiente vigente
    con `limitePagoUtc` y `horaLimitePago` (en Madrid, truncada al minuto);
  - **HU5 esc. 5** (`[Trait("Categoria","Limites")]`): una pendiente creada hace 10 min exactos, sin
    barrido → `estado = caducada`, sin `horaLimitePago`; en la base sigue `pendiente_pago` (es una lectura);
  - **HU5 esc. 4**: no aparece ninguna reserva de otro usuario; `GET /api/reservas/{id ajeno}` → 404;
  - `personal.oeste@si18.test` → 403 `ROL_NO_PERMITIDO`.

  Depende de T038 y T011.
- [ ] T040 [US5] Crear `src/SI18.Cliente/Reservas/MisReservas.razor` (ruta `/mis-reservas`; research.md E11):
  - mismo control de acceso que T024;
  - `<PageTitle>Mis reservas · SI-18</PageTitle>` y `<h1>` «Mis reservas»;
  - sin reservas: «Todavía no tienes reservas.» en `MensajeAccesible` (`Severity.Info`);
  - `<ul role="list">` con un `<li role="listitem">` por reserva: `<h2>` con pista y centro; fecha
    (dd/MM/yyyy) y «de HH:mm a HH:mm»; importe con `FormatoEs.Importe`; estado **en texto**
    («Confirmada», «Pendiente de pago», «Caducada», «Cancelada») y un `MudChip` de color solo como refuerzo;
  - confirmadas: `CodigoAcceso` con la descripción de la reserva; pendientes vigentes: «Paga antes de
    las {HoraLimitePago}» y enlace «Pagar» (`MudButton Href="/reservas/{id}/pago"`, `aria-label`
    «Pagar la reserva de {pista}, {fecha}, de {horaInicio} a {horaFin}») (RF-017);
  - un comentario `@* Hueco de Cancelaciones (spec-003): botón «Cancelar» en las confirmadas. *@`
    dentro del `<li>` (plan.md, C4).

  Depende de T038 y T023.
- [ ] T041 [P] [US5] Añadir «Mis reservas» (`MudButton Href="/mis-reservas" Color="Color.Inherit" Variant="Variant.Text"`)
  en el hueco `@* Hueco de Reservas (spec-002, 003): mis reservas. *@` de
  `src/SI18.Cliente/Layout/MainLayout.razor`, quitando la parte «mis reservas» del comentario (RF-002;
  T056 de 000-core). **Ojo**: añade un botón más a la barra superior, que ya se desborda a 320 px
  (hallazgo 6 de `specs/000-core/checklists/cierre-saul.md`); se comprueba en T043.

**Punto de control**: el QR de una reserva confirmada se puede recuperar desde «Mis reservas».

---

## Fase 8: Historia 6 · Reservar y pagar sin barreras de accesibilidad (P2)

**Objetivo**: reserva y pago completos con teclado y lector de pantalla (WCAG 2.1 AA; CE-008).

**Prueba independiente**: quickstart.md §5 (que aplica el §6 de `000-core/quickstart.md`).

- [ ] T042 [US6] Completar la accesibilidad de `NuevaReserva.razor`, `PagarReserva.razor` y
  `MisReservas.razor` en `src/SI18.Cliente/Reservas/` (RF-021; research.md E11):
  - `<PageTitle>` y `<h1>` en cada una: «Confirmar la reserva», «Pagar la reserva», «Mis reservas»;
  - orden de tabulación: resumen → campo → botón → enlaces del resultado; sin trampas de foco;
  - tras el pago, el foco va al `<h2 tabindex="-1">` del resultado (T025, T032, T035), con su
    excepción de recuadro igual que `h1:focus` en `app.css` (sin tocar `app.css`: clase propia en el
    `.razor.css` del componente);
  - **reflujo a 320 px** (lección de T073): ningún elemento de `<main>` de estas pantallas pasa del
    borde derecho; el `<dl>` del resumen se apila en una columna, los botones se parten en varias
    líneas y el SVG del QR usa `max-width: 100%`. Los estilos van en los `.razor.css` de Reservas.

  Depende de T037, T035 y T040.
- [ ] T043 [US6] Comprobar a mano quickstart.md §5 sobre el botón «Reservar» de `/buscar-pistas`,
  `/reservas/nueva`, `/reservas/{id}/pago` y `/mis-reservas`, **aplicando lo aprendido en T073 de
  000-core** (`specs/000-core/checklists/cierre-saul.md`, hallazgos 5 y 6):
  - **foco visible con MudBlazor**: Lighthouse y axe no lo detectan. Recorrer con el teclado y, en cada
    tipo de control nuevo —`MudButton` con `Href` (`EnlaceReservar`, «Pagar», «Volver a buscar
    pistas», «Ver mis reservas»), `MudButton` de envío («Confirmar reserva», «Pagar …») y
    `MudTextField` («Número de tarjeta»)—, mirar en DevTools el estilo calculado con foco de teclado:
    `outline-style` no puede ser `none`. MudBlazor lo anula con `a:focus-visible`, `button:focus` y
    `.mud-input>input.mud-input-root:focus`, que ganan al `:focus-visible` de `app.css`;
  - **barra superior a 320 px**: con la sesión de una ciudadana y «Mis reservas» ya en el menú (T041),
    a 320 px de ancho y con zoom al 200 % sobre 1280 px, comprobar que no hay desplazamiento horizontal y
    que «Mis reservas», el nombre y «Cerrar sesión» se pueden alcanzar y leer;
  - si algo de lo anterior falla por `app.css` o `MainLayout.razor` (código de Artur, propuesta ya
    hecha en el PR #28), **no se corrige en Reservas ni se duplica la regla**: se anota en el PR con el
    estilo calculado o la medida, y se coordina con Artur;
  - lector de pantalla: nombre completo de cada «Reservar», etiqueta y error del campo de tarjeta,
    resultado anunciado con el foco en su encabezado, QR con descripción y código en texto;
  - estados escritos en texto, no solo con color;
  - Lighthouse ≥ 95 y axe sin infracciones graves ni críticas en las cuatro pantallas.

  Anotar el resultado en la descripción del PR (principio I). Depende de T022, T041 y T042.

**Punto de control**: CE-008 cumplido, o sus fallos ajenos anotados y coordinados.

---

## Fase 9: Operaciones para HU-03 y HU-07 (RF-019a, sin historia propia)

**Propósito**: que C pueda cancelar por el usuario y aplicar un cierre sin escribir en `reservas` ni
`pagos`, dentro de su propia transacción (aclaración 1; research.md E8).

- [ ] T044 Crear `src/SI18.Api/Modulos/Reservas/ServicioReservas.cs`, que implementa
  `IServicioReservas` **sin cambiar su firma** (contracts/modulos.md), con
  `CancelarPorUsuario(reservaId, usuarioId, ct)`:
  - `CargaReservas.CargarParaModificar(db, reservaId, usuarioId)` (que exige la transacción abierta:
    sin ella, `InvalidOperationException`); nula → `NO_ENCONTRADO`;
  - `Cancelar(MotivoCancelacion.Usuario, ahora)` (la entidad lanza `TRANSICION_NO_PERMITIDA` si no está
    confirmada y `RESERVA_YA_EMPEZADA` si `ahora ≥ inicio`, en ese orden);
  - devuelve `ReservaAfectada(Id, UsuarioId, InicioUtc, Pago.Id, Pago.Importe)`;
  - **no** llama a `SaveChanges` ni abre o confirma transacciones.

  Registrarlo con `AddScoped<IServicioReservas, ServicioReservas>()` en `AddReservas()`. Depende de T004.
- [ ] T045 Añadir a `ServicioReservas` `AplicarCierre(instalacionId, inicioUtc, finUtc, ct)`
  (data-model.md § 2.5):
  - exige la transacción abierta (si no, `InvalidOperationException`);
  - carga con `FOR UPDATE` las reservas de la instalación con estado guardado `confirmada` o
    `pendiente_pago` y `inicio_utc < finUtc AND fin_utc > inicioUtc` (semiabierto), con su `Pago`;
  - confirmadas → `Cancelar(MotivoCancelacion.CierreMantenimiento, ahora)` → `Canceladas`;
    pendientes, vigentes o no → `Caducar(ahora)` → `Caducadas`;
  - las dos listas ordenadas por `InicioUtc`; sin `SaveChanges`. Depende de T044.
- [ ] T046 Crear `tests/SI18.Api.Tests/Reservas/ServicioReservasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Norte, «Pista de tenis 1» y «Pista de tenis 2»,
  enero de 2027), con un llamador de prueba que abre la transacción, llama, hace `SaveChanges` y
  `Commit`, como harán HU-03 y HU-07:
  - **CancelarPorUsuario**: ajena → `NO_ENCONTRADO`; pendiente → `TRANSICION_NO_PERMITIDA`; empezada
    (una confirmada insertada con inicio el 20/12/2026, ya pasado) → `RESERVA_YA_EMPEZADA`; correcta →
    `cancelada`, motivo `usuario`, `cancelada_en_utc`, pago sigue `completado` y los datos de
    `ReservaAfectada`;
  - **AplicarCierre** (`[Trait("Categoria","Limites")]` en el solape): un cierre de 10:00 a 12:00 no
    afecta a la franja de 09:00 ni a la de 12:00 y sí a las de 10:00 y 11:00; confirmadas → `cancelada`
    con motivo `cierre_mantenimiento`; pendientes vigentes y no vigentes → `caducada` con pago `fallido`;
    las listas, ordenadas por inicio;
  - **transacción del llamador**: sin transacción → `InvalidOperationException`; con `Rollback` en vez
    de `Commit`, no cambia nada en la base.

  Depende de T045.
- [ ] T047 Añadir a `tests/SI18.Api.Tests/Reservas/PagarReservaTests.cs` el caso límite «cierre
  posterior» (spec, «Casos límite»): una pendiente **vigente** caducada por `AplicarCierre` →
  el pago responde 409 `TRANSICION_NO_PERMITIDA` (no `RESERVA_CADUCADA`: se caducó antes del límite),
  sin cobro. Depende de T045 y T033.

**Punto de control**: C puede usar `IServicioReservas` en HU-03 y HU-07 con el contrato publicado.

---

## Fase 10: Pulido y cierre

- [ ] T048 Revisar que el número de tarjeta no aparece en ningún registro (R9; research.md E4):
  `git grep -n -i "numeroTarjeta" -- fuentes/src` solo en el DTO, el endpoint y el servicio, ninguno
  dentro de un `Log…`; y una prueba en `PagarReservaTests` que, con un `ILoggerProvider` de prueba,
  paga con `4000000000000001` y comprueba que ningún mensaje registrado contiene «4000000000000001» ni
  «0001». Depende de T034.
- [ ] T049 Ejecutar quickstart.md §2–§4 de principio a fin: `dotnet test SI18.slnx` en verde (también
  con `--filter-trait "Categoria=Limites"`), los escenarios R1–R18 contra la API y los pasos de la
  pantalla. Cronometrar los pasos 2–4 de §4 (de pulsar «Reservar» a ver el QR) y anotar el tiempo:
  debe ser < 2 min (CE-001). Medir con la API local 10 veces `POST /api/reservas` y `POST /api/reservas/{id}/pago` y
  anotar en el PR el peor valor (objetivo < 500 ms, research.md E14).
- [ ] T050 [P] Avisar al grupo (constitución, «Acuerdos de colaboración»; plan.md, «Puntos a coordinar»):
  - a **C** (C3, C4): `IServicioReservas` ya está implementada; enlace a contracts/modulos.md (abrir la
    transacción antes, no esperar `SaveChanges`, guardar devolución y aviso en la misma transacción) y a
    `ServicioReservasTests` como ejemplo; el hueco de «Cancelar» en `MisReservas.razor`;
  - a **Artur** (C1, C2): revisión de T009, T010 y T022, y el cambio de `DatosDisponibilidad.cs` que
    no estaba en el plan.

  Anotarlo en la descripción del PR.
- [ ] T051 Abrir los PR hacia `develop`, con la lista de los principios I–IX revisados, como en spec-001:
  - **PR de API**: Fases 1–7 de la API y Fase 9 (T001–T020, T026–T031, T033–T034, T036, T038–T039,
    T044–T048). Incluye **en el mismo PR** `ConsultaReservas` (T007–T008) y la retirada de la provisional
    (T009–T010); **revisa Artur**;
  - **PR de pantallas**: T021–T025, T032, T035, T037, T040–T043, con la comprobación de accesibilidad de
    T043; **revisa Artur** el cambio de `BuscarPistas.razor` (T022).

  Los aprueba otra persona del grupo.
- [ ] T052 Después de fusionar, ejecutar `/speckit-converge` para la spec 002.

---

## Dependencias y orden de ejecución

### Dependencias entre fases

```text
Fase 1 Setup (T001 ∥ T002 → T003)
   └─► Fase 2 Fundacional (T004 ∥ T005 → T006 ∥ T007 → T008 → T009 → T010; T011)
          ├─► US1 (T012 → T013 → T014 ∥ T015 ∥ T016 → T017 → T018 ∥ T019 ∥ T020)
          │      (cliente: T021 → T022; T023 → T024 ∥ T025)
          │      ├─► US2 (T026 → T028 ∥ T029; T027 → T030 ∥ T031; T032 tras T025)
          │      │      └─► US3 (T033 → T034; T035 tras T032)
          │      ├─► US4 (T036 tras T026; T037 tras T024)
          │      └─► US5 (T038 → T039 ∥ T040; T041)
          │             └─► US6 (T042 → T043)            *tras US3, US4 y US5 en el cliente
          ├─► Fase 9 IServicioReservas (T044 → T045 → T046; T047 tras T033)
          └─► Fase 10 Pulido (T048–T052)
```

- **T007–T010 van en el mismo PR** y con revisión de Artur: la consulta definitiva no se publica sin
  retirar la provisional.
- **US2 y US3** completan el mismo método `Pagar` que US1 (T015 → T027 → T033), por eso van en serie
  en el servicio; sus pruebas se pueden escribir antes (fallan hasta que la tarea correspondiente esté).
- **Fase 9** solo depende de T004 (`CargaReservas`): se puede adelantar para desbloquear a C.
- **US6** va al final porque comprueba las cuatro pantallas ya construidas; T043 depende además de
  que Artur resuelva los hallazgos 5 y 6 de T073 (PR #28) para poder marcarse como superada.

### Paralelismo dentro de cada fase

- **Fase 1**: T001 y T002 en paralelo.
- **Fase 2**: T004, T005 y T007 en paralelo; T011 en cuanto exista T007.
- **US1**: T018, T019 y T020 en paralelo tras T017; en el cliente, T021 y T023 en paralelo con toda la API.
- **US2**: T028 y T029 en paralelo tras T026.
- **US4**: T036 en paralelo con todo lo del cliente.
- **US5**: T041 en paralelo con todo.

### Ejemplo de reparto en el tiempo (una persona)

```text
Día 1:  T001 ∥ T002 → T003 → T004 ∥ T005 ∥ T007 → T006 → T008 → T009 → T010 → T011
Día 2:  T012 → T013 → T014 → T015 → T016 → T017 → T018 → T019 → T020          (MVP de API)
Día 3:  T026 → T028 → T029 → T027 → T030 → T031 → T033 → T034 → T036 → T038 → T039
Día 4:  T044 → T045 → T046 → T047 → T048                                       (PR de API)
Día 5:  T021 → T022 → T023 → T024 → T025 → T032 → T035 → T037 → T040 → T041 → T042 → T043
Día 6:  T049 → T050 → T051 → T052
```

## Estrategia de implementación

### MVP (US1)

1. Fases 1 y 2: módulo, DTO, `CargaReservas`, barrido con `SKIP LOCKED`, `ConsultaReservas` con la
   provisional retirada y datos de prueba.
2. US1: resumen, crear, pagar y ver una, con sus pruebas; `EnlaceReservar` y las pantallas de resumen y pago.
3. **Validar**: quickstart.md R1, R2, R5 y R14.

### Entrega incremental

US1 (reservar y pagar) → US2 (exclusión mutua y caducidad) → US3 (pago rechazado y formato) → US4
(motivos de rechazo) → US5 («Mis reservas») → US6 (accesibilidad) → Fase 9 (`IServicioReservas`, se
puede adelantar) → Pulido. Si las pantallas se retrasan, el PR de API se abre antes (T051).

## Notas

- `[P]` = ficheros distintos, sin dependencias pendientes.
- Ninguna tarea crea tablas, migraciones, entidades, estados, valores de negocio ni códigos de error:
  todo es de 000-core (principio VII). Si hiciera falta alguno, se propone primero en 000-core.
- Fuera de `*/Reservas` solo se tocan: `Program.cs` (T001), `BarridoCaducidades.cs` (T005–T006, de
  Saul), `MainLayout.razor` (T041, el hueco de Reservas) y, con revisión de Artur, los ficheros de T009
  y `BuscarPistas.razor` (T022).
