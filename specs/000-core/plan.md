# Plan de implementación: 000-core · Núcleo común de SI-18

**Rama**: `develop` (la base común no tiene rama propia; el script de Spec Kit la identifica como
`000-core`) | **Fecha**: 2026-10-28 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/000-core/spec.md` (con las aclaraciones del 2026-10-28).

## Resumen

`000-core` es el **plan técnico compartido** de SI-18: las specs 001–008 lo referencian y no lo
redefinen (principio VII). Fija lo siguiente:

- la solución .NET en `fuentes/`, con cuatro proyectos (Dominio, Contratos, Api, Cliente) y dos de
  pruebas;
- la organización en cinco módulos con un único dueño cada uno;
- el modelo de datos completo en PostgreSQL con una única migración inicial;
- y las piezas transversales que todas las historias necesitan:
  - identificación por cookie con usuarios precargados y hash PBKDF2;
  - roles, tarifa y centro de trabajo;
  - `TimeProvider` y `HorarioMadrid` (Europe/Madrid, UTC en la base de datos);
  - `ValoresNegocio` como única fuente de las cifras;
  - Problem Details en español con un catálogo de códigos;
  - el índice único parcial para «una reserva activa por franja» y la caducidad perezosa de las
    pendientes de pago;
  - la pasarela de pago simulada;
  - «Mis avisos»;
  - la siembra ficticia idempotente;
  - docker-compose y `.env.example`.

Las decisiones y sus motivos están en [research.md](./research.md) (R1–R19).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre **.NET 10 LTS** (`net10.0`). El SDK instalado (9.0.312, STS) no
es LTS y hay que actualizarlo (R1).

**Dependencias principales**:
- ASP.NET Core Minimal API y `Microsoft.AspNetCore.Components.WebAssembly.Server` (la API sirve el
  cliente).
- Blazor WebAssembly y MudBlazor 8.x.
- EF Core 10 con `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x y `EFCore.NamingConventions`.
- `Microsoft.Extensions.Identity.Core` (solo `PasswordHasher`).
- QRCoder (en el cliente).
- `Microsoft.AspNetCore.OpenApi`.

Las versiones exactas se fijan en `fuentes/Directory.Packages.props`.

**Almacenamiento**: PostgreSQL 17 (`postgres:17-alpine` en docker-compose), con migraciones de EF
Core versionadas en `src/SI18.Api/Infraestructura/Migraciones`.

**Pruebas**:
- xUnit v3 y `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`);
- `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`);
- `Testcontainers.PostgreSql` para la integración contra PostgreSQL real (R15).

**Plataforma**: navegadores actuales (WebAssembly) y una API en Windows o Linux (Kestrel). El
desarrollo es local, con PostgreSQL en Docker.

**Tipo de proyecto**: aplicación web (SPA Blazor WASM servida por la misma API: un único origen).

**Objetivos de rendimiento**: escala municipal y de demo. Búsqueda de disponibilidad de un centro y un
día < 500 ms p95; validación de QR < 300 ms p95 (HU-06: «claro y rápido»). No se fijan objetivos de
throughput: ninguna spec los pide (principio VIII).

**Restricciones**:
- importes `decimal` / `numeric(8,2)`;
- horas evaluadas en Europe/Madrid y guardadas en UTC;
- ninguna lectura directa de `DateTime.Now` / `UtcNow` en el código de negocio (se aplica con un
  analizador: `BannedApiAnalyzers` con `BannedSymbols.txt`);
- WCAG 2.1 AA;
- sin secretos en el repositorio.

**Escala y alcance**: 4 centros, 26 instalaciones, unos 12 usuarios precargados, 8 historias, unas
15–20 pantallas en total. Cuatro desarrolladores en paralelo.

## Comprobación de la constitución

*Puerta: debe superarse antes de la Fase 0 y volver a comprobarse tras la Fase 1.*

