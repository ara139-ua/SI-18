# Investigación (Fase 0): 000-core · Núcleo común de SI-18

**Fecha**: 2026-10-28 · **Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

Cada apartado sigue el formato *Decisión / Motivo / Alternativas descartadas*. No queda ningún
«NEEDS CLARIFICATION» abierto en el contexto técnico.

---

## R1. Versión de .NET

- **Decisión**: .NET 10 (LTS), `net10.0`, C# 14. Las versiones exactas de los paquetes se fijan una
  sola vez en `fuentes/Directory.Packages.props` (gestión central de paquetes).
- **Motivo**: la petición pide «la versión LTS de .NET instalada», pero el único SDK instalado en el
  equipo de referencia es **9.0.312 (STS)**, cuyo soporte termina el 10/11/2026, antes de la entrega.
  La LTS vigente es .NET 10 (soporte hasta noviembre de 2028). Cada persona del grupo DEBE instalar el
  SDK de .NET 10 (ver [quickstart.md](./quickstart.md)); `global.json` fija la banda de versiones
  `10.0.x` con `rollForward: latestFeature`.
- **Alternativas descartadas**: .NET 9 (sin soporte en dos semanas); .NET 8 LTS (soporte hasta
  noviembre de 2026, también caduca durante el curso).

## R2. Estructura de la solución

- **Decisión**: monorepo en `fuentes/` con cuatro proyectos de código y dos de pruebas:
  `SI18.Dominio`, `SI18.Contratos`, `SI18.Api`, `SI18.Cliente`, `SI18.Dominio.Tests` y
  `SI18.Api.Tests`. La persistencia (EF Core) vive en `SI18.Api/Infraestructura`, no en un proyecto
  aparte.
- **Motivo**: son exactamente los proyectos pedidos. `SI18.Dominio` no depende de ASP.NET Core ni de
  EF Core, así que las reglas se prueban con pruebas unitarias puras (constitución, Restricciones de
  pila). Un quinto proyecto de infraestructura no aporta nada que pida la spec (principio VIII).
- **Alternativas descartadas**: Clean Architecture con proyecto `Application` + `Infrastructure`
  (dos capas más sin necesidad); un único proyecto (mezcla HTTP y reglas, contra el principio III).

## R3. Organización por módulos y dueños

- **Decisión**: cinco módulos, cada uno con una carpeta homónima en `Dominio`, `Contratos`, `Api`,
  `Cliente` y en los dos proyectos de pruebas:

  | Módulo           | Historias      | Dueño (propuesta del README) |
  | ---------------- | -------------- | ---------------------------- |
  | `Comun`          | 000-core       | Todo el grupo, en `develop`  |
  | `Disponibilidad` | HU-01, HU-05   | Artur                        |
  | `Reservas`       | HU-02, HU-06   | Saul                         |
  | `Cancelaciones`  | HU-03, HU-07   | Persona C                    |
  | `CursosInformes` | HU-04, HU-08   | Persona D                    |

  El reparto definitivo lo fija `docs/agrupamiento.md`, que aún no existe. Los módulos solo se
  comunican mediante las interfaces públicas de [contracts/modulos.md](./contracts/modulos.md).
- **Motivo**: coincide con el reparto por pares de historias del README y con «un único dueño por
  módulo» (Acuerdos de colaboración). Cada persona toca solo sus carpetas y se reducen los conflictos
  de merge.
- **Alternativas descartadas**: un proyecto por módulo (cuatro proyectos más y referencias cruzadas
  difíciles); organizar por capa técnica (Controllers/Services), que mezcla a los cuatro dueños en
  cada carpeta.

## R4. Quién escribe en la entidad Reserva (varios responsables)

- **Decisión**: las transiciones de estado son métodos de la entidad de dominio
  (`Reserva.Confirmar`, `Reserva.Caducar`, `Reserva.Cancelar`), que validan RF-015 y lanzan un error
  de negocio si no se cumple. `Reserva` y `Pago` pertenecen al módulo `Reservas` (HU-02). HU-03 y
  HU-07 no escriben en la tabla directamente: llaman a `IServicioReservas.Cancelar(...)` y a
  `IServicioReservas.CaducarPorCierre(...)`, que expone el módulo `Reservas`. `Devolucion` pertenece a
  `Cancelaciones`.
- **Motivo**: cumple a la vez la tabla «Responsables de los cambios de estado» de la spec (HU-03/HU-07
  deciden cuándo cancelar) y la regla de la constitución de que solo el dueño escribe en sus tablas.
  La máquina de estados existe en un único sitio.
