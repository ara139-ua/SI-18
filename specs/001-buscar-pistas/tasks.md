---

description: "Lista de tareas de 001 · Buscar pistas libres (HU-01)"
---

# Tareas: 001 · Buscar pistas libres (HU-01)

**Entrada**: documentos de diseño de `specs/001-buscar-pistas/` ([plan.md](./plan.md),
[spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md),
[contracts/](./contracts/modulos.md), [quickstart.md](./quickstart.md)).

**Pruebas**: incluidas, porque las exige la constitución (principio II).

**Responsable**: Artur (módulo `Disponibilidad`). Todas las tareas son suyas. La única dependencia
externa es T054 de 000-core (catálogo, Saul), que solo bloquea la pantalla.

**Organización**: por historia de usuario de la spec (US1–US4). Todas las rutas de código parten de
`fuentes/`. Commits con el formato de la constitución: `spec-001 T00X: …`.

## Formato: `- [ ] [ID] [P?] [Historia?] Descripción con ruta`

- **[P]**: se puede hacer en paralelo (ficheros distintos y sin dependencias pendientes).
- **[USn]**: historia de usuario de la spec a la que pertenece.

## Datos de las pruebas de integración

`FabricaApi` comparte una base de datos entre todas las pruebas de la colección y fija el reloj en el
**lunes 21/12/2026 a las 18:00 de Madrid**. Para que las pruebas no se pisen, cada clase de pruebas usa
su propio centro y sus propias fechas (anotadas en cada tarea), e inserta lo que necesita con
`fabrica.Services.CreateScope()` + `Si18DbContext`:

- reservas con `new Reserva(usuarioId, instalacionId, inicioUtc, tarifa, importe, creadaEnUtc)` (y
  `Confirmar` o `Caducar` si hace falta);
- cierres con `new CierreMantenimiento(instalacionId, inicioUtc, finUtc, motivo, registradoPorId, registradoEnUtc)`;
- franjas fijas con `new SolicitudFranjasFijas(clubId, instalacionId, temporadaId, diasSemana, horaInicio, horaFin, creadaEnUtc)` + `Aprobar(ahora)`.
  La temporada sembrada es «2026-2027» (01/09/2026–30/06/2027).

Cada centro sembrado tiene «Pista de tenis 1», «Pista de tenis 2», «Pista de pádel 1» y «Pista de pádel 2».

---

## Fase 1: Setup (entrada del módulo)

**Propósito**: que el módulo `Disponibilidad` exista en la API con su registro y sus contratos.

- [X] T001 Crear `src/SI18.Api/Modulos/Disponibilidad/RegistroDisponibilidad.cs` con
  `AddDisponibilidad(this IServiceCollection)` y `MapDisponibilidad(this WebApplication)`, de momento
  vacíos, y añadir en `src/SI18.Api/Program.cs` **solo dos líneas**: `builder.Services.AddDisponibilidad();`
  después de `AddComun()` y `app.MapDisponibilidad();` después de `MapComun()` (research.md D6;
  convención propuesta para 002–008).
- [X] T002 [P] Crear los DTO en `src/SI18.Contratos/Disponibilidad/`, uno por fichero, según
  [data-model.md § 4](./data-model.md) y [contracts/api-disponibilidad.openapi.yaml](./contracts/api-disponibilidad.openapi.yaml):
  - `ResultadoBusquedaDto(DateOnly Fecha, int CentroId, int DeporteId, int TotalPistas, IReadOnlyList<PistaLibreDto> Pistas)`;
  - `PistaLibreDto(int InstalacionId, string Nombre, TipoInstalacion Tipo, IReadOnlyList<FranjaLibreDto> Franjas)`,
    usando el `TipoInstalacion` de `SI18.Contratos.Comun`;
  - `FranjaLibreDto(DateTime InicioUtc, DateTime FinUtc, string HoraInicio, string HoraFin, decimal Precio)`,
    con `HoraInicio`/`HoraFin` en formato «`HH:mm`» y `Precio` en euros con «dos decimales».

**Punto de control**: `dotnet build SI18.slnx` sin advertencias.

---

## Fase 2: Fundacional (consultas provisionales y evaluación de una pista)

