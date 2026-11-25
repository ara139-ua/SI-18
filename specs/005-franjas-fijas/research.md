# Investigación y decisiones: 005 · Franjas fijas de temporada para clubes

**Entrada**: [spec.md](./spec.md) (con las aclaraciones del 2026-11-25), la base común de
[`000-core`](../000-core/plan.md) y lo ya construido en [`001-buscar-pistas`](../001-buscar-pistas/research.md).
Las decisiones de 000-core (R1–R19) y de spec-001 (D1–D10) **no se repiten**. Las de esta spec se numeran
**F1–F11** para no confundirlas.

No queda ningún «NEEDS CLARIFICATION»: la pila, el almacenamiento, el reloj y los errores ya los fija
000-core, y los tres códigos nuevos ya están en `develop` (PR #32).

## F1. Endpoints dentro de `/api/solicitudes-franjas-fijas`

- **Decisión** (contrato: [contracts/api-solicitudes-franjas-fijas.openapi.yaml](./contracts/api-solicitudes-franjas-fijas.openapi.yaml)):

  | Método y ruta                                          | Quién                      | Qué hace                                                                 |
  | ------------------------------------------------------ | -------------------------- | ------------------------------------------------------------------------ |
  | `GET  /api/solicitudes-franjas-fijas`                  | representante o técnico    | Representante: las de su club. Técnico: todas, primero las pendientes     |
  | `POST /api/solicitudes-franjas-fijas`                  | representante (`EsRepresentante`) | Crea una solicitud pendiente a nombre del club de la sesión → 201 |
  | `GET  /api/solicitudes-franjas-fijas/{id}/conflictos`  | técnico (`EsTecnico`)      | Reservas en conflicto y solicitudes aprobadas solapadas (F3)             |
  | `POST /api/solicitudes-franjas-fijas/{id}/aprobacion`  | técnico (`EsTecnico`)      | Aprueba si no hay conflictos (F4) → 200                                  |
  | `POST /api/solicitudes-franjas-fijas/{id}/rechazo`     | técnico (`EsTecnico`)      | Rechaza → 200                                                            |
  | `GET  /api/solicitudes-franjas-fijas/temporadas`       | representante              | Temporadas no terminadas, para el formulario (RF-003)                    |

  El listado admite los dos roles con `RequireAuthorization()` y una comprobación del rol en el propio
  endpoint (cualquier otro rol → 403 `ROL_NO_PERMITIDO`), para no añadir una política nueva a `Comun`.
- **Motivo**: todo dentro del prefijo reservado (contracts/README.md de 000-core). Aprobar y rechazar son
  acciones con efectos (avisos), así que van como `POST` a un subrecurso, igual que el estilo
  `/api/reservas/{id}/cancelacion` de 000-core. Las temporadas no tienen endpoint en `Comun`; se publican
  aquí, porque solo las usa esta historia.
- **Alternativas descartadas**: `PATCH` con el estado nuevo (mezcla dos acciones con reglas distintas en un
  mismo cuerpo); una política `EsRepresentanteOTecnico` en `Comun` (toca un fichero común para un solo
  endpoint); `/api/temporadas` (prefijo de `Comun`, que es de todo el grupo).

## F2. Validación de una solicitud nueva

- **Decisión**: se valida **antes** de construir la entidad, porque el constructor de
  `SolicitudFranjasFijas` lanza `ArgumentException` (que acabaría en 500). Orden:
  1. formato → 400 `PETICION_INVALIDA` con `errors` por campo y en español:
     - `diasSemana` vacío, con valores fuera de 1–7 o repetidos → «Elige al menos un día, sin repetir.»;
     - `horaInicio`/`horaFin` que no son `HH:00`, fuera de 08:00–22:00, o fin ≤ inicio →
       «Elige una hora en punto entre las 08:00 y las 22:00.» / «La hora de fin debe ser posterior a la
       de inicio.»;
  2. la instalación no existe, o no es pista ni pabellón → 400 con
     `errors.instalacionId` = «Elige una pista o un pabellón.» (D2 de 000-core);
  3. la temporada no existe o ya ha terminado (`FechaFin < hoy` en Madrid) → 400 con
     `errors.temporadaId` = «Esa temporada ya ha terminado.»;
  4. el usuario no tiene club → 403 `ROL_NO_PERMITIDO` (no debería pasar con la política
     `EsRepresentante`, porque 000-core obliga a que el representante tenga club).

  El club **nunca** llega en el cuerpo: sale de `IUsuarioActual.ClubId` (RF-001).
- **Motivo**: RF-002, RF-003 y RF-005 sin códigos nuevos: son errores de un formulario, y
  `PETICION_INVALIDA` con `errors` marca el campo, que es lo que pide el escenario 4 de la historia 1.
- **Alternativas descartadas**: `INSTALACION_NO_RESERVABLE` para una sala (su texto, «no se reserva por
  franjas», confunde con las franjas sueltas); capturar `ArgumentException` del dominio (los mensajes del
  dominio no son textos para el usuario).

## F3. Regla de franjas fijas en el dominio (función pura)

- **Decisión**: una clase estática nueva, `SI18.Dominio/Disponibilidad/ReglaFranjasFijas.cs`, sin
  dependencias:
  - `bool SeSolapan(SolicitudFranjasFijas a, SolicitudFranjasFijas b)`: misma instalación y temporada, al
    menos un día en común y `a.HoraInicio < b.HoraFin && b.HoraInicio < a.HoraFin` (intervalos
    semiabiertos: 19–20 y 20–21 no se solapan);
  - `bool Cubre(SolicitudFranjasFijas s, DateOnly fecha, TimeOnly hora)`: día ISO de la fecha en
    `DiasSemana` y `HoraInicio ≤ hora < HoraFin`;
  - `IReadOnlyList<Franja> Expandir(SolicitudFranjasFijas s, Temporada t, DateOnly desde, DateOnly hasta)`:
    para cada fecha de `[desde, hasta] ∩ [t.FechaInicio, t.FechaFin]` cubierta por `s`, las franjas de
    `CalculadoraFranjas.FranjasDelDia` con `Cubre`. Funciona igual los días de cambio de hora.
- **Motivo**: principios II y III. La misma regla sirve para el solape (F4), los conflictos (F4) y las
  franjas bloqueadas (F6), así que no puede haber tres versiones. Se prueba con pruebas unitarias de
  límites, sin base de datos.
- **Alternativas descartadas**: calcular el solape en SQL (dos versiones de la regla); dejar la expansión
  dentro de la consulta como hacía la provisional (no se podría reutilizar ni probar sola).

## F4. Conflictos: calcular, mostrar y volver a comprobar al aprobar

- **Decisión**: un servicio `ServicioSolicitudesFranjasFijas` (módulo `Disponibilidad`, scoped) con un
  método privado único `Conflictos(solicitud)`:
  - **reservas**: `IConsultaReservas.Activas(instalación, ahora, AUtc(temporada.FechaFin + 1, 00:00))`,
    que ya devuelve solo confirmadas y pendientes vigentes, filtradas con
    `ReglaFranjasFijas.Cubre(solicitud, fecha, hora)` de su `InicioUtc` en hora de Madrid. **Solo** se
    exponen la fecha, la hora y el estado (aclaración 3: sin `UsuarioId`). Cuenta cualquier titular,
    también el propio club (aclaración 5);
  - **solapes**: las solicitudes `Aprobada` de la misma instalación y temporada con
    `ReglaFranjasFijas.SeSolapan`.

  `GET …/conflictos` lo muestra. `POST …/aprobacion` lo **vuelve a calcular** dentro de la misma
  petición (RF-009):
  1. no existe → 404 `NO_ENCONTRADO`;
  2. ya resuelta → 409 `SOLICITUD_YA_RESUELTA` (lo lanza el dominio);
  3. hay reservas en conflicto → 409 `SOLICITUD_CON_CONFLICTOS`;
  4. solo hay solapes → 409 `FRANJA_FIJA_SOLAPADA`;
  5. si no, `Aprobar(ahora)`, los avisos (F5) y **un único** `SaveChanges`.
- **Motivo**: RF-008, RF-009, RF-012 y D8 de 000-core. Un único cálculo para mostrar y para decidir. Lo
  hace el backend (principio III).
- **Concurrencia (riesgo aceptado)**: entre comprobar y guardar podría colarse una reserva nueva, o
  aprobarse a la vez otra solicitud solapada. A escala municipal la ventana es de milisegundos. Además, en
  cuanto se guarda la aprobación, HU-01 y HU-02 dejan de ofrecer y de aceptar esas franjas (F6), y una
  aprobación duplicada de la misma solicitud la para el dominio (`SOLICITUD_YA_RESUELTA`). No se añade
  bloqueo serializable (principio VIII); se anota en «Riesgos» del plan.
- **Alternativas descartadas**: transacción `SERIALIZABLE` con reintentos (complejidad que ninguna spec
  pide); bloquear con `FOR UPDATE` las reservas de toda la temporada (bloqueo largo y amplio).

## F5. Avisos a los representantes

- **Decisión**: al aprobar o rechazar, para cada usuario con `ClubId == solicitud.ClubId` y rol
  `RepresentanteClub`, `IServicioAvisos.Avisar(usuarioId, texto)`, en la misma unidad de trabajo que el
  cambio de estado: un solo `SaveChanges` (RF-013, CE-004; R18 de 000-core). Textos:
  - «Tu solicitud de franjas fijas para {instalación} ({centro}), {días} de {HH:mm} a {HH:mm}, temporada
    {temporada}, ha sido aprobada.»
  - «… ha sido rechazada.» (sin motivo, aclaración 1).

  `{días}` se escribe en español: «martes y jueves», «lunes, miércoles y viernes».
- **Motivo**: RF-013 y RF-024 de 000-core; el aviso no puede quedar guardado sin el cambio ni al revés.
- **Alternativas descartadas**: avisar solo a quien envió la solicitud (la entidad no guarda quién fue, y
  la spec dice «el club»).

## F6. `IConsultaFranjasFijas` definitiva y retirada de la provisional

- **Decisión**: `Modulos/Disponibilidad/ConsultaFranjasFijas.cs` (dependencias: `Si18DbContext` e
  `IConsultaCierres`):
  - `FranjasBloqueadas(instalación, desde, hasta)`: solicitudes `Aprobada` de la instalación cuya
    temporada se cruza con `[desde, hasta]`, expandidas con `ReglaFranjasFijas.Expandir`, **menos** las
    franjas que afecta algún cierre (`IConsultaCierres.Cierres(instalación, AUtc(desde), AUtc(hasta + 1))`
    + `CierreMantenimiento.Afecta`), sin repetir. Es RF-014 y la aclaración de 000-core sobre los cierres.
  - `RepresentantesAfectados(instalación, inicioUtc, finUtc)`: se expanden las solicitudes aprobadas de
    la instalación para las fechas de `[inicioUtc, finUtc)`, **sin** descontar cierres (es justo el
    cierre el que las afecta); los clubes con alguna franja que se solape con ese periodo; y de esos
    clubes, sus usuarios representantes, sin repetir (RF-016).

  En `RegistroDisponibilidad`:
  - `TryAddScoped<IConsultaFranjasFijas, ConsultaFranjasFijasProvisional>()` pasa a
    `AddScoped<IConsultaFranjasFijas, ConsultaFranjasFijas>()`;
  - se borra `Provisional/ConsultaFranjasFijasProvisional.cs`;
  - las pruebas de franjas fijas de `ConsultasProvisionalesTests` se mueven a `ConsultaFranjasFijasTests`;
  - `DatosDisponibilidad.ConServicio` usa la definitiva.
- **Motivo**: RF-015, RF-016 y CE-005; es lo que pactó D5 de spec-001. Al descontar los cierres aquí,
  `ReglaDisponibilidad` sigue dando `INSTALACION_CERRADA` en esas horas (el cierre va antes), así que la
  búsqueda no cambia.
- **Alternativas descartadas**: dejar la provisional y añadir solo la regla de cierres (dos clases
  vivas para lo mismo).

## F7. Pantalla del representante: «Franjas fijas de mi club»

- **Decisión**: `SI18.Cliente/Disponibilidad/FranjasFijasClub.razor`, ruta `/franjas-fijas`:
  - **Formulario** (`<form>` + `MensajeAccesible`):
    - `MudSelect` de instalación: pistas y pabellones de `GET /api/instalaciones`, con el nombre del
      centro delante («Polideportivo Norte · Pabellón»);
    - `<fieldset>` con `<legend>` «Días de la semana» y siete `MudCheckBox` (lunes…domingo);
    - `MudSelect` de hora de inicio (08:00…21:00) y de fin (09:00…22:00): solo horas en punto;
    - `MudSelect` de temporada, de `GET …/temporadas`;
    - botón «Enviar solicitud», que no se desactiva al enviar (lección de spec-001).
  - **Lista** de las solicitudes del club (`role="list"`): instalación, días, horario, temporada y estado
    **en texto** («Pendiente», «Aprobada», «Rechazada»), y la fecha de resolución.
  - **Acceso** por URL como en spec-001: sin sesión → `/iniciar-sesion`; otro rol → aviso sin formulario.
  - **Foco**: tras elegir en un `MudSelect`, el foco vuelve al desplegable (T026 de spec-001).
- **Motivo**: RF-001–RF-006 y RF-018. Con desplegables de horas en punto no se puede elegir una hora
  incorrecta, aunque la API la valida igual (principio III).

## F8. Pantalla del técnico: «Revisar franjas fijas»

- **Decisión**: `SI18.Cliente/Disponibilidad/RevisarFranjasFijas.razor`, ruta `/franjas-fijas/revision`:
  - lista de todas las solicitudes, primero las pendientes, con club, instalación, días, horario,
    temporada y estado en texto;
  - para cada pendiente, un botón «Ver conflictos» que carga `GET …/conflictos` y los muestra debajo:
    «No hay conflictos» o la lista de reservas («martes 01/12/2026, 19:00–20:00 · confirmada») y de
    franjas fijas solapadas (club, días y horario);
  - botones «Aprobar» y «Rechazar». «Aprobar» está desactivado mientras haya conflictos, con un texto
    junto a él que dice por qué. La API vuelve a comprobarlo (F4);
  - resultado anunciado en `MensajeAccesible`; tras resolver, el foco pasa a la lista (lección de
    «Mis avisos»).
- **Motivo**: RF-007–RF-012 y RF-018; D8 de 000-core («la acción aprobar queda bloqueada y se listan las
  reservas»).

## F9. Contratos (DTO) y enumerados

- **Decisión**: en `SI18.Contratos/Disponibilidad/`: `PeticionSolicitudFranjasFijas`,
  `SolicitudFranjasFijasDto`, `ConflictosSolicitudDto` (con `ReservaEnConflictoDto` y
  `SolicitudSolapadaDto`), `TemporadaDto` y los enumerados espejo `EstadoSolicitud`
  (`pendiente`/`aprobada`/`rechazada`) y `EstadoReservaEnConflicto` (`confirmada`/`pendiente_pago`).
  Las horas viajan como `"HH:mm"` y los días como números ISO. El detalle está en [data-model.md](./data-model.md).
  `EstadoSolicitud` se añade a `EnumeracionesEspejoTests`.
- **Motivo**: convenciones de contracts/README.md de 000-core.

## F10. Pruebas

- **Dominio** (`SI18.Dominio.Tests/Disponibilidad/ReglaFranjasFijasTests.cs`, `Categoria=Limites`):
  solape 19–21/20–22 sí, 19–20/20–21 no, otro día no, otra instalación o temporada no; `Cubre` en 19:00 y
  20:00 sí y en 21:00 no; `Expandir` fuera de temporada vacío, el 25/10/2026 y el 28/03/2027 con las
  horas en Madrid.
- **Integración** (`SI18.Api.Tests/Disponibilidad/`, `FabricaApi` y `DatosDisponibilidad`):
  - `SolicitarFranjasFijasTests`: crear (201, pendiente y a nombre del club de la sesión), no bloquea
    la búsqueda, errores por campo, sala o piscina, temporada terminada, roles (403) y que otro club no
    la ve;
  - `RevisarFranjasFijasTests`: aprobar sin conflictos (estado, búsqueda sin esas franjas, avisos a
    todos los representantes), con una reserva confirmada o pendiente vigente (409 y listada, sin
    datos personales), pendiente caducada (ya no cuenta), reserva del propio club (cuenta), solape con
    una aprobada (409), dos pendientes solapadas (la segunda se bloquea), rechazar (estado, avisos, la
    búsqueda sigue libre), resolver dos veces (409) y roles;
  - `ConsultaFranjasFijasTests`: las pruebas movidas de la provisional, más el cierre que suspende y
    `RepresentantesAfectados`.
- **Accesibilidad**: comprobación manual de quickstart §6 de 000-core en las dos pantallas.

## F11. Coordinación con spec-002 (Saul)

- **Decisión**: spec-002 (C2 de su plan) borra `ConsultaReservasProvisional` y toca los mismos ficheros
  que F6: `RegistroDisponibilidad.cs`, `ConsultasProvisionalesTests.cs` y `DatosDisponibilidad.cs`. El
  PR que llegue segundo hace `rebase` sobre `develop` y resuelve el conflicto, que es de líneas
  contiguas (cada uno quita su provisional). Se avisa a Saul en el PR.
- **Motivo**: constitución, «Acuerdos de colaboración»: la dependencia se anota en las dos specs.