- **Alternativas descartadas**: que cada módulo actualice la tabla por su cuenta (duplica la máquina
  de estados); eventos de dominio con bus (complejidad innecesaria para un monolito).

## R5. Fechas, horas y zona horaria

- **Decisión**:
  - Todas las marcas de tiempo son `DateTime` en UTC, guardadas como `timestamp with time zone` en
    PostgreSQL (comportamiento por defecto de Npgsql 6+, que rechaza `DateTime` no UTC).
  - La hora actual se obtiene SIEMPRE de `TimeProvider` inyectado (`TimeProvider.System` en
    producción, `FakeTimeProvider` de `Microsoft.Extensions.TimeProvider.Testing` en las pruebas).
  - La conversión se centraliza en `HorarioMadrid` (en `Dominio/Comun`), que usa
    `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")`. El identificador IANA funciona en
    Windows (ICU, .NET 6+) y en Linux.
  - Las horas «de pared» (horario de apertura, franjas fijas, horario de cursos) se guardan como
    `time` local de Madrid (`TimeOnly`), y las fechas de calendario como `date` (`DateOnly`).
  - Cómo se evalúa cada regla:
    - **7 días**: por fecha local de Madrid. La franja es reservable si
      `hoyMadrid ≤ fechaFranja ≤ hoyMadrid + 7`.
    - **24 h**: por tiempo real transcurrido. Hay devolución si `inicioUtc − ahoraUtc > 24 h`.
    - **15 min (QR)**: entre instantes. Es válido si
      `inicioUtc − 15 min ≤ ahoraUtc ≤ finUtc`.
    - **10 min (bloqueo)**: la reserva sigue vigente si `ahoraUtc < creadaEnUtc + 10 min`.
  - Las franjas se calculan a partir de una fecha local y las horas 08…21, y se convierten a UTC.
    El cambio de hora en España ocurre a las 02:00/03:00, fuera del horario de apertura, así que
    todas las franjas existen y duran 1 hora real.
- **Motivo**: principio IX y RF-026. Comparar instantes UTC hace que «más de 24 h» signifique
  24 horas reales también el fin de semana del cambio de hora (por ejemplo: 25 h de pared pueden ser
  24 h reales). Es la interpretación que protege al usuario y la que se prueba en CE-006.
- **Alternativas descartadas**: guardar hora local sin zona (ambigua en el cambio de hora);
  `DateTimeOffset` en todas partes (Npgsql lo guarda igualmente en UTC y complica las comparaciones);
  NodaTime (dependencia extra; `TimeProvider` + `TimeZoneInfo` basta).

## R6. Valores de negocio en un único sitio

- **Decisión**: la clase estática `ValoresNegocio` (`Dominio/Comun/ValoresNegocio.cs`) contiene, como
  constantes con nombre, todas las cifras de la tabla «Valores de negocio» de la spec salvo los precios:
  `DuracionFranja`, `HoraApertura` (08:00), `HoraCierre` (22:00), `AntelacionMaximaDias` (7),
  `TiempoBloqueoPago` (10 min), `PlazoDevolucion` (24 h), `MargenQrAntesDelInicio` (15 min),
  `MesInicioTemporada` (9), `MesFinTemporada` (6) y `ZonaHoraria` ("Europe/Madrid").
  - Los **precios** viven solo en la tabla `tarifas`, precargada por el inicializador. La entidad
    Tarifa de la spec es un dato, no una constante.
  - El **horario de apertura del centro** no se guarda en la tabla `centros`: los cuatro centros
    tienen el mismo horario y se lee de `ValoresNegocio`. El atributo «horario de apertura» de la
    entidad Centro se expone en la API como dato derivado de esa constante.
- **Motivo**: principio III («constantes con nombre, nunca literales repetidos») y CE-002 (cada cifra
  se escribe una sola vez). Guardar el horario en cada fila de `centros` duplicaría la cifra.
- **Alternativas descartadas**: `appsettings.json` (una cifra de negocio no debe poder cambiarse por
  entorno sin pasar por la spec); horario por centro en la base de datos (duplicaría el valor).

## R7. Una sola reserva activa por instalación y franja

- **Decisión**: índice único parcial en PostgreSQL:
  `CREATE UNIQUE INDEX ux_reservas_activa ON reservas (instalacion_id, inicio_utc) WHERE estado IN ('pendiente_pago','confirmada');`.
  Si al insertar se viola ese índice (SQLSTATE `23505`), la API responde con 409 y el código
  `FRANJA_OCUPADA`.
- **Motivo**: principio III (la restricción de concurrencia DEBE estar también en la base de datos) y
  RF-014. Como las franjas son siempre de 1 hora en punto, `(instalacion_id, inicio_utc)` identifica
  la franja sin solapamientos parciales posibles.