**Propósito**: lo que necesitan todas las historias: leer reservas, cierres y franjas fijas, y decidir
qué franjas de una pista están libres.

**⚠️ CRÍTICO**: ninguna historia puede empezar sin esta fase.

- [X] T003 [P] Crear `src/SI18.Api/Modulos/Disponibilidad/Provisional/ConsultaReservasProvisional.cs`,
  que implementa `IConsultaReservas` (research.md D5):
  - `Activas(instalacionId, desdeUtc, hastaUtc)` devuelve, con `AsNoTracking`, las reservas de la
    instalación con `desdeUtc ≤ InicioUtc < hastaUtc` y
    `Estado == Confirmada || (Estado == PendientePago && CreadaEnUtc > ahora − ValoresNegocio.TiempoBloqueoPago)`,
    con `ahora` tomado de `TimeProvider` (la misma vigencia que `Reserva.EstaVigente`), como `ReservaActiva`;
  - comentario de cabecera: «PROVISIONAL (spec-001, RF-014a): sustituir por la implementación de spec-002 (Saul) y borrar este fichero».
- [X] T004 [P] Crear `src/SI18.Api/Modulos/Disponibilidad/Provisional/ConsultaCierresProvisional.cs`,
  que implementa `IConsultaCierres`:
  - `Cierres(instalacionId, desdeUtc, hastaUtc)` devuelve, con `AsNoTracking`, los cierres de la
    instalación que se solapan con `[desdeUtc, hastaUtc)` (`InicioUtc < hastaUtc && desdeUtc < FinUtc`);
  - comentario «PROVISIONAL (spec-001, RF-014a): sustituir por la implementación de spec-007 (C) y borrar este fichero».
- [X] T005 [P] Crear `src/SI18.Api/Modulos/Disponibilidad/Provisional/ConsultaFranjasFijasProvisional.cs`,
  que implementa `IConsultaFranjasFijas`:
  - `FranjasBloqueadas(instalacionId, desde, hasta)`: para cada solicitud `Aprobada` de la
    instalación, y para cada fecha de `[desde, hasta]` que esté dentro de su temporada
    (`FechaInicio ≤ fecha ≤ FechaFin`) y cuyo día ISO (lunes = 1 … domingo = 7) esté en `DiasSemana`,
    devuelve las franjas de `CalculadoraFranjas.FranjasDelDia` con `HoraInicio ≥ solicitud.HoraInicio`
    y `HoraInicio < solicitud.HoraFin`;
  - `RepresentantesAfectados` lanza `NotImplementedException("Lo implementa spec-005 para HU-07")`;
  - comentario «PROVISIONAL (spec-001, RF-014a): sustituir por la implementación definitiva de spec-005 y borrar este fichero».
    No descuenta los cierres (research.md D5).
- [X] T006 Registrar las tres provisionales en `AddDisponibilidad()`
  (`src/SI18.Api/Modulos/Disponibilidad/RegistroDisponibilidad.cs`) con `services.TryAddScoped<IConsultaReservas, ConsultaReservasProvisional>()`
  y lo mismo para las otras dos, con un comentario que explique por qué `TryAdd` (research.md D5).
  Depende de T001 y T003–T005.
