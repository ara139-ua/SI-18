---

description: "Lista de tareas de 000-core · Núcleo común de SI-18"
---

# Tareas: 000-core · Núcleo común de SI-18

**Entrada**: documentos de diseño de `specs/000-core/` ([plan.md](./plan.md), [spec.md](./spec.md),
[research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/README.md),
[quickstart.md](./quickstart.md)).

**Pruebas**: incluidas. Son obligatorias por la constitución (principio II: pruebas a los dos lados de
cada límite) y por CE-006.

**Organización**: por historia de usuario de la spec (US1–US4 = HU-C1…HU-C4). Cada tarea lleva su
**responsable**: `[Saul]`, `[Artur]` o `[Ambos]`. En las de `[Ambos]` se indica quién la coge por
defecto (`sugerido: …`) para no duplicar trabajo.

## Formato: `- [ ] [ID] [P?] [Historia?] [Responsable] Descripción con ruta`

- **[P]**: puede hacerse en paralelo (ficheros distintos y sin dependencias pendientes).
- **[USn]**: historia de usuario de la spec a la que pertenece.
- Todas las rutas de código parten de `fuentes/`, como pide la entrega (ver plan.md).

## Correspondencia con el reparto acordado

| Reparto acordado                               | Fases de este fichero                                                                 | Ramas sugeridas |
| ---------------------------------------------- | ------------------------------------------------------------------------------------- | --------------- |
| **Fase 0**: esqueleto [Saul], bloquea al resto | Fase 1 (Setup)                                                                        | `feature/000-core-esqueleto` |
| **Fase 1**: en paralelo                        | Fase 2 (Fundacional) + las tareas `[Saul]` y `[Artur]` de las fases 3–6               | Saul: `feature/000-core-dominio`, `feature/000-core-persistencia` · Artur: `feature/000-core-errores-cliente` |
| **Fase 2**: integración [Ambos]                | Las tareas `[Ambos]` de las fases 3–6 + Fase 7 (Pulido)                               | `feature/000-core-sesion`, `feature/000-core-avisos` |

**Reglas de colaboración**:
- Cada rama corta va a `develop` por un PR que aprueba el otro (constitución, «Acuerdos de
  colaboración»).
- **Solo Saul genera migraciones.**
- Cada uno registra sus servicios en su propio fichero de extensión (`RegistroInfraestructura.cs`
  para Saul, `RegistroErrores.cs` para Artur, `RegistroComun.cs` para Ambos). `Program.cs` no se toca
  después de T009.
- **Accesibilidad en cada PR** (principio I): todo PR que incluya ficheros de `SI18.Cliente` anota en
  su descripción la comprobación de quickstart.md §6 (teclado, foco, anuncio de mensajes y
  Lighthouse/axe). T073 es la revisión final conjunta, no la única.

---

## Fase 1: Setup (esqueleto · Fase 0 del reparto)

**Propósito**: crear la solución y el entorno local. Bloquea todo lo demás salvo T011.

- [ ] T001 [Saul] Crear la solución `fuentes/SI18.slnx` con seis proyectos:
  - `src/SI18.Dominio`: classlib, **sin** referencias a ASP.NET Core ni a EF Core.
  - `src/SI18.Contratos`: classlib, sin referencias.
  - `src/SI18.Api`: proyecto `web`; referencia a Dominio, Contratos y Cliente (la API sirve el
    cliente).
  - `src/SI18.Cliente`: `blazorwasm`; referencia a Contratos.
  - `tests/SI18.Dominio.Tests`: xUnit v3; referencia a Dominio.
  - `tests/SI18.Api.Tests`: xUnit v3; referencia a Api.

  En `Dominio`, `Api`, `Cliente` y en los dos proyectos de pruebas, crear las carpetas de módulo
  `Comun/`, `Disponibilidad/`, `Reservas/`, `Cancelaciones/` y `CursosInformes/` (en `Api`, dentro de
  `Modulos/`) con un `.gitkeep`.
- [ ] T002 [P] [Saul] Crear `fuentes/global.json` con `"sdk": { "version": "10.0.100", "rollForward": "latestFeature" }`.
- [ ] T003 [P] [Saul] Crear `fuentes/Directory.Build.props`:
  - `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable` y `TreatWarningsAsErrors true`;
  - para los proyectos de `src/` (condición sobre `$(MSBuildProjectDirectory)`), referencia a
    `Microsoft.CodeAnalysis.BannedApiAnalyzers` y `<AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />`.
- [ ] T004 [P] [Saul] Crear `fuentes/Directory.Packages.props` con `ManagePackageVersionsCentrally=true`
  y **todos** los paquetes del plan, en la última versión estable compatible con net10.0:
  - `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer`,
    `Microsoft.AspNetCore.Components.WebAssembly.Server`;
  - `MudBlazor` (8.x), `QRCoder`;
  - `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL` (10.x),
    `EFCore.NamingConventions`;
  - `Microsoft.Extensions.Identity.Core`, `Microsoft.AspNetCore.OpenApi`,
    `Microsoft.CodeAnalysis.BannedApiAnalyzers`;
  - `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`;
  - `Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.AspNetCore.Mvc.Testing`,
    `Testcontainers.PostgreSql`.

  Añadir a cada `.csproj` sus `PackageReference` sin versión.
- [ ] T005 [P] [Saul] Crear `fuentes/BannedSymbols.txt` que prohíba `P:System.DateTime.Now`,
  `P:System.DateTime.UtcNow`, `P:System.DateTime.Today`, `P:System.DateTimeOffset.Now` y
  `P:System.DateTimeOffset.UtcNow`, con el mensaje «Usa TimeProvider inyectado (constitución, principio II)».
- [ ] T006 [P] [Saul] Crear `fuentes/.config/dotnet-tools.json` con `dotnet-ef` 10.x.
- [ ] T007 [P] [Saul] Crear `fuentes/docker-compose.yml` con un único servicio `postgres`:
  - imagen `postgres:17-alpine`;
  - `POSTGRES_USER`, `POSTGRES_PASSWORD` y `POSTGRES_DB` leídos de `.env`;
  - puerto `${POSTGRES_PORT:-5432}:5432`;
  - volumen nombrado `si18-datos`;
  - `healthcheck` con `pg_isready -U $${POSTGRES_USER}`.
- [ ] T008 [P] [Saul] Crear `fuentes/.env.example` y `fuentes/.gitignore`:
  - `.env.example`, con valores de relleno y nunca reales (principio V): `POSTGRES_USER=si18`,
    `POSTGRES_PASSWORD=<cambia-esto>`, `POSTGRES_DB=si18`, `POSTGRES_PORT=5432`,
    `ConnectionStrings__Si18=Host=localhost;Port=5432;Database=si18;Username=si18;Password=<cambia-esto>`,
    `SI18_CLAVE_DEMO=<cambia-esto>`, `SI18_SEMBRAR_DATOS=true` y `SI18_TEST_CONNECTION=`;
  - `.gitignore`: `bin/`, `obj/`, `.vs/`, `*.user` y `.env`.
- [ ] T009 [Saul] Crear `fuentes/src/SI18.Api/Program.cs` limitado a la composición:
  - llamadas a `builder.Services.AddInfraestructura(builder.Configuration)`, `AddErrores()` y `AddComun()`;
  - después, `app.UseErrores()`, `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`,
    `app.UseAuthentication()`, `app.UseAuthorization()`, `app.MapComun()` y
    `app.MapFallbackToFile("index.html")`;
  - al final, `public partial class Program;`.

  Crear también los tres ficheros de extensión con métodos vacíos:
  - `Infraestructura/RegistroInfraestructura.cs` (dueño: Saul);
  - `Infraestructura/RegistroErrores.cs` (dueño: Artur);
  - `Modulos/Comun/RegistroComun.cs` (dueño: Ambos).
