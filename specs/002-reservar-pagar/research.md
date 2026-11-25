# Investigación y decisiones: 002 · Reservar y pagar una pista

**Entrada**: [spec.md](./spec.md) (con las aclaraciones del 2026-11-25), la base común de
[`000-core`](../000-core/plan.md) y lo que ya está en `develop` de [spec-001](../001-buscar-pistas/plan.md).
Las decisiones de 000-core (R1–R19) y de spec-001 (D1–D10 de su research.md) **no se repiten**: aquí
solo figuran las que añade HU-02. Se numeran E1–E14 para no confundirlas con ninguna de las dos.

No queda ningún «NEEDS CLARIFICATION» en el contexto técnico: la pila, el almacenamiento, las pruebas,
el reloj, la pasarela, el código de acceso, la máquina de estados y la secuencia de reserva y caducidad
ya los fija 000-core. Tampoco hace falta ningún código de error nuevo.

## Punto de partida: lo que ya existe en `develop`

| Pieza                                                         | Estado                         | Dueño |
| ------------------------------------------------------------- | ------------------------------ | ----- |
| `Reserva` y `Pago` (entidades, máquina de estados: `Confirmar`, `Caducar`, `Cancelar`, `EstaVigente`) | Hecho en 000-core, con pruebas | Saul |
| `ReservaConfig`, `PagoConfig` e índice `ux_reservas_activa`   | Hecho en 000-core (migración `NucleoComun`) | Saul |
| `IPasarelaPago` + `PasarelaSimulada` (`0002` rechazada)       | Hecho en 000-core              | Artur |
| `GeneradorCodigoAcceso` (16 bytes, Base64url)                 | Hecho en 000-core              | Saul |
| `BarridoCaducidades` (cada 60 s)                              | Hecho en 000-core              | Saul |
| Interfaces `IConsultaReservas` e `IServicioReservas` (sin implementar) | Hecho en 000-core      | Saul |
| `IConsultaDisponibilidad` (implementada por `ServicioDisponibilidad`) | Hecho en spec-001      | Artur |
| `ConsultaReservasProvisional` + su línea `TryAddScoped` en `RegistroDisponibilidad` | Hecho en spec-001 (provisional) | Artur → se retira aquí |
| Pantalla `/buscar-pistas` (franjas como texto, sin botón)     | Hecho en spec-001              | Artur |

HU-02, por tanto, **no añade tablas ni migraciones** y no toca el dominio salvo para usarlo: compone lo
que hay en un servicio, unos endpoints y unas pantallas.

## E1. Endpoints dentro de `/api/reservas`

- **Decisión**: cinco endpoints en el prefijo reservado de Reservas (contracts/README.md de 000-core),
  todos con la política `EsCiudadanoORepresentante`:

  | Método y ruta                         | Para qué                                     | Spec |
  | ------------------------------------- | -------------------------------------------- | ---- |
  | `GET /api/reservas/resumen?instalacionId&inicioUtc` | Resumen previo de una franja (E2) | RF-003 |
  | `POST /api/reservas`                  | Crear la reserva pendiente de pago (E3)      | RF-004–RF-007 |
  | `POST /api/reservas/{id}/pago`        | Pagar (E4)                                   | RF-008–RF-013 |
  | `GET /api/reservas/{id}`              | Ver una reserva propia (pantalla de pago y resultado) | RF-008a, RF-013 |
  | `GET /api/reservas`                   | «Mis reservas» (E6)                          | RF-015–RF-017 |

  Contrato: [contracts/api-reservas.openapi.yaml](./contracts/api-reservas.openapi.yaml).
  `/api/reservas/{id}/cancelacion` **no** es de esta spec: es de Cancelaciones (HU-03).
- **Motivo**: es lo que pide la spec y cabe en el prefijo que 000-core reservó para «crear, pagar,
  listar las mías». `GET /{id}` hace falta para que la pantalla de pago se pueda recargar o abrir desde
  «Mis reservas» (RF-017) sin pasar datos por la URL.
