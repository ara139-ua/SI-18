---

description: "Lista de tareas de 005 · Franjas fijas de temporada para clubes (HU-05)"
---

# Tareas: 005 · Franjas fijas de temporada para clubes (HU-05)

**Entrada**: documentos de diseño de `specs/005-franjas-fijas/` ([plan.md](./plan.md),
[spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md),
[contracts/](./contracts/modulos.md), [quickstart.md](./quickstart.md)).

**Pruebas**: incluidas, porque las exige la constitución (principio II).

**Responsable**: Artur (módulo `Disponibilidad`). Todas las tareas son suyas.

**Organización**: por historia de usuario de la spec (US1–US6). Todas las rutas de código parten de
`fuentes/`. Commits con el formato `spec-005 T00X: …`.

## Formato: `- [ ] [ID] [P?] [Historia?] Descripción con ruta`

- **[P]**: se puede hacer en paralelo (ficheros distintos y sin dependencias pendientes).
- **[USn]**: historia de usuario de la spec a la que pertenece.

## Datos de las pruebas de integración

Como en spec-001: `FabricaApi` comparte la base de datos y el reloj (lunes 21/12/2026, 18:00 en Madrid),
que **no retrocede**. Cada clase usa sus propias instalaciones o fechas, y las pruebas que necesitan otra
hora usan un reloj propio, como `DatosDisponibilidad.ConServicio`. Temporada sembrada: «2026-2027»
(01/09/2026–30/06/2027). Clubes: «Club Deportivo Ficticio Norte» (`representante.norte@si18.test`) y
«Club de Pádel Ejemplo» (`representante.padel@si18.test`). Técnico: `tecnico@si18.test`.

Para que las solicitudes de clases distintas no se solapen entre sí, cada clase usa **un centro
distinto**: `SolicitarFranjasFijasTests` → Sur; `RevisarFranjasFijasTests` → Este;
`ConflictosFranjasFijasTests` → Oeste; `ConsultaFranjasFijasTests` → Norte, pabellón y «Pista de tenis 2»
(las pistas de pádel de Norte las usa spec-001). La única excepción son las dos pruebas que T023 mueve
desde spec-001, que conservan sus pistas de Oeste (T023 explica por qué no chocan).

**No se insertan temporadas ni usuarios**: `InicializadorDatosTests` (000-core) exige exactamente
`Temporadas: 1` y `Usuarios: 10` en la base compartida. Las fechas fuera de la temporada se prueban con un
reloj propio (`ConServicioSolicitudes`, T006).

---

## Fase 1: Setup (contratos)

- [X] T001 [P] Crear en `src/SI18.Contratos/Disponibilidad/` los DTO de [data-model.md § 3](./data-model.md),
  uno por fichero salvo los anidados:
  - `PeticionSolicitudFranjasFijas(int InstalacionId, int TemporadaId, IReadOnlyList<int> DiasSemana, string HoraInicio, string HoraFin)`,
    con «`diasSemana` ≥ 1, valores 1–7 (ISO), sin repetir» y horas `"HH:mm"`;
  - `SolicitudFranjasFijasDto(int Id, string ClubNombre, int InstalacionId, string InstalacionNombre, string CentroNombre, string TemporadaNombre, IReadOnlyList<int> DiasSemana, string HoraInicio, string HoraFin, EstadoSolicitud Estado, DateTime CreadaEnUtc, DateTime? ResueltaEnUtc)`;
  - `ConflictosSolicitudDto(IReadOnlyList<ReservaEnConflictoDto> Reservas, IReadOnlyList<SolicitudSolapadaDto> Solapadas, bool PuedeAprobarse)`,
    con `ReservaEnConflictoDto(DateOnly Fecha, string HoraInicio, string HoraFin, EstadoReservaEnConflicto Estado)`
    («**sin** identificador de usuario ni datos personales») y
    `SolicitudSolapadaDto(int Id, string ClubNombre, IReadOnlyList<int> DiasSemana, string HoraInicio, string HoraFin)`
    en el mismo fichero;
  - `TemporadaDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin)`;
  - `EnumeracionesFranjasFijas.cs` con `EstadoSolicitud { Pendiente, Aprobada, Rechazada }` y
    `EstadoReservaEnConflicto { Confirmada, PendientePago }`.