- [ ] T010 [Saul] Comprobar que `dotnet build SI18.slnx` compila sin advertencias y que
  `docker compose up -d` deja `postgres` en `healthy`. Abrir el PR de `feature/000-core-esqueleto` a
  `develop`; lo aprueba Artur.
- [ ] T011 [P] [Artur] Crear `docs/agrupamiento.md` (en la raíz del repositorio) con el reparto
  definitivo de las historias y los dueños de módulo de la tabla «Dueños de módulo» de plan.md
  (Comun: todo el grupo; Disponibilidad HU-01 y HU-05: Artur; Reservas HU-02 y HU-06: Saul;
  Cancelaciones HU-03 y HU-07: persona C; CursosInformes HU-04 y HU-08: persona D), con los nombres
  reales de C y D. No toca `fuentes/`, así que se hace mientras Saul termina T001–T010.

**Punto de control**: con el esqueleto en `develop`, Artur hace `pull` y empiezan las dos líneas en
paralelo.

---

## Fase 2: Fundacional (tipos base y errores · inicio de la Fase 1 del reparto)

**Propósito**: los tipos que usan todas las historias. Saul y Artur trabajan aquí a la vez sobre
ficheros distintos.

**⚠️ CRÍTICO**: ninguna historia puede cerrarse sin esta fase.

### Línea de Saul

- [ ] T012 [P] [Saul] Crear los enumerados en `fuentes/src/SI18.Dominio/Comun/Enumeraciones.cs`, con
  los valores exactos de data-model.md:
  - `Rol {Ciudadano, RepresentanteClub, PersonalCentro, TecnicoConcejalia}`;
  - `TipoTarifa {Empadronado, NoEmpadronado, Club}`;
  - `TipoInstalacion {PistaTenis, PistaPadel, Pabellon, Sala, Piscina}`;
  - `EstadoReserva {PendientePago, Confirmada, Caducada, Cancelada}`;
  - `MotivoCancelacion {Usuario, CierreMantenimiento}`;
  - `EstadoPago {Pendiente, Completado, Fallido}`;
  - `MotivoDevolucion {CancelacionUsuario, CierreMantenimiento}`;
  - `EstadoSolicitud {Pendiente, Aprobada, Rechazada}`;
  - `EstadoInscripcion {Confirmada, ListaEspera, Baja}`.
- [ ] T013 [P] [Saul] Crear `fuentes/src/SI18.Dominio/Comun/ValoresNegocio.cs` (R6), una clase
  estática con constantes con nombre:
  - `DuracionFranja = TimeSpan.FromHours(1)`;
  - `HoraApertura = new TimeOnly(8,0)` y `HoraCierre = new TimeOnly(22,0)`;
  - `AntelacionMaximaDias = 7`;
  - `TiempoBloqueoPago = TimeSpan.FromMinutes(10)`;
  - `PlazoDevolucion = TimeSpan.FromHours(24)`;
  - `MargenQrAntesDelInicio = TimeSpan.FromMinutes(15)`;
  - `MesInicioTemporada = 9` y `MesFinTemporada = 6`;
  - `ZonaHoraria = "Europe/Madrid"`.

  Un comentario por constante con la fila de la tabla «Valores de negocio» de la spec. **No** incluye
  precios: están en la tabla `tarifas`.
- [ ] T014 [P] [Saul] Crear `fuentes/src/SI18.Dominio/Comun/ErrorNegocio.cs`:
  `sealed class ErrorNegocio(string codigo, string mensaje) : Exception`, con la propiedad `Codigo`.
- [ ] T015 [Saul] Crear `fuentes/src/SI18.Dominio/Comun/HorarioMadrid.cs` (R5), una clase estática
  con:
  - `DateOnly Hoy(DateTime ahoraUtc)`;
  - `DateTime AUtc(DateOnly fecha, TimeOnly hora)`, que devuelve `DateTimeKind.Utc`;
  - `(DateOnly Fecha, TimeOnly Hora) ALocal(DateTime utc)`.

  Usa `TimeZoneInfo.FindSystemTimeZoneById(ValoresNegocio.ZonaHoraria)`. Si recibe un `DateTime` que
  no es UTC, lanza `ArgumentException`. Depende de T013.
- [ ] T016 [Saul] Crear las pruebas `fuentes/tests/SI18.Dominio.Tests/Comun/HorarioMadridTests.cs`,
  con `[Trait("Categoria","Limites")]`:
  - 23:30 UTC del 31/12/2026 es el 01/01/2027 en Madrid;
  - `AUtc(25/10/2026, 08:00)` = 07:00 UTC y `AUtc(24/10/2026, 08:00)` = 06:00 UTC;
  - `AUtc(28/03/2027, 08:00)` = 06:00 UTC y `AUtc(27/03/2027, 08:00)` = 07:00 UTC;
  - ida y vuelta con `ALocal`;
  - un `DateTime` no UTC lanza una excepción.
- [ ] T017 [P] [Saul] Crear `fuentes/tests/SI18.Dominio.Tests/Comun/ArquitecturaTests.cs`: comprobar
  que el ensamblado `SI18.Dominio` no referencia ningún ensamblado `Microsoft.AspNetCore.*` ni
  `Microsoft.EntityFrameworkCore*` (plan, «Decisión de estructura»).

### Línea de Artur

- [ ] T018 [P] [Artur] Crear `fuentes/src/SI18.Dominio/Comun/CodigosError.cs` con:
  - una constante `string` por cada código de [contracts/errores.md](./contracts/errores.md), los
    22 de la tabla «Catálogo de códigos de 000-core»;
  - `Titulo(codigo)` y `Detalle(codigo)`, con los textos en español copiados literalmente de la
    columna `detail`;
  - `ErrorNegocio Error(string codigo)`, que crea la excepción con su texto.

  Ninguna otra parte del código escribe esos textos. Depende de T014.
- [ ] T019 [Artur] Crear `fuentes/src/SI18.Api/Infraestructura/ManejadorErrores.cs`:
  - un `IExceptionHandler` con la tabla código → estado HTTP de contracts/errores.md;
  - escribe `application/problem+json` con `type` = `https://si18.test/errores/{codigo-en-kebab-case}`,
    `title`, `status`, `detail` y las extensiones `codigo` y `traceId`;
  - cualquier excepción que no sea `ErrorNegocio` → 500 `ERROR_INTERNO` sin traza;
  - los errores de enlace o validación de la petición → 400 `PETICION_INVALIDA` con `errors`.

  Registrarlo en `fuentes/src/SI18.Api/Infraestructura/RegistroErrores.cs` (`AddErrores` →
  `AddProblemDetails` + `AddExceptionHandler<ManejadorErrores>`; `UseErrores` → `UseExceptionHandler`
  + `UseStatusCodePages`). Depende de T018.
- [ ] T020 [P] [Artur] Crear `fuentes/tests/SI18.Api.Tests/Comun/CatalogoErroresTests.cs` (sin base de
  datos): cada constante de `CodigosError` tiene un estado HTTP en la tabla de `ManejadorErrores` y un
  `Detalle` no vacío.