- [X] T007 Crear `tests/SI18.Api.Tests/Disponibilidad/ConsultasProvisionalesTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, `[Trait("Categoria","Limites")]`, Polideportivo Oeste,
  fechas de enero de 2027):
  - **Reservas**: una pendiente creada hace 9:59 sigue activa, y una creada hace exactamente 10:00 ya
    no (`fabrica.Reloj`); una confirmada sí; una caducada y una cancelada no; fuera de `[desde, hasta)` no.
  - **Cierres**: con un cierre de 10:00 a 12:00, la ventana de 09:00 a 10:00 no lo devuelve y la de
    11:00 a 12:00 sí.
  - **Franjas fijas**: una solicitud aprobada de martes y jueves de 19:00 a 21:00 da las franjas de
    19:00 y 20:00 un martes de la temporada y ninguna un miércoles; ninguna en julio de 2027; una
    solicitud pendiente o rechazada no da ninguna.

  Depende de T006.
- [X] T008 Crear `src/SI18.Api/Modulos/Disponibilidad/ServicioDisponibilidad.cs` (clase `sealed`,
  scoped; dependencias: `Si18DbContext`, `TimeProvider`, `IConsultaReservas`, `IConsultaCierres` e
  `IConsultaFranjasFijas`) con un método **privado** único
  `Task<IReadOnlyList<(Franja Franja, string? Motivo)>> EvaluarPista(Instalacion pista, DateOnly fecha, CancellationToken ct)`:
  - calcula `CalculadoraFranjas.FranjasDelDia(pista.Id, fecha)`;
  - lee cierres, bloqueos y reservas activas del día de Madrid
    `[HorarioMadrid.AUtc(fecha, 00:00), HorarioMadrid.AUtc(fecha.AddDays(1), 00:00))`;
  - aplica `ReglaDisponibilidad.MotivoNoDisponible(franja, ahoraUtc, pista.ReservablePorFranjas, cierres, bloqueos, inicios)`.

  Es el único sitio donde se decide si una franja está libre: lo usan la búsqueda (US1) y
  `IConsultaDisponibilidad` (US2) (research.md D4). Registrarlo como `AddScoped<ServicioDisponibilidad>()`
  en `AddDisponibilidad()`. Depende de T006.

**Punto de control**: las consultas provisionales están probadas y `EvaluarPista` compila.

---

## Fase 3: Historia 1 · Ver las franjas libres de un deporte en un centro y una fecha (P1) 🎯 MVP

**Objetivo**: `GET /api/disponibilidad/franjas` devuelve solo las franjas libres según RF-011 de
000-core, agrupadas por pista y en orden horario, y la pantalla las muestra.

**Prueba independiente**: quickstart.md B1, B4 y B7–B10, más los escenarios 1–6 de la historia 1
contra PostgreSQL.

- [X] T009 [US1] Añadir a `ServicioDisponibilidad` el método público
  `Task<ResultadoBusquedaDto> Buscar(int deporteId, int centroId, DateOnly fecha, TipoTarifa tarifa, CancellationToken ct)`:
  - si `fecha < HorarioMadrid.Hoy(ahora)` o `fecha > Hoy + ValoresNegocio.AntelacionMaximaDias`, lanza
    `CodigosError.Error(FueraDeAntelacion)` sin consultar nada;
  - si el deporte o el centro no existen, lanza `CodigosError.Error(NoEncontrado)`;
  - las pistas candidatas son las instalaciones con `CentroId == centroId`, `ReservablePorFranjas` y el
    deporte entre sus deportes (data-model.md § 2); `TotalPistas = candidatas.Count`;
  - para cada candidata, `EvaluarPista` y se queda con las franjas de `Motivo == null`; las pistas sin
    ninguna se omiten;
  - cada franja lleva `HoraInicio`/`HoraFin` de `HorarioMadrid.ALocal` en «`HH:mm`» y
    `Precio = CalculadoraPrecio.Precio(pista.Tipo, tarifa, tarifas)`, con las tarifas leídas una sola
    vez por petición;
  - orden: pistas por `Nombre` y franjas por `InicioUtc` (RF-009).

  Depende de T002 y T008.
- [X] T010 [US1] Crear `src/SI18.Api/Modulos/Disponibilidad/EndpointsDisponibilidad.cs` con
  `GET /api/disponibilidad/franjas` (`.RequireAuthorization(Politicas.EsCiudadanoORepresentante)`):
  - parámetros `int? deporteId`, `int? centroId` y `string? fecha`; si falta alguno o `fecha` no es
    `yyyy-MM-dd`, responde 400 `PETICION_INVALIDA` con `ManejadorErrores.CrearValidacion` y
    `errors` por campo en camelCase y en español («Elige un deporte.», «Elige un centro.», «Elige una
    fecha válida.»);
  - la tarifa sale de `IUsuarioActual.TipoTarifa`;
  - devuelve `Results.Ok(await servicio.Buscar(...))`.

  Mapearlo en `MapDisponibilidad()`. Depende de T009.
- [X] T011 [US1] Crear `tests/SI18.Api.Tests/Disponibilidad/BuscarFranjasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Norte, «Pádel», fechas del 22 al 28/12/2026),
  con `fabrica.CrearClienteConSesion("ciudadana.empadronada@si18.test")`:
  - **esc. 1**: sin nada en la fecha, 2 pistas × 14 franjas de «08:00» a «21:00», `TotalPistas = 2`;
  - **esc. 2**: una reserva confirmada a las 18:00 y una pendiente vigente a las 19:00 en «Pista de
    pádel 1» → esas franjas faltan en esa pista y están en la «Pista de pádel 2»;
  - **esc. 3**: una pendiente caducada → la franja sí aparece;
  - **esc. 4**: un cierre de 10:00 a 12:00 en la «Pista de pádel 2» → faltan las de 10:00 y 11:00, y
    están las de 09:00 y 12:00;
  - **esc. 5**: una franja fija aprobada en el día de la semana de la fecha, de 19:00 a 21:00, en la
    «Pista de pádel 1» → faltan las de 19:00 y 20:00;
  - **franja fija con cierre** (spec, «Casos límite»): en otra fecha, la misma franja fija más un
    cierre de 19:00 a 20:00 en esa pista → faltan igualmente las de 19:00 y 20:00;
  - **esc. 6**: con la fecha de hoy (21/12/2026, 18:00), solo aparecen franjas de 19:00 en adelante, y
    con `Reloj` a las 17:59:59, la de 18:00 sí aparece (`[Trait("Categoria","Limites")]`);
  - **orden**: pistas por nombre y franjas por hora;
  - **errores**: sin sesión → 401 `NO_AUTENTICADO`; `personal.norte@si18.test` y `tecnico@si18.test`
    → 403 `ROL_NO_PERMITIDO`; sin `fecha` → 400 `PETICION_INVALIDA` con `errors.fecha`;
    `centroId=999999` → 404 `NO_ENCONTRADO`;
  - **cambio de hora** (`[Trait("Categoria","Limites")]`): con `Reloj` el 20/10/2026, la búsqueda del
    25/10/2026 da 14 franjas por pista y la primera es `2026-10-25T07:00:00Z` con «08:00».

  Los límites de tiempo se prueban con `DatosDisponibilidad.ConServicio(ahoraUtc, …)`, que usa un reloj propio:
  `FakeTimeProvider` no retrocede y el reloj de `FabricaApi` lo comparten todas las pruebas, así que no se
  cambia. Depende de T010.