- [X] T002 [P] Añadir a `tests/SI18.Api.Tests/Comun/EnumeracionesEspejoTests.cs` la comprobación de que
  `SI18.Contratos.Disponibilidad.EstadoSolicitud` tiene los mismos nombres que
  `SI18.Dominio.Comun.EstadoSolicitud` y que en JSON viaja con el texto de `ConversorEnumSnakeCase`.
  Depende de T001.

**Punto de control**: `dotnet build SI18.slnx` sin advertencias.

---

## Fase 2: Fundacional (regla de dominio, servicio y conflictos)

**⚠️ CRÍTICO**: ninguna historia puede empezar sin esta fase.

- [X] T003 [P] Crear `src/SI18.Dominio/Disponibilidad/ReglaFranjasFijas.cs` (clase estática; research.md F3):
  - `bool SeSolapan(SolicitudFranjasFijas a, SolicitudFranjasFijas b)` = misma `InstalacionId` ∧ misma
    `TemporadaId` ∧ «`a.dias_semana ∩ b.dias_semana ≠ ∅`» ∧ «`a.hora_inicio < b.hora_fin` ∧
    `b.hora_inicio < a.hora_fin`» (y `a.Id != b.Id` cuando los dos tienen identificador);
  - `bool Cubre(SolicitudFranjasFijas s, DateOnly fecha, TimeOnly hora)` = día ISO de `fecha` (lunes = 1 …
    domingo = 7) en `DiasSemana` ∧ `HoraInicio ≤ hora < HoraFin`;
  - `IReadOnlyList<Franja> Expandir(SolicitudFranjasFijas s, Temporada t, DateOnly desde, DateOnly hasta)`
    = para cada fecha de `[desde, hasta] ∩ [t.FechaInicio, t.FechaFin]`, las franjas de
    `CalculadoraFranjas.FranjasDelDia(s.InstalacionId, fecha)` con `Cubre(s, fecha, franja.HoraInicio)`,
    ordenadas por `InicioUtc`.
- [X] T004 Crear `tests/SI18.Dominio.Tests/Disponibilidad/ReglaFranjasFijasTests.cs`
  (`[Trait("Categoria","Limites")]`):
  - **solapes**: martes 19–21 con martes 20–22 → sí; martes 19–20 con martes 20–21 → no; martes 19–21
    con miércoles 19–21 → no; otra instalación u otra temporada → no;
  - **`Cubre`** con martes 19–21: martes 19:00 y 20:00 → sí; 21:00 → no; miércoles 19:00 → no;
  - **`Expandir`**: una fecha fuera de la temporada da una lista vacía; martes y jueves 19–21 durante
    una semana da 4 franjas; el 25/10/2026 (domingo) y el 28/03/2027 (domingo) con una solicitud de
    domingo 08–10 dan las franjas de 08:00 y 09:00 en hora de Madrid, cada una de 1 h real.

  Depende de T003.
- [X] T005 Crear `src/SI18.Api/Modulos/Disponibilidad/ServicioSolicitudesFranjasFijas.cs` (`sealed`,
  scoped; dependencias: `Si18DbContext`, `TimeProvider`, `IConsultaReservas`, `IServicioAvisos`) con lo
  común a todas las historias:
  - `private Task<SolicitudFranjasFijasDto> ADto(SolicitudFranjasFijas s, CancellationToken ct)` y una
    variante para listas, con los nombres del club, la instalación, el centro y la temporada, horas
    `"HH:mm"` y `DiasSemana` ordenados;
  - `private Task<(IReadOnlyList<ReservaEnConflictoDto> Reservas, IReadOnlyList<SolicitudSolapadaDto> Solapadas)> Conflictos(SolicitudFranjasFijas s, CancellationToken ct)`
    según research.md F4:
    - reservas: `IConsultaReservas.Activas(s.InstalacionId, ahora, HorarioMadrid.AUtc(temporada.FechaFin.AddDays(1), 00:00))`
      filtradas con `ReglaFranjasFijas.Cubre` sobre `HorarioMadrid.ALocal(r.InicioUtc)`; estado
      `Confirmada` o `PendientePago`; ordenadas por fecha y hora; **sin** `UsuarioId`;
    - solapadas: solicitudes `Aprobada` con `ReglaFranjasFijas.SeSolapan(s, otra)`;
  - `private static string TextoDias(IEnumerable<short> dias)`: «martes y jueves», «lunes, miércoles y
    viernes», «lunes».

  Registrarlo con `services.AddScoped<ServicioSolicitudesFranjasFijas>()` en
  `src/SI18.Api/Modulos/Disponibilidad/RegistroDisponibilidad.cs`. Crear también
  `src/SI18.Api/Modulos/Disponibilidad/EndpointsSolicitudesFranjasFijas.cs` con
  `MapEndpointsSolicitudesFranjasFijas()` vacío, llamado desde `MapDisponibilidad()`. Depende de T001
  y T003.