- **Alternativas descartadas**: un único `POST` que reserve y pague a la vez (sin el paso intermedio
  el bloqueo de 10 minutos no protege nada mientras se teclea la tarjeta; spec, «Supuestos: dos
  pasos»); `PUT /api/reservas/{id}` con un estado en el cuerpo (expone la máquina de estados al
  cliente).

## E2. Resumen previo sin crear nada

- **Decisión**: `GET /api/reservas/resumen` recibe la franja tal como la entrega HU-01 (`instalacionId`
  e `inicioUtc`, RF-003) y devuelve pista, centro, fecha, horas de Madrid e importe para la tarifa del
  usuario. Hace las mismas comprobaciones que la creación (E3, pasos 1–2) sin escribir nada: si la
  franja ya no está libre, responde con el código del motivo, y así el usuario se entera antes de
  confirmar. El importe sale de `IConsultaDisponibilidad.Precio`.
- **Motivo**:
  - RF-003 y HU1 esc. 1 piden un resumen con datos que la búsqueda no pasa por la URL (centro, fecha);
  - las horas de pared las calcula la API con `HorarioMadrid`, como en spec-001 (D7: el cliente no
    convierte zonas);
  - el precio que el usuario «ve» y luego envía (`importeMostrado`, E3) es el del resumen, calculado
    por HU-01: no se recalcula en Reservas (RF-004).
- **Alternativas descartadas**: pasar el precio y los nombres en la URL desde `/buscar-pistas` (más
  parámetros en un enlace de otro dueño, y el cliente tendría que convertir horas); que el resumen lo
  arme el cliente con `/api/instalaciones` y `/api/centros` (lo mismo, y duplica el cálculo de horas).

## E3. Crear la reserva

- **Decisión**: `POST /api/reservas` con `{ instalacionId, inicioUtc, importeMostrado }`. Sigue el
  «Flujo de una reserva» de 000-core (plan.md) en este orden, y gana el primero que falla:
  1. cuerpo mal formado (campo ausente, `importeMostrado ≤ 0`) → 400 `PETICION_INVALIDA`;
  2. `IConsultaDisponibilidad.MotivoNoDisponible(instalacionId, inicioUtc)`:
     - lanza `PETICION_INVALIDA` (no es inicio de franja) o `NO_ENCONTRADO` (no existe la pista), que
       llegan solos a `ManejadorErrores`;
     - devuelve un motivo de RF-011 de 000-core → se lanza ese código (`FUERA_DE_ANTELACION`,
       `FRANJA_YA_EMPEZADA`, `INSTALACION_NO_RESERVABLE`, `INSTALACION_CERRADA`,
       `FRANJA_BLOQUEADA_CLUB` o `FRANJA_OCUPADA`);
  3. `IConsultaDisponibilidad.Precio(instalacionId, IUsuarioActual.TipoTarifa)` ≠ `importeMostrado`
     → 409 `PRECIO_CAMBIADO`;
  4. en una transacción:
     - se cargan con bloqueo (E5) las reservas `pendiente_pago` **no vigentes** de esa instalación e
       inicio, y se caducan con `Reserva.Caducar` (pago → fallido) (R8.2);
     - se añade `new Reserva(usuario, instalación, inicio, tarifa, importe, ahora)`, que crea también
       su pago pendiente;
     - `SaveChanges` y `COMMIT`. Si PostgreSQL responde `23505` en `ux_reservas_activa`, se responde
       409 `FRANJA_OCUPADA` (R7; RF-007).
  5. 201 con la reserva (`ReservaDto`, E12) y `Location: /api/reservas/{id}`.

  `ahora` se lee **una sola vez** de `TimeProvider` al principio y se usa en todos los pasos.
- **Motivo**: RF-004–RF-007 de la spec; RF-013, RF-014 y R8.2 de 000-core. La disponibilidad la decide
  siempre HU-01 y la exclusión mutua, la base de datos (principio III): dos peticiones a la vez pasan
  las dos el paso 2, pero solo una supera el índice único.