- [ ] T012 [US1] Crear `src/SI18.Cliente/Disponibilidad/BuscarPistas.razor` (ruta `/buscar-pistas`;
  research.md D7):
  - **acceso** (spec, «Casos límite» y aclaración 2; RF-001): en `OnInitializedAsync`, antes de cargar
    los filtros, se lee el `AuthenticationState` de `EstadoSesion`:
    - sin sesión → `NavigationManager.NavigateTo("/iniciar-sesion")`;
    - con sesión pero sin rol `ciudadano` ni `representante_club` → no se carga ni se muestra el
      formulario, y aparece en `MensajeAccesible` (`Severity.Warning`) «La búsqueda de pistas es para
      ciudadanos y representantes de club.». Es un texto propio de la pantalla, no el de
      `ROL_NO_PERMITIDO`, para no duplicar el catálogo de errores. La API sigue respondiendo 403
      (T010–T011).

    No se toca `App.razor`, que es compartido;
  - `<form>` con `MudSelect` de deporte y de centro, y `MudDatePicker` de fecha, los tres con `Label`
    visible y `Required`. Deportes: solo los de `deporteIds` de
    `GET /api/instalaciones?reservablePorFranjas=true` con el nombre de `GET /api/deportes` (RF-004).
    Centros: de `GET /api/centros`;
  - botón «Buscar pistas» (`ButtonType.Submit`), que llama a
    `ClienteApi.GetAsync<ResultadoBusquedaDto>("/api/disponibilidad/franjas?deporteId=…&centroId=…&fecha=yyyy-MM-dd")`;
  - el resultado se pinta con una sección por pista, con `<h2>` y su nombre, y una lista de franjas
    con el texto «{HoraInicio}–{HoraFin}».

  **Depende de T054 de 000-core** (`/api/centros`, `/api/deportes`, `/api/instalaciones`) y de T010.