- [X] T006 [P] Ampliar `tests/SI18.Api.Tests/Disponibilidad/DatosDisponibilidad.cs`:
  - `FranjaFija(...)` devuelve el `Id` de la solicitud creada y admite el club por nombre (por defecto
    el primero, como ahora);
  - `Instalacion(string centro, string nombre)` ya existe como `Pista`: se reutiliza para pabellones,
    salas y piscinas;
  - `Task<int> Temporada(string nombre)`;
  - `ConServicioSolicitudes<T>(DateTime ahoraUtc, Func<ServicioSolicitudesFranjasFijas, Task<T>> accion)`,
    igual que `ConServicio` pero para el servicio de solicitudes, con su propio `FakeTimeProvider`. Sirve
    para probar fechas sin tocar el reloj compartido y **sin insertar** temporadas ni usuarios:
    `InicializadorDatosTests` exige exactamente `Temporadas: 1` y `Usuarios: 10` en la base compartida.

  Depende de T003 (y de T005 para la última ayuda).

**Punto de control**: `ReglaFranjasFijasTests` en verde y el servicio compila.

---

## Fase 3: Historia 1 · Solicitar franjas fijas para mi club (P1) 🎯 MVP

**Objetivo**: un representante envía una solicitud válida, que queda pendiente a nombre de su club, sin
bloquear nada, y ve la lista de su club.

**Prueba independiente**: quickstart.md S1–S5.

- [X] T007 [US1] Añadir a `ServicioSolicitudesFranjasFijas` y a `EndpointsSolicitudesFranjasFijas`:
  - `Crear(PeticionSolicitudFranjasFijas p, int clubId)`, con la validación de research.md F2 **antes**
    de construir la entidad y `errors` por campo en español:
    - `diasSemana` vacío, fuera de 1–7 o repetido → «Elige al menos un día, sin repetir.»;
    - hora que no es `HH:00` o está fuera de 08:00–22:00 → «Elige una hora en punto entre las 08:00 y
      las 22:00.»;
    - `horaFin ≤ horaInicio` → «La hora de fin debe ser posterior a la de inicio.»;
    - instalación inexistente o que no es `pista_tenis`, `pista_padel` ni `pabellon` →
      `errors.instalacionId` «Elige una pista o un pabellón.»;
    - temporada inexistente o con `FechaFin < hoy` (Madrid) → `errors.temporadaId` «Esa temporada ya ha
      terminado.».

    Después: `new SolicitudFranjasFijas(clubId, …, ahora)` y `SaveChanges`;
  - `POST /api/solicitudes-franjas-fijas` (`EsRepresentante`): el club sale de `IUsuarioActual.ClubId`
    (si es `null` → `ROL_NO_PERMITIDO`) y responde `201` con `SolicitudFranjasFijasDto` y `Location`;
  - `GET /api/solicitudes-franjas-fijas` con `RequireAuthorization()`: un representante ve las de su club,
    un técnico ve todas, otro rol → `ROL_NO_PERMITIDO`. Orden: «pendientes primero; dentro de cada
    estado, por `creadaEnUtc` (más antigua primero)»;
  - `GET /api/solicitudes-franjas-fijas/temporadas` (`EsRepresentante`): temporadas con `FechaFin ≥ hoy`.

  Depende de T005.