- **Alternativas descartadas**:
  - bloquear toda la tabla o usar `SERIALIZABLE` (más caro y el índice ya lo garantiza);
  - devolver en `PRECIO_CAMBIADO` el importe nuevo como campo extra del Problem Details (cambia el
    formato común; el cliente vuelve a pedir el resumen, E11);
  - traducir `23505` en el `ManejadorErrores` global (es una regla de Reservas; se captura en su
    servicio y se comprueba el nombre del índice, como ya hace `ReservaActivaUnicaTests`).

## E4. Pagar

- **Decisión**: `POST /api/reservas/{id}/pago` con `{ numeroTarjeta }`. En este orden:
  1. `numeroTarjeta` sin espacios no tiene 16 dígitos → 400 `PETICION_INVALIDA` con
     `errors.numeroTarjeta`, **sin** llamar a la pasarela ni tocar la reserva (RF-012). Se valida en el
     endpoint, antes de cargar nada, así que tampoco revela si la reserva existe;
  2. transacción; se carga la reserva con su pago y con bloqueo (E5) **filtrando por
     `usuario_id = IUsuarioActual.Id`**: si no existe o es de otro → 404 `NO_ENCONTRADO` (RF-013);
  3. estado guardado distinto de `pendiente_pago` → 409 `TRANSICION_NO_PERMITIDA` (RF-011: sin
     reintento);
  4. `!EstaVigente(ahora)` → `Caducar(ahora)`, `SaveChanges`, `COMMIT` y 409 `RESERVA_CADUCADA`, sin
     llamar a la pasarela (RF-009; R8.3);
  5. `IPasarelaPago.Cobrar(reserva.Importe, numeroTarjeta)`:
     - `true` → `Confirmar(GeneradorCodigoAcceso.Nuevo(), ahora)`, `SaveChanges`, `COMMIT` y 200 con
       la reserva confirmada y su `codigoAcceso` (RF-010);
     - `false` → `Caducar(ahora)`, `SaveChanges`, `COMMIT` y 422 `PAGO_RECHAZADO` (RF-011).

  En los pasos 4 y 5 (rechazo) el cambio de estado **se guarda antes de lanzar** el `ErrorNegocio`: el
  error explica lo que ya ha pasado. `ahora` se lee una vez al principio, así que una reserva vigente en
  el paso 4 no puede caducar entre el cobro y `Confirmar`.
- **Datos de la tarjeta**: el número solo vive en la petición. No se guarda, no se registra (ni el
  cuerpo de la petición ni el número en ningún `ILogger`), no se devuelve y la clase de la petición
  redefine `ToString()` para no imprimirlo (R9; RF-008).
- **Después del barrido**: si `BarridoCaducidades` ya ha guardado la reserva como caducada, el paso 3
  responde `TRANSICION_NO_PERMITIDA` en vez de `RESERVA_CADUCADA`. Es coherente con RF-011 (no se paga
  lo que no está pendiente) y el texto del catálogo es válido para ese caso. La pantalla de pago, que ya
  muestra la hora límite, lo explica igual (E11).
- **Alternativas descartadas**: llamar a la pasarela y luego comprobar la vigencia (cobraría una
  reserva caducada); dejar que `PasarelaSimulada` valide el formato (lanza el mismo código, pero ya
  dentro de la transacción y después de cargar la reserva; RF-012 pide no llamarla).

## E5. Bloqueo de fila en toda escritura sobre una reserva existente

- **Decisión**: toda operación que cambia el estado de una reserva **ya guardada** (pagar, caducar al
  reservar, cancelar por el usuario, aplicar un cierre y el barrido) la carga dentro de una transacción
  con `SELECT … FOR UPDATE` (el patrón de R19 de 000-core), vuelve a mirar su estado ya bloqueada y
  solo entonces llama a la máquina de estados. Un único método interno de Reservas
  (`CargarParaModificar`) hace la consulta y carga el `Pago`. `BarridoCaducidades` (dueño: Saul) pasa a
  usar `FOR UPDATE SKIP LOCKED`, para no esperar a un pago en curso: lo que se salte lo coge en la
  siguiente vuelta.
- **Motivo**: sin bloqueo, dos pagos simultáneos de la misma reserva cobrarían dos veces, y un pago que
  confirma a los 9:59 podría cruzarse con el barrido o con un cierre que la caduca. El índice único solo
  protege la creación. La transacción ya existe (cada caso de uso tiene una), así que el coste es una
  cláusula en una consulta.