- [X] T013 [P] [US1] Añadir la entrada «Buscar pistas» (`MudButton Href="/buscar-pistas"`) en el hueco
  `<AuthorizeView Roles="ciudadano,representante_club">` de `src/SI18.Cliente/Layout/MainLayout.razor`,
  quitando el comentario de ese hueco que se refiere a la búsqueda (RF-002).

**Punto de control**: la búsqueda funciona de punta a punta para una ciudadana empadronada.

---

## Fase 4: Historia 2 · Ver el precio de mi tarifa en cada franja (P1)

**Objetivo**: cada franja muestra el precio de la tarifa de quien busca, y HU-02 obtiene exactamente
la misma disponibilidad y el mismo precio por `IConsultaDisponibilidad`.

**Prueba independiente**: quickstart.md B1–B3 y la coherencia búsqueda ↔ `IConsultaDisponibilidad`.

- [X] T014 [P] [US2] Crear `tests/SI18.Api.Tests/Disponibilidad/PrecioPorTarifaTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Sur, «Tenis», 23/12/2026): la misma franja
  vale `6.00` para `ciudadana.empadronada@si18.test`, `10.00` para `ciudadano.noempadronado@si18.test`
  y `4.00` para `representante.padel@si18.test` (no empadronado, tarifa club). Depende de T010.
- [X] T015 [US2] Hacer que `ServicioDisponibilidad` implemente `IConsultaDisponibilidad`
  (contracts/modulos.md; research.md D4):
  - `MotivoNoDisponible(instalacionId, inicioUtc, ct)`:
    - si la instalación no existe, lanza `CodigosError.Error(NoEncontrado)`;
    - con `HorarioMadrid.ALocal(inicioUtc)` obtiene la fecha y la hora; si no es en punto o no
      coincide con el inicio de ninguna franja de `CalculadoraFranjas.FranjasDelDia`, **lanza**
      `CodigosError.Error(PeticionInvalida)`;
    - si no, devuelve el `Motivo` de esa franja según `EvaluarPista`: solo uno de los seis motivos de
      RF-011 o `null` (contracts/modulos.md).
  - `Precio(instalacionId, tarifa, ct)`: `CalculadoraPrecio.Precio(instalacion.Tipo, tarifa, tarifas)`.

  Registrar `services.AddScoped<IConsultaDisponibilidad>(sp => sp.GetRequiredService<ServicioDisponibilidad>())`
  en `AddDisponibilidad()`. Depende de T008.
- [X] T016 [US2] Crear `tests/SI18.Api.Tests/Disponibilidad/ConsultaDisponibilidadTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Este, «Pádel», 24/12/2026), con la
  `IConsultaDisponibilidad` resuelta de `fabrica.Services`:
  - para cada franja que la búsqueda devuelve libre, `MotivoNoDisponible` da `null` y `Precio` da el
    mismo importe (CE-004);
  - tras insertar una reserva confirmada, un cierre y una franja fija, da `FRANJA_OCUPADA`,
    `INSTALACION_CERRADA` y `FRANJA_BLOQUEADA_CLUB` en esas franjas, que tampoco aparecen en la búsqueda;
  - con una franja fija de 19:00 a 21:00 y un cierre de 19:00 a 20:00 en la misma pista, da
    `INSTALACION_CERRADA` a las 19:00 (el cierre va antes en el orden de RF-011) y
    `FRANJA_BLOQUEADA_CLUB` a las 20:00;
  - una hora que no es en punto (18:30) y una de las 22:00 lanzan `ErrorNegocio` con `PETICION_INVALIDA`;
  - una instalación inexistente lanza `ErrorNegocio` con `NO_ENCONTRADO`;
  - el pabellón da `INSTALACION_NO_RESERVABLE`.

  Depende de T015.
- [ ] T017 [US2] En `src/SI18.Cliente/Disponibilidad/BuscarPistas.razor`, añadir a cada franja el
  precio con `FormatoEs.Importe(franja.Precio)`: «18:00–19:00 · 6,00 €» (RF-008). Depende de T012.

**Punto de control**: los tres precios por tarifa son correctos y HU-02 ya puede usar `IConsultaDisponibilidad`.

---

## Fase 5: Historia 3 · Saber por qué no hay resultados (P2)