- [X] T008 [US1] Crear `tests/SI18.Api.Tests/Disponibilidad/SolicitarFranjasFijasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Sur):
  - **esc. 1**: `representante.norte` pide el pabellón de Sur martes y jueves 19–21 → 201, `Pendiente`,
    `ClubNombre = "Club Deportivo Ficticio Norte"`, y aparece en su `GET`;
  - **esc. 2**: con una solicitud pendiente sobre «Pista de pádel 1» de Sur, la búsqueda de HU-01 de pádel
    en Sur el martes 22/12/2026 sigue mostrando las franjas de 19:00 y 20:00 de esa pista;
  - **esc. 3**: la sala y la piscina de Sur → 400 con `errors.instalacionId`;
  - **esc. 4**: días `[]`, `[2,2]` y `[0]`; `19:30`; `07:00`; fin = inicio; fin < inicio → 400 con el
    campo marcado;
  - **esc. 5**: temporada terminada **sin insertar ninguna** (no se pueden añadir temporadas a la base
    compartida: `InicializadorDatosTests` exige `Temporadas: 1`). Con
    `DatosDisponibilidad.ConServicioSolicitudes` y el reloj el 01/07/2027, «2026-2027» ya ha terminado:
    `Crear` lanza `PETICION_INVALIDA` con `errors.temporadaId`, y la lista de temporadas no terminadas
    sale vacía. El 30/06/2027 a las 23:59 de Madrid todavía se acepta (`[Trait("Categoria","Limites")]`);
  - **esc. 6**: `representante.padel` no ve las solicitudes de Norte;
  - **roles**: `ciudadana.empadronada` y `tecnico` en `POST` → 403; sin sesión → 401.

  Depende de T006 y T007.
- [ ] T009 [US1] Crear `src/SI18.Cliente/Disponibilidad/FranjasFijasClub.razor` (ruta `/franjas-fijas`;
  research.md F7):
  - acceso como en `BuscarPistas`: sin sesión → `/iniciar-sesion`; otro rol que no sea
    `representante_club` → aviso «Las franjas fijas son para representantes de club.» sin formulario;
  - formulario: `MudSelect` de instalación (pistas y pabellones de `GET /api/instalaciones`, con
    «{centro} · {instalación}»), `<fieldset>` con `<legend>` «Días de la semana» y siete `MudCheckBox`
    (lunes…domingo), `MudSelect` de hora de inicio (08:00…21:00) y de fin (09:00…22:00), y `MudSelect`
    de temporada (`GET …/temporadas`); botón «Enviar solicitud», que no se desactiva al enviar;
  - resultado en `MensajeAccesible` («Solicitud enviada. Queda pendiente de revisión.» o el `Detalle`
    del error, con los `Errores` por campo en cada control);
  - lista del club (`role="list"`): «{centro} · {instalación} — {días} de {inicio} a {fin} — temporada
    {nombre} — **{Pendiente|Aprobada|Rechazada}**», y la fecha de resolución si la hay.

  Depende de T007.
- [ ] T010 [P] [US1] Añadir «Franjas fijas» (`MudButton Href="/franjas-fijas"`) en el hueco
  `<AuthorizeView Roles="representante_club">` de `src/SI18.Cliente/Layout/MainLayout.razor`, quitando
  el comentario de ese hueco.

**Punto de control**: un representante envía y ve sus solicitudes.

---

## Fase 4: Historia 2 · Aprobar una solicitud sin conflictos (P1)

**Objetivo**: el técnico aprueba; las franjas quedan bloqueadas en la temporada y los representantes
reciben un aviso.

**Prueba independiente**: quickstart.md S6–S9.

- [X] T011 [US2] Añadir `Aprobar(int id)` a `ServicioSolicitudesFranjasFijas` y
  `POST /api/solicitudes-franjas-fijas/{id}/aprobacion` (`EsTecnico`), en el orden de research.md F4:
  1. no existe → `NO_ENCONTRADO`;
  2. ya resuelta → `SOLICITUD_YA_RESUELTA` (lo lanza el dominio al llamar a `Aprobar`; comprobarlo
     antes de calcular conflictos);
  3. `Conflictos()` con reservas → `SOLICITUD_CON_CONFLICTOS`;
  4. solo solapadas → `FRANJA_FIJA_SOLAPADA`;
  5. `solicitud.Aprobar(ahora)`, `IServicioAvisos.Avisar(id, texto)` para **cada** usuario con
     `ClubId == solicitud.ClubId` y `Rol == RepresentanteClub`, con «Tu solicitud de franjas fijas para
     {instalación} ({centro}), {días} de {HH:mm} a {HH:mm}, temporada {temporada}, ha sido aprobada.», y
     **un único** `SaveChanges` → 200 con el DTO.

  Depende de T005.
- [X] T012 [US2] Crear `tests/SI18.Api.Tests/Disponibilidad/RevisarFranjasFijasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, Polideportivo Este). Reparto dentro de la clase, para que una
  aprobación no estropee otra prueba: **aprobar** usa «Pista de pádel 1» los **martes**; **rechazar**
  (T019) usa «Pista de pádel 2» los **miércoles**; **ya resuelta** (T019) usa «Pista de tenis 1» los
  **viernes**. Parte de aprobar:
  - **esc. 1**: una solicitud pendiente sin conflictos sobre «Pista de pádel 1» de Este, martes 19–21 →
    200, `Aprobada`, `ResueltaEnUtc` informada;
  - **esc. 2 y 5**: la búsqueda de HU-01 de pádel en Este el martes 22/12/2026 ya no tiene las franjas de
    19:00 ni de 20:00 de esa pista; un miércoles sí; julio de 2027 no se busca (fuera de antelación),
    así que se comprueba con `IConsultaFranjasFijas.FranjasBloqueadas` para el 06/07/2027 → vacía;
  - **esc. 3**: `IConsultaDisponibilidad.MotivoNoDisponible` en esa franja → `FRANJA_BLOQUEADA_CLUB`;
  - **esc. 4**: `representante.norte` tiene en `GET /api/avisos` el aviso «… ha sido aprobada.» con
    «martes» y «19:00 a 21:00». Como la siembra solo tiene un representante por club y no se pueden
    añadir usuarios (`InicializadorDatosTests` exige `Usuarios: 10`), «todos los representantes» se
    comprueba así: el número de avisos nuevos es igual al número de usuarios `RepresentanteClub` del club
    consultado en la base, y hay uno para cada uno;
  - **listado del técnico**: las pendientes salen antes que las resueltas;
  - **roles**: `representante.norte` en `POST …/aprobacion` → 403; id inexistente → 404.

  Depende de T006 y T011.