- **Alternativas descartadas**: concurrencia optimista con `xmin` (detecta el choque después de haber
  cobrado; además cambia `ReservaConfig` y el snapshot del modelo); un `UPDATE … WHERE estado =
  'pendiente_pago'` condicional (salta la máquina de estados de la entidad, que es la única regla).

## E6. «Mis reservas» y ver una reserva

- **Decisión**:
  - `GET /api/reservas` devuelve **todas** las reservas del usuario de la sesión, sin paginación, con
    el orden de RF-015a calculado en la API con `ahora`:
    1. primero las no terminadas (`finUtc > ahora`), por `inicioUtc` ascendente;
    2. después las terminadas, por `inicioUtc` descendente.
  - `GET /api/reservas/{id}` devuelve una sola, filtrando por usuario (otra persona → 404).
  - El **estado que se muestra** es el efectivo: una `pendiente_pago` no vigente se devuelve como
    `caducada` aunque el barrido aún no la haya guardado (RF-015; HU5 esc. 5). Es una lectura: no se
    escribe nada.
  - `codigoAcceso` solo viaja si el estado es `confirmada` (RF-016); `limitePagoUtc` y `horaLimitePago`,
    solo si es `pendiente_pago` vigente (RF-008a, RF-017).
  - Se lee con `AsNoTracking` uniendo `reservas` con `instalaciones` y `centros` (tablas de Comun, solo
    lectura) para el nombre de la pista y del centro.
- **Motivo**: RF-015–RF-017 y las aclaraciones 4 y 5. El índice (`usuario_id`, `inicio_utc`) de
  000-core ya existe para esta consulta.
- **Alternativas descartadas**: paginar o filtrar por estado (la aclaración 4 lo descarta); ordenar en
  el cliente (la regla depende de `ahora`, que es del servidor; principio III).

## E7. `IConsultaReservas` definitiva y retirada de la provisional

- **Decisión**:
  - `Modulos/Reservas/ConsultaReservas.cs` implementa `IConsultaReservas` con **la misma consulta** que
    la provisional: reservas de la instalación con `inicio_utc ∈ [desdeUtc, hastaUtc)` y estado
    `confirmada`, o `pendiente_pago` con `creada_en_utc > ahora − TiempoBloqueoPago` (la vigencia de
    `Reserva.EstaVigente`), ordenadas por inicio, `AsNoTracking`.
  - Se registra con `AddScoped` en `RegistroReservas` (E9).
  - En el mismo PR, como acordó spec-001 (research.md D5):
    1. se borra `Modulos/Disponibilidad/Provisional/ConsultaReservasProvisional.cs`;
    2. se borra **su línea** `services.TryAddScoped<IConsultaReservas, ConsultaReservasProvisional>();`
       de `RegistroDisponibilidad.cs`; las otras dos provisionales y el resto del fichero no cambian;
    3. las dos pruebas de `IConsultaReservas` de `ConsultasProvisionalesTests.cs`
       (`Una_pendiente_bloquea_hasta_los_10_minutos_y_no_despues` y
       `Solo_cuentan_confirmadas_y_pendientes_vigentes_dentro_del_rango`) se **mueven sin cambiar sus
       casos** a `tests/SI18.Api.Tests/Reservas/ConsultaReservasTests.cs`, apuntando a
       `ConsultaReservas`. Las de cierres y franjas fijas se quedan donde están.
  - `BuscarFranjasTests`, `BuscarSinResultadosTests`, `ConsultaDisponibilidadTests` y
    `PrecioPorTarifaTests` de spec-001 **no se tocan** y deben seguir en verde (RF-019; CE-007).
- **Motivo**: RF-018, RF-019 y CE-007. Con la misma consulta y las mismas pruebas, la búsqueda da el
  mismo resultado. Los tres ficheros que se tocan son de Artur, pero el cambio es el que él mismo dejó
  pactado en D5 y se le pide revisión en el PR (plan.md, «Puntos a coordinar»).