- **Alternativas descartadas**: restricción de exclusión con `tstzrange` y `btree_gist` (útil para
  rangos arbitrarios, innecesaria con franjas fijas de 1 h); bloqueo pesimista en código (no
  protege ante inserciones fuera de la lógica).

## R8. Reservas pendientes de pago que caducan por tiempo

- **Decisión**: la corrección no depende de ningún proceso en segundo plano.
  1. **Regla de vigencia única** (`Reserva.EstaVigente(ahora)`): una reserva pendiente de pago está
     vigente solo si `ahora < creadaEnUtc + 10 min`. La disponibilidad (HU-01) trata como libre una
     franja cuya única reserva es una pendiente no vigente.
  2. **Caducidad perezosa al reservar**: dentro de la misma transacción del `INSERT`, el módulo
     `Reservas` marca antes como `caducada` (y su pago como `fallido`) cualquier reserva pendiente no
     vigente de esa instalación y franja. Así se libera el índice único y se inserta la nueva.
  3. **Caducidad al pagar**: si se intenta pagar con la reserva ya no vigente (el caso de exactamente
     10 minutos incluido), se caduca y se responde 409 `RESERVA_CADUCADA`.
  4. **Barrido periódico**: `BarridoCaducidades` (`BackgroundService`, cada 60 s) persiste el estado
     `caducada` y el pago `fallido` del resto de reservas no vigentes, para que «Mis reservas» y el
     informe vean el estado real. Usa el mismo método `Reserva.Caducar`.
- **Motivo**: con los puntos 1–3, el resultado es correcto y determinista en las pruebas, que usan
  `FakeTimeProvider` sin esperar al barrido (principio II). El barrido solo mantiene ordenados los
  datos guardados. Cumple las aclaraciones de la sesión 2026-10-28 (caducidad ⇒ pago fallido, sin
  reintento).
- **Alternativas descartadas**: depender solo del barrido (la franja quedaría bloqueada hasta 60 s de
  más); `pg_cron` (extensión no estándar en la imagen de Docker); fijar una columna «expira_en» y
  filtrar el índice por ella (un índice parcial no puede depender de `now()`).

## R9. Pasarela de pago simulada

- **Decisión**: la interfaz `IPasarelaPago` en `Dominio/Reservas`, con dos operaciones:
  `Cobrar(importe, datosTarjetaFicticia)` → aceptado o rechazado, y `Devolver(pago)` → siempre
  completada. La implementación `PasarelaSimulada` está en `Api/Infraestructura`. Regla
  determinista: una tarjeta que termina en `0002` se rechaza y cualquier otra con 16 dígitos se
  acepta. **No se guarda ningún dato de tarjeta**, ni siquiera los últimos dígitos.
- **Motivo**: RF-021, y permite mostrar en la demo los dos caminos (confirmada / caducada) sin
  parámetros «de prueba» en la API. El patrón es el habitual de las tarjetas de prueba de las
  pasarelas reales.
- **Alternativas descartadas**: un campo `resultadoSimulado` en la petición (lógica de prueba en el
  contrato público); resultado aleatorio (no determinista, rompe el principio II).

## R10. Identificación, sesión y permisos

- **Decisión**:
  - **Hash**: `PasswordHasher<Usuario>` de `Microsoft.Extensions.Identity.Core` (PBKDF2-HMAC-SHA512,
    100 000 iteraciones, sal aleatoria por usuario), sin el resto de ASP.NET Core Identity.
  - **Sesión**: autenticación por cookie (`HttpOnly`, `Secure`, `SameSite=Strict`, caducidad
    deslizante de 8 h). La API sirve también los ficheros del cliente Blazor WASM
    (`UseBlazorFrameworkFiles` + `MapFallbackToFile`), así que todo comparte origen: no hace falta CORS
    ni guardar tokens en el navegador. Las respuestas 401/403 son Problem Details, no redirecciones.
  - **Claims**: `sub` (id), `role`, `tipo_tarifa` y `centro_id` (solo personal) y `club_id` (solo
    representantes). Se calculan una vez al iniciar sesión con `CalculadoraTarifa` (RF-007).
  - **Autorización**: políticas por rol (`EsCiudadanoORepresentante`, `EsRepresentante`,
    `EsPersonal`, `EsTecnico`). La comprobación de centro (D7) la hace el caso de uso con
    `PermisosCentro.ExigirMismoCentro(personal, instalacion)` y, si falla, devuelve 403 `OTRO_CENTRO`.
  - **Login fallido**: siempre el mismo 401 `CREDENCIALES_INVALIDAS` con el texto «Correo o
    contraseña incorrectos.», exista o no el correo. Si el correo no existe, se verifica un hash
    ficticio para igualar el tiempo de respuesta.