- [ ] T013 [US2] Crear `src/SI18.Cliente/Disponibilidad/RevisarFranjasFijas.razor` (ruta
  `/franjas-fijas/revision`; research.md F8), parte de aprobar:
  - acceso: sin sesión → `/iniciar-sesion`; otro rol que no sea `tecnico_concejalia` → aviso «La revisión
    de franjas fijas es para técnicos de la concejalía.» sin lista;
  - lista de todas las solicitudes (`role="list"`) con club, instalación, días, horario, temporada y
    estado en texto; las pendientes primero;
  - en cada pendiente, botón «Aprobar» con `aria-label` «Aprobar la solicitud de {club}: {instalación},
    {días} de {inicio} a {fin}»; resultado en `MensajeAccesible` y la lista se recarga.

  Depende de T011.
- [ ] T014 [P] [US2] Añadir «Revisar franjas fijas» (`MudButton Href="/franjas-fijas/revision"`) en el
  hueco `<AuthorizeView Roles="tecnico_concejalia">` de `src/SI18.Cliente/Layout/MainLayout.razor`,
  quitando la parte del comentario que se refiere a las solicitudes de clubes.

**Punto de control**: aprobar bloquea las franjas y avisa al club.

---

## Fase 5: Historia 3 · Ver los conflictos que impiden aprobar (P1)

**Objetivo**: el técnico ve las reservas y las franjas fijas que chocan, y no puede aprobar mientras
existan.

**Prueba independiente**: quickstart.md S10 y S11.

- [X] T015 [US3] Añadir `GET /api/solicitudes-franjas-fijas/{id}/conflictos` (`EsTecnico`) a
  `EndpointsSolicitudesFranjasFijas.cs`, con un método público `ConflictosDe(int id)` en el servicio que
  usa el `Conflictos()` privado de T005 y devuelve `ConflictosSolicitudDto` con
  `PuedeAprobarse = Reservas.Count == 0 && Solapadas.Count == 0`. Si no existe → `NO_ENCONTRADO`.
  Depende de T005.