- **Alternativas descartadas**: dejar la provisional registrada «por si acaso» (dos implementaciones de
  la misma regla; `/speckit-converge` la marcaría); reutilizar `Reserva.EstaVigente` en memoria
  (obligaría a traer todas las pendientes; la condición traducida a SQL es la misma fórmula).

## E8. `IServicioReservas` para HU-03 y HU-07 (RF-019a)

- **Decisión**: `Modulos/Reservas/ServicioReservas.cs` implementa la interfaz de 000-core **sin cambiar
  su firma**:
  - `CancelarPorUsuario(reservaId, usuarioId)`:
    1. carga con bloqueo (E5) la reserva **de ese usuario**; si no existe o es de otro → `NO_ENCONTRADO`;
    2. `Cancelar(MotivoCancelacion.Usuario, ahora)`: si no está confirmada →
       `TRANSICION_NO_PERMITIDA`; si ya ha empezado (`ahora ≥ inicio`) → `RESERVA_YA_EMPEZADA` (la
       máquina de estados ya lo hace en ese orden);
    3. devuelve `ReservaAfectada(id, usuarioId, inicioUtc, pago.Id, pago.Importe)`.
  - `AplicarCierre(instalacionId, inicioUtc, finUtc)`: carga con bloqueo las reservas de la instalación
    que se **solapan** con el cierre (`r.inicio_utc < finUtc && r.fin_utc > inicioUtc`, intervalos
    semiabiertos como en data-model.md de 000-core) y con estado guardado `confirmada` o
    `pendiente_pago`:
    - las confirmadas → `Cancelar(MotivoCancelacion.CierreMantenimiento, ahora)` (sin límite de hora)
      → lista `Canceladas`;
    - las pendientes → `Caducar(ahora)` (pago → fallido) → lista `Caducadas`. Entran todas las que
      siguen guardadas como pendientes, vigentes o no: así ninguna queda colgando, y HU-07 decide a
      quién avisa;
    - las dos listas, ordenadas por inicio.
  - **Transacción del llamador** (R18 y «Los cambios se guardan en la misma operación que los de quien
    llama», RF-019a): ninguno de los dos métodos llama a `SaveChanges` ni abre o confirma
    transacciones. Modifican entidades del `Si18DbContext` con ámbito de petición, el mismo que usa
    quien llama, y **exigen** que el llamador haya abierto ya la transacción
    (`Database.CurrentTransaction` no nulo; si no, `InvalidOperationException`, que es un error de
    programación y no de negocio), porque sin ella el `FOR UPDATE` no bloquea nada. El llamador crea
    la devolución y el aviso, hace `SaveChanges` y `Commit`: todo o nada.
  - Se prueban aquí (spec-002), con un llamador de prueba que abre la transacción, igual que lo harán
    HU-03 y HU-07 (aclaración 1).
- **Motivo**: aclaración 1 y RF-019a de la spec; RF-015, RF-020 y R4 de 000-core; constitución,
  «Acuerdos de colaboración» (solo el dueño escribe en sus tablas).
- **Alternativas descartadas**: que cada método haga su propio `SaveChanges` (la devolución o el aviso
  podrían quedarse sin guardar con la reserva ya cancelada); que HU-03 y HU-07 llamen directamente a
  `Reserva.Cancelar` (escribirían en tablas ajenas).

## E9. Registro del módulo Reservas

- **Decisión**: `Modulos/Reservas/RegistroReservas.cs` con:
  - `AddReservas()`: `ConsultaReservas` como `IConsultaReservas`, `ServicioReservas` como
    `IServicioReservas` y el servicio de casos de uso de los endpoints (`ServicioReservar`);
  - `MapReservas()`: los endpoints de `EndpointsReservas.cs`.

  `Program.cs` recibe las **dos líneas** de la convención de 000-core (plan.md, «Registro de los módulos
  en la API»), en su sitio fijo: `builder.Services.AddReservas();` justo después de
  `AddDisponibilidad()`, y `app.MapReservas();` justo después de `MapDisponibilidad()`.
  `IPasarelaPago` sigue registrada donde está (`RegistroPasarela`, 000-core).
