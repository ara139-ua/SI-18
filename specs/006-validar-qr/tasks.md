---

description: "Lista de tareas de 006 · Validar el acceso con el código QR (HU-06)"
---

# Tareas: 006 · Validar el acceso con el código QR (HU-06)

**Entrada**: documentos de diseño de `specs/006-validar-qr/` ([plan.md](./plan.md), [spec.md](./spec.md),
[research.md](./research.md), [data-model.md](./data-model.md),
[contracts/](./contracts/modulos.md), [quickstart.md](./quickstart.md)).

**Pruebas**: incluidas, porque las exige la constitución (principio II). En cada historia, las pruebas
van **antes** de su implementación y fallan hasta que la tarea correspondiente esté hecha.

**Responsable**: Saul (módulo `Reservas`). Todas las tareas son suyas. Tocan código común solo dos, las
dos pactadas en el plan y con **revisión de @ara139-ua** en el PR:

- **T016** (C2: `ErrorApi.Extensiones` en `SI18.Cliente/Comun`, research.md V2);
- **T024** (C1: «Validar acceso» en el hueco del personal de `MainLayout.razor`, research.md V10).

**Organización**: por historia de usuario de la spec (US1–US5). Todas las rutas de código parten de
`fuentes/`. Commits con el formato de la constitución: `spec-006 T00X: …`.

## Formato: `- [ ] [ID] [P?] [Historia?] Descripción con ruta`

- **[P]**: se puede hacer en paralelo (ficheros distintos y sin dependencias pendientes).
- **[USn]**: historia de usuario de la spec a la que pertenece.

## Datos de las pruebas de integración