- [ ] T021 [P] [Artur] Crear los DTO comunes en `fuentes/src/SI18.Contratos/Comun/`, uno por fichero y
  según `components/schemas` de [contracts/api-comun.openapi.yaml](./contracts/api-comun.openapi.yaml):
  - `PeticionInicioSesion` (`correo` con «maxLength: 254», `contrasena` con «minLength: 1, maxLength: 200»);
  - `UsuarioActualDto`, `CentroDto`, `DeporteDto`, `InstalacionDto`, `AvisoDto` y `ProblemaDto`;
  - `Enumeraciones.cs`, con copias de `Rol`, `TipoTarifa` y `TipoInstalacion` para el cliente.

  Crear también `OpcionesJson.cs` con un `JsonSerializerOptions` compartido: camelCase y
  `JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)`.
- [ ] T022 [P] [Artur] Crear la base de pruebas de integración en
  `fuentes/tests/SI18.Api.Tests/Infraestructura/FabricaApi.cs`:
  - `WebApplicationFactory<Program>` + `IAsyncLifetime`;
  - arranca `PostgreSqlBuilder().WithImage("postgres:17-alpine")` de Testcontainers, salvo que esté
    definida `SI18_TEST_CONNECTION`;
  - inyecta `ConnectionStrings:Si18`, `SI18_CLAVE_DEMO=clave-de-prueba` y `SI18_SEMBRAR_DATOS=true`;
  - sustituye `TimeProvider` por un `FakeTimeProvider` expuesto como propiedad `Reloj`;
  - ofrece `CrearClienteConSesion(correo)`;
  - fija `ClientOptions.BaseAddress = new Uri("https://localhost")`, para que el `HttpClient` de prueba
    devuelva la cookie `Secure` `si18.sesion` (con el `http://localhost` por defecto no la enviaría).

  Crear también `ColeccionApi.cs` (`[CollectionDefinition]`). Las migraciones y la siembra las aplica
  el arranque (`InicializadorDatos`, T038).
- [ ] T023 [P] [Artur] Montar la base del cliente:
  - `fuentes/src/SI18.Cliente/wwwroot/index.html` con `<html lang="es">` y el título «SI-18 · Reservas
    deportivas»;
  - `fuentes/src/SI18.Cliente/Program.cs`, que registra MudBlazor, `HttpClient` con `BaseAddress` igual
    al origen y `ClienteApi`;
  - `fuentes/src/SI18.Cliente/Layout/Tema.cs`, un `MudTheme` con contraste AA.
- [ ] T024 [Artur] Crear `fuentes/src/SI18.Cliente/Layout/MainLayout.razor`:
  - enlace «Ir al contenido» como primer elemento enfocable;
  - `<main id="contenido" tabindex="-1">`;
  - `MudThemeProvider` con `Tema`;
  - encabezado con el nombre del servicio y un hueco para el menú por rol (se completa en T056).

  Depende de T023. Antes del PR: comprobar quickstart.md §6 sobre el layout y anotarlo en el PR.
- [ ] T025 [P] [Artur] Crear `fuentes/src/SI18.Cliente/Comun/MensajeAccesible.razor` (R16; se llama
  «Mensaje» para no confundirlo con las notificaciones de «Mis avisos»):
  - una región con `role="status"` y `aria-live="polite"` que siempre está en el DOM;
  - `MudAlert` con icono **y** texto (nunca solo color);
  - método público `Mostrar(string texto, Severity tipo)`.
- [ ] T026 [Artur] Crear `fuentes/src/SI18.Cliente/Comun/ClienteApi.cs`:
  - envuelve `HttpClient` usando `OpcionesJson`;
  - ante una respuesta no satisfactoria, lee `application/problem+json` en `ProblemaDto` y lanza
    `ErrorApi(Codigo, Detalle)`;
  - ante un 401, navega a `/iniciar-sesion`.

  Depende de T021 y T023.

**Punto de control**: base lista. Saul sigue con US1/US2 y Artur con su parte de US2–US4.

---

## Fase 3: Historia 1 · Un único modelo de datos para las cuatro personas (P1) 🎯 MVP

**Objetivo**: todas las entidades de «Entidades clave» existen una sola vez en código y en
PostgreSQL, con sus restricciones, y hay datos ficticios para trabajar.

**Prueba independiente**: sobre una base de datos vacía se aplica la migración. Existen las 17 tablas
(16 entidades persistidas más `instalacion_deportes`; la Franja no se almacena), con los índices y `CHECK` de data-model.md, la siembra
crea los datos indicados y ejecutarla dos veces no duplica nada.

### Entidades de dominio (Saul, en paralelo)

- [ ] T027 [P] [US1] [Saul] Crear las entidades de catálogo en `fuentes/src/SI18.Dominio/Comun/`
  (`Centro.cs`, `Deporte.cs`, `Instalacion.cs`, `Tarifa.cs`, `Club.cs`, `Temporada.cs`), con los
  campos de data-model.md:
  - **Centro**: `nombre` «varchar(100) obligatorio, único», `direccion` «varchar(200)». No tiene
    columnas de horario.
  - **Instalacion**: `nombre` «único por centro», `Tipo`, `ReservablePorFranjas`, `CentroId` y la
    colección de `Deporte`. El estado operativa/cerrada se calcula a partir de los cierres; no es una
    propiedad.
  - **Tarifa**: `PrecioHora` en `decimal`.
  - **Temporada**: `FechaInicio` y `FechaFin` (`DateOnly`).
- [ ] T028 [P] [US1] [Saul] Crear `fuentes/src/SI18.Dominio/Comun/Usuario.cs` y
  `fuentes/src/SI18.Dominio/Comun/Notificacion.cs`:
  - **Usuario**: `Correo` «se guarda en minúsculas», `HashContrasena` (nunca se expone), `Rol`,
    `Empadronado`, `ClubId?`, `CentroId?`;
  - **Notificacion**: `UsuarioId`, `Texto` «varchar(500)», `FechaUtc`, `Leida` «por defecto false» y
    método `MarcarLeida()`.
- [ ] T029 [P] [US1] [Saul] Crear `fuentes/src/SI18.Dominio/Disponibilidad/SolicitudFranjasFijas.cs`
  con los campos de data-model.md:
  - `DiasSemana` (`short[]`), con la regla «1 = lunes … 7 = domingo (ISO); no vacío; sin repetidos»;
  - `HoraInicio` y `HoraFin` «en punto», `≥ 08:00` y `≤ 22:00`, con `> hora_inicio`;
  - `Estado`, `CreadaEnUtc` y `ResueltaEnUtc?`.

  Métodos `Aprobar(ahora)` y `Rechazar(ahora)`: solo desde `Pendiente`; si no, lanzan
  `TRANSICION_NO_PERMITIDA`. La comprobación de conflictos al aprobar es de la spec-005.

  Incluye `fuentes/tests/SI18.Dominio.Tests/Disponibilidad/SolicitudFranjasFijasTests.cs`: `Aprobar` y
  `Rechazar` desde `Pendiente` funcionan y rellenan `ResueltaEnUtc`; desde `Aprobada` o `Rechazada`
  → `TRANSICION_NO_PERMITIDA` (principio II).
- [ ] T030 [P] [US1] [Saul] Crear `fuentes/src/SI18.Dominio/Reservas/Reserva.cs`, `Pago.cs` y
  `ValidacionAcceso.cs`, solo con los campos de data-model.md (los métodos de estado van en T060):
  - **Reserva**: `InicioUtc`, `FinUtc`, `TipoTarifa`, `Importe`, `Estado`, `CreadaEnUtc`,
    `CodigoAcceso?`, `CanceladaEnUtc?`, `MotivoCancelacion?` y la navegación `Pago`;
  - **Pago**: `Importe`, `Estado`, `CreadoEnUtc`, `ResueltoEnUtc?`;
  - **ValidacionAcceso**: `ReservaId?`, `CodigoLeido` «recortado a 64», `Aceptada`, `MotivoRechazo?`,
    `FechaUtc`, `PersonalId`.