- **Motivo**: es la convención aprobada en 000-core; no hay que proponer nada nuevo.
- **Alternativas descartadas**: registrar Reservas dentro de `RegistroInfraestructura` (mezcla módulo e
  infraestructura).

## E10. El botón «Reservar» en `/buscar-pistas` (pantalla de Artur) — **punto a coordinar**

- **Decisión**: el botón lo construye Reservas en un componente propio,
  `SI18.Cliente/Reservas/EnlaceReservar.razor`, con parámetros `Pista` (`PistaLibreDto`), `Franja`
  (`FranjaLibreDto`) y `Fecha` (`DateOnly`). Pinta un `MudButton` con `Href` (un enlace, no un
  formulario) a `/reservas/nueva?instalacionId={id}&inicioUtc={ISO 8601}` y con un nombre accesible
  completo: «Reservar Pista de pádel 1, sábado 26/12/2026, de 18:00 a 19:00» (RF-003; HU6 esc. 1).

  En `BuscarPistas.razor` (Artur) el cambio es **una línea** dentro del `<li>` de cada franja, más su
  `@using`:

  ```razor
  @using SI18.Cliente.Reservas
  …
  <li role="listitem">@franja.HoraInicio–@franja.HoraFin · @FormatoEs.Importe(franja.Precio)
      <EnlaceReservar Pista="pista" Franja="franja" Fecha="_resultado.Fecha" /></li>
  ```

  No cambia ningún DTO, endpoint, prueba ni lógica de la búsqueda: `FranjaLibreDto.InicioUtc` y
  `PistaLibreDto.InstalacionId` ya existen para esto (spec-001, data-model.md § 4). Si hace falta
  separar el botón del texto, el ajuste de estilo va en el propio componente.
- **Coordinación**: lo hace Saul en este PR **o** Artur en el suyo, a elección de Artur. En los dos
  casos Artur revisa el cambio de `BuscarPistas.razor`, y se avisa al grupo (constitución, «Acuerdos de
  colaboración»). Hasta que se fusione, la reserva se puede probar abriendo `/reservas/nueva?…` a mano
  (quickstart.md).
- **Motivo**: aclaración de spec-001 y RF-003 de esta spec, tocando el mínimo de código ajeno: toda la
  lógica, el texto accesible y la ruta de destino son de Reservas.
- **Alternativas descartadas**: escribir el `MudButton` y su `aria-label` directamente en
  `BuscarPistas.razor` (más líneas ajenas y la ruta de Reservas repetida en otro módulo); un evento
  `OnReservar` que `BuscarPistas` tenga que cablear (más cambios en su código); un botón que reserve
  sin pasar por el resumen (RF-003 lo exige).

## E11. Pantallas del cliente