- **Motivo**: RF-004 a RF-009 y la HU-3, escenario 5. Usar cookie con el mismo origen es lo más simple
  y seguro para una SPA servida por su propia API.
- **Alternativas descartadas**: ASP.NET Core Identity completo (registro, tablas y UI que la spec
  excluye); JWT en `localStorage` (expuesto a XSS, y hay que gestionar la renovación); cliente y API en
  orígenes distintos (exige CORS y cookies `SameSite=None`).

## R11. Errores con Problem Details en español

- **Decisión**:
  - El dominio lanza `ErrorNegocio(codigo, mensaje)` sin conocer HTTP. El catálogo de códigos está en
    `Dominio/Comun/CodigosError.cs`.
  - En `SI18.Api`, un `IExceptionHandler` global traduce el código a su estado HTTP según la tabla de
    [contracts/errores.md](./contracts/errores.md) y escribe un `ProblemDetails` (RFC 9457) con
    `title` y `detail` en español y las extensiones `codigo` y `traceId`.
  - Cualquier otra excepción se convierte en 500 `ERROR_INTERNO` con un mensaje genérico, sin trazas.
  - Los errores de validación del formato de la petición se devuelven como 400 `PETICION_INVALIDA`
    con `errors` por campo.
- **Motivo**: principio IV y RF-028. El cliente Blazor muestra `detail` tal cual y decide por
  `codigo`, nunca por el texto.
- **Alternativas descartadas**: devolver `Result<T>` hasta el endpoint (válido, pero más código
  repetido en cada uno de los ocho módulos); un formato propio de error (incumple el principio IV).

## R12. Persistencia y migraciones

- **Decisión**:
  - Un único `Si18DbContext` (`Api/Infraestructura`), con un fichero
    `IEntityTypeConfiguration<T>` por entidad dentro de la carpeta del módulo dueño.
  - Nombres en `snake_case` con `EFCore.NamingConventions`. Los enumerados se guardan como texto con
    una restricción `CHECK`.
  - Los importes son `numeric(8,2)` ↔ `decimal`.
  - La migración inicial `0001_NucleoComun` crea TODO el modelo compartido de 000-core, así que las
    specs 001–008 no deberían necesitar migraciones propias. Si alguna la necesita, se rebasa sobre
    `develop` y se regenera antes de fusionar, para no chocar en `ModelSnapshot`.
- **Motivo**: la constitución exige migraciones versionadas. Con un único contexto basta para un
  monolito, y crear el esquema completo en 000-core evita cuatro ramas peleando por el mismo snapshot.
- **Alternativas descartadas**: un `DbContext` por módulo (transacciones entre módulos, como el
  cierre que cancela y devuelve, más difíciles); `EnsureCreated` (no versiona el esquema).

## R13. Datos de inicialización ficticios