| Principio | Cómo lo cumple el diseño | Antes de F0 | Tras F1 |
| --------- | ------------------------ | ----------- | ------- |
| I. WCAG 2.1 AA | MudBlazor con etiquetas accesibles, `lang="es"`, región `aria-live` común, enlace «Ir al contenido», tema con contraste AA y una lista de verificación en cada PR de interfaz (R16, quickstart §6) | ✅ | ✅ |
| II. Lógica probada | Reglas puras en `SI18.Dominio` con `FakeTimeProvider` y pruebas a los dos lados de cada límite, incluido el cambio de hora (R15, quickstart §5). `DateTime.Now` está prohibido por analizador | ✅ | ✅ |
| III. Reglas en el backend | Reglas en `SI18.Dominio`, que llama solo la API. Cifras en `ValoresNegocio` (R6). Concurrencia con el índice parcial `ux_reservas_activa` (R7) y `FOR UPDATE` en cursos (R19). El cliente solo muestra | ✅ | ✅ |
| IV. Errores coherentes | `ErrorNegocio` → `IExceptionHandler` → Problem Details con `codigo` y texto en español. Catálogo en [contracts/errores.md](./contracts/errores.md) (R11) | ✅ | ✅ |
| V. Sin secretos | Cadena de conexión y contraseña de demo en user-secrets o variables de entorno; `.env.example` con valores de relleno; `.env` ignorado; `git grep` antes de fusionar (R17) | ✅ | ✅ |
| VI. Datos ficticios | Siembra con nombres inventados y correos `@si18.test`; no se guardan datos de tarjeta (R9, R13) | ✅ | ✅ |
| VII. Base común en 000-core | Este plan, [data-model.md](./data-model.md) y [contracts/](./contracts/README.md) son la base. La migración inicial crea todo el esquema compartido (R12) | ✅ | ✅ |
| VIII. Cero alcance fantasma | Solo los proyectos pedidos; sin capa de infraestructura aparte, sin bus de eventos, sin E2E ni contenedor de la API. El barrido de caducidades se justifica por «la franja vuelve a estar libre» y por el informe (R8) | ✅ | ✅ |
| IX. Idioma, moneda y zona | Textos `es-ES`, `CultureInfo("es-ES")` para importes, `HorarioMadrid` y UTC en la base de datos (R5) | ✅ | ✅ |
| Pila y seguridad | Blazor WASM + MudBlazor, Minimal API, PostgreSQL con migraciones, reglas en una capa independiente de HTTP. Única desviación: la versión de .NET (10 en lugar de la 9 instalada), justificada en R1 | ✅ | ✅ |
| Colaboración | Un dueño por módulo; las escrituras entre módulos van por interfaz ([contracts/modulos.md](./contracts/modulos.md)); prefijos de ruta reservados | ✅ | ✅ |