**Objetivo**: un mensaje claro y anunciado cuando no hay franjas, cuando el centro no tiene pistas del
deporte y cuando la fecha está fuera de plazo.

**Prueba independiente**: quickstart.md B5, B6 y el paso 4 de §4.

- [X] T018 [P] [US3] Crear `tests/SI18.Api.Tests/Disponibilidad/BuscarSinResultadosTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, `[Trait("Categoria","Limites")]`, Polideportivo Norte):
  - **«Baloncesto»** → 200 con `TotalPistas = 0` y `Pistas` vacía;
  - **hoy + 7** (28/12/2026) → 200 con franjas; **hoy + 8** (29/12/2026) → 422 `FUERA_DE_ANTELACION`
    con el texto «Solo puedes reservar desde hoy hasta dentro de 7 días.»; **ayer** → 422;
  - **hoy a las 21:00:00** (`Reloj`) → 200 con `TotalPistas = 2` y `Pistas` vacía;
  - **orden de validación** (research.md D2): `centroId=999999` con fecha hoy + 8 → 422 (la fecha se
    valida antes que la existencia del centro).

  Los límites de tiempo se prueban con `DatosDisponibilidad.ConServicio(ahoraUtc, …)`, que usa un reloj propio:
  `FakeTimeProvider` no retrocede y el reloj de `FabricaApi` lo comparten todas las pruebas, así que no se
  cambia. Depende de T010.
- [ ] T019 [US3] En `src/SI18.Cliente/Disponibilidad/BuscarPistas.razor`, mostrar con
  `MensajeAccesible` (RF-011):
  - con `TotalPistas == 0`: «{centro} no tiene pistas de {deporte}.» (`Severity.Info`);
  - con `Pistas` vacía: «No hay pistas libres de {deporte} en {centro} el {fecha dd/MM/yyyy}. Prueba
    otra fecha u otro centro.» (`Severity.Info`);
  - con un `ErrorApi`: su `Detalle` (`Severity.Error`), por ejemplo el de `FUERA_DE_ANTELACION`.

  Depende de T017.

**Punto de control**: ninguna búsqueda deja la pantalla vacía sin explicación (CE-007).

---

## Fase 6: Historia 4 · Buscar sin barreras de accesibilidad (P2)

**Objetivo**: la búsqueda completa se hace con teclado y lector de pantalla (WCAG 2.1 AA).

**Prueba independiente**: quickstart.md §5.

- [ ] T020 [US4] Completar la accesibilidad de `src/SI18.Cliente/Disponibilidad/BuscarPistas.razor`
  (RF-015; research.md D7, D8):
  - **fecha**: hoy en Madrid por defecto, con `MinDate`/`MaxDate` entre hoy y hoy + 7, calculados
    con `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")` (solo ayuda: la regla la aplica la API);
  - **anuncio**: tras buscar con resultados, «Hay N franjas libres en M pistas.» en `MensajeAccesible`
    (`Severity.Success`), sin mover el foco;
  - **lista**: cada lista de franjas con `role="list"` y cada franja con `role="listitem"`;
  - **foco**: orden de tabulación filtros → botón → resultados y foco siempre visible;
  - **títulos**: `<PageTitle>Buscar pistas · SI-18</PageTitle>` y un `<h1>` «Buscar pistas».

  Depende de T019.
- [ ] T021 [US4] Comprobar a mano quickstart.md §5 sobre `/buscar-pistas`: recorrido solo con teclado,
  anuncio del resultado, información sin color, Lighthouse ≥ 95 y axe sin infracciones graves ni
  críticas. Anotar el resultado en la descripción del PR (principio I). Depende de T020.

**Punto de control**: CE-006 cumplido.

---

## Fase 7: Pulido y cierre

- [ ] T022 Ejecutar quickstart.md §2–§4 de principio a fin: `dotnet test SI18.slnx` en verde
  (también con `--filter-trait "Categoria=Limites"`), los escenarios B1–B10 contra la API y los pasos
  de la pantalla. Medir el tiempo de B1 con la API local (por ejemplo, 10 peticiones con
  `Measure-Command`) y anotar en el PR el peor valor: debe ser < 2 s (CE-002) y, como objetivo,
  < 500 ms (research.md D10).