- [ ] T031 [P] [US1] [Saul] Crear `fuentes/src/SI18.Dominio/Cancelaciones/Devolucion.cs` y
  `CierreMantenimiento.cs`:
  - **Devolucion**: `PagoId`, `Importe` «= pagos.importe (siempre íntegra)», `Motivo`, `FechaUtc`; sin
    estado;
  - **CierreMantenimiento**: `InstalacionId`, `InicioUtc` y `FinUtc` «en punto (hora de Madrid)»,
    `Motivo` «varchar(500) obligatorio», `RegistradoPorId`, `RegistradoEnUtc`, y el método
    `Afecta(inicioUtc, finUtc)` para el solape semiabierto `[inicio, fin)`.

  Incluye `fuentes/tests/SI18.Dominio.Tests/Cancelaciones/CierreMantenimientoTests.cs` con
  `[Trait("Categoria","Limites")]` (principio II). Para un cierre de 10:00 a 12:00:
  - la franja de 09:00 a 10:00 no se ve afectada;
  - las de 10:00 a 11:00 y de 11:00 a 12:00 sí;
  - la de 12:00 a 13:00 no.
- [ ] T032 [P] [US1] [Saul] Crear `fuentes/src/SI18.Dominio/CursosInformes/Curso.cs` e
  `Inscripcion.cs`:
  - **Curso**: `Trimestre` «varchar(30)», `FechaInicio`, `FechaFin`, `DiasSemana`, `HoraInicio`,
    `HoraFin` y `PlazasMaximas` «> 0». **Sin precio** (D10).
  - **Inscripcion**: `Estado` y `LlegadaEnUtc`.

### Persistencia (Saul; solo Saul genera migraciones)

- [ ] T033 [US1] [Saul] Crear `fuentes/src/SI18.Api/Infraestructura/Si18DbContext.cs`:
  - un `DbSet` por entidad;
  - `ApplyConfigurationsFromAssembly`;
  - convención de enumerados como texto `snake_case`, con un conversor genérico
    `Infraestructura/ConversorEnumSnakeCase.cs` cuyos valores coinciden con la tabla «Enumerados» de
    data-model.md;
  - `decimal` → `numeric(8,2)` por convención.

  Depende de T027–T032.
- [ ] T034 [P] [US1] [Saul] Crear las configuraciones EF de Comun en
  `fuentes/src/SI18.Api/Modulos/Comun/Configuraciones/`, una por entidad:
  - `centros` e `instalaciones` con `nombre` único (en instalaciones, por centro);
  - `instalacion_deportes` con PK compuesta;
  - `CHECK (reservable_por_franjas = (tipo IN ('pista_tenis','pista_padel')))`;
  - `tarifas` con `UNIQUE (tipo_instalacion, tipo_tarifa)` y `CHECK (precio_hora > 0)`;
  - `usuarios` con `correo` UNIQUE, `CHECK ((rol = 'representante_club') = (club_id IS NOT NULL))` y
    `CHECK ((rol = 'personal_centro') = (centro_id IS NOT NULL))`;
  - `temporadas` con `CHECK (fecha_fin > fecha_inicio)`;
  - `notificaciones` con el índice (`usuario_id`, `fecha_utc` DESC).
- [ ] T035 [P] [US1] [Saul] Crear las configuraciones EF de los módulos (desde ese momento, cada
  fichero pertenece al dueño de su módulo):
  - `Modulos/Disponibilidad/Configuraciones/SolicitudFranjasFijasConfig.cs`: `dias_semana smallint[]`;
    `CHECK` «obligatorio si y solo si estado ≠ 'pendiente'» para `resuelta_en_utc`;
  - `Modulos/Reservas/Configuraciones/ReservaConfig.cs`:
    - índice `ux_reservas_activa UNIQUE (instalacion_id, inicio_utc) WHERE estado IN ('pendiente_pago','confirmada')`;
    - `CHECK (fin_utc = inicio_utc + interval '1 hour')`;
    - `CHECK ((estado IN ('pendiente_pago','caducada')) = (codigo_acceso IS NULL))`;
    - `CHECK ((estado = 'cancelada') = (cancelada_en_utc IS NOT NULL))`;
    - `motivo_cancelacion` obligatorio si y solo si `estado = 'cancelada'`;
    - `codigo_acceso` UNIQUE;
    - índices (`usuario_id`, `inicio_utc`) y (`estado`, `creada_en_utc`);
  - `PagoConfig.cs`: `reserva_id` UNIQUE; `resuelto_en_utc` obligatorio si y solo si
    `estado ≠ 'pendiente'`;
  - `ValidacionAccesoConfig.cs`: `motivo_rechazo` obligatorio si y solo si `NOT aceptada`;
  - `Modulos/Cancelaciones/Configuraciones/`: `devoluciones.pago_id` UNIQUE; `cierres_mantenimiento`
    con `CHECK (fin_utc > inicio_utc)`;
  - `Modulos/CursosInformes/Configuraciones/`: `cursos` con `CHECK (plazas_maximas > 0)`,
    `fecha_fin > fecha_inicio` y `hora_fin > hora_inicio`; `inscripciones` solo con sus claves
    foráneas. El índice único por usuario y curso **no** se crea: lo propondrá la spec-004 (principio
    VIII).
- [ ] T036 [US1] [Saul] Rellenar `fuentes/src/SI18.Api/Infraestructura/RegistroInfraestructura.cs`:
  - `AddDbContext<Si18DbContext>` con `UseNpgsql(config.GetConnectionString("Si18"))` +
    `UseSnakeCaseNamingConvention()`;
  - `services.AddSingleton(TimeProvider.System)`;
  - registro de `InicializadorDatos` como `IHostedService`.

  Si falta la cadena de conexión, el arranque falla con el mensaje «Falta ConnectionStrings:Si18
  (ver quickstart.md §2)».
- [ ] T037 [US1] [Saul] Generar la migración inicial con `dotnet ef migrations add NucleoComun --project src/SI18.Api --output-dir Infraestructura/Migraciones`
  (es la `0001_NucleoComun` del plan). Revisar el código generado: deben estar el filtro del índice
  parcial, todos los `CHECK` de T034–T035 y los tipos `timestamptz`, `date`, `time`, `numeric(8,2)` y
  `smallint[]`. Depende de T033–T035.