**Resultado**: se supera sin infracciones. «Seguimiento de la complejidad» queda vacío.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/000-core/
├── spec.md
├── plan.md              # este fichero
├── research.md          # Fase 0
├── data-model.md        # Fase 1: modelo compartido por 001–008
├── quickstart.md        # Fase 1: arranque y validación
├── contracts/           # Fase 1
│   ├── README.md                # convenciones HTTP y prefijos de ruta por módulo
│   ├── api-comun.openapi.yaml   # sesión, catálogo y avisos
│   ├── errores.md               # Problem Details y catálogo de códigos
│   └── modulos.md               # interfaces C# entre módulos
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks; no la crea este comando)
```

### Código fuente (en `fuentes/`, como pide la entrega)

```text
fuentes/
├── SI18.slnx
├── global.json                      # SDK 10.0.x
├── Directory.Build.props            # net10.0, Nullable, TreatWarningsAsErrors, analizadores
├── Directory.Packages.props         # versiones centralizadas
├── BannedSymbols.txt                # prohíbe DateTime.Now/UtcNow y DateTimeOffset.Now/UtcNow
├── .config/dotnet-tools.json        # dotnet-ef
├── docker-compose.yml               # postgres:17-alpine
├── .env.example
├── src/
│   ├── SI18.Dominio/                # sin dependencias de ASP.NET Core ni EF Core
│   │   ├── Comun/                   # ValoresNegocio, HorarioMadrid, ErrorNegocio, CodigosError,
│   │   │                            # enumerados, CalculadoraTarifa, PermisosCentro,
│   │   │                            # Centro, Deporte, Instalacion, Tarifa, Usuario, Club, Temporada, Notificacion
│   │   ├── Disponibilidad/          # Franja, CalculadoraFranjas, CalculadoraPrecio, ReglaDisponibilidad, SolicitudFranjasFijas
│   │   ├── Reservas/                # Reserva, Pago, ValidacionAcceso, ReglaAcceso, IPasarelaPago, GeneradorCodigoAcceso
│   │   ├── Cancelaciones/           # Devolucion, CierreMantenimiento, PoliticaDevolucion
│   │   └── CursosInformes/          # Curso, Inscripcion, (cálculos del informe en spec-008)
│   ├── SI18.Contratos/              # DTO compartidos API ↔ cliente, por módulo (Comun/, Disponibilidad/, …)
│   ├── SI18.Api/
│   │   ├── Program.cs               # composición: autenticación, Problem Details, módulos, ficheros del cliente
│   │   ├── Infraestructura/
│   │   │   ├── Si18DbContext.cs
│   │   │   ├── Migraciones/         # 0001_NucleoComun
│   │   │   ├── InicializadorDatos.cs
│   │   │   ├── ManejadorErrores.cs  # IExceptionHandler + tabla código → HTTP
│   │   │   ├── PasarelaSimulada.cs
│   │   │   └── BarridoCaducidades.cs
│   │   └── Modulos/
│   │       ├── Comun/               # EndpointsSesion, EndpointsCatalogo, EndpointsAvisos, UsuarioActual,
│   │       │                        # ServicioAvisos, Configuraciones/ (EF)
│   │       ├── Disponibilidad/      # HU-01, HU-05 · Endpoints*, servicios, Configuraciones/
│   │       ├── Reservas/            # HU-02, HU-06
│   │       ├── Cancelaciones/       # HU-03, HU-07
│   │       └── CursosInformes/      # HU-04, HU-08
│   └── SI18.Cliente/                # Blazor WebAssembly + MudBlazor
│       ├── wwwroot/index.html       # lang="es"
│       ├── Layout/                  # MainLayout con «Ir al contenido», menú por rol
│       ├── Comun/                   # InicioSesion.razor, MisAvisos.razor, MensajeAccesible.razor,
│       │                            # ClienteApi (lee ProblemDetails); FormatoEs está en SI18.Contratos/Comun
│       ├── Disponibilidad/  Reservas/  Cancelaciones/  CursosInformes/
└── tests/
    ├── SI18.Dominio.Tests/          # unitarias; una carpeta por módulo
    └── SI18.Api.Tests/              # integración: WebApplicationFactory + Testcontainers; una carpeta por módulo