- **Decisión**: en `SI18.Cliente/Reservas/` (dueño: Saul):
  - **`NuevaReserva.razor`** · `/reservas/nueva?instalacionId&inicioUtc`: pide el resumen (E2) y lo
    muestra como lista de definiciones (pista, centro, fecha, de … a …, importe con `FormatoEs`). Botón
    «Confirmar reserva» → `POST /api/reservas` con el importe mostrado → navega a
    `/reservas/{id}/pago`. Si responde `PRECIO_CAMBIADO`, vuelve a pedir el resumen, muestra el importe
    nuevo y anuncia el mensaje (HU4 esc. 5). Cualquier otro rechazo se anuncia con su `detail` y un
    enlace «Volver a buscar pistas».
  - **`PagarReserva.razor`** · `/reservas/{id}/pago`: `GET /api/reservas/{id}`; muestra el resumen y el
    texto fijo «Paga antes de las HH:mm; después la reserva caduca.» (aclaración 5, sin cuenta atrás).
    Un `MudTextField` «Número de tarjeta» con etiqueta visible, `inputmode="numeric"`,
    `autocomplete="cc-number"`, texto de ayuda («16 dígitos; es una tarjeta ficticia») y el error de
    `errors.numeroTarjeta` asociado al campo. Botón «Pagar 6,00 €». Si la reserva ya no está pendiente
    vigente, no muestra el formulario y explica su estado.
  - **Resultado** (en la misma página, sin cambiar de ruta): confirmada → mensaje «Reserva confirmada»
    anunciado, `CodigoAcceso` y enlace a «Mis reservas»; `PAGO_RECHAZADO`, `RESERVA_CADUCADA` o
    `TRANSICION_NO_PERMITIDA` → mensaje del catálogo anunciado y enlace «Volver a buscar pistas». El
    foco pasa al encabezado del resultado (que lleva `tabindex="-1"`), porque el formulario desaparece.
  - **`MisReservas.razor`** · `/mis-reservas`: una lista (`role="list"`) con un elemento por reserva:
    pista y centro (encabezado), fecha y horas, importe y estado **en texto** («Confirmada»,
    «Pendiente de pago», «Caducada», «Cancelada»), con un `MudChip` de color solo como refuerzo. En las
    confirmadas, `CodigoAcceso`; en las pendientes vigentes, enlace «Pagar» a `/reservas/{id}/pago`.
    Deja marcado el hueco donde HU-03 pondrá «Cancelar».
  - **`CodigoAcceso.razor`**: dibuja el QR en SVG con QRCoder (ya referenciado en el cliente, R14) dentro
    de un `<figure>` con `role="img"` y un `aria-label` que describe la reserva, y debajo el código
    escrito en texto seleccionable con un `<figcaption>` (HU6 esc. 4; RF-016).
  - **Menú**: «Mis reservas» en el hueco «Reservas (spec-002, 003)» del grupo ciudadano/representante de
    `MainLayout` (RF-002; T056 de 000-core).
  - **Acceso por URL**: igual que `/buscar-pistas` (spec-001 D7): sin sesión, a `/iniciar-sesion`; con
    otro rol, aviso sin formulario.
  - **Hora límite**: la manda la API como `horaLimitePago` («18:42») ya en Madrid, truncada al minuto
    hacia abajo: nunca promete más tiempo del que hay. El cliente no calcula nada.
- **Motivo**: RF-003, RF-008a, RF-010, RF-011, RF-015–RF-017 y RF-020–RF-022, con los componentes que
  ya fijó 000-core (`MensajeAccesible`, `FormatoEs`, `ClienteApi`) y el mismo patrón de spec-001.
- **Alternativas descartadas**: cuenta atrás en pantalla (la aclaración 5 la descarta y es un problema
  de accesibilidad: WCAG 2.2.1); un diálogo modal para pagar (peor con lector de pantalla y se pierde
  al recargar).

## E12. DTO compartidos

- **Decisión**: en `SI18.Contratos/Reservas/` (dueño: Saul): `PeticionReserva`, `PeticionPago`,
  `ResumenReservaDto`, `ReservaDto` y la copia del enumerado `EstadoReserva` (detalle en
  [data-model.md](./data-model.md) § 3). La prueba espejo de `EstadoReserva` (mismos nombres que en el
  dominio y mismo texto JSON) va en `tests/SI18.Api.Tests/Reservas/`, con el mismo patrón que
  `EnumeracionesEspejoTests` de 000-core.
- **Motivo**: contracts/README.md de 000-core («Los DTO están en `SI18.Contratos/<Modulo>/`»). Dejarlos
  en la carpeta del módulo evita tocar `Contratos/Comun/Enumeraciones.cs`, que es común.
- **Alternativas descartadas**: añadir `EstadoReserva` a `Contratos/Comun/Enumeraciones.cs` (un fichero
  común para algo que solo usa Reservas, de momento); devolver la entidad `Reserva` (expone
  `CodigoAcceso` en estados que no toca y acopla el contrato al dominio).

## E13. Pruebas