`FabricaApi` comparte una base de datos y un reloj entre todas las pruebas de la colección, y las 16
pistas ya las usa alguna prueba. Para no pisar a nadie (lo que pasó en el #37), HU-06 usa **titular,
personal, pista, días y horas que no usa ninguna otra prueba** (research.md V11, comprobado con
`git grep` el 2026-11-26; T002 lo repite antes de escribir las pruebas):

| Dato             | Valor                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------- |
| Titular          | `ciudadana2.empadronada@si18.test` (Carla Vecina)                                     |
| Personal         | `personal.este@si18.test`; para «otro centro», `personal.oeste@si18.test`              |
| Pista            | «Pista de pádel 2» de Polideportivo Este                                              |
| Fechas y horas   | 16–31/01/2027 a las 10:00 de Madrid, **una fecha por reserva** (ver T008 y T014); 25/10/2026 y 28/03/2027 a las 10:00 (cambios de hora) |
| Reserva HTTP compartida | **una sola** confirmada de Carla en esa pista, en la hora en punto actual de `fabrica.Reloj` (21/12/2026 a las 18:00 de Madrid), creada de forma idempotente por `ValidarAccesoTests` (T007; análisis C1) |

Asignación de fechas (cada reserva, su fecha, para no chocar con `ux_reservas_activa` ni depender del
orden de ejecución; análisis C1 y C2):

| Fecha (10:00–11:00) | Para |
| ------------------- | ---- |
| 18/01/2027 (lunes)  | T008, secuencia primera → segunda → tercera validación |
| 20/01/2027 (miércoles; la franja fija rechazada de esa pista es de 19 a 21) | T008, un rechazo previo no cuenta |
| 21/01/2027 (jueves) | T014, reserva compartida de solo lectura para los límites |
| 22/01/2027 (viernes) | T008, validación aceptada anterior de otra persona |
| 25/01/2027 (lunes)  | T018b (A2), fallo de `SaveChanges` |

**Comprobación del 2026-11-26** (análisis C1): `git grep` de `Este, "Pista de pádel 2"` solo devuelve
`ConsultaDisponibilidadTests.cs:83` (24/12/2026, cierre y franja fija) y `RevisarFranjasFijasTests.cs:113`
(solicitud de los miércoles 19–21 que se rechaza); ninguna prueba usa fechas del 16–31/01/2027 ni a Carla
o `personal.este`; las dos pruebas que miran «hoy» (21/12/2026) buscan Pádel en Polideportivo **Norte**
(`BuscarFranjasTests.cs:141`, `BuscarSinResultadosTests.cs:77`), así que la reserva compartida de las
18:00 en Este no les afecta.

Reglas (E13 de spec-002):

- ninguna prueba de esta spec adelanta `fabrica.Reloj`;
- toda prueba lee `ahora` de `fabrica.Reloj` y construye sus instantes a partir de él;
- los límites y los días de cambio de hora se prueban con `DatosValidaciones.ConServicio(ahoraUtc, …)`
  (T006), con un reloj propio;
- ninguna prueba cuenta filas globales de `validaciones_acceso`: se comprueba por `validacionId` o por
  `reserva_id`, y «no se ha registrado nada» se comprueba de una de estas dos formas (análisis F1):
  - si la petición es del personal, con el máximo `id` antes y después, filtrando por su `personal_id`
    (`DatosValidaciones.UltimoId`);
  - si no lo es (sin sesión u otro rol, que no tienen `personal_id`), buscando un `codigo_leido` único
    de la prueba (un `Guid` nuevo), que no debe aparecer.

---

## Fase 1: Setup (contratos y comprobación de los datos)

**Propósito**: los DTO del endpoint y la certeza de que los datos de prueba siguen libres.

- [X] T001 [P] Crear los DTO en `src/SI18.Contratos/Reservas/`, uno por fichero, según
  [data-model.md § 3](./data-model.md) y
  [contracts/api-validaciones-acceso.openapi.yaml](./contracts/api-validaciones-acceso.openapi.yaml):
  - `PeticionValidacionAcceso(string? Codigo)`: «Obligatorio; se quitan los espacios de los extremos;
    vacío → 400 `PETICION_INVALIDA` con `errors.codigo`»;
  - `ValidacionAccesoDto(long ValidacionId, string Titular, string Instalacion, string Centro, DateOnly Fecha, string HoraInicio, string HoraFin, DateTime? ValidacionAnteriorUtc, string? ValidacionAnteriorHora)`,
    horas «`HH:mm`» de Madrid;
  - `ReservaValidadaDto(string Instalacion, string Centro, DateOnly Fecha, string HoraInicio, string HoraFin)`:
    **sin campo de titular** («para que no se pueda devolver por error», CE-003).
- [X] T002 [P] Repetir el `git grep` de los datos propios (tabla «Datos de las pruebas de integración»)
  antes de escribir ninguna prueba, y anotar el resultado debajo de esta tarea:
  ```powershell
  git grep -n -e "ciudadana2.empadronada" -e "personal.este" -- fuentes/tests
  git grep -n -e "Este, \"Pista de pádel 2\"" -- fuentes/tests
  git grep -n -E "DateOnly\(2027, ?1, ?(1[6-9]|2[0-9]|3[01])\)" -- fuentes/tests
  git grep -n -E "DateOnly\(2026, ?10, ?25\)|DateOnly\(2027, ?3, ?28\)" -- fuentes/tests
  ```
  Si alguna otra prueba usa ya esa pista en esos días y horas, o a Carla o a `personal.este`, elegir
  otros datos libres y actualizar la tabla y research.md V11 **antes** de seguir (lección del #37).

  **Resultado (2026-11-26)**: sin coincidencias de `ciudadana2.empadronada` ni `personal.este`; «Pista de
  pádel 2» de Este solo en `ConsultaDisponibilidadTests.cs:83` y `RevisarFranjasFijasTests.cs:113`; sin
  `DateOnly(2027, 1, 16–31)`. Las fechas 25/10/2026 y 28/03/2027 las usan `MotivosRechazoTests` y
  `BuscarFranjasTests` en pistas de **Norte** (tenis 1 y pádel), y `ServicioReservasTests` usa 18–20/01/2027
  en Norte tenis: ninguna choca con Este «Pista de pádel 2». Datos libres; la tabla no cambia.

**Punto de control**: `dotnet build SI18.slnx` sin advertencias; datos de prueba confirmados libres.

---

## Fase 2: Fundacional (servicio, endpoint y datos de prueba)

**Propósito**: el esqueleto que necesitan todas las historias.

**⚠️ CRÍTICO**: ninguna historia puede empezar sin esta fase.

- [X] T003 Crear `src/SI18.Api/Modulos/Reservas/ServicioValidarAcceso.cs` (clase `sealed`, scoped;
  dependencias: `Si18DbContext`, `TimeProvider`, `IUsuarioActual`) con:
  - el método público `Task<ResultadoValidacion> Validar(string codigo, CancellationToken ct)`, de
    momento con `throw new NotImplementedException()`;
  - el tipo `ResultadoValidacion` en el mismo fichero: o bien aceptada (`ValidacionAccesoDto`), o bien
    rechazada (`string Codigo`, `long ValidacionId`, `ReservaValidadaDto? Reserva`);
  - dos métodos privados que convierten fechas con `HorarioMadrid.ALocal` (fecha `yyyy-MM-dd` y horas
    «`HH:mm`»), **sin escribir `"Europe/Madrid"`** (research.md V8).

  Registrarlo con `services.AddScoped<ServicioValidarAcceso>();` en `AddReservas()` de
  `src/SI18.Api/Modulos/Reservas/RegistroReservas.cs`. Depende de T001.
- [X] T004 Crear `src/SI18.Api/Modulos/Reservas/EndpointsValidacionesAcceso.cs` con
  `MapValidacionesAcceso(this WebApplication app)` y llamarlo desde `MapReservas()` en
  `RegistroReservas.cs` (`Program.cs` no cambia; research.md V1, V7):
  - grupo `/api/validaciones-acceso` con **exactamente** `.RequireAuthorization(Politicas.EsPersonal)`
    (sin sesión → 401 `NO_AUTENTICADO`, otro rol → 403 `ROL_NO_PERMITIDO`; lo prueba T020; análisis A1);
  - `POST /`: lee `PeticionValidacionAcceso`; `codigo = peticion.Codigo?.Trim()`; si es nulo o vacío →
    `ManejadorErrores.CrearValidacion(httpContext, { ["codigo"] = ["Escribe el código de acceso."] })`
    (400, **sin llamar al servicio**: no se registra, RF-004);
  - si no, `ServicioValidarAcceso.Validar(codigo, ct)` y, según el resultado: aceptada →
    `Results.Created("/api/validaciones-acceso/{validacionId}", dto)`; rechazada → Problem Details con
    `ManejadorErrores.Crear(codigo, httpContext)` (estado del catálogo: 403 `QR_OTRO_CENTRO`, 422 el
    resto) más `Extensions["validacionId"]` y, solo si `Reserva` no es nulo, `Extensions["reserva"]`
    (research.md V2).

  Depende de T003.
- ~~T005~~ **Retirada** (análisis A1): no tenía entregable propio. La política va en T004 y la prueba en
  T020. Se conserva el número para no renumerar.
- [X] T006 Crear `tests/SI18.Api.Tests/Reservas/DatosValidaciones.cs` (se apoya en `DatosReservas` sin
  cambiarlo):
  - constantes de la tabla «Datos de las pruebas de integración» (`Carla`, `PersonalEste`,
    `PersonalOeste`, `Este`, `"Pista de pádel 2"`, las fechas);
  - `Task<HttpResponseMessage> Validar(HttpClient cliente, string? codigo)`: `POST /api/validaciones-acceso`;
  - `Task<T> ConServicio<T>(DateTime ahoraUtc, string correoPersonal, Func<ServicioValidarAcceso, Task<T>> accion)`:
    un ámbito y un `Si18DbContext` propios y un `FakeTimeProvider` fijado en `ahoraUtc`, como
    `DatosReservas.ConServicio`; y una sobrecarga que recibe directamente un `IUsuarioActual` de prueba en
    lugar del correo (la usa T018b);
  - `Task<List<ValidacionAcceso>> DeLaReserva(long reservaId)` y `Task<ValidacionAcceso> Leer(long validacionId)`;
  - `Task<long> UltimoId(string correoPersonal)`: el mayor `id` de `validaciones_acceso` de esa persona
    (para comprobar que una petición no registra nada);
  - `Task<bool> HayValidacionConCodigo(string codigoLeido)` (análisis F1);
  - `Task<long> ConfirmadaCompartida()`: la reserva confirmada de Carla en Este «Pista de pádel 2» a la
    hora en punto actual de `fabrica.Reloj` (21/12/2026 a las 18:00 de Madrid), **idempotente**: si ya
    existe una confirmada de Carla en esa pista e inicio (por ejemplo, porque la base de pruebas se
    reutiliza con `SI18_TEST_CONNECTION`), devuelve su `id`; si no, la inserta con
    `DatosReservas.Confirmada` (análisis C1).

  Depende de T002 y T003.

**Punto de control**: `dotnet build SI18.slnx` sin advertencias; `POST /api/validaciones-acceso` responde
400 con el código vacío.

---

## Fase 3: Historia 1 · Validar un código y saber al momento si la persona puede pasar (P1) 🎯 MVP

**Objetivo**: el personal introduce un código válido y ve «Acceso permitido» con titular, instalación y
franja; si ya se había aceptado, con el aviso de la hora anterior.

**Prueba independiente**: quickstart.md Q2, Q3, Q4 y los pasos 1, 3 y 4 de §4.

### Pruebas de US1 (antes de la implementación)

- [X] T007 [P] [US1] Crear `tests/SI18.Api.Tests/Reservas/ValidarAccesoTests.cs` (HTTP, sesión de
  `personal.este`), que implementa `IAsyncLifetime` y en `InitializeAsync` obtiene **una sola vez** la
  reserva compartida con `DatosValidaciones.ConfirmadaCompartida()` (idempotente; análisis C1). Todas
  las pruebas de la clase que necesitan una confirmada en su ventana usan esa reserva y **ninguna
  inserta otra confirmada** en esa pista e inicio. Antes de escribirla, repetir el `git grep` de T002
  para la pista y el 21/12/2026. Casos aceptados:
  - la reserva compartida → 201 con `titular = "Carla Vecina"`,
    `instalacion`, `centro = "Polideportivo Este"`, `fecha` y horas de Madrid, `validacionAnteriorHora`
    nulo **o con la hora de una validación anterior de la misma reserva** (la reserva es compartida y
    otras pruebas de la clase pueden haberla validado antes; el aviso lo prueba T008); y su fila con
    `aceptada = true`, `motivo_rechazo` nulo, `reserva_id`, `personal_id` y `fecha_utc = ahora` (HU1
    esc. 1; HU3 esc. 1);
  - el mismo código con espacios delante y detrás → 201 (RF-004).

  Depende de T006.
- [X] T008 [P] [US1] Crear `tests/SI18.Api.Tests/Reservas/AvisoValidacionAnteriorTests.cs` con
  `DatosValidaciones.ConServicio` (RF-006a, CE-007). **Cada caso tiene su propia reserva de Carla en Este
  «Pista de pádel 2», de 10:00 a 11:00, en su propia fecha** (tabla «Asignación de fechas»; análisis C2):
  - **18/01/2027**, un único `[Fact]` con la secuencia: primera aceptada a las 09:50 → sin aviso;
    segunda a las 10:20 → `ValidacionAnteriorHora = "09:50"`; tercera a las 10:40 → `"10:20"` (la más
    reciente; HU1 esc. 6);
  - **20/01/2027**: un rechazo previo a las 09:40 (fuera de ventana) no cuenta: la primera aceptada, a
    las 09:50, no lleva aviso (HU1 esc. 7);
  - **22/01/2027**: una aceptada por **otra** persona del personal sí cuenta (análisis U1): insertar
    directamente una `ValidacionAcceso` aceptada a las 09:48 con `personal_id` de `personal.oeste` (la
    siembra solo tiene una persona del personal por centro, y la consulta del aviso no filtra por persona
    ni por centro) y validar a las 09:55 con `personal.este` → `ValidacionAnteriorHora = "09:48"`.

  Depende de T006.

### Implementación de US1

- [X] T009 [US1] Implementar `ServicioValidarAcceso.Validar` (research.md V3, V4; data-model.md § 2):
  - `ahora` se lee **una sola vez** al principio;
  - busca con `AsNoTracking` la reserva cuyo `CodigoAcceso == codigo`, con su instalación, su centro y
    el nombre de su titular;
  - `motivo`: sin reserva → `CodigosError.QrDesconocido`; `instalacion.CentroId != IUsuarioActual.CentroId`
    → `CodigosError.QrOtroCentro`; si no, `ReglaAcceso.MotivoRechazo(reserva, ahora)` (**no** usar
    `PermisosCentro.ExigirMismoCentro`);
  - crea `new ValidacionAcceso(reserva?.Id, codigo, motivo is null, motivo, ahora, IUsuarioActual.Id)`
    («`codigo_leido` recortado a 64» lo hace la entidad), `Add` y **un solo `SaveChanges`** (RF-008,
    RF-009); la reserva no se modifica (RF-010);
  - aceptada → `ValidacionAccesoDto` con titular, instalación, centro, fecha y horas; rechazada →
    `Codigo` y `ValidacionId` (la `Reserva` del rechazo la completa T015).

  Depende de T003, T007.
- [X] T010 [US1] Añadir a `Validar` el aviso de RF-006a (research.md V5): solo si se acepta y **antes**
  de insertar, `MAX(FechaUtc)` de `ValidacionesAcceso` con `ReservaId == reserva.Id && Aceptada`; si hay
  valor, `ValidacionAnteriorUtc` y `ValidacionAnteriorHora` («`HH:mm`» de Madrid). Depende de T008, T009.
- [ ] T011 [P] [US1] Añadir `ComprobarPersonal(EstadoSesion sesion)` a
  `src/SI18.Cliente/Reservas/AccesoReservas.cs`, con el mismo patrón que `Comprobar`: sin sesión →
  `SinSesion`; rol `personal_centro` → `Permitido`; otro → `SinPermiso`; y la constante
  `AvisoSinPermisoValidar = "Validar códigos de acceso es para el personal de centro."`.
- [ ] T012 [US1] Crear `src/SI18.Cliente/Reservas/ValidarAcceso.razor` (`@page "/validar-acceso"`) y
  `ValidarAcceso.razor.css` (**sin tocar `app.css`**), con el resultado aceptado (research.md V10):
  - `<PageTitle>Validar acceso · SI-18</PageTitle>` y `<h1>` «Validar acceso»; acceso por URL con
    `ComprobarPersonal` (sin sesión → `/iniciar-sesion`; sin permiso → aviso sin formulario);
  - `<form novalidate>` con `MudTextField` «Código de acceso» (etiqueta visible, `HelperText` «El código
    que aparece bajo el QR», `UserAttributes` con `autocomplete="off"`, `autocapitalize="none"`,
    `spellcheck="false"`) y `MudButton` de envío «Validar»;
  - campo vacío: no se envía; error con el **patrón de `PagarReserva.razor`** (MudBlazor 8.15):
    `<span id="error-codigo" role="alert" class="solo-lector">Escribe el código de acceso.</span>` y
    `["aria-errormessage"] = "error-codigo"` en `UserAttributes` solo mientras hay error;
  - aceptada: `<section>` con `<h2>` «Acceso permitido» y un icono (`MudIcon`, `aria-hidden`), y un
    `<dl>` con titular, instalación y centro, fecha (`TextosReservas.DiaYFecha`) y
    `TextosReservas.Horas(...)`; si hay `ValidacionAnteriorHora`, un aviso con icono «Este código ya se
    validó hoy a las HH:mm»;
  - una frase completa con `MensajeAccesible.Mostrar(...)` (la única región viva), el campo vacío y el
    **foco de vuelta al campo** (`FocusAsync`; RF-014).

  Depende de T004 y T011.

**Punto de control**: T007 y T008 en verde; quickstart.md Q2–Q4 y §4 pasos 1, 3 y 4.

---

## Fase 4: Historia 2 · Rechazar con un motivo claro los códigos que no dan acceso (P1)

**Objetivo**: cada código que no da acceso se rechaza con su motivo del catálogo y, si es del mismo
centro, con la instalación y la franja (nunca el titular).

**Prueba independiente**: quickstart.md Q1, Q6, Q7, Q10 y los pasos 5 y 6 de §4.

### Pruebas de US2 (antes de la implementación)

- [ ] T013 [P] [US2] Añadir a `tests/SI18.Api.Tests/Reservas/ValidarAccesoTests.cs` los rechazos por HTTP:
  - `"noexiste"` → 422 `QR_DESCONOCIDO`, con `validacionId` y **sin** extensión `reserva`; fila con
    `reserva_id` nulo (HU2 esc. 4; HU3 esc. 3);
  - la reserva compartida (T007) con la sesión de `personal.oeste` → 403 `QR_OTRO_CENTRO`, sin extensión
    `reserva`; fila con su `reserva_id` y `aceptada = false` (HU2 esc. 5);
  - reserva cancelada de Carla (`DatosReservas.Cancelada`) en la misma pista e inicio que la compartida
    (una cancelada no es activa y no choca con `ux_reservas_activa`), también idempotente → 422
    `QR_RESERVA_NO_CONFIRMADA` con la extensión `reserva` (instalación, centro, fecha y horas) y sin
    ningún campo de titular (HU2 esc. 3; CE-003);
  - un código de 100 caracteres → 422 `QR_DESCONOCIDO`, registrado recortado a 64.

  Depende de T006.
- [ ] T014 [P] [US2] Crear `tests/SI18.Api.Tests/Reservas/LimitesValidacionTests.cs` con
  **`[Trait("Categoria", "Limites")]`** y `DatosValidaciones.ConServicio` (reloj propio; research.md V11;
  CE-002):
  - **una** reserva de Carla el 21/01/2027 de 10:00 a 11:00, compartida de solo lectura por todos los
    casos de la clase (creada de forma idempotente en `IAsyncLifetime`; los resultados de estos casos
    no dependen del historial de validaciones; análisis C2): 09:45:00 aceptada; 09:46 aceptada; 09:44:59
    `QR_FUERA_DE_VENTANA` con `Reserva` informada; 11:00:00 aceptada; 11:00:01 y 11:01
    `QR_FUERA_DE_VENTANA` (HU1 esc. 2; HU2 esc. 1 y 2);
  - cambios de hora: reserva a las 10:00 de Madrid del 25/10/2026 y del 28/03/2027: aceptada a las
    09:45:00 de Madrid y rechazada a las 09:44:59 (15 minutos **reales**, con `HorarioMadrid.AUtc`).

  Depende de T006.

### Implementación de US2

- [ ] T015 [US2] Completar los rechazos en `ServicioValidarAcceso.Validar`: con
  `QR_RESERVA_NO_CONFIRMADA` y `QR_FUERA_DE_VENTANA`, `Reserva = new ReservaValidadaDto(...)` con
  instalación, centro, fecha y horas de Madrid; con `QR_DESCONOCIDO` y `QR_OTRO_CENTRO`, `Reserva = null`
  (RF-007; research.md V2). Depende de T009, T013, T014.
- [ ] T016 [US2] **[código común, revisión de @ara139-ua — C2]** Añadir a
  `src/SI18.Cliente/Comun/ErrorApi.cs` la propiedad
  `IReadOnlyDictionary<string, JsonElement> Extensiones { get; init; }` (vacía por defecto) y rellenarla
  en `src/SI18.Cliente/Comun/ClienteApi.cs` al leer el Problem Details (todas las propiedades que no son
  `type`, `title`, `status`, `detail`, `instance`, `codigo`, `traceId` ni `errors`). Aditivo: ninguna
  pantalla existente cambia. Prueba en `tests/SI18.Api.Tests/Comun/ClienteApiExtensionesTests.cs` (la API
  referencia `SI18.Cliente`) con un `HttpMessageHandler` falso que devuelve un 422 con `validacionId` y
  `reserva`: `ErrorApi.Extensiones` los contiene y `Codigo`, `Detalle` y `Errores` no cambian. El
  constructor de `ClienteApi` necesita un `NavigationManager`: usar en la prueba una subclase mínima
  (`NavegacionDePrueba : NavigationManager`, que llama a `Initialize("http://localhost/", "http://localhost/")`
  y no navega; análisis U3).
  Anotar en el PR que es un cambio en código común (research.md V2; plan.md C2).
- [ ] T017 [US2] Completar `ValidarAcceso.razor` con el resultado rechazado: `<h2>` «Acceso denegado» con
  un icono distinto del de «permitido», el `Detalle` del catálogo y, si
  `ErrorApi.Extensiones["reserva"]` existe, instalación, centro, fecha y horas (nunca el titular); frase
  completa con `MensajeAccesible` y foco de vuelta al campo. Con `ROL_NO_PERMITIDO` (sesión cambiada),
  el aviso sin formulario. Depende de T012 y T016.

**Punto de control**: T013 y T014 en verde (`--filter-trait "Categoria=Limites"` incluido); quickstart.md
Q1, Q6, Q7 y Q10 y §4 pasos 5 y 6.

---

## Fase 5: Historia 3 · Que cada intento de validación quede registrado (P1)

**Objetivo**: una fila por intento aceptado o rechazado, y ninguna con 400.

**Prueba independiente**: quickstart.md Q5 y las filas de Q1–Q7 en `validaciones_acceso`.

- [ ] T018 [P] [US3] Añadir a `ValidarAccesoTests.cs`: `{ "codigo": "   " }` y `{}` → 400
  `PETICION_INVALIDA` con `errors.codigo` y **ningún** registro nuevo de `personal.este`
  (`DatosValidaciones.UltimoId` igual antes y después; RF-004, CE-004); y que una validación aceptada no
  cambia la reserva (`estado`, `codigo_acceso` y `cancelada_en_utc` iguales; RF-010). Depende de T006.
- [ ] T018b [US3] Añadir a `tests/SI18.Api.Tests/Reservas/ValidarAccesoTests.cs` la prueba de RF-009
  (análisis A2), **con el fallo provocado dentro de la propia prueba y sin dejar filas a medias en la
  base compartida**:
  - reserva confirmada de Carla el **25/01/2027** de 10:00 a 11:00 (idempotente);
  - `DatosValidaciones.ConServicio` con un `IUsuarioActual` de prueba de rol personal de centro, centro
    Este e `Id` que no existe en `usuarios` (por ejemplo, `int.MaxValue`), a las 09:50: el `INSERT` viola
    la FK `personal_id → usuarios`, así que `SaveChanges` lanza;
  - comprobar que `Validar` **lanza** (no devuelve ningún `ResultadoValidacion` aceptado ni rechazado)
    y que, después, no hay **ninguna** fila de `validaciones_acceso` de esa reserva (el `INSERT` fallido
    no deja nada: un solo `SaveChanges`, en su propia transacción implícita);
  - que la excepción llega al manejador global como 500 `ERROR_INTERNO` sin trazas ya lo prueba
    `Comun/ErroresTests.cs` de 000-core; no se duplica.

  Depende de T006.
- [ ] T019 [US3] Comprobar que `ServicioValidarAcceso` hace un único `SaveChanges` y que T018b pasa
  (RF-009). Depende de T009, T018, T018b.

**Punto de control**: CE-004 cubierto por las pruebas de T007, T013 y T018.

---

## Fase 6: Historia 4 · Solo el personal de centro puede validar (P1)

**Objetivo**: los demás roles y las peticiones sin sesión no validan ni dejan registro; el menú solo lo
ve el personal.

**Prueba independiente**: quickstart.md Q8, Q9 y el paso 7 de §4.

- [ ] T020 [P] [US4] Añadir a `ValidarAccesoTests.cs`: sin sesión → 401 `NO_AUTENTICADO`; con
  `ciudadana2.empadronada`, `representante.padel` y `tecnico` → 403 `ROL_NO_PERMITIDO`; en ninguno se
  registra nada: cada petición envía un `codigo_leido` único (un `Guid` nuevo) y
  `DatosValidaciones.HayValidacionConCodigo` devuelve `false` (esas peticiones no tienen `personal_id`;
  análisis F1) (HU4 esc. 1 y 2; CE-004). Depende de T006.
- ~~T021~~ **Retirada** (análisis A1): era condicional («si T020 no pasa»). La política se fija en T004 y
  T020 la prueba. Se conserva el número para no renumerar.
- [ ] T022 [P] [US4] Comprobar en `ValidarAcceso.razor` el acceso por URL (T012): sin sesión →
  `/iniciar-sesion`; con un ciudadano → `AvisoSinPermisoValidar` y ningún formulario. Depende de T012.
- [ ] T023 [US4] (sin código) Confirmar con @ara139-ua el texto «Validar acceso» y su posición en el hueco
  del personal, dejando sitio al enlace de cierres de spec-007 (plan.md C1).
- [ ] T024 [US4] **[código común, revisión de @ara139-ua — C1]** En
  `src/SI18.Cliente/Layout/MainLayout.razor`, dentro de
  `<AuthorizeView Roles="personal_centro" Context="personal">`, sustituir el comentario del hueco por
  `<MudButton Href="/validar-acceso" Color="Color.Inherit" Variant="Variant.Text">Validar acceso</MudButton>`
  y un comentario con el hueco que queda para spec-007 (cierres). Una línea, como «Mis reservas» en
  spec-002. Depende de T023.

**Punto de control**: T020 en verde; el menú muestra «Validar acceso» solo al personal.

---

## Fase 7: Historia 5 · Validar desde el móvil sin barreras de accesibilidad (P2)

**Objetivo**: validación completa con teclado, lector de pantalla y a 320 px (WCAG 2.1 AA; CE-006).

**Prueba independiente**: quickstart.md §5 (que aplica el §6 de `000-core/quickstart.md`).

- [ ] T025 [US5] Completar la accesibilidad de `ValidarAcceso.razor` y `ValidarAcceso.razor.css`
  (RF-013–RF-015; research.md V10):
  - orden de tabulación: campo → «Validar»; sin trampas de foco; tras cada resultado el foco vuelve al
    campo;
  - `MensajeAccesible` es la única región viva; la `<section>` del resultado no lleva `role="status"` ni
    `aria-live` (evitar el doble anuncio);
  - **reflujo a 320 px**: una sola columna; campo y botón al 100 % del ancho por debajo de 600 px; el
    `<dl>` del resultado apilado; ningún elemento de `<main>` pasa del borde derecho. Estilos solo en
    `ValidarAcceso.razor.css`.

  Depende de T012 y T017.
- [ ] T026 [US5] Comprobar a mano quickstart.md §5 sobre `/validar-acceso`, **aplicando lo aprendido en
  T073 de 000-core** (#41, #42):
  - **foco visible con MudBlazor**: Lighthouse y axe no lo detectan. Recorrer con el teclado y, en el
    `MudTextField` «Código de acceso», el `MudButton` «Validar» y el enlace «Validar acceso» del menú,
    mirar en DevTools el estilo calculado con foco de teclado: `outline-style` no puede ser `none`;
  - **barra superior a 320 px** con la sesión de `personal.este` y «Validar acceso» en el menú (T024),
    y con zoom al 200 % sobre 1280 px: sin desplazamiento horizontal y con «Validar acceso», el nombre y
    «Cerrar sesión» alcanzables y legibles;
  - si algo falla por `app.css` o `MainLayout.razor` (código común), **no se corrige en Reservas ni se
    duplica la regla**: se anota en el PR con el estilo calculado o la medida, y se coordina con
    @ara139-ua;
  - NVDA: etiqueta, ayuda y error del campo; cada resultado (permitido, permitido con aviso, denegado
    con y sin datos de la reserva) anunciado **una sola vez** con el texto completo;
  - «Acceso permitido» y «Acceso denegado» escritos y con iconos distintos, no solo por el color;
  - **menú de los demás roles** (análisis E1; HU-4 esc. 3, RF-016): con `ciudadana2.empadronada@si18.test`
    y con `tecnico@si18.test`, el menú **no** muestra «Validar acceso»;
  - Lighthouse ≥ 95 y axe sin infracciones graves ni críticas con el formulario vacío, con un resultado
    aceptado con aviso y con uno rechazado.

  Anotar el resultado en la descripción del PR (principio I). Depende de T024 y T025.

**Punto de control**: CE-006 cumplido, o sus fallos ajenos anotados y coordinados.

---

## Fase 8: Pulido y cierre

- [ ] T027 [P] Comprobar que no se ha escrito la zona horaria a mano (research.md V8; lección de T078 de
  000-core): `git grep -n "Europe/Madrid" -- fuentes/src/SI18.Api/Modulos/Reservas fuentes/src/SI18.Cliente/Reservas fuentes/src/SI18.Contratos/Reservas`
  sin resultados.
- [ ] T028 Ejecutar quickstart.md §2–§5 de principio a fin: `dotnet test SI18.slnx` en verde (también
  con `--filter-trait "Categoria=Limites"`), los escenarios Q1–Q10 contra la API y los pasos de la
  pantalla. Medir y anotar en el PR:
  - el tiempo desde pulsar «Validar» hasta que se ve y se anuncia el resultado, **20 veces**: < 1 s en
    al menos **19 de 20** (CE-001, 95 %; análisis U2);
  - con un cronómetro, el tiempo de validar un código escrito a mano desde que se tiene delante:
    < 15 s (CE-005);
  - 20 llamadas a `POST /api/validaciones-acceso` con la API local (aceptadas y rechazadas): p95
    < 300 ms (000-core; research.md V12).
- [ ] T029 [P] Avisar al grupo (constitución, «Acuerdos de colaboración»; plan.md, «Puntos a coordinar»):
  - a **C** (C4): HU-03 y HU-07 **no tienen que hacer nada** para invalidar un código; una reserva
    cancelada por `IServicioReservas` se rechaza sola con `QR_RESERVA_NO_CONFIRMADA`; enlace a
    contracts/modulos.md;
  - a **@ara139-ua** (C1, C2): revisión de T016 y T024, y recordatorio del hueco de cierres que queda en
    el menú del personal.

  Anotarlo en la descripción del PR.
- [ ] T030 Abrir los PR hacia `develop`, con la lista de los principios I–IX revisados, como en spec-002.
  **Antes de abrir cada PR, revisar los secretos** (constitución, «Flujo de trabajo», principio V;
  análisis D1): `git grep -nE "Password=|SI18_CLAVE_DEMO=.+[^>]$" -- ":!*.example"` (quickstart §7 de
  000-core; solo deben salir las coincidencias ya justificadas en T075 de 000-core) y
  `git ls-files fuentes/.env` (sin resultados), con el resultado en la descripción del PR.
  - **PR de API**: T001–T004, T006–T010, T013–T015, T018, T018b, T019, T020 y T027 (T005 y T021,
    retiradas). Sin código común; se puede fusionar antes (el endpoint se prueba con quickstart.md §3);
  - **PR de pantallas**: T011, T012, T016, T017, T022–T026, con la comprobación de accesibilidad de
    T026; **revisa @ara139-ua** el código común de T016 (C2) y T024 (C1).

  Los aprueba otra persona del grupo.
- [ ] T031 Después de fusionar los dos PR, ejecutar `/speckit-converge` para la spec 006.

---

## Dependencias y orden de ejecución

### Dependencias entre fases

```text
Fase 1 Setup (T001 ∥ T002)
   └─► Fase 2 Fundacional (T003 → T004; T006 tras T002 y T003)          *T005 retirada
          ├─► US1 (T007 ∥ T008 → T009 → T010; cliente: T011 → T012)
          │      ├─► US2 (T013 ∥ T014 → T015; T016 → T017 tras T012)
          │      ├─► US3 (T018 ∥ T018b → T019)
          │      └─► US4 (T020; T022 tras T012; T023 → T024)          *T021 retirada
          │             └─► US5 (T025 → T026)          *tras US1, US2 y US4 en el cliente
          └─► Fase 8 Pulido (T027–T031)
```

- **US1 y US2** completan el mismo método `Validar` (T009 → T010 → T015), por eso van en serie en el
  servicio; sus pruebas se pueden escribir antes (fallan hasta que la tarea correspondiente esté).
- **T016 (C2)** solo depende de la Fase 1: se puede adelantar para dar tiempo a la revisión de
  @ara139-ua. T017 no se puede terminar sin él.
- **US5** va al final porque comprueba la pantalla ya construida y el enlace del menú (T024).
- **T026** no se marca hasta haberla hecho a mano, con su resultado en el PR.

### Paralelismo dentro de cada fase

- **Fase 1**: T001 y T002 en paralelo.
- **US1**: T007 y T008 en paralelo; en el cliente, T011 en paralelo con toda la API.
- **US2**: T013 y T014 en paralelo; T016 en paralelo con toda la API.
- **US3 y US4**: T018, T018b, T020 y T022 en paralelo.
- **Datos**: cada clase de prueba usa sus propias fechas (tabla «Asignación de fechas»), así que las
  clases se pueden escribir y ejecutar en paralelo sin chocar.

### Ejemplo de reparto en el tiempo (una persona)

```text
Día 1:  T001 ∥ T002 → T003 → T004 → T006 → T007 ∥ T008 → T009 → T010            (MVP de API)
Día 2:  T013 ∥ T014 → T015 → T018 ∥ T018b → T019 → T020 → T027                  (PR de API)
Día 3:  T016 → T011 → T012 → T017 → T022 → T023 → T024 → T025 → T026            (PR de pantallas)
Día 4:  T028 → T029 → T030 → T031
```

## Estrategia de implementación

### MVP (US1)

1. Fases 1 y 2: DTO, datos de prueba comprobados, servicio y endpoint.
2. US1: validar un código y ver el resultado aceptado, con el aviso de la validación anterior.
3. **Validar**: quickstart.md Q2–Q4.

### Entrega incremental

US1 (aceptar y avisar) → US2 (rechazos con su motivo y límites) → US3 (registro) → US4 (solo el personal
y el menú) → US5 (accesibilidad) → Pulido. Si las pantallas se retrasan (por ejemplo, por la revisión de
C2), el PR de API se abre antes (T030).

## Notas

- `[P]` = ficheros distintos, sin dependencias pendientes.
- Ninguna tarea crea tablas, migraciones, entidades, estados, valores de negocio ni códigos de error:
  todo es de 000-core (principio VII). Si hiciera falta alguno, se propone primero en 000-core.
- `ReglaAcceso`, `ValidacionAcceso`, `CodigosError` y `ManejadorErrores` se usan sin cambiarlos.
- Fuera de `*/Reservas` solo se tocan, con revisión de @ara139-ua: `ErrorApi.cs` y `ClienteApi.cs`
  (T016, C2) y `MainLayout.razor` (T024, C1). `Program.cs` no cambia.
- Ninguna fecha ni hora se convierte con `"Europe/Madrid"` escrito a mano (T027).