```

**Decisión de estructura**: una solución web con cuatro proyectos y la misma subdivisión por módulo
en todos ellos. Cada persona trabaja solo en las carpetas de su módulo (`*/Disponibilidad`,
`*/Reservas`, …). `Comun` y `Infraestructura` se tocan solo en `develop` y por PR revisado.
Dependencias entre proyectos:
- `Dominio` → nada;
- `Contratos` → nada;
- `Api` → `Dominio` y `Contratos`;
- `Cliente` → `Contratos`;
- `Dominio.Tests` → `Dominio`;
- `Api.Tests` → `Api`.

### Dueños de módulo

| Módulo           | Historias    | Tablas que escribe                                   | Dueño (propuesta README; definitivo en `docs/agrupamiento.md`) |
| ---------------- | ------------ | ---------------------------------------------------- | -------------- |
| `Comun`          | 000-core     | `centros`, `deportes`, `instalaciones`, `instalacion_deportes`, `tarifas`, `usuarios`, `clubes`, `temporadas`, `notificaciones` | Todo el grupo |
| `Disponibilidad` | HU-01, HU-05 | `solicitudes_franjas_fijas`                          | Artur          |
| `Reservas`       | HU-02, HU-06 | `reservas`, `pagos`, `validaciones_acceso`           | Saul           |
| `Cancelaciones`  | HU-03, HU-07 | `devoluciones`, `cierres_mantenimiento`              | Persona C      |
| `CursosInformes` | HU-04, HU-08 | `cursos` (solo la siembra), `inscripciones`          | Persona D      |

## Piezas transversales (qué entrega 000-core en `develop`)

1. **Esqueleto de la solución**: proyectos, props centralizados, analizador de `DateTime.Now` y
   `global.json`.
2. **Dominio común**:
   - `ValoresNegocio`, `HorarioMadrid`, enumerados, `ErrorNegocio` y `CodigosError`;
   - las entidades del modelo con sus métodos de transición. `Reserva` y `Pago` incluyen la máquina de
     estados completa, porque la aplican HU-02, HU-03 y HU-07.
3. **Persistencia**: `Si18DbContext`, configuraciones EF por módulo y la migración `0001_NucleoComun`
   con todos los índices y `CHECK` de [data-model.md](./data-model.md).
4. **Siembra**: `InicializadorDatos`, idempotente y con la contraseña tomada de `SI18_CLAVE_DEMO`.
5. **Identificación**: `/api/sesion` (cookie), `IUsuarioActual`, políticas por rol y `PermisosCentro`.
6. **Errores**: `ManejadorErrores` y el registro de Problem Details.
7. **Reloj**: `TimeProvider.System` registrado como singleton; las pruebas lo sustituyen.
8. **Pasarela simulada y barrido de caducidades**: se registran en 000-core; la lógica de pago y
   caducidad la completa la spec-002 sobre la máquina de estados ya probada.
9. **Avisos**: `IServicioAvisos` y `/api/avisos` (más la pantalla «Mis avisos»).
10. **Cliente base**: layout accesible, inicio de sesión, menú por rol, `ClienteApi` con lectura de
    Problem Details, `MensajeAccesible` y formato `es-ES` (`FormatoEs`, en `SI18.Contratos`).
11. **Entorno**: `docker-compose.yml`, `.env.example`, `.gitignore` y la base de pruebas de integración
    (`FabricaApi` con Testcontainers y `FakeTimeProvider`).

### Flujo de una reserva y su caducidad (referencia para las specs 002, 003 y 007)

```text
POST /api/reservas ──► BEGIN
   ├─ IConsultaDisponibilidad.MotivoNoDisponible(...)  → 422 si no está libre
   ├─ precio = IConsultaDisponibilidad.Precio(...)     → 409 PRECIO_CAMBIADO si difiere del mostrado
   ├─ UPDATE: caducar pendientes NO vigentes de esa franja (+ pago → fallido)          (R8.2)
   ├─ INSERT reserva(pendiente_pago) + pago(pendiente)
   └─ COMMIT  ── 23505 en ux_reservas_activa ──► 409 FRANJA_OCUPADA                    (R7)
POST /api/reservas/{id}/pago ──► BEGIN
   ├─ si !EstaVigente(ahora): Caducar → 409 RESERVA_CADUCADA                          (R8.3)
   ├─ IPasarelaPago.Cobrar: ok → Confirmar(código QR) · ko → Caducar → 422 PAGO_RECHAZADO
   └─ COMMIT
BarridoCaducidades (cada 60 s): Caducar las pendientes no vigentes                     (R8.4)
```

Las rutas exactas y los cuerpos de `POST /api/reservas` los define la spec-002. Aquí solo se fija la
secuencia que garantiza RF-013, RF-014 y RF-016.

## Riesgos y mitigación

| Riesgo | Mitigación |
| ------ | ---------- |
| Conflictos en `ModelSnapshot` con cuatro ramas | El esquema completo va en `0001_NucleoComun`. Si una spec necesita una migración, la rebasa sobre `develop` y la regenera antes de fusionar (R12) |
| Alguien no instala .NET 10 | `global.json` hace fallar la compilación con un mensaje claro; quickstart §1 |
| Divergencia entre buscar y reservar | Las dos usan `IConsultaDisponibilidad`, con una prueba de integración que compara el precio mostrado con el cobrado (CE-005) |
| Uso de `DateTime.Now` | `BannedApiAnalyzers` + `TreatWarningsAsErrors` |
| Escritura cruzada entre tablas | Interfaces de [contracts/modulos.md](./contracts/modulos.md) y revisión en el PR; `/speckit-converge` lo detecta |

## Seguimiento de la complejidad

No aplica: no hay infracciones de la constitución que justificar.