- [X] T016 [US3] Crear `tests/SI18.Api.Tests/Disponibilidad/ConflictosFranjasFijasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, `[Trait("Categoria","Limites")]`, Polideportivo Oeste,
  «Pista de tenis 1» y «Pista de tenis 2»):
  - **esc. 1–2**: una reserva **confirmada** el martes 22/12/2026 a las 19:00 → la lista la tiene
    (fecha, «19:00», «20:00», `Confirmada`), el JSON no contiene `usuarioId` ni el correo, y aprobar da
    409 `SOLICITUD_CON_CONFLICTOS` con la solicitud aún `Pendiente`;
  - **esc. 3**: una **pendiente vigente** cuenta; una **pendiente caducada** (creada hace 10 min) no;
  - **esc. 4**: una reserva **cancelada** no cuenta; una reserva a las 21:00 (fuera del horario 19–21) no
    cuenta;
  - **reserva del propio club** (de `representante.norte`) → cuenta;
  - **esc. 5**: con otra solicitud **aprobada** martes 20–22 en la misma pista → aprobar da 409
    `FRANJA_FIJA_SOLAPADA` y la lista la tiene en `Solapadas`;
  - **esc. 6**: dos pendientes solapadas; se aprueba la primera y la segunda pasa a
    `PuedeAprobarse = false`;
  - **orden**: si hay reservas en conflicto y además solapes, el error es `SOLICITUD_CON_CONFLICTOS`;
  - **roles**: un representante en `GET …/conflictos` → 403.

  Depende de T006, T011 y T015.
- [ ] T017 [US3] En `src/SI18.Cliente/Disponibilidad/RevisarFranjasFijas.razor`, para cada pendiente:
  botón «Ver conflictos» (`aria-expanded`) que carga `GET …/conflictos` y muestra debajo «No hay
  conflictos» o una lista con «{día dd/MM/yyyy}, {inicio}–{fin} · {confirmada|pendiente de pago}» y
  «Franja fija de {club}: {días} de {inicio} a {fin}». Si `PuedeAprobarse` es `false`, «Aprobar» queda
  desactivado y al lado aparece «No se puede aprobar: hay reservas o franjas fijas en conflicto.».
  Depende de T013 y T015.

**Punto de control**: no se puede aprobar nada con conflictos (CE-003).

---

## Fase 6: Historia 4 · Rechazar una solicitud (P2)

**Objetivo**: el técnico rechaza; las franjas siguen libres y los representantes reciben un aviso.

**Prueba independiente**: quickstart.md S12 y S13.

- [X] T018 [US4] Añadir `Rechazar(int id)` al servicio y `POST /api/solicitudes-franjas-fijas/{id}/rechazo`
  (`EsTecnico`): no existe → `NO_ENCONTRADO`; `solicitud.Rechazar(ahora)` (ya resuelta →
  `SOLICITUD_YA_RESUELTA`), aviso a cada representante con «… ha sido rechazada.» (sin motivo,
  aclaración 1) y un único `SaveChanges` → 200. Se puede rechazar aunque tenga conflictos. Depende de T011.
- [X] T019 [US4] Añadir a `tests/SI18.Api.Tests/Disponibilidad/RevisarFranjasFijasTests.cs`, con el reparto
  de T012 (rechazar en «Pista de pádel 2» de Este los **miércoles**; ya resuelta en «Pista de tenis 1» de
  Este los **viernes**), para que la aprobación de los martes en «Pista de pádel 1» no bloquee nada aquí:
  - **esc. 1–3**: rechazar una pendiente de «Pista de pádel 2», miércoles 19–21 (también una con
    conflictos) → 200, `Rechazada`; la búsqueda de HU-01 de pádel en Este el miércoles 23/12/2026 sigue
    mostrando las franjas de 19:00 y 20:00 de esa pista; los representantes tienen el aviso «… ha sido
    rechazada.»;
  - **esc. 4**: en «Pista de tenis 1», viernes: aprobar o rechazar una ya aprobada, y una ya rechazada →
    409 `SOLICITUD_YA_RESUELTA`, sin aviso nuevo;
  - **atomicidad** (CE-004): tras un 409, el número de avisos del club no cambia.

  Depende de T012 y T018.
- [ ] T020 [US4] En `src/SI18.Cliente/Disponibilidad/RevisarFranjasFijas.razor`, botón «Rechazar» en cada
  pendiente (con `aria-label` como el de «Aprobar»), siempre activo; resultado en `MensajeAccesible` y
  la lista se recarga. Depende de T017 y T018.

**Punto de control**: el ciclo pendiente → aprobada | rechazada está completo.

---

## Fase 7: Historia 5 · Las franjas fijas bloquean de verdad, también con cierres (P2)

**Objetivo**: `IConsultaFranjasFijas` definitiva, que descuenta los cierres y avisa a HU-07, y la
provisional de spec-001 retirada.

**Prueba independiente**: `ConsultaFranjasFijasTests` y todas las pruebas de spec-001 en verde (CE-005).

- [X] T021 [US5] Crear `src/SI18.Api/Modulos/Disponibilidad/ConsultaFranjasFijas.cs` (implementa
  `IConsultaFranjasFijas`; dependencias `Si18DbContext` e `IConsultaCierres`; research.md F6):
  - `FranjasBloqueadas(instalacionId, desde, hasta)`: solicitudes `Aprobada` de la instalación cuya
    temporada se cruza con `[desde, hasta]`, `ReglaFranjasFijas.Expandir` y **menos** las franjas a las
    que `CierreMantenimiento.Afecta` alguno de
    `IConsultaCierres.Cierres(instalacionId, AUtc(desde, 00:00), AUtc(hasta + 1, 00:00))`; sin repetir y
    ordenadas por `InicioUtc`;
  - `RepresentantesAfectados(instalacionId, inicioUtc, finUtc)`: expandir las aprobadas para las fechas
    de Madrid de `[inicioUtc, finUtc)` **sin** descontar cierres; los `ClubId` con alguna franja que se
    solape con ese periodo; y los usuarios `RepresentanteClub` de esos clubes, sin repetir.
- [X] T022 [US5] Retirar la provisional (research.md F6):
  - en `src/SI18.Api/Modulos/Disponibilidad/RegistroDisponibilidad.cs`,
    `TryAddScoped<IConsultaFranjasFijas, ConsultaFranjasFijasProvisional>()` pasa a
    `AddScoped<IConsultaFranjasFijas, ConsultaFranjasFijas>()`;
  - borrar `src/SI18.Api/Modulos/Disponibilidad/Provisional/ConsultaFranjasFijasProvisional.cs`;
  - en `tests/SI18.Api.Tests/Disponibilidad/DatosDisponibilidad.cs`, `ConServicio` construye
    `ConsultaFranjasFijas(db, new ConsultaCierresProvisional(db))`, o la definitiva de cierres si spec-007
    ya existe.

  Depende de T021.
- [X] T023 [US5] Crear `tests/SI18.Api.Tests/Disponibilidad/ConsultaFranjasFijasTests.cs`
  (`[Collection(ColeccionApi.Nombre)]`, `[Trait("Categoria","Limites")]`, Polideportivo Norte, pabellón
  y «Pista de tenis 2»):
  - **movidas** de `ConsultasProvisionalesTests` (y borradas de allí): «una franja fija aprobada bloquea
    sus días y horas dentro de la temporada» y «una solicitud no aprobada no bloquea nada». **Conservan sus
    pistas de Oeste** («Pista de pádel 2» y «Pista de tenis 1», enero de 2027), aunque Oeste sea el centro
    de `ConflictosFranjasFijasTests`: esta usa «Pista de tenis 1» los martes de 19 a 21 en diciembre, y las
    movidas usan martes y jueves de 19 a 21 en «Pista de pádel 2» y miércoles por la tarde en «Pista de
    tenis 1», así que no se solapan. Las pruebas nuevas de esta tarea usan Norte;
  - **esc. 1–2**: martes 19–21 aprobada y un cierre ese martes de 19:00 a 20:00 → solo la franja de 20:00;
    un cierre de 18:00 a 22:00 → ninguna;
  - **esc. 3**: dos clubes con franjas fijas aprobadas en la misma instalación que se solapan con un
    cierre → los dos representantes; un club cuya franja fija no se solapa → no aparece; sin repetir;
  - **CE-005**: la búsqueda de HU-01 con una franja fija aprobada y un cierre en la misma pista da en esas
    horas `INSTALACION_CERRADA` (cierre) y `FRANJA_BLOQUEADA_CLUB` (el resto), igual que antes.

  Depende de T022.

**Punto de control**: `dotnet test` en verde, incluidas todas las pruebas de spec-001.

---

## Fase 8: Historia 6 · Solicitar y revisar sin barreras de accesibilidad (P3)

- [ ] T024 [US6] Completar la accesibilidad de `src/SI18.Cliente/Disponibilidad/FranjasFijasClub.razor` y
  `src/SI18.Cliente/Disponibilidad/RevisarFranjasFijas.razor` (RF-018; research.md F7, F8):
  - tras elegir en cada `MudSelect`, el foco vuelve al desplegable (`ValueChanged` + `FocusAsync`, como
    T026 de spec-001);
  - tras aprobar o rechazar, el foco pasa al contenedor de la lista (`tabindex="-1"`), como en «Mis
    avisos»;
  - los elementos de las listas no entran en el tabulador (`tabindex="-1"` en los `MudListItem`, si se
    usan) y los estados van en texto;
  - `<PageTitle>` y `<h1>` en las dos pantallas.

  Depende de T009, T020.
- [ ] T025 [US6] Comprobar a mano quickstart.md §5 en `/franjas-fijas` y `/franjas-fijas/revision`
  (teclado, lector de pantalla, Lighthouse ≥ 95 y axe) y anotarlo en el PR (principio I). Depende de T024.

---

## Fase 9: Pulido y cierre

- [ ] T026 Ejecutar quickstart.md §2–§4: `dotnet test SI18.slnx` en verde (también
  `--filter-trait "Categoria=Limites"`), los escenarios S1–S13 contra la API y los pasos de las
  pantallas. Medir el tiempo de `GET …/conflictos` sobre una solicitud con reservas (10 peticiones) y
  anotar el peor valor en el PR (objetivo < 500 ms).
- [ ] T027 [P] Avisar al grupo (constitución, «Acuerdos de colaboración»):
  - a Saul: este PR toca `RegistroDisponibilidad.cs`, `ConsultasProvisionalesTests.cs` y
    `DatosDisponibilidad.cs`, igual que su retirada de `ConsultaReservasProvisional`; el segundo PR hace
    `rebase` (research.md F11);
  - a C: `IConsultaFranjasFijas.RepresentantesAfectados` ya está disponible para spec-007.

  Anotarlo en la descripción del PR.
- [ ] T028 Abrir el PR de `feature/005-franjas-fijas` a `develop`, con la lista de los principios I–IX
  revisados y la comprobación de accesibilidad de T025. Lo aprueba otra persona del grupo.
- [ ] T029 Después de fusionar, ejecutar `/speckit-converge` para la spec 005.

---

## Dependencias y orden de ejecución

```text
Fase 1 (T001 → T002)
   └─► Fase 2 (T003 → T004 ∥ T006; T005 tras T001 y T003)
          ├─► US1: T007 → T008 ∥ T009 ∥ T010
          ├─► US2: T011 → T012 ∥ T013 ∥ T014
          │      ├─► US3: T015 → T016; T017 tras T013
          │      └─► US4: T018 → T019; T020 tras T017
          └─► US5: T021 → T022 → T023            (independiente de US1–US4)
                     └─► US6: T024 → T025 (tras las dos pantallas)
                            └─► Pulido: T026–T029
