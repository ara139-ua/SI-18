# Memoria del proyecto: SI-18 · Reserva de instalaciones deportivas municipales

**Aplicación web para que un ayuntamiento gestione la reserva de pistas deportivas de sus polideportivos**

Autores: Saul Pérez y Artur Ramazanov · Grupo G18 · Fecha de la memoria: 26 de noviembre de 2026 · Estado del código: rama `develop`, commit `f4090a9` (después del #57)

---

## Índice

1. [Portada y resumen](#1-portada-y-resumen)
2. [URL del repositorio en GITHUB](#2-url-del-repositorio-en-github)
3. [Introducción](#3-introducción)
4. [Análisis](#4-análisis)
5. [Diseño](#5-diseño)
6. [Implementación](#6-implementación)
7. [Pruebas y validación](#7-pruebas-y-validación)
8. [Despliegue y uso](#8-despliegue-y-uso)
9. [Planificación y seguimiento](#9-planificación-y-seguimiento)
10. [Conclusiones](#10-conclusiones)
11. [Demo](#11-demo)
12. [Anexos](#12-anexos)

---

## 1. Portada y resumen

**SI-18** es una aplicación web para que un ayuntamiento gestione la reserva de las pistas de tenis y pádel de cuatro polideportivos (Norte, Sur, Este y Oeste). El ciudadano busca una franja libre, la reserva y la paga, y recibe un código de acceso que el personal del centro valida en la entrada. Los clubes piden franjas fijas para toda la temporada y la concejalía las aprueba o las rechaza.

**Para quién.** Cinco tipos de usuario con permisos distintos:

- **Ciudadano** (empadronado o no): busca pistas, reserva, paga y consulta sus reservas.
- **Representante de club**: lo mismo que el ciudadano, con tarifa de club, y además solicita franjas fijas para su club.
- **Personal de centro**: valida los códigos de acceso en la entrada del centro al que está asignado.
- **Técnico de la concejalía**: revisa las solicitudes de franjas fijas.
- **Sistema**: aplica la tarifa, cobra con una pasarela simulada, caduca las reservas sin pagar y avisa a los usuarios.

**Resultado.** El grupo son **dos personas** y el alcance está **acordado con el profesor**: cuatro de las ocho historias de la descripción del sistema. Están implementadas y verificadas la base común `000-core` y **HU-01** (buscar pistas), **HU-02** (reservar y pagar), **HU-05** (franjas fijas de clubes) y **HU-06** (validar el código de acceso). HU-03, HU-04, HU-07 y HU-08 quedan fuera del alcance; `000-core` ya deja preparada su base. La aplicación es un monorepo .NET 10 (Blazor WebAssembly, ASP.NET Core Minimal API, PostgreSQL y MudBlazor) con 408 pruebas automáticas en verde. Se ha desarrollado con **Spec Kit** (desarrollo guiado por especificaciones) y, sobre la versión funcional, se aplicaron las actividades de refinamiento `clarify`, `analyze` y `converge` (apartado 9.3).

**Lo que no se hizo.** La comprobación manual de accesibilidad (NVDA, Lighthouse y axe) de los dos estados nuevos del cliente de HU-02 no se pudo hacer antes de la entrega (T058, excepción justificada; apartado 7.5), y la revisión de dos cambios de código común de HU-06 por parte de Artur sigue pendiente (apartado 10.2).

---

## 2. URL del repositorio en GITHUB

<https://github.com/ara139-ua/SI-18>

La rama de integración es `develop`. La entrega es el pull request **#43** (`develop` → `main`), que se fusiona al final.

---

## 3. Introducción

### 3.1 Contexto

Un ayuntamiento tiene cuatro polideportivos con pistas de tenis y pádel, pabellones, salas y piscinas. Las pistas se reservan en franjas de una hora, con un máximo de siete días de antelación, y el precio depende de quién reserva (empadronado, no empadronado o club). Hoy el ciudadano no sabe qué pistas están libres sin ir al centro, y el personal no tiene una forma fiable de comprobar quién ha reservado.

### 3.2 Problema que resuelve

- **Disponibilidad fiable.** Una franja solo puede tener una reserva activa, aunque dos personas reserven a la vez; la búsqueda debe mostrar solo lo que de verdad está libre.
- **Reservar y pagar sin dejar la pista bloqueada.** La reserva solo queda confirmada tras el pago; mientras el pago está pendiente, la franja se bloquea diez minutos y después se libera sola.
- **Acceso comprobable.** La reserva confirmada genera un código de acceso (con QR) que solo vale desde 15 minutos antes del inicio y mientras dura la franja, y solo en el centro de la reserva.
- **Franjas fijas de clubes.** Un club pide un horario fijo para toda la temporada; mientras no se apruebe no bloquea nada, y una vez aprobado nadie más puede reservar esa franja.
- **Reglas aplicadas siempre igual.** Las reglas de negocio viven en el servidor (principio III de la constitución); la interfaz solo las muestra.

### 3.3 Objetivos

1. Cubrir de extremo a extremo el recorrido «buscar → reservar y pagar → validar en la entrada» y el de las franjas fijas de los clubes.
2. Que cada historia tenga especificación, plan, tareas, pruebas y trazabilidad (apartado 9.2 y [trazabilidad.md](./trazabilidad.md)).
3. Accesibilidad WCAG 2.1 AA en todas las pantallas (requisito de administración pública).
4. Aplicar el refinamiento del enunciado (clarify, analyze, converge) sobre la versión funcional y dejar sus evidencias.
5. Que cada integrante pueda explicar y defender sus dos historias (reparto en [agrupamiento.md](./agrupamiento.md)).

### 3.4 Constitución del proyecto

La constitución ([`.specify/memory/constitution.md`](../.specify/memory/constitution.md), versión **1.2.1**) fija nueve principios que gobiernan todas las especificaciones:

| # | Principio | Qué impone |
| - | --------- | ---------- |
| I | Accesibilidad WCAG 2.1 AA (no negociable) | Teclado, foco visible, lector de pantalla, 320 px, texto además del color |
| II | La lógica de negocio está probada | Pruebas automáticas de reglas, límites y rechazos |
| III | Las reglas de negocio viven en el backend | La interfaz no decide nada que el servidor deba decidir |
| IV | Errores de la API coherentes | Problem Details con un catálogo de códigos único |
| V | Sin secretos en el repositorio (no negociable) | `.env` fuera de git, revisión de secretos antes de cada PR |
| VI | Datos de prueba ficticios | Usuarios `@si18.test`, tarjetas y códigos ficticios |
| VII | Base común en `000-core` | Lo que comparten varias historias se cambia en `000-core`, por PR revisado |
| VIII | Cero alcance fantasma | Nada que no esté en una spec |
| IX | Idioma, moneda y zona horaria | Español, euros y `Europe/Madrid` |

**Alcance de dos personas.** La constitución 1.2.0 (#49) fijó que el equipo son dos personas y que el alcance, **acordado con el profesor**, son cuatro historias: HU-01 y HU-05 (Artur) y HU-02 y HU-06 (Saul). La 1.2.1 (#53) corrigió la convención de ramas (`feature/NNN-…`) a raíz de un hallazgo de analyze (C1 de HU-01 y HU-05). HU-03, HU-04, HU-07 y HU-08 no están asignadas a nadie y **no son trabajo pendiente** ([agrupamiento.md](./agrupamiento.md)). Hasta el 26/11/2026 ese documento repartía las cuatro a dos personas, «C» y «D», que no llegaron a formar parte del grupo (versión anterior en el historial, #9).

---

## 4. Análisis

### 4.1 Actores y roles

| Rol | Qué puede hacer | Historias |
| --- | --------------- | --------- |
| Ciudadano (empadronado o no) | Buscar, reservar, pagar, ver sus reservas | HU-01, HU-02 |
| Representante de club | Lo anterior con tarifa de club; solicitar franjas fijas de su club | HU-01, HU-02, HU-05 |
| Personal de centro | Validar códigos de acceso de **su** centro | HU-06 |
| Técnico de la concejalía | Aprobar o rechazar solicitudes de franjas fijas | HU-05 |

Las especificaciones y el detalle de roles y permisos están en `specs/000-core/spec.md`, «Roles y permisos».

### 4.2 Historias de usuario

| Historia | Spec | Responsable | Qué cubre |
| -------- | ---- | ----------- | --------- |
| HU-01 | [001-buscar-pistas](../specs/001-buscar-pistas/spec.md) | Artur | Buscar franjas libres por deporte, centro y fecha, con el precio de la tarifa del usuario |
| HU-02 | [002-reservar-pagar](../specs/002-reservar-pagar/spec.md) | Saul | Reservar, pagar, ver «Mis reservas»; caducidad a los 10 minutos |
| HU-05 | [005-franjas-fijas](../specs/005-franjas-fijas/spec.md) | Artur | Solicitar y revisar franjas fijas de club, con conflictos y avisos |
| HU-06 | [006-validar-qr](../specs/006-validar-qr/spec.md) | Saul | Validar el código de acceso en la entrada y registrar cada intento |
| Base | [000-core](../specs/000-core/spec.md) | Saul y Artur | Modelo de datos, valores de negocio, estados, sesión, errores, interfaces entre módulos |

Fuera del alcance comprometido: HU-03 (cancelar una reserva), HU-04 (inscribirse en cursos con lista de espera), HU-07 (cierre por mantenimiento) y HU-08 (informe mensual).

### 4.3 Requisitos funcionales

Cada spec numera sus requisitos (`RF-NNN`) y criterios de éxito (`CE-NNN`):

| Spec | Requisitos funcionales | Criterios de éxito | Tareas |
| ---- | ---------------------: | -----------------: | -----: |
| 000-core | 28 | 7 | 79 |
| 001-buscar-pistas | 17 | 7 | 27 |
| 002-reservar-pagar | 26 | 8 | 61 |
| 005-franjas-fijas | 19 | 6 | 30 |
| 006-validar-qr | 18 | 7 | 34 |

Resumen por historia (el texto completo está en cada `spec.md`):

- **HU-01**: solo con sesión; el resultado muestra únicamente las franjas libres (no las ocupadas, bloqueadas por un club o cerradas); el precio es el de la tarifa de quien busca; mensajes distintos cuando el centro no tiene pistas del deporte y cuando no quedan franjas.
- **HU-02**: reservar bloquea la franja diez minutos; el pago confirma la reserva y genera el código; si el pago se rechaza o pasa el tiempo, la reserva caduca; seis motivos de rechazo al reservar (fuera de antelación, franja ya empezada, franja ocupada, bloqueada por un club, instalación cerrada o no reservable); una persona no puede tener dos reservas activas de la misma franja.
- **HU-05**: el representante solicita franjas fijas (días de la semana, horas) para una temporada; la solicitud queda pendiente; el técnico ve los conflictos con reservas existentes, y no puede aprobar si los hay; al resolver, el club recibe un aviso.
- **HU-06**: solo el personal de centro valida; el código se compara exacto; se rechaza con motivo (desconocido, otro centro, reserva no confirmada, fuera de ventana); una validación aceptada repetida muestra el aviso de la anterior; cada intento queda registrado y la reserva no cambia.

### 4.4 Requisitos no funcionales

- **Accesibilidad** WCAG 2.1 AA (principio I), también a 320 px.
- **Rendimiento**: umbral de 300 ms en el percentil 95 con los datos de la siembra (por ejemplo, la validación de HU-06; `000-core`); medidas en las tareas de cada spec (por ejemplo, la búsqueda de HU-01 entre 30 y 44 ms, `specs/001-buscar-pistas/tasks.md` T022).
- **Seguridad**: sesión por cookie `HttpOnly` y `Secure`, política por rol, sin secretos en el repositorio.
- **Idioma, moneda y zona horaria**: español, euros y `Europe/Madrid`, incluidos los cambios de hora del 25/10/2026 y el 28/03/2027.

### 4.5 Reglas que cruzan historias

- **Una franja, una reserva activa** (índice único parcial en la base de datos y bloqueo de fila al escribir): HU-01 la usa para no ofrecer lo ocupado y HU-02 para no reservar dos veces.
- **Franja fija aprobada** (HU-05) bloquea la búsqueda (HU-01) y la reserva (HU-02, `FRANJA_BLOQUEADA_CLUB`), también para el propio club.
- **El código de acceso lo genera HU-02 y lo valida HU-06**; una reserva cancelada deja de valer sin más cambios.
- **La caducidad al pagar con la franja ya empezada** (RF-009a de HU-02) obligó a cambiar la regla de `000-core` (apartado 6.4).

---

## 5. Diseño

### 5.1 Arquitectura

Monorepo .NET 10 en `fuentes/` con un único despliegue: la API sirve también el cliente Blazor.

| Proyecto | Función |
| -------- | ------- |
| `SI18.Dominio` | Entidades, reglas puras (`ReglaDisponibilidad`, `ReglaAcceso`, `ReglaFranjaFija`…), máquina de estados de `Reserva` y `Pago`, catálogo de errores, `ValoresNegocio` |
| `SI18.Contratos` | DTO compartidos entre API y cliente, enumerados espejo, formato en español |
| `SI18.Api` | ASP.NET Core Minimal API organizada por **módulos** (`Comun`, `Disponibilidad`, `Reservas`, `Cancelaciones`, `CursosInformes`), EF Core, sesión y políticas |
| `SI18.Cliente` | Blazor WebAssembly con MudBlazor: pantallas por módulo |
| `SI18.Api.Tests`, `SI18.Dominio.Tests` | xUnit v3, con PostgreSQL real en Testcontainers |

**Dueños de módulo**: cada módulo tiene un único dueño que escribe en sus tablas; los demás usan su interfaz pública (`IConsultaDisponibilidad`, `IConsultaReservas`, `IConsultaFranjasFijas`, `IServicioReservas`, `IServicioAvisos`…). Artur es dueño de `Disponibilidad` (HU-01, HU-05) y Saul de `Reservas` (HU-02, HU-06); `Comun` es de los dos. `Cancelaciones` y `CursosInformes` no tienen dueño (fuera de alcance): solo existe la base de `000-core`. Detalle en `specs/000-core/plan.md` y [agrupamiento.md](./agrupamiento.md).

### 5.2 Modelo de datos

Definido una vez en [`specs/000-core/data-model.md`](../specs/000-core/data-model.md) y migrado en `20261104144623_NucleoComun`. Entidades: `Centro`, `Deporte`, `Instalacion`, `Tarifa`, `Usuario`, `Club`, `Temporada`, `Reserva`, `Pago`, `SolicitudFranjasFijas`, `ValidacionAcceso`, `Notificacion`, `CierreMantenimiento`, `Devolucion`, `Curso` e `Inscripcion`; la **franja** no se almacena, se calcula. Las cuatro últimas existen por la base de las historias fuera de alcance. Convenciones: tablas y columnas en `snake_case`, fechas en UTC y presentación en hora de Madrid.

Máquina de estados de una reserva: `pendiente_pago → confirmada | caducada`, y `confirmada → cancelada` (esta última, para HU-03/HU-07); el pago pasa por `pendiente → pagado | fallido`. La reserva pendiente caduca por tiempo (10 minutos), por pago rechazado o, desde RF-009a, por pagar con la franja ya empezada.

### 5.3 Contrato de la API

Cada spec tiene su contrato OpenAPI en `specs/NNN/contracts/` (por ejemplo, [`api-reservas.openapi.yaml`](../specs/002-reservar-pagar/contracts/api-reservas.openapi.yaml)), y el documento generado está en `/openapi/v1.json` con la API en marcha. Rutas principales:

| Ruta | Historia |
| ---- | -------- |
| `GET /api/disponibilidad/franjas` | HU-01 |
| `GET /api/reservas/resumen`, `POST /api/reservas`, `POST /api/reservas/{id}/pago`, `GET /api/reservas`, `GET /api/reservas/{id}` | HU-02 |
| `/api/solicitudes-franjas-fijas` (alta, lista, conflictos, aprobación, rechazo) | HU-05 |
| `POST /api/validaciones-acceso` | HU-06 |
| `/api/sesion`, `/api/avisos`, catálogo de centros, deportes e instalaciones | Base |

Los errores son Problem Details con un `codigo` del catálogo de `000-core` (por ejemplo, `FRANJA_OCUPADA`, `RESERVA_CADUCADA`, `FRANJA_YA_EMPEZADA`, `QR_OTRO_CENTRO`, `ROL_NO_PERMITIDO`).

### 5.4 Decisiones técnicas y por qué se tomaron

Cada spec razona sus decisiones en `research.md`:

| Spec | Decisiones |
| ---- | ---------- |
| 001 | D1 un único endpoint de búsqueda · D3 reutilizar las reglas puras de 000-core · D4 `IConsultaDisponibilidad` para HU-02 · D5 consultas provisionales de solo lectura mientras HU-02 y HU-05 no existían |
| 002 | E2 resumen previo sin crear nada · E3 crear la reserva · E4 secuencia del pago · E5 bloqueo de fila en toda escritura · E7 `IConsultaReservas` definitiva y retirada de la provisional · E8 `IServicioReservas` para HU-03 y HU-07 · E13 pruebas de concurrencia |
| 005 | F3 regla de franjas fijas como función pura · F4 conflictos: calcular, mostrar y volver a comprobar al aprobar · F5 avisos a los representantes · F6 `IConsultaFranjasFijas` definitiva |
| 006 | V3 decisión en `ServicioValidarAcceso` con la regla de 000-core · V4 decisión y registro en la misma operación · V5 aviso de la validación anterior sin estado nuevo · V7 quién valida y cuál es su centro · V11 pruebas de límites |

Decisiones transversales:

- **Backend manda** (III): los cuatro rechazos de reserva y de validación, el orden de los motivos y los límites de tiempo están en el servidor; el cliente solo los muestra.
- **Pasarela simulada**: una tarjeta ficticia que termina en `0002` se rechaza; cualquier otra se acepta (R9 de 000-core).
- **Una sola reserva activa por franja** garantizada por un índice único parcial y un bloqueo de fila, no por comprobaciones en código (`ReservaConcurrenteTests`).
- **Hora de Madrid sin literales**: una sola constante (`ValoresNegocio.ZonaHoraria`) y una prueba de que el formato del cliente usa la misma zona.
- **Registro de módulos**: dos líneas por módulo en `Program.cs` (`Add<Modulo>()` y `Map<Modulo>()`), acordado en el #18 tras un hallazgo de analyze.

---

## 6. Implementación

### 6.1 Pila tecnológica

| Capa | Tecnología (versión) |
| ---- | -------------------- |
| Lenguaje y plataforma | C# sobre .NET SDK 10.0 (`fuentes/global.json`) |
| API | ASP.NET Core Minimal API 10.0.14, OpenAPI |
| Cliente | Blazor WebAssembly 10.0.14, MudBlazor 8.15.0 |
| Datos | PostgreSQL 17 (docker-compose), Entity Framework Core 10.0.14, Npgsql 10.0.3, `EFCore.NamingConventions` |
| QR | QRCoder 1.8.1 |
| Pruebas | xUnit v3 4.0.2, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers.PostgreSql 4.16.0, `FakeTimeProvider` |
| Calidad | `BannedSymbols.txt` con `BannedApiAnalyzers` (por ejemplo, prohibido el reloj del sistema) |
| Especificaciones | GitHub Spec Kit 1.1.2.dev0 |

### 6.2 Estructura del repositorio

```
SI-18/
├── fuentes/
│   ├── SI18.slnx · global.json · Directory.*.props · docker-compose.yml · .env.example
│   ├── src/
│   │   ├── SI18.Dominio/        reglas puras, entidades, estados, catálogo de errores
│   │   ├── SI18.Contratos/      DTO y enumerados compartidos
│   │   ├── SI18.Api/            Infraestructura/ y Modulos/{Comun,Disponibilidad,Reservas,…}
│   │   └── SI18.Cliente/        Layout/, Comun/, Disponibilidad/, Reservas/, …
│   └── tests/                   SI18.Api.Tests, SI18.Dominio.Tests
├── specs/                       000-core, 001-buscar-pistas, 002-reservar-pagar, 005-franjas-fijas, 006-validar-qr
├── .specify/                    constitución (memory/constitution.md), plantillas y scripts de Spec Kit
└── docs/                        agrupamiento.md, memoria.md, trazabilidad.md, verificacion-integrada.md, evidencias/
```

El código de producción son unas 9 900 líneas entre `.cs` y `.razor` (`git ls-files` sobre `fuentes/src`), repartidas en 126 ficheros C# y 18 componentes Razor.

### 6.3 Partes más relevantes del código

- **Reservar y pagar** — `ServicioReservar` (`SI18.Api/Modulos/Reservas`): `Pagar` carga la reserva con bloqueo de fila, aplica en orden «ya caducada → no pagable → no vigente → franja ya empezada → pasarela → confirmar» y guarda el resultado antes de responder; la reserva se confirma en `Reserva.Confirmar`, que vuelve a comprobar vigencia y hora.
- **Máquina de estados** — `SI18.Dominio/Reservas/Reserva.cs`: el constructor (crear), `Confirmar`, `Caducar` y `Cancelar`; es lo único que cambia el estado.
- **Validar el acceso** — `ServicioValidarAcceso` y `ReglaAcceso`: busca la reserva por código exacto, decide en el orden de RF-005 (desconocido → otro centro → no confirmada → fuera de ventana) y registra el intento en la misma operación.
- **Reglas puras de disponibilidad** — `ReglaDisponibilidad` y `ReglaFranjaFija`, reutilizadas por la búsqueda y la reserva para que ofrezcan y rechacen lo mismo.
- **Franjas fijas** — `ServicioSolicitudesFranjasFijas`: conflictos con las reservas de la temporada, aprobación que vuelve a comprobarlos y avisos a todos los representantes del club.
- **Cliente** — `PagarReserva.razor`, `MisReservas.razor`, `ValidarAcceso.razor` y el componente `MensajeAccesible`, que anuncia los resultados a los lectores de pantalla.

### 6.4 Decisiones tomadas sobre la marcha

- **#37 · `develop` volvió a compilar tras fusionar #31 y #33.** Cada PR retiraba una clase provisional que el otro aún usaba en las pruebas, y las pruebas de HU-02 y HU-05 pisaban las mismas franjas de Oeste en la base compartida. Se arregló solo en pruebas (ayudantes y dos fechas) y desde entonces cada prueba elige fecha y pista con `git grep` para no pisar otras.
- **Retirada de las provisionales.** HU-01 empezó con consultas provisionales de solo lectura (`ConsultaReservasProvisional`, `ConsultaFranjasFijasProvisional`) porque HU-02 y HU-05 aún no existían (aclaración 1 de 001). Se retiraron al fusionar #31 y #33 y la dependencia quedó anotada en las dos specs. `ConsultaCierresProvisional` se mantiene porque HU-07 está fuera de alcance.
- **C1 y C2 de HU-06.** Dos cambios mínimos de código común, pactados en el plan y pedidos a revisión de Artur: el enlace «Validar acceso» en el menú del personal (`MainLayout.razor`, C1) y la lectura de las extensiones del Problem Details en `ErrorApi`/`ClienteApi` (C2). El #46 se fusionó antes de esa revisión por la entrega. Artur la hizo después (26/11/2026) y los aprobó sin cambios: menú por rol y 320 px comprobados en la app, y la pantalla muestra los datos de la reserva que llegan como extensión del Problem Details ([specs/006-validar-qr/revision-c1-c2.md](../specs/006-validar-qr/revision-c1-c2.md)).
- **T056 · pagar con la franja ya empezada.** El `clarify` sobre la versión funcional descubrió que una reserva pendiente se podía pagar después del inicio de la franja y quedaba confirmada, algo que no se podría haber creado. Se decidió que la reserva caduque, no se cobre y se responda `FRANJA_YA_EMPEZADA`. Como añade una causa de caducidad a `000-core` (RF-016 y `Reserva.Confirmar`), primero se llevó el cambio a `000-core` por PR revisado (T055, principio VII) y después se implementó (prueba escrita antes, 3 de 6 en rojo con el código anterior y 6 de 6 en verde después, T056) junto con la hora límite «la más temprana entre creación + 10 minutos y el inicio» (T057).
- **T058 · «Pagar» oculto y resultado final en el cliente.** Pasada la hora límite, «Mis reservas» no ofrece «Pagar», y `PagarReserva` trata `FRANJA_YA_EMPEZADA` como resultado final (sin formulario). La comprobación manual de accesibilidad no se hizo (apartado 7.5).
- **Revisión de secretos en cada PR** (principio V), con el `git grep` de `specs/000-core/quickstart.md` §7 y una comprobación de que `fuentes/.env` no está versionado. El `converge` de `000-core` corrigió además el plan: la API toma su configuración de variables de entorno y no de `dotnet user-secrets` (T077).

---

## 7. Pruebas y validación

### 7.1 Estrategia

- **Pruebas unitarias de dominio** (`SI18.Dominio.Tests`): reglas puras, máquina de estados y valores de negocio, con límites exactos.
- **Pruebas de integración por HTTP** (`SI18.Api.Tests`): la API completa contra PostgreSQL real (Testcontainers) con una fábrica compartida (`FabricaApi`) y un reloj controlado (`FakeTimeProvider`). Las que necesitan otro instante usan un reloj propio.
- **Categoría `Limites`**: las pruebas de los límites de tiempo (15 y 14 minutos antes del inicio, 10 minutos de bloqueo, cambios de hora del 25/10/2026 y 28/03/2027, fin de la franja).
- **Concurrencia**: `ReservaConcurrenteTests` (veinte peticiones simultáneas por la misma franja) y `ReservaActivaUnicaTests`.
- **Verificación integrada** (enunciado 5.4): dos recorridos que cruzan historias, automatizados en `RecorridosIntegradosTests` ([verificacion-integrada.md](./verificacion-integrada.md)).
- **Comprobación manual**: accesibilidad (teclado, foco, lector de pantalla, 320 px, Lighthouse y axe), rendimiento y menú por rol, descritos en el `quickstart.md` de cada spec.
- **Refinamiento**: cada hallazgo de `clarify`, `analyze` y `converge` terminó en una prueba, una tarea o una justificación ([trazabilidad.md](./trazabilidad.md), sección 3).

### 7.2 Resultados de las pruebas automáticas

Sobre `develop` después del #57 (`f4090a9`):

| Comando | Resultado |
| ------- | --------- |
| `dotnet build SI18.slnx` | 0 advertencias, 0 errores |
| `dotnet test SI18.slnx` | **410 de 410** correctas, 0 omitidas |
| `dotnet test SI18.slnx --filter-trait "Categoria=Limites"` | **167 de 167** correctas |

Los métodos de prueba (`[Fact]` y `[Theory]`) pasaron de 271 en el commit de referencia del refinamiento (`36a3166`) a 283 en la versión final. Salidas y detalle en [verificacion-integrada.md](./verificacion-integrada.md), «Regresión».

### 7.3 Pruebas de concurrencia y de reglas críticas

- `ReservaConcurrenteTests`: de veinte reservas simultáneas de la misma franja solo una se crea; las demás reciben `FRANJA_OCUPADA`.
- `ReservaActivaUnicaTests` (000-core): el índice único parcial rechaza dos reservas activas de la misma franja.
- `PagarReservaTests.RF009a_…`: pagar con la franja ya empezada caduca la reserva, no llama a la pasarela y un segundo intento da `TRANSICION_NO_PERMITIDA`.
- `ValidarAccesoTests` y `LimitesValidacionTests`: orden de los motivos de rechazo, ventana de 15 minutos y registro de cada intento, incluido el recorte a 64 caracteres.
- `ConflictosFranjasFijasTests`: conflictos de una solicitud y cómputo solo dentro de la temporada (T030 de converge).

### 7.4 Pruebas de extremo a extremo

No hay herramienta de pruebas de componentes de Blazor ni pruebas de navegador automatizadas en el repositorio; las pantallas se comprueban a mano y con auditorías de Lighthouse (apartado 7.5). Los recorridos completos se verifican por HTTP contra la API real (`RecorridosIntegradosTests`):

- **R1** (satisfactorio): HU-01 → HU-02 → HU-06. Búsqueda en Este, reserva y pago, código en «Mis reservas» y dos validaciones con `personal.este`; la segunda trae el aviso de la primera.
- **R2** (error, permiso y estado): HU-05 → HU-01 → HU-02 → HU-06. Franja fija aprobada → desaparece de la búsqueda → reservarla da `FRANJA_BLOQUEADA_CLUB` → `personal.oeste` con un código de Este da `QR_OTRO_CENTRO` (sin datos de la reserva y con el intento registrado) → una ciudadana recibe `ROL_NO_PERMITIDO`.

Se automatizaron porque la ventana horaria real (franjas de 08:00 a 22:00 y QR desde 15 minutos antes) no permitía hacerlos a mano antes de la entrega; las pruebas **no ejercen el cliente Blazor**.

### 7.5 Validación manual y accesibilidad

| Evidencia | Qué es |
| --------- | ------ |
| [auditorías 000-core T073](./evidencias/auditorias/000-core-t073/) | Lighthouse (flujo) de inicio de sesión, menú y «Mis avisos» |
| [auditorías 002](./evidencias/auditorias/002-pantallas/) | Lighthouse de las pantallas de reservar y pagar, y `resultados.json` (nombres accesibles, foco y campo de la tarjeta) |
| HU-06 T026 | Lighthouse 100 y axe sin infracciones, 320 px y menús de los demás roles (anotado en el #46; no se guardó como fichero) |
| [t058-revision-de-marcado.md](./evidencias/t058-revision-de-marcado.md) | Revisión del marcado de los dos estados nuevos del cliente de HU-02, **leyendo el código** |

**Lo que no se hizo.** La comprobación con NVDA, Lighthouse y axe de los dos estados nuevos del cliente de HU-02 (T058: «Mis reservas» con una pendiente pasada de plazo y `/reservas/{id}/pago` con `FRANJA_YA_EMPEZADA`) **no se pudo hacer por la hora**: esos estados solo existen entre el inicio de la franja y «creación + 10 minutos», y los centros cierran a las 22:00. Se revisó el marcado (foco al encabezado, `role` y `aria-live`, texto e icono, sin formulario, enlace alcanzable) y queda registrada como **excepción justificada**, no como comprobada. El comportamiento lo cubren las pruebas de T056 y T057. Queda sin confirmar con lector de pantalla que el mensaje se anuncie una sola vez (`MudAlert` dentro de una región `status`).

### 7.6 Rendimiento

Medidas manuales sobre la API local, en cada spec: la búsqueda de HU-01 respondió entre 30 y 44 ms (T022 de 001); el recorrido de reserva y pago de HU-02 se cronometra en T049 y la validación de HU-06 en T028 contra el umbral de 300 ms del percentil 95 de `000-core`. No se han hecho pruebas de carga.

---

## 8. Despliegue y uso

### 8.1 Requisitos previos

SDK de .NET **10.0.x**, Docker Desktop con Compose y Git. Detalle y comprobaciones en [`specs/000-core/quickstart.md`](../specs/000-core/quickstart.md).

### 8.2 Instalación y arranque

```powershell
cd fuentes
Copy-Item .env.example .env          # rellena los valores; .env no se versiona
docker compose up -d                 # PostgreSQL 17 (healthcheck)

# Carga .env en la sesión de PowerShell (la API no lee .env por sí sola)
Get-Content .env | Where-Object { $_ -match '^\s*[^#\s][^=]*=' } | ForEach-Object {
    $nombre, $valor = $_ -split '=', 2
    Set-Item -Path "Env:$($nombre.Trim())" -Value $valor.Trim()
}

dotnet build SI18.slnx
$env:SI18_SEMBRAR_DATOS = "true"; dotnet run --project src/SI18.Api
```

La API aplica las migraciones y siembra los datos de demostración al arrancar con `SI18_SEMBRAR_DATOS=true`. El cliente se abre en `https://localhost:7180` y el documento OpenAPI en `https://localhost:7180/openapi/v1.json`.

Variables de `.env.example` (sin valores en este documento): `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT`, `ConnectionStrings__Si18`, `SI18_CLAVE_DEMO` (contraseña común de los usuarios de demo), `SI18_SEMBRAR_DATOS` y `SI18_TEST_CONNECTION` (opcional: usar una base propia en las pruebas en lugar de Testcontainers).

### 8.3 Comandos útiles

```powershell
dotnet test SI18.slnx                                    # suite completa (Testcontainers arranca su PostgreSQL)
dotnet test SI18.slnx --filter-trait "Categoria=Limites" # solo los límites de tiempo
dotnet tool restore; dotnet ef database update --project src/SI18.Api --connection $env:ConnectionStrings__Si18
docker compose down -v                                   # borra la base local
```

### 8.4 Integración continua

No hay flujo de integración continua configurado en el repositorio. Cada PR pasa por la revisión de la otra persona del grupo y por la comprobación local de build y pruebas; los PR #31, #36 y #46 se fusionaron sin revisión por la entrega (ver 10.2).

### 8.5 Usuarios de ejemplo

Todos son ficticios (`@si18.test`) y comparten la contraseña de `SI18_CLAVE_DEMO`:

| Correo | Rol |
| ------ | --- |
| `ciudadana.empadronada@si18.test`, `ciudadana2.empadronada@si18.test` | Ciudadano empadronado |
| `ciudadano.noempadronado@si18.test` | Ciudadano no empadronado |
| `representante.norte@si18.test`, `representante.padel@si18.test` | Representante de club |
| `personal.norte@`, `personal.sur@`, `personal.este@`, `personal.oeste@si18.test` | Personal del centro respectivo |
| `tecnico@si18.test` | Técnico de la concejalía |

### 8.6 Manual de usuario breve

- **Ciudadano o representante**: inicia sesión → «Buscar pistas» → elige deporte, centro y fecha → pulsa «Reservar» en la franja → revisa el resumen y confirma → paga con una tarjeta ficticia de 16 dígitos antes de la hora límite que se muestra → en «Mis reservas» aparece la reserva confirmada con su código de acceso.
- **Representante de club**: «Franjas fijas de mi club» → solicita pista, días y horas de la temporada → espera la resolución; recibe un aviso en «Mis avisos».
- **Técnico**: «Revisar franjas fijas» → abre una solicitud, mira los conflictos → aprueba o rechaza.
- **Personal de centro**: «Validar acceso» → escribe el código de la persona → el resultado dice «Acceso permitido» con el titular y la franja, o «Acceso denegado» con el motivo; si ya se validó antes, avisa de cuándo.

---

## 9. Planificación y seguimiento

### 9.1 Método de trabajo

Spec-driven development con **Spec Kit**: cada historia sigue el ciclo `specify → clarify → plan → tasks → analyze → implement → converge`, con una carpeta `specs/NNN-…` que guarda todos sus documentos. Un PR por bloque de trabajo hacia `develop` con ramas `feature/NNN-…`, fusionado con merge commit, y la otra persona del grupo como revisora. El reparto de historias ([agrupamiento.md](./agrupamiento.md)) se mantiene trazable por commits, autoría de documentos y tareas cerradas.

### 9.2 Tareas por especificación

| Spec | Tareas | Marcadas | Fases de convergencia |
| ---- | -----: | -------: | --------------------- |
| 000-core | 79 | 79 | T077–T079 |
| 001-buscar-pistas | 27 | 27 | — |
| 002-reservar-pagar | 61 | 61 | T053–T054, T055–T060, T061 |
| 005-franjas-fijas | 30 | 30 | T028–T030 |
| 006-validar-qr | 34 | 34 | T031–T033, T034–T035 |

La **matriz de trazabilidad global** (112 criterios de HU-01, HU-02, HU-05 y HU-06 → decisión del plan → tareas → prueba, más una fila por cada historia fuera de alcance) y el **registro mínimo** del enunciado están en [trazabilidad.md](./trazabilidad.md).

### 9.3 Iteraciones

1. **Base y especificación** (14/10/2026 – 28/10/2026): constitución, `specify` y `clarify` de `000-core`, plan, tareas y primer `analyze` (PR #5–#8).
2. **Implementación** (noviembre de 2026): `000-core` (#10–#22), HU-01 (#19, #23, #24, #27), HU-02 (#25, #29–#31, #36, #39), HU-05 (#32–#34) y HU-06 (#45–#47).
3. **Primer refinamiento** (25–26/11/2026), antes de la versión funcional completa: `clarify` y `analyze` previos al plan en HU-02, `converge` de HU-01, HU-05 y `000-core`.
4. **Refinamiento sobre la versión funcional** (26/11/2026, enunciado 5.1–5.3). Commit de referencia `36a3166` ([commit-referencia.md](./evidencias/commit-referencia.md)); versión de Spec Kit 1.1.2.dev0.
   - **clarify**: 27 preguntas respondidas e integradas en las cuatro specs, 9 de ellas sobre la versión funcional (2 en HU-01, 3 en HU-02, 2 en HU-05, 2 en HU-06). Informes: `clarify-diff.md` de cada spec y `clarify-funcional.md` de las cuatro specs, con tres situaciones probadas por historia contra la API local.
   - **analyze**: informes inicial y final por spec (`analyze-registro.md`, `analyze-final*.md`, `analyze-1/2/3.md` de 006) y revisión entre compañeros; 54 hallazgos, todos cerrados.
   - **converge**: 24 hallazgos, 17 resueltos con tarea, 1 de proceso resuelto con la revisión de C1 y C2, y 6 aceptados con justificación; sin brechas pendientes. La prueba del fallo previo de T030 está en `docs/evidencias/t030-antes-despues.md`. Informes `converge-*.md` de cada spec (002: pasadas 1, 2 y final; 006: pasadas 1 a 4).
5. **Verificación integrada** (26/11/2026, enunciado 5.4): R1 y R2 automatizados ([verificacion-integrada.md](./verificacion-integrada.md)) y balance con denominadores ([trazabilidad.md](./trazabilidad.md), sección 4).

Las entradas de las iteraciones 1 a 3 son el historial real de PR y commits; las fechas del refinamiento son las de los informes. Los informes originales de `analyze` de HU-02 y de `000-core` no se conservaron: sus registros son reconstrucciones marcadas como tales.

### 9.4 Historial de commits

222 commits en `develop`, desde el 14/10/2026: 151 de Saul y 71 de Artur (dos identidades de git, «Artur R A» y «Artur Ramazanov Anichkin»). Todo commit indica la spec o la tarea (por ejemplo, `spec-002 T056: …`). Consulta con `git log --oneline develop`.

### 9.5 Issues y pull requests

Los PR están numerados del #1 al #57 en <https://github.com/ara139-ua/SI-18/pulls>. No se han usado issues. El **#43** (`develop` → `main`) es la entrega y sigue abierto hasta el final. Los PR del refinamiento son el #51 (evidencias), #52 (analyze y converge finales de 001 y 005), #53 (constitución 1.2.1), #54 (refinamiento de 002 y 006), #55 (cierre de la fase 1), #56 (verificación integrada) y #57 (memoria y trazabilidad).

---

## 10. Conclusiones

### 10.1 Qué se consiguió

- Cuatro historias de extremo a extremo, más la base común, con 410 pruebas automáticas en verde y una categoría de pruebas de límites de tiempo (169).
- Un proceso trazable: cada criterio de las cuatro historias tiene decisión de plan, tareas y prueba ([trazabilidad.md](./trazabilidad.md)).
- Un refinamiento con consecuencias reales en el código: el `clarify` sobre la versión funcional encontró un defecto de comportamiento (pagar con la franja ya empezada) que las pruebas existentes no cubrían, y converge generó tareas con prueba del fallo previo.
- Una constitución que obligó a llevar los cambios de reglas compartidas a `000-core` antes que a una historia (principio VII).

### 10.2 Problemas encontrados

- **Plazo y ventana horaria.** La verificación integrada y la comprobación de accesibilidad de los dos estados nuevos no se pudieron hacer a mano: los estados solo existen unos minutos antes de que cierre el centro. Se sustituyeron por pruebas automáticas y una revisión de marcado, ambas declaradas como tales.
- **Revisión cruzada incompleta.** El #46 (pantalla de HU-06 con los cambios de código común C1 y C2), el #31 y el #36 se fusionaron sin la revisión de la otra persona del grupo, por la entrega. La de C1 y C2 la hizo Artur después y quedó cerrada sin cambios (F1 de converge, P1 de analyze-3).
- **Informes no conservados.** Los informes originales de `analyze` de HU-02 y de `000-core` se perdieron; sus registros son reconstrucciones a partir de commits y PR, marcadas como tales.
- **Pruebas que se pisan.** La base de pruebas se comparte; hubo que repartir fechas y pistas por spec (#37) y comprobarlas con `git grep`.
- **Incidencias documentales abiertas**: las tres de `000-core` (I1–I3); ninguna afecta al comportamiento. Las de las cuatro historias están cerradas (001 I2 e I3 con notas en `plan.md`, 005 E2 verificada en la demo).

### 10.3 Trabajo futuro

- **HU-03, HU-04, HU-07 y HU-08.** Están fuera del alcance acordado con el profesor, pero `000-core` ya prepara su base: `Devolucion`, `PoliticaDevolucion` y `IServicioReservas.CancelarPorUsuario` (HU-03); `Curso`, `Inscripcion` y la siembra de cursos (HU-04); `CierreMantenimiento`, `IConsultaCierres`, `IServicioReservas.AplicarCierre`, `IConsultaFranjasFijas.RepresentantesAfectados` e `IServicioAvisos` (HU-07); y las reglas de cómputo y el rol de técnico (HU-08). La consulta provisional de cierres (`ConsultaCierresProvisional`) se retiraría al implementar HU-07.
- **Pendientes de las historias hechas**: la comprobación manual de T058 (NVDA, Lighthouse y axe sobre los dos estados nuevos), y el escenario R19 de `quickstart.md` a mano.
- **Mejoras**: integración continua, pruebas de componentes del cliente y un recorrido de navegador automatizado.

---

## 11. Demo

La demostración en vídeo la prepara Artur (`DemoSSD_G18.zip`, formato `.mp4`, con la página de acceso al vídeo). No forma parte de este repositorio.

| Dato | Valor |
| ---- | ----- |
| Enlace | **Pendiente de Artur** |
| Tamaño del fichero `DemoSSD_G18.zip` | **Pendiente de Artur** |
| Qué muestra | Pendiente de Artur |

El enunciado limita a 100 MB el tamaño de subida de los dos ficheros zip entre los dos.

---

## 12. Anexos

### 12.1 Repositorio

<https://github.com/ara139-ua/SI-18> · rama de integración `develop` · entrega: PR #43 (`develop` → `main`).

### 12.2 Especificaciones completas

Cada especificación tiene `spec.md`, `plan.md`, `research.md`, `data-model.md` (la base común), `contracts/`, `quickstart.md`, `tasks.md`, `checklists/` y los informes del refinamiento.

| Spec | Documentos |
| ---- | ---------- |
| [000-core](../specs/000-core/) | `spec.md`, `plan.md`, `data-model.md`, `research.md`, `quickstart.md`, `tasks.md`, `clarify-diff.md`, `analyze-registro.md`, `analyze-final-2026-11-26.md`, `converge-2026-11-26.md` |
| [001-buscar-pistas](../specs/001-buscar-pistas/) | `spec.md`, `plan.md`, `research.md`, `tasks.md`, `clarify-diff.md`, `analyze-registro.md`, `analyze-final.md`, `converge-2026-11-26.md` |
| [002-reservar-pagar](../specs/002-reservar-pagar/) | `spec.md`, `plan.md`, `research.md`, `tasks.md`, `clarify-diff.md`, `clarify-funcional.md`, `analyze-registro.md`, `analyze-final-2026-11-26.md`, `converge-2026-11-26.md`, `-2.md`, `-final.md` |
| [005-franjas-fijas](../specs/005-franjas-fijas/) | `spec.md`, `plan.md`, `research.md`, `tasks.md`, `clarify-diff.md`, `analyze-registro.md`, `analyze-final.md`, `converge-2026-11-26.md` |
| [006-validar-qr](../specs/006-validar-qr/) | `spec.md`, `plan.md`, `research.md`, `tasks.md`, `clarify-diff.md`, `clarify-funcional.md`, `analyze-1.md`, `-2.md`, `-3.md`, `analyze-registro.md`, `converge-2026-11-26.md`, `-2.md`, `-3.md`, `-4.md` |

Otras evidencias: [trazabilidad.md](./trazabilidad.md), [verificacion-integrada.md](./verificacion-integrada.md), [evidencias/](./evidencias/) y la constitución en [`.specify/memory/constitution.md`](../.specify/memory/constitution.md).

### 12.3 Glosario

| Término | Significado |
| ------- | ----------- |
| Franja | Hora de una pista (una hora); se calcula, no se almacena |
| Franja fija | Horario que un club tiene aprobado para toda la temporada y que bloquea la reserva a los demás |
| Reserva pendiente de pago | Reserva creada que bloquea la franja hasta 10 minutos o hasta el inicio, lo que ocurra antes |
| Código de acceso | Texto único de una reserva confirmada que se muestra como QR y se valida en la entrada |
| Ventana de validación | Desde 15 minutos antes del inicio hasta el fin de la franja, ambos extremos incluidos |
| Tarifa | Empadronado, no empadronado o club |
| Módulo | Parte de la API con un único dueño que escribe en sus tablas |
| Problem Details | Formato de los errores de la API, con un `codigo` del catálogo |
| Spec Kit | Herramienta de desarrollo guiado por especificaciones (`specify`, `clarify`, `plan`, `tasks`, `analyze`, `implement`, `converge`) |
| `clarify`, `analyze`, `converge` | Aclarar la especificación, comprobar la coherencia entre documentos y cerrar las brechas entre documentos y código |
| WCAG 2.1 AA | Nivel de accesibilidad exigido a las administraciones públicas |