- [ ] T038 [US1] [Saul] Crear `fuentes/src/SI18.Api/Infraestructura/InicializadorDatos.cs`
  (`IHostedService`, R13):
  - si `SI18_SEMBRAR_DATOS=true`, aplica `MigrateAsync()` y, si `centros` está vacía, inserta
    exactamente los datos de [data-model.md § Datos de inicialización](./data-model.md#datos-de-inicialización):
    - 4 centros y 7 deportes;
    - 26 instalaciones (por centro: 2 pistas de tenis, 2 de pádel, pabellón y sala; más una piscina
      en Norte y otra en Sur);
    - 6 tarifas (6,00 / 10,00 / 4,00 para tenis y pádel);
    - 2 clubes y la temporada «2026-2027»;
    - 10 usuarios `@si18.test`;
    - 3 cursos «Invierno 2027» (Yoga con 2 plazas);
  - calcula los hashes con `PasswordHasher<Usuario>` a partir de `SI18_CLAVE_DEMO`; si falta, lanza
    «Falta SI18_CLAVE_DEMO para sembrar los datos de demostración»;
  - registra «Datos de demostración sembrados» o «Los datos de demostración ya existen».

  Depende de T037.

### Pruebas de US1 (integración · Fase 2 del reparto)

- [ ] T039 [P] [US1] [Ambos · sugerido: Saul] Crear `fuentes/tests/SI18.Api.Tests/Comun/MigracionesTests.cs`
  con `FabricaApi`: consultando `information_schema` y `pg_indexes`, comprobar que existen las 17 tablas
  de data-model.md (sin contar `__EFMigrationsHistory`) y el índice `ux_reservas_activa` con su filtro.
- [ ] T040 [P] [US1] [Ambos · sugerido: Saul] Crear `fuentes/tests/SI18.Api.Tests/Comun/InicializadorDatosTests.cs`:
  - los recuentos son los de T038;
  - ejecutar el inicializador dos veces no cambia los recuentos;
  - `hash_contrasena` no contiene `clave-de-prueba` (quickstart V10).
- [ ] T041 [P] [US1] [Ambos · sugerido: Artur] Crear
  `fuentes/tests/SI18.Api.Tests/Comun/ReservaActivaUnicaTests.cs` con `[Trait("Categoria","Limites")]`:
  - 20 tareas simultáneas insertan una reserva `pendiente_pago` (y su pago) en la misma instalación e
    inicio, cada una con su propio `DbContext` → exactamente 1 éxito y 19 `DbUpdateException` con
    SQLSTATE `23505` sobre `ux_reservas_activa`;
  - una reserva `caducada` en la misma franja no impide una nueva (RF-014, quickstart V9).

**Punto de control**: el modelo compartido está en `develop`. Las specs 001–008 ya pueden referenciar
las tablas reales.

---

## Fase 4: Historia 2 · Valores de negocio definidos una sola vez (P1)

**Objetivo**: cada cifra de negocio y cada cálculo con límites existe en un único sitio y está probado
a los dos lados del límite.

**Prueba independiente**: `dotnet test --filter "Categoria=Limites"` en verde, con casos de
7 días, 10 min, 24 h y 15 min, incluido el cambio de hora (CE-006).

- [ ] T042 [P] [US2] [Artur] Crear `fuentes/src/SI18.Dominio/Disponibilidad/Franja.cs`
  (`sealed record Franja(int InstalacionId, DateOnly Fecha, TimeOnly HoraInicio, DateTime InicioUtc, DateTime FinUtc)`)
  y `CalculadoraFranjas.cs`: `FranjasDelDia(int instalacionId, DateOnly fecha)` devuelve las
  14 franjas de `ValoresNegocio.HoraApertura` a `HoraCierre − DuracionFranja`, convertidas con
  `HorarioMadrid.AUtc`.
- [ ] T043 [P] [US2] [Artur] Crear `fuentes/src/SI18.Dominio/Disponibilidad/CalculadoraPrecio.cs`:
  `decimal Precio(TipoInstalacion tipo, TipoTarifa tarifa, IReadOnlyList<Tarifa> tarifas)` =
  `precio_hora × 1 h`. Si no hay tarifa para ese tipo, lanza `INSTALACION_NO_RESERVABLE`.
- [ ] T044 [US2] [Artur] Crear `fuentes/src/SI18.Dominio/Disponibilidad/ReglaDisponibilidad.cs`, una
  función pura:

  ```
  string? MotivoNoDisponible(Franja franja, DateTime ahoraUtc, bool reservablePorFranjas,
      IReadOnlyList<CierreMantenimiento> cierres, IReadOnlyList<Franja> bloqueosClub,
      IReadOnlyList<DateTime> iniciosConReservaActiva)
  ```

  Devuelve el primer motivo que se cumple, en el orden de data-model.md § Franja:
  1. `FUERA_DE_ANTELACION` si la fecha no cumple `hoyMadrid ≤ fecha ≤ hoyMadrid + AntelacionMaximaDias`;
  2. `FRANJA_YA_EMPEZADA` si `ahoraUtc ≥ InicioUtc`;
  3. `INSTALACION_NO_RESERVABLE`;
  4. `INSTALACION_CERRADA`;
  5. `FRANJA_BLOQUEADA_CLUB`;
  6. `FRANJA_OCUPADA`.

  Incluye las pruebas de T042–T044 en `fuentes/tests/SI18.Dominio.Tests/Disponibilidad/`, todas con
  `[Trait("Categoria","Limites")]`:
  - `CalculadoraFranjasTests.cs`: 14 franjas el 25/10/2026 y el 28/03/2027; la primera empieza a las
    08:00 de Madrid; cada una dura 1 h real.
  - `CalculadoraPrecioTests.cs`: con las tarifas del D5 → 6,00 / 10,00 / 4,00.
  - `ReglaDisponibilidadTests.cs`:
    - el lunes 21/12/2026 a las 18:00 de Madrid, la franja de las 21:00 del 28/12 está disponible y
      cualquiera del 29/12 da `FUERA_DE_ANTELACION`;
    - hoy + 7 sí, hoy + 8 no;
    - empezada hace 1 minuto → `FRANJA_YA_EMPEZADA`;
    - el orden de los motivos se respeta.

  Depende de T042, T043 y T031 (`CierreMantenimiento`, de Saul).
- [ ] T045 [P] [US2] [Saul] Crear `fuentes/src/SI18.Dominio/Cancelaciones/PoliticaDevolucion.cs`:
  `bool HayDevolucion(MotivoDevolucion motivo, DateTime inicioUtc, DateTime ahoraUtc)` =
  `{ true, si motivo = CierreMantenimiento ; inicioUtc − ahoraUtc > ValoresNegocio.PlazoDevolucion, si motivo = CancelacionUsuario }`.
- [ ] T046 [P] [US2] [Saul] Crear `fuentes/src/SI18.Dominio/Reservas/ReglaAcceso.cs`:
  `string? MotivoRechazo(Reserva? r, DateTime ahoraUtc)`:
  - `QR_DESCONOCIDO` si `r` es null;
  - `QR_RESERVA_NO_CONFIRMADA` si `Estado ≠ Confirmada`;
  - `QR_FUERA_DE_VENTANA` si no se cumple `InicioUtc − MargenQrAntesDelInicio ≤ ahoraUtc ≤ FinUtc`;
  - null si es válido.

  `QR_OTRO_CENTRO` lo comprueba la spec-006.
- [ ] T047 [US2] [Saul] Crear las pruebas de límites de T045 y T046 en `fuentes/tests/SI18.Dominio.Tests/`,
  con `[Trait("Categoria","Limites")]`:
  - `Cancelaciones/PoliticaDevolucionTests.cs`:
    - 24 h + 1 s → sí; 24 h exactas → no;
    - con el cambio de hora (ahora el 27/03/2027 a las 10:00 de Madrid = 09:00 UTC; inicio el
      28/03/2027 a las 11:00 de Madrid = 09:00 UTC): 25 h de pared pero 24 h reales → no;
    - cierre → siempre sí.
  - `Reservas/ReglaAccesoTests.cs`:
    - inicio − 15 min → válido; inicio − 15 min − 1 s → `QR_FUERA_DE_VENTANA`;
    - fin → válido; fin + 1 s → rechazado;
    - cancelada → `QR_RESERVA_NO_CONFIRMADA`;
    - null → `QR_DESCONOCIDO`.

  Depende de T045 y T046.
- [ ] T048 [P] [US2] [Artur] Crear `fuentes/src/SI18.Contratos/Comun/FormatoEs.cs` (RF-027, principio
  IX). Va en Contratos para que el cliente lo use y se pueda probar:
  - `Importe(decimal)` → `ToString("C2", new CultureInfo("es-ES"))`, que da «6,00 €»;
  - `FechaHora(DateTime utc)` → hora de Madrid con `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")`,
    en formato `dd/MM/yyyy HH:mm`.

  Incluye `fuentes/tests/SI18.Api.Tests/Comun/FormatoEsTests.cs`:
  - `6m` → «6,00 €» (con espacio duro antes del €) y `10m` → «10,00 €»;
  - 25/10/2026 a las 07:00 UTC → «25/10/2026 08:00»;
  - 24/10/2026 a las 06:00 UTC → «24/10/2026 08:00».

  Verificar que el proyecto Cliente no desactiva `BlazorEnableTimeZoneSupport`.

**Punto de control**: las reglas puras de las specs 001, 002, 003 y 006 ya están disponibles y
probadas.

---

## Fase 5: Historia 3 · Identidad, rol, tarifa y centro de trabajo comunes (P1)

**Objetivo**: iniciar sesión con correo y contraseña. El sistema conoce el rol, la tarifa y el centro;
rechaza las acciones fuera del rol o del centro, explicando el motivo.

**Prueba independiente**: quickstart V1–V6 contra la API. También un endpoint exclusivo de un rol,
llamado con otro rol, devuelve 403 `ROL_NO_PERMITIDO`.

- [ ] T049 [P] [US3] [Saul] Crear `fuentes/src/SI18.Dominio/Comun/CalculadoraTarifa.cs` (RF-007):
  `TipoTarifa? Para(Rol rol, bool empadronado)` =
  `{ Club, si rol = RepresentanteClub ; Empadronado, si rol = Ciudadano ∧ empadronado ; NoEmpadronado, si rol = Ciudadano ∧ ¬empadronado ; null, en otro caso }`.
  El `null` corresponde a personal y técnico, igual que `UsuarioActual.tipoTarifa` en el OpenAPI.
  Ajustar también la firma en `contracts/modulos.md` (cambio de PARCHE en 000-core).
- [ ] T050 [P] [US3] [Saul] Crear `fuentes/src/SI18.Dominio/Comun/PermisosCentro.cs`:
  `void ExigirMismoCentro(int? centroPersonal, int centroInstalacion)` lanza
  `CodigosError.Error(OTRO_CENTRO)` si son distintos o si `centroPersonal` es null.
- [ ] T051 [P] [US3] [Saul] Crear `fuentes/tests/SI18.Dominio.Tests/Comun/CalculadoraTarifaTests.cs`
  (los cuatro casos, incluido el representante empadronado → `Club`) y `PermisosCentroTests.cs`
  (mismo centro → pasa; otro centro → `OTRO_CENTRO`; null → `OTRO_CENTRO`).
- [ ] T052 [US3] [Ambos · sugerido: Artur] Crear la sesión en la API:
  - `fuentes/src/SI18.Api/Modulos/Comun/IUsuarioActual.cs` y `UsuarioActual.cs`, que leen los claims
    `sub`, `role`, `tipo_tarifa`, `centro_id` y `club_id` del `HttpContext`.
  - En `RegistroComun.cs`:
    - `AddAuthentication().AddCookie` con nombre `si18.sesion`, `HttpOnly`, `SecurePolicy Always`,
      `SameSite Strict`, `ExpireTimeSpan 8 h` y `SlidingExpiration`;
    - eventos `OnRedirectToLogin` → 401 Problem Details `NO_AUTENTICADO` y
      `OnRedirectToAccessDenied` → 403 `ROL_NO_PERMITIDO`, nunca una redirección;
    - políticas `EsCiudadanoORepresentante`, `EsRepresentante`, `EsPersonal` y `EsTecnico`.
- [ ] T053 [US3] [Ambos · sugerido: Artur] Crear `fuentes/src/SI18.Api/Modulos/Comun/EndpointsSesion.cs`
  según el OpenAPI:
  - `POST /api/sesion`:
    - normaliza el correo a minúsculas y verifica con `PasswordHasher<Usuario>`;
    - si el correo no existe, verifica igualmente contra un hash ficticio, para que el tiempo de
      respuesta sea parecido;
    - si falla, 401 `CREDENCIALES_INVALIDAS`;
    - si acierta, emite la cookie con los claims calculados por `CalculadoraTarifa` y devuelve
      `UsuarioActualDto` con `centroNombre` y `clubNombre`;
  - `DELETE /api/sesion` → 204;
  - `GET /api/sesion/usuario` → 200 o 401.

  Mapear en `MapComun`. Depende de T052, T049, T019 y T038.
- [ ] T054 [P] [US3] [Ambos · sugerido: Saul] Crear `fuentes/src/SI18.Api/Modulos/Comun/EndpointsCatalogo.cs`
  (anónimos):
  - `GET /api/centros`, con `horaApertura` y `horaCierre` tomados de `ValoresNegocio` y formateados
    `HH:mm`;
  - `GET /api/deportes`;
  - `GET /api/instalaciones?centroId&deporteId&reservablePorFranjas`, con `deporteIds`.

  Usar `AsNoTracking`.
- [ ] T055 [P] [US3] [Artur] Crear `fuentes/src/SI18.Cliente/Comun/InicioSesion.razor` (ruta
  `/iniciar-sesion`):
  - `MudTextField` con `Label="Correo electrónico"`, `InputType.Email` y `autocomplete="username"`;
  - `MudTextField` con `Label="Contraseña"`, `InputType.Password` y
    `autocomplete="current-password"`;
  - un botón «Iniciar sesión»;
  - los errores se muestran con `MensajeAccesible`, y el foco vuelve al primer campo si hay error.

  Antes del PR: comprobar quickstart.md §6 sobre esta pantalla y anotarlo en el PR.
- [ ] T056 [US3] [Artur] Crear `fuentes/src/SI18.Cliente/Comun/EstadoSesion.cs`, un
  `AuthenticationStateProvider` que consulta `GET /api/sesion/usuario`. Completar el menú por rol en
  `MainLayout.razor`: «Mis avisos» para cualquier usuario identificado y «Cerrar sesión», más los
  huecos de menú de cada módulo según `Rol`. Depende de T024, T026 y T053.
- [ ] T057 [P] [US3] [Ambos · sugerido: Artur] Crear `fuentes/tests/SI18.Api.Tests/Comun/SesionTests.cs`:
  - V1–V4 de quickstart.md (rol, tarifa y centro de cada usuario precargado; cookie con `HttpOnly`);
  - V5: contraseña incorrecta y correo inexistente dan el mismo 401 y el mismo `detail`;
  - V6: `GET /api/avisos` sin cookie → 401 `NO_AUTENTICADO` en `application/problem+json`;
  - HU-C3 esc. 6, en `fuentes/tests/SI18.Api.Tests/Comun/PoliticasTests.cs`: obtener
    `IAuthorizationService` de `FabricaApi.Services` y comprobar que un `ClaimsPrincipal` con
    `role=ciudadano` no cumple `EsTecnico`, `EsPersonal` ni `EsRepresentante`, y que uno con
    `role=tecnico_concejalia` cumple `EsTecnico`. Los 403 reales de cada endpoint los prueba su spec.
- [ ] T058 [P] [US3] [Ambos · sugerido: Saul] Crear `fuentes/tests/SI18.Api.Tests/Comun/CatalogoTests.cs`:
  - V7: 4 centros con `08:00`/`22:00`;
  - V8: `reservablePorFranjas=true` → 16 instalaciones, todas pistas de tenis o pádel.
- [ ] T059 [P] [US3] [Artur] Crear `fuentes/tests/SI18.Api.Tests/Comun/EnumeracionesEspejoTests.cs`:
  los enumerados de `SI18.Contratos.Comun` tienen los mismos nombres que los de `SI18.Dominio.Comun`
  (evita que se separen).

**Punto de control**: cualquier spec puede proteger sus endpoints por rol y centro y probarlos con
`CrearClienteConSesion`.

---

## Fase 6: Historia 4 · Eventos entre historias con un responsable claro (P2)

**Objetivo**: la máquina de estados de Reserva y Pago, las interfaces entre módulos, «Mis avisos», la
pasarela simulada y la caducidad quedan definidas una sola vez y cada transición tiene un único
responsable.

**Prueba independiente**:
- todas las transiciones de RF-015 están probadas y las no permitidas devuelven
  `TRANSICION_NO_PERMITIDA`;
- una reserva pendiente pasa a caducada (y su pago a fallido) al llegar a los 10 min;
- los avisos solo los ve su destinatario.

- [ ] T060 [P] [US4] [Saul] Añadir la máquina de estados (RF-015, RF-016, data-model.md § Reserva) a
  `fuentes/src/SI18.Dominio/Reservas/Reserva.cs` y `Pago.cs`:
  - `EstaVigente(ahora)` =
    `{ ahora < CreadaEnUtc + TiempoBloqueoPago, si PendientePago ; true, si Confirmada ; false, en otro caso }`;
  - `Confirmar(codigo, ahora)`: solo desde `PendientePago`; si no está vigente, lanza
    `RESERVA_CADUCADA`; marca el pago `Completado`;
  - `Caducar(ahora)`: solo desde `PendientePago`; marca el pago `Fallido` con `ResueltoEnUtc`;
  - `Cancelar(motivo, ahora)`: solo desde `Confirmada`; con `motivo = Usuario` exige
    `ahora < InicioUtc` (si no, `RESERVA_YA_EMPEZADA`); con `CierreMantenimiento`, sin límite de hora;
  - cualquier otra transición → `TRANSICION_NO_PERMITIDA`;
  - `Pago.Completar(ahora)` y `Pago.Fallar(ahora)`: solo desde `Pendiente`.
- [ ] T061 [P] [US4] [Saul] Crear `fuentes/src/SI18.Dominio/Reservas/GeneradorCodigoAcceso.cs` (R14):
  16 bytes de `RandomNumberGenerator`, codificados en Base64url sin relleno (22 caracteres).
- [ ] T062 [US4] [Saul] Crear `fuentes/tests/SI18.Dominio.Tests/Reservas/ReservaTests.cs` con
  `[Trait("Categoria","Limites")]`:
  - las cuatro transiciones válidas;
  - `pendiente_pago → cancelada` y cualquier cambio desde `caducada` o `cancelada` →
    `TRANSICION_NO_PERMITIDA` (HU-C4 esc. 3);
  - `EstaVigente` a los 9:59 → sí y a los 10:00 → no;
  - `Confirmar` a los 10:00 → `RESERVA_CADUCADA`;
  - `Caducar` deja el pago en `Fallido`;
  - `Cancelar(Usuario)` 1 s antes del inicio → bien, y en el inicio exacto → `RESERVA_YA_EMPEZADA`;
  - `Cancelar(CierreMantenimiento)` después del inicio → bien.

  Crear también `GeneradorCodigoAccesoTests.cs`: longitud 22, solo caracteres Base64url y
  1000 códigos distintos. Depende de T060 y T061.
- [ ] T063 [P] [US4] [Artur] Crear `fuentes/src/SI18.Dominio/Reservas/IPasarelaPago.cs` (firma de
  contracts/modulos.md) y `fuentes/src/SI18.Api/Infraestructura/PasarelaSimulada.cs` (R9):
  - `Cobrar` acepta cualquier número de 16 dígitos salvo los que terminan en `0002`, que rechaza;
  - si el número no tiene 16 dígitos, lanza `PETICION_INVALIDA`;
  - `Devolver` termina siempre correctamente;
  - **no guarda ni registra en logs ningún dato de tarjeta**.

  Registrarla en un fichero propio de Artur, `Infraestructura/RegistroPasarela.cs` (`AddPasarela()`),
  que se llama desde `AddErrores()` para no tocar `Program.cs` ni `RegistroInfraestructura.cs`.
- [ ] T064 [P] [US4] [Artur] Crear `fuentes/tests/SI18.Api.Tests/Comun/PasarelaSimuladaTests.cs` (sin
  base de datos):
  - `4111111111111111` → aceptado;
  - `4000000000000002` → rechazado;
  - 15 dígitos → `PETICION_INVALIDA`;
  - `Devolver` no lanza.
- [ ] T065 [P] [US4] [Artur] Declarar, **sin implementación**, las interfaces entre módulos con las
  firmas exactas de [contracts/modulos.md](./contracts/modulos.md):
  - `fuentes/src/SI18.Api/Modulos/Comun/IServicioAvisos.cs`;
  - `Modulos/Disponibilidad/IConsultaDisponibilidad.cs` e `IConsultaFranjasFijas.cs`;
  - `Modulos/Reservas/IConsultaReservas.cs` e `IServicioReservas.cs`, con los records
    `ReservaActiva`, `ReservaAfectada` y `ResultadoCierre`;
  - `Modulos/Cancelaciones/IConsultaCierres.cs`.

  Las implementaciones son de las specs 001–008.
- [ ] T066 [US4] [Ambos · sugerido: Artur] Crear `fuentes/src/SI18.Api/Modulos/Comun/ServicioAvisos.cs`:
  `Avisar(usuarioId, texto)` añade una `Notificacion` con `FechaUtc` tomada de `TimeProvider` y
  `Leida=false`, **sin** `SaveChanges`, para que se guarde en la transacción del llamador (R18).
  Crear también `EndpointsAvisos.cs`:
  - `GET /api/avisos`: solo los del usuario identificado, ordenados por `fecha_utc` DESC;
  - `PUT /api/avisos/{id}/leido`: 204 idempotente; si el aviso no existe o no es suyo, 404
    `NO_ENCONTRADO`.

  Registrar ambos en `RegistroComun.cs`. Depende de T052 y T065.
- [ ] T067 [P] [US4] [Artur] Crear `fuentes/src/SI18.Cliente/Comun/MisAvisos.razor` (ruta
  `/mis-avisos`):
  - lista accesible con `MudList`, donde cada aviso muestra la fecha con `FormatoEs.FechaHora` y el
    estado «No leído» en texto (no solo con color);
  - botón «Marcar como leído» con `aria-label` que incluye el texto del aviso;
  - mensaje «No tienes avisos» si la lista está vacía.

  Depende de T066. Antes del PR: comprobar quickstart.md §6 sobre esta pantalla y anotarlo en el PR.
- [ ] T068 [US4] [Ambos · sugerido: Saul] Crear `fuentes/src/SI18.Api/Infraestructura/BarridoCaducidades.cs`
  (R8.4):
  - un `BackgroundService` con `PeriodicTimer(TimeSpan.FromSeconds(60), timeProvider)`;
  - en cada ciclo crea un ámbito y llama a `EjecutarUnaVez(ct)`;
  - `EjecutarUnaVez` busca las reservas `PendientePago` con
    `creada_en_utc ≤ ahora − ValoresNegocio.TiempoBloqueoPago` y llama a `Reserva.Caducar(ahora)`
    para cada una, guardando con `SaveChanges`.

  `EjecutarUnaVez` es público para las pruebas. Registrarlo en `RegistroInfraestructura.cs`.
  Depende de T060.
- [ ] T069 [P] [US4] [Ambos · sugerido: Artur] Crear `fuentes/tests/SI18.Api.Tests/Comun/AvisosTests.cs`:
  - un usuario solo ve sus avisos;
  - marcar como leído dos veces → 204 las dos veces;
  - marcar el aviso de otra persona → 404 `NO_ENCONTRADO`.
- [ ] T070 [P] [US4] [Ambos · sugerido: Saul] Crear
  `fuentes/tests/SI18.Api.Tests/Comun/BarridoCaducidadesTests.cs` con `[Trait("Categoria","Limites")]`:
  se inserta una reserva `pendiente_pago` creada en `T`;
  - con `Reloj` en `T + 9:59`, `EjecutarUnaVez` → sigue pendiente;
  - en `T + 10:00` → `caducada`, con el pago `fallido` y `resuelto_en_utc` informado.

**Punto de control**: 000-core completa. Las cuatro personas pueden abrir `spec-001` … `spec-008`.

---

## Fase 7: Pulido y validación (Fase 2 del reparto · Ambos)

- [ ] T071 [Ambos · sugerido: Artur] Crear las pruebas de errores, sin endpoints de prueba:
  - `fuentes/tests/SI18.Api.Tests/Comun/ErroresTests.cs`: una `FabricaApi` con `ConnectionStrings:Si18`
    apuntando a un puerto cerrado; `GET /api/centros` → 500 `ERROR_INTERNO`, y el cuerpo no contiene
    «Npgsql» ni ningún fragmento de la traza;
  - `fuentes/tests/SI18.Api.Tests/Comun/ManejadorErroresTests.cs`: invocar
    `ManejadorErrores.TryHandleAsync` con `DefaultHttpContext` y `ErrorNegocio(FRANJA_OCUPADA)` → 409 y
    `application/problem+json` con `type`, `title`, `detail`, `codigo` y `traceId`.
- [ ] T072 [Ambos · sugerido: Saul] Revisar `fuentes/src/SI18.Api/Program.cs`:
  - solo contiene la composición de T009;
  - añadir `AddOpenApi()` y `MapOpenApi()` (solo en Development) para servir `/openapi/v1.json`;
  - comprobar que ningún `.cs` de `src/` usa `DateTime.Now` ni `UtcNow` (lo garantiza el analizador:
    `dotnet build` en verde).
- [ ] T073 [Ambos] Revisar la accesibilidad de `InicioSesion`, `MainLayout` y `MisAvisos` según
  quickstart.md §6:
  - recorrido solo con teclado;
  - mensaje de error anunciado;
  - Lighthouse ≥ 95 y axe sin infracciones graves ni críticas.

  Anotar el resultado en el PR (principio I).
- [ ] T074 [Ambos] Ejecutar quickstart.md §2–§5 de principio a fin en las dos máquinas, con .NET 10:
  docker compose, `dotnet ef database update`, siembra, V1–V10 manuales y `dotnet test SI18.slnx`
  en verde.
- [ ] T075 [Ambos] Revisar los secretos según quickstart.md §7 (`git grep` sin resultados) y comprobar
  con `git ls-files fuentes/.env` que `.env` no está versionado.
- [ ] T076 [Ambos] Abrir el PR final de `develop` → `main`, con la lista de los principios I–IX
  revisados (constitución, «Flujo de trabajo»). Después, ejecutar `/speckit-converge` para 000-core.

---

## Dependencias y orden de ejecución

### Dependencias entre fases

```text
Fase 1 Setup (Saul) ──► Fase 2 Fundacional (Saul ∥ Artur)
                              │
          ┌───────────────────┼─────────────────────────┐
          ▼                   ▼                         ▼
  US1 modelo (Saul)    US2 valores (Saul: devolución y QR · Artur: disponibilidad, precio y formato)  US3/US4 partes de Artur
          │                   │
          └──────┬────────────┘
                 ▼
  US3 sesión y catálogo (Ambos)  ── necesita T038 (siembra) + T019 (errores)
                 ▼
  US4 avisos y barrido (Ambos)   ── necesita T052 (sesión) + T060 (máquina de estados)
                 ▼
  Fase 7 Pulido (Ambos)
```

- **T011** (agrupamiento) no depende de nada y se hace durante la Fase 1.
- **US1** depende de T012–T015. **US2** depende de T013–T015 y de T031 (para `CierreMantenimiento`
  en `ReglaDisponibilidad`).
- **Las pruebas de integración** (T039–T041, T057, T058, T069–T071) dependen de T022 (`FabricaApi`)
  y de T038.
- **T060** modifica `Reserva.cs` y `Pago.cs`, que creó T030: lo hace el mismo responsable (Saul)
  después.

### Paralelismo dentro de cada fase

- **Fase 1**: T002–T008 en paralelo después de T001; T011 en paralelo con toda la fase.
- **Fase 2**: la línea de Saul (T012–T017) y la de Artur (T018–T026) son completamente paralelas.
- **US1**: T027–T032 en paralelo; T034 y T035 en paralelo entre sí.
- **US2**: T042–T044 y T048 (Artur) ∥ T045–T047 (Saul).
- **US3**: T049–T051 (Saul) ∥ T055 (Artur) ∥ T054.
- **US4**: T060–T062 (Saul) ∥ T063–T065 y T067 (Artur).

## Ejemplo de reparto en paralelo (Fase 1 del reparto)

```text
Saul  (feature/000-core-dominio):      T012 → T013 → T014 → T015 → T016 → T017
                                       → T027…T032 → T045 → T046 → T047 → T049…T051 → T060…T062
Saul  (feature/000-core-persistencia): T033 → T034 ∥ T035 → T036 → T037 → T038
Artur (feature/000-core-errores-cliente): T018 → T019 → T020 ∥ T021 ∥ T022 ∥ T023
                                       → T024 → T025 → T026 → T048 → T055 → T059 → T063 → T064 → T065
Artur (feature/000-core-disponibilidad): T042 → T043 → T044 (cuando T031 de Saul esté en develop)
```

Con `/speckit-implement`, cada uno lanza solo sus tareas; por ejemplo:
`/speckit-implement solo las tareas [Artur] de la Fase 2 (T018–T026)`.

## Estrategia de implementación

### MVP (US1 + base)

1. Fase 1 → PR del esqueleto.
2. Fase 2 en paralelo → dos PR.
3. US1 → PR de persistencia. **Validar**: migración y siembra contra docker-compose (quickstart §3).

Con esto, las specs 001–008 ya pueden empezar su `/speckit-plan` contra tablas reales.

### Entrega incremental

US2 (reglas puras) → US3 (sesión y catálogo) → US4 (avisos, máquina de estados, barrido) → Pulido.
Cada historia se fusiona en `develop` con un PR que aprueba el otro, y `quickstart.md` se valida al
final de cada una.

## Notas

- `[P]` = ficheros distintos, sin dependencias pendientes. Una tarea `[Ambos]` la hace **una** persona
  (la sugerida) y la revisa la otra.
- Commits con el formato de la constitución: `000-core T037: migración NucleoComun`.
- Las configuraciones EF de T035 se crean en las carpetas de los módulos; después de 000-core, su dueño
  es el del módulo.
- Ninguna tarea implementa la lógica de las historias HU-01…HU-08: solo las piezas comunes y sus
  interfaces (principio VIII).