```

- **US5** no depende de US1–US4: se puede hacer primero si C necesita pronto `RepresentantesAfectados`.
- **US2, US3 y US4** comparten `RevisarFranjasFijas.razor` y `RevisarFranjasFijasTests.cs`, así que en
  esos ficheros van en serie.
- **T022** choca en ficheros con la retirada de Saul en spec-002 (F11): coordinar antes de abrir el PR.

### Paralelismo

- **Fase 1**: T001 y luego T002.
- **Fase 2**: T003 en paralelo con T001; T004 y T006 en paralelo tras T003.
- **US1**: T008, T009 y T010 en paralelo tras T007.
- **US2**: T012, T013 y T014 en paralelo tras T011.
- **US5**: en paralelo con cualquier historia del cliente.

### Ejemplo de reparto en el tiempo (una persona)

```text
Día 1: T001 ∥ T003 → T002 ∥ T004 ∥ T006 → T005 → T007 → T008
Día 2: T011 → T012 → T015 → T016 → T018 → T019 → T021 → T022 → T023   (API completa)
Día 3: T009 → T010 → T013 → T014 → T017 → T020 → T024 → T025          (pantallas)
Día 4: T026 → T027 → T028 → T029
```

## Estrategia de implementación

### MVP (US1 + US2 + US3)

Las tres son P1: solicitar, aprobar sin conflictos y bloquear la aprobación con conflictos. Juntas
cumplen los cuatro criterios de aceptación de la historia salvo el rechazo. **Validar**: quickstart.md
S1–S11.

### Entrega incremental

API de US1–US5 (con sus pruebas) → pantallas (US1–US4) → accesibilidad (US6) → pulido. Si conviene,
se abre un PR solo con la API, como en spec-001.

## Notas

- `[P]` = ficheros distintos, sin dependencias pendientes.
- Ninguna tarea crea tablas, columnas, estados ni migraciones. Los códigos de error ya están en
  `develop` (PR #32).
- `RepresentantesAfectados` es para HU-07: esta spec solo lo publica y lo prueba, no crea los avisos de
  suspensión (los crea HU-07).