- **Decisión**:
  - `InicializadorDatos` es idempotente: solo inserta si la tabla `centros` está vacía. Se ejecuta al
    arrancar cuando `SI18_SEMBRAR_DATOS=true`, después de aplicar las migraciones.
  - La contraseña común de los usuarios de demo se lee de la variable `SI18_CLAVE_DEMO` y nunca está
    en el repositorio (principio V). Si falta y hay que sembrar, el arranque falla con un mensaje
    claro.
  - Los correos usan el dominio reservado `.test` (por ejemplo, `ciudadana.empadronada@si18.test`).
  - El contenido detallado está en [data-model.md § Datos de inicialización](./data-model.md#datos-de-inicialización).
  - El curso «iniciación al tenis» del README **no se siembra**. Según D2, los cursos usan salas y
    piscinas; un curso en una pista de tenis bloquearía franjas, y ese caso no lo contempla RF-011. Si
    el grupo lo quiere, debe proponerlo antes en 000-core (RF-003).
- **Motivo**: principio VI y RF-004. La idempotencia permite reiniciar la API sin duplicar datos.
- **Alternativas descartadas**: `HasData` de EF (no permite calcular hashes con sal en tiempo de
  ejecución y ensucia las migraciones); scripts SQL sueltos (no versionados con el modelo).

## R14. Código de acceso (QR)

- **Decisión**: el código son 16 bytes de `RandomNumberGenerator` codificados en Base64url
  (22 caracteres), con índice único en `reservas.codigo_acceso`. El cliente dibuja el QR en SVG con
  **QRCoder** (funciona en WebAssembly). La validación recibe el texto del código. Cómo lo lee el
  personal (cámara o teclado) lo decide la spec-006.
- **Motivo**: RF-017 (opaco, aleatorio, no deducible). Con 128 bits no se puede adivinar ni enumerar.
- **Alternativas descartadas**: `Guid.NewGuid()` (no está garantizado como valor criptográfico);
  un QR firmado con HMAC (exige gestionar claves sin necesidad, porque el servidor ya valida contra la
  base de datos).

## R15. Pruebas

- **Decisión**:
  - **Framework**: xUnit v3 (`xunit.v3`).
  - **`SI18.Dominio.Tests`**: pruebas unitarias puras con `FakeTimeProvider`. Cada límite tiene
    pruebas a los dos lados (CE-006), incluido el cambio de horario de verano del 25/10/2026 y del
    28/03/2027.
  - **`SI18.Api.Tests`**: pruebas de integración con `WebApplicationFactory<Program>` contra un
    PostgreSQL real levantado con **Testcontainers** (`Testcontainers.PostgreSql`, imagen
    `postgres:17-alpine`, la misma de docker-compose). Comprueban el índice único con peticiones
    concurrentes, Problem Details, permisos y migraciones.
  - Si `SI18_TEST_CONNECTION` está definida, se usa esa base de datos en lugar del contenedor (por
    ejemplo, la de docker-compose).
- **Motivo**: principio II. El índice parcial y `23505` solo se pueden probar contra PostgreSQL de
  verdad; SQLite o el proveedor InMemory no los reproducen.
- **Alternativas descartadas**: el proveedor InMemory de EF (no aplica índices ni transacciones);
  una base de datos compartida sin aislamiento (pruebas no deterministas).

## R16. Accesibilidad del cliente

- **Decisión**:
  - Se usan los componentes de MudBlazor con etiqueta visible (`Label`) o `aria-label`, y
    `<html lang="es">`.
  - Los mensajes de resultado se muestran con `MudAlert`, con texto e icono (nunca solo color), dentro
    de una región `role="status"` / `aria-live="polite"` común (`AvisoAccesible.razor`, módulo
    `Comun`).
  - Hay un salto «Ir al contenido» en el layout. El tema de MudBlazor se ajusta a contraste AA.
  - **Comprobación**: lista de verificación de teclado y foco más Lighthouse/axe DevTools en cada PR
    de interfaz (ver [quickstart.md](./quickstart.md)).
- **Motivo**: principio I, con el mínimo de piezas.
- **Alternativas descartadas**: pruebas E2E automáticas con Playwright + axe (útiles, pero ninguna
  spec las pide; se pueden proponer más adelante).

## R17. Entorno local y secretos

- **Decisión**:
  - `fuentes/docker-compose.yml` levanta solo PostgreSQL (`postgres:17-alpine`, volumen nombrado,
    `healthcheck`). La API y el cliente se ejecutan con `dotnet run`.
  - `fuentes/.env.example` lleva valores de relleno; `.env` está en `.gitignore`.
  - La API lee `ConnectionStrings__Si18`, `SI18_CLAVE_DEMO` y `SI18_SEMBRAR_DATOS` de variables de
    entorno o de `dotnet user-secrets`.
- **Motivo**: principio V y la petición del grupo. Contenerizar la API no lo pide ninguna spec
  (principio VIII).
- **Alternativas descartadas**: cadenas de conexión en `appsettings.Development.json` (secreto en el
  repositorio); .NET Aspire (otra pieza que aprender sin necesidad).

## R18. Notificaciones («Mis avisos»)

- **Decisión**: `IServicioAvisos.Avisar(usuarioId, texto)` en el módulo `Comun`. La tabla
  `notificaciones` es de `Comun`, y HU-04, HU-05 y HU-07 solo la usan a través de esa interfaz y dentro
  de su propia transacción, para que el aviso y el cambio de estado se guarden juntos.
- **Motivo**: RF-024 y D6. Al compartir la transacción, nunca se cancela una reserva sin avisar al
  titular.
- **Alternativas descartadas**: correo electrónico (excluido por D6); notificaciones en tiempo real
  con SignalR (no las pide ninguna spec).

## R19. Concurrencia en las plazas de los cursos (pauta para la spec-004)

- **Decisión**: la inscripción bloquea la fila del curso con `SELECT … FOR UPDATE` dentro de la
  transacción antes de contar las plazas confirmadas.
- **Motivo**: «nunca se superan las plazas máximas» (README, HU4) es una restricción de concurrencia
  y, según el principio III, se garantiza en la base de datos.
- **Alternativas descartadas**: un contador con control optimista (más reintentos y más código).