- [ ] T023 [P] Avisar al grupo de las consultas provisionales (constitución, «Acuerdos de
  colaboración»): mensaje a Saul (`IConsultaReservas`, spec-002) y a C (`IConsultaCierres`, spec-007)
  con la ruta del fichero provisional y su prueba en `ConsultasProvisionalesTests`, para que las
  sustituyan y las borren en su PR. Anotarlo en la descripción del PR. Como las specs 002, 005 y 007
  aún no existen, pedir a cada dueño que, al crearla con `/speckit-specify`, anote en su sección de
  dependencias la provisional que sustituye (constitución: «la dependencia se anota en las dos
  specs»). Para la 005, que es tuya, hacerlo tú.
- [ ] T024 Abrir el PR de `feature/001-buscar-pistas` a `develop`, con la propuesta de convención de
  `Program.cs` (dos líneas por módulo, research.md D6), la lista de los principios I–IX revisados y la
  comprobación de accesibilidad de T021. Lo aprueba otra persona del grupo.
- [ ] T025 Después de fusionar, ejecutar `/speckit-converge` para la spec 001.

---

## Dependencias y orden de ejecución

### Dependencias entre fases

```text
Fase 1 Setup (T001, T002)
   └─► Fase 2 Fundacional (T003–T005 ∥ → T006 → T007 ∥ T008)
          ├─► US1 (T009 → T010 → T011 ∥ T012* ∥ T013)          *T012 también necesita T054 de 000-core
          │      ├─► US2 (T014 ∥ T015 → T016; T017 tras T012)
          │      └─► US3 (T018; T019 tras T017)
          │             └─► US4 (T020 → T021)
          └─► Fase 7 Pulido (T022–T025)
```

- **US2** depende de US1 para el endpoint (T014) y la pantalla (T017). T015–T016
  (`IConsultaDisponibilidad`) solo dependen de T008, así que se pueden adelantar para desbloquear a
  Saul en la spec-002.
- **US3** y **US4** tocan la misma pantalla que US1 y US2, por eso van en serie en el cliente.
- **T054 (Saul)** solo bloquea T012 y lo que va detrás en el cliente. Toda la API y sus pruebas se
  pueden terminar sin él.

### Paralelismo dentro de cada fase

- **Fase 1**: T002 en paralelo con T001.
- **Fase 2**: T003, T004 y T005 en paralelo; después T006; luego T007 y T008 en paralelo.
- **US1**: T011, T012 y T013 en paralelo una vez hecho T010 (y T054 para T012).
- **US2**: T014 en paralelo con T015.
- **US3**: T018 en paralelo con todo lo del cliente.

### Ejemplo de reparto en el tiempo (una persona)

```text
Día 1:  T001 ∥ T002 → T003 ∥ T004 ∥ T005 → T006 → T007 ∥ T008
Día 2:  T009 → T010 → T011 → T015 → T016 → T014 → T018          (API completa y probada)
Día 3:  T012 → T013 → T017 → T019 → T020 → T021                  (cuando T054 esté en develop)
Día 4:  T022 → T023 → T024 → T025
```

## Estrategia de implementación

### MVP (US1)

1. Fases 1 y 2: módulo, DTO, consultas provisionales probadas y `EvaluarPista`.
2. US1: endpoint, sus pruebas y la pantalla básica.
3. **Validar**: quickstart.md B1, B4 y B7–B10.

### Entrega incremental

US1 (franjas libres) → US2 (precio por tarifa e `IConsultaDisponibilidad` para HU-02) → US3
(mensajes sin resultados) → US4 (accesibilidad completa) → Pulido. Si la pantalla se retrasa por
T054, se puede abrir antes un PR solo con la API (Fases 1–2, T009–T011, T014–T016 y T018).

## Notas

- `[P]` = ficheros distintos, sin dependencias pendientes.
- Ninguna tarea crea tablas, migraciones, entidades, estados ni códigos de error: todo es de 000-core
  (principio VII). Si durante la implementación hiciera falta alguno, se propone primero en 000-core.
- Las provisionales (T003–T005) se borran en el PR del dueño de cada interfaz, no en esta spec.