- **Decisión**:
  - **Dominio**: la máquina de estados, `EstaVigente`, `GeneradorCodigoAcceso` y la pasarela ya tienen
    pruebas de límites en 000-core. No se duplican.
  - **Integración** (`tests/SI18.Api.Tests/Reservas/`, con `FabricaApi`, `FakeTimeProvider` y datos
    insertados directamente, como `DatosDisponibilidad` de spec-001):
    - `CrearReservaTests`: HU1 esc. 1–2 y 4–5, HU4 esc. 1–5 (cada motivo de RF-011, en orden), 400,
      404, 403 con personal y técnico, 401 sin sesión; doble envío de la misma persona; caducidad
      perezosa de una pendiente ajena de 10 min exactos (HU2 esc. 4).
    - `ReservaConcurrenteTests`: 20 peticiones simultáneas sobre la misma franja con usuarios
      distintos → exactamente una 201 y diecinueve 409 `FRANJA_OCUPADA` (CE-002).
    - `PagarReservaTests`: aceptada → confirmada con código de 22 caracteres; `…0002` → 422, caducada y
      pago fallido; 9:59 → se cobra; 10:00 → 409 `RESERVA_CADUCADA` sin llamar a la pasarela; segundo
      pago → 409 `TRANSICION_NO_PERMITIDA`; tarjeta de 15 dígitos → 400 y sigue pendiente; reserva
      ajena → 404; dos pagos simultáneos de la misma reserva → un solo cobro (con una pasarela de
      prueba que cuenta llamadas).
    - `MisReservasTests`: solo las propias; orden de RF-015a; pendiente no vigente devuelta como
      caducada; código solo en confirmadas; `horaLimitePago` en Madrid y truncada.
    - `ConsultaReservasTests`: las dos pruebas movidas desde `ConsultasProvisionalesTests` (E7).
    - `ServicioReservasTests`: `CancelarPorUsuario` (ajena → 404, empezada → `RESERVA_YA_EMPEZADA`,
      pendiente → `TRANSICION_NO_PERMITIDA`, correcta → datos de `ReservaAfectada`) y `AplicarCierre`
      (solape semiabierto: la franja que termina justo al empezar el cierre no entra; confirmadas →
      canceladas con motivo `cierre_mantenimiento`; pendientes → caducadas con pago fallido; sin
      transacción → `InvalidOperationException`; si el llamador no hace `Commit`, no cambia nada).
    - `ReservarComoEnLaBusquedaTests`: de extremo a extremo, para varias franjas y las tres tarifas, el
      importe cobrado es el `precio` de `GET /api/disponibilidad/franjas` (CE-005; base que dejó
      `ConsultaDisponibilidadTests` de spec-001).
    - `EstadoReservaEspejoTests` (E12).
    - Cambio de hora: reservar y pagar una franja del 28/03/2027 (1 h real).
  - **Reloj y barrido**: `BarridoCaducidades` corre en las pruebas con el `FakeTimeProvider`, y adelantar
    el reloj puede dispararlo. Las pruebas de límite de los 10 minutos **no adelantan el reloj**: crean
    la reserva con `creada_en_utc = ahora − 10 min` (o `− 9 min 59 s`) por inserción directa, como ya
    hace `ConsultasProvisionalesTests`.
  - **Límites** con `[Trait("Categoria", "Limites")]`: 7/8 días, inicio exacto, 9:59/10:00, solape del
    cierre.
  - **Accesibilidad**: comprobación manual de quickstart §5 sobre las cuatro pantallas y el botón.
- **Motivo**: principio II y CE-002–CE-008 de la spec.
- **Alternativas descartadas**: pruebas E2E con navegador (R16 de 000-core: no se piden).

## E14. Rendimiento y escala

- **Decisión**: sin caché ni colas. Crear una reserva hace las consultas de `MotivoNoDisponible` (de
  HU-01: una franja de una pista), una de precio y una transacción corta. Pagar, una consulta con
  bloqueo y la llamada a la pasarela simulada (en memoria). «Mis reservas», una consulta por el índice
  (`usuario_id`, `inicio_utc`). El objetivo es el de 000-core (< 500 ms p95 por petición), muy por debajo
  de CE-001 (reservar y pagar en < 2 min, que depende sobre todo de la persona).
- **Motivo**: escala municipal (000-core, «Escala y alcance»); principio VIII.
- **Alternativas descartadas**: paginar «Mis reservas» (la aclaración 4 lo descarta y un usuario tiene
  pocas decenas de reservas).
