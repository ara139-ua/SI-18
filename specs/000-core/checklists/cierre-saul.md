# Validación de cierre de 000-core: máquina de Saul

**Fecha**: 2026-11-25 · **Rama**: `feature/000-core-cierre-saul` (desde `develop` en `1d452d1`) ·
**Tareas**: [tasks.md](../tasks.md) T073–T075 (parte de Saul; T076 fuera de alcance)

## Entorno

| Herramienta            | Versión                                                        |
| ---------------------- | -------------------------------------------------------------- |
| Windows                | 11 Pro 10.0.26300                                              |
| SDK de .NET            | **10.0.404** (también instalado 9.0.312; `global.json` elige 10) |
| Docker                 | 29.9.2 · Docker Compose v5.6.1                                 |
| PostgreSQL (contenedor) | `postgres:17-alpine`, publicado en el **5433**                |
| Node.js                | 26.13.0 (solo para Lighthouse y axe)                           |
| Lighthouse             | 13.6.0                                                         |
| axe-core               | 4.14.1 (con `@axe-core/puppeteer` 4.14.1 y Chrome instalado)   |

## T074: quickstart.md §2–§5 (sin marcar hasta que Artur haga su parte)

### §2 Configuración local

- [x] `.env` creado a partir de `.env.example`, con todos los valores rellenos y sin versionar
  (`git check-ignore` → `fuentes/.gitignore:15:.env`).
- [x] Puerto: en esta máquina hay un PostgreSQL de Windows escuchando en el 5432, así que se aplica la
  nota de §2: `POSTGRES_PORT=5433` y `Port=5433` en la cadena de conexión.
- [x] `docker compose up -d` → `fuentes-postgres-1` **healthy** (`0.0.0.0:5433->5432/tcp`).
- [ ] ⚠️ `dotnet user-secrets --project src/SI18.Api set …` **falla**: «Could not find the global property
  'UserSecretsId' in MSBuild project». `SI18.Api.csproj` no declara `UserSecretsId`. Se ha usado la
  alternativa que documenta §2: cargar las variables de `.env` en el entorno del proceso
  (`ConnectionStrings__Si18`, `SI18_CLAVE_DEMO`). **Pendiente de decidir**: añadir `UserSecretsId` o
  quitar el paso de la guía.

### §3 Compilar, migrar, sembrar y arrancar

- [x] `dotnet build SI18.slnx` → compilación correcta, 0 advertencias, 0 errores.
- [x] `dotnet tool restore` → correcto.
- [ ] ⚠️ `dotnet ef database update --project src/SI18.Api` tal como está escrito **falla** («No password
  has been provided…»): `Si18DbContextDiseno` usa a propósito una cadena ficticia (`Database=si18_diseno`)
  y su comentario indica usar `--connection`. Con
  `dotnet ef database update --project src/SI18.Api --connection "$env:ConnectionStrings__Si18"` →
  `Applying migration '20261104144623_NucleoComun'. Done.` **Pendiente**: corregir el comando en §3.
- [x] `SI18_SEMBRAR_DATOS=true; dotnet run --project src/SI18.Api` → «Datos de demostración sembrados»;
  segundo arranque → «Los datos de demostración ya existen» (idempotente: 10 usuarios, 4 centros y
  26 instalaciones en los dos casos).
- [x] `https://localhost:7180/` → 200 `text/html` (cliente Blazor servido por la API).
- [x] `https://localhost:7180/openapi/v1.json` → 200, OpenAPI 3.1.1, «SI18.Api | v1».

### §4 Escenarios manuales V1–V10

| #   | Resultado | Detalle observado |
| --- | --------- | ----------------- |
| V1  | ✅ | 200; `rol=ciudadano`, `tipoTarifa=empadronado`; cookie `si18.sesion` con `httponly; secure; samesite=strict` |
| V2  | ✅ | 200; `tipoTarifa=no_empadronado` |
| V3  | ✅ | 200; `rol=representante_club`, `tipoTarifa=club`, `clubNombre=Club de Pádel Ejemplo` |
| V4  | ✅ | 200; `rol=personal_centro`, `centroNombre=Polideportivo Norte` |
| V5  | ✅ | Contraseña incorrecta y correo inexistente → los dos 401 `CREDENCIALES_INVALIDAS`, mismo `detail` («Correo o contraseña incorrectos.») |
| V6  | ✅ | 401 `application/problem+json`, `NO_AUTENTICADO`, «Tienes que iniciar sesión para continuar.», sin traza |
| V7  | ✅ | 4 centros, todos con `horaApertura 08:00` y `horaCierre 22:00` |
| V8  | ✅ | 16 instalaciones (8 `pista_tenis` + 8 `pista_padel`), todas `reservablePorFranjas=true` |
| V9  | ✅ | Segunda inserción → `ERROR: 23505 … violates unique constraint "ux_reservas_activa"` (dentro de una transacción con `ROLLBACK`; no quedan filas de prueba) |
| V10 | ✅ | `hash_contrasena` en Base64 de 84 caracteres con prefijo `AQAAAAI…` (formato v3 de `PasswordHasher`, PBKDF2); nunca la contraseña en claro |

### §5 Pruebas automáticas

- [x] `dotnet test SI18.slnx` (con Testcontainers, sin `SI18_TEST_CONNECTION`) → **242/242 correctas**,
  0 omitidas (Dominio 1,2 s; Api 17,4 s).
- [x] `dotnet test SI18.slnx --filter "Categoria=Limites"` → **101/101 correctas**, 0 omitidas.

## T075: revisión de secretos (quickstart.md §7)

- [x] `git ls-files fuentes/.env` → vacío (`.env` no está versionado).
- [X] `git grep -nE "Password=|SI18_CLAVE_DEMO=.+[^>]$" -- ":!*.example"` → **revisada a mano**. La
  salida no está vacía (6 coincidencias), pero se han revisado una a una y ninguna es un secreto real:

  | Fichero | Coincidencia | Por qué no es un secreto |
  | ------- | ------------ | ------------------------ |
  | `fuentes/tests/SI18.Api.Tests/Comun/ErroresTests.cs:18` | `Host=127.0.0.1;Port=1;…;Password=no-importa;Timeout=3` | Cadena de prueba hacia un puerto sin servidor, a propósito para provocar el 500 `ERROR_INTERNO` (T071). La contraseña no existe en ningún servidor. |
  | `specs/000-core/quickstart.md:29` | `Password=<la de .env>` | Marcador en la guía; el valor real solo está en el `.env` local. |
  | `specs/000-core/quickstart.md:119` | `git grep -nE "Password=\|SI18_CLAVE_DEMO=.+[^>]$" …` | Es el propio comando de §7, que se encuentra a sí mismo. |
  | `specs/000-core/tasks.md:95` | `…;Password=<cambia-esto>` | Marcador de `.env.example` citado en la tarea T008. |
  | `specs/000-core/tasks.md:96` | `SI18_CLAVE_DEMO=<cambia-esto>`, `SI18_SEMBRAR_DATOS=true` … | Mismo marcador; coincide solo porque la línea sigue después del `>`. |
  | `specs/000-core/tasks.md:213` | `SI18_CLAVE_DEMO=clave-de-prueba` | Valor ficticio que inyecta la base de pruebas de integración (T022); no es la clave de demo del `.env`. |

## T073: accesibilidad, parte automática (sin marcar hasta la revisión manual y la parte de Artur)

Con la API en marcha (`https://localhost:7180`), Chrome sin interfaz, `ciudadana.empadronada@si18.test`
para las pantallas con sesión.

| Pantalla | Lighthouse (accesibilidad) | axe (WCAG 2.x A/AA + buenas prácticas) |
| -------- | -------------------------- | -------------------------------------- |
| `InicioSesion` (`/iniciar-sesion`), estado inicial | **100** | 0 infracciones · 40 reglas superadas |
| `InicioSesion` tras credenciales incorrectas | — | 0 infracciones · 40 reglas superadas |
| `MainLayout` (`/`, con sesión) | **100** | 0 infracciones · 35 reglas superadas |
| `MisAvisos` (`/mis-avisos`, con sesión) | **100** | 0 infracciones · 35 reglas superadas |

Comprobaciones automáticas complementarias:

- **Mensaje de error**: el texto «Correo o contraseña incorrectos.» aparece en un `role="status"`,
  `aria-live="polite"`, y después el foco vuelve al campo de correo.
- **Orden de tabulación** en `/iniciar-sesion`: «Ir al contenido» → «SI-18 · Reservas deportivas» →
  «Iniciar sesión» (menú) → correo → contraseña → botón. Con sesión: «Ir al contenido» → logotipo →
  «Buscar pistas» → «Mis avisos» → «Cerrar sesión».
- **«Ir al contenido»**: con el foco de teclado sobre el enlace, Intro lleva el foco a `main#contenido`.
- `#blazor-error-ui` sigue oculto (`display:none`) durante todo el recorrido.
- Consola: solo un 404 de `/favicon.ico` y el 401 esperado de `/api/sesion/usuario` sin sesión.
- **Lo que axe deja «para revisar»**: `color-contrast` (3–4 nodos por pantalla) no se puede decidir
  automáticamente. Hay que revisarlo a mano.
- **Observación**: con el foco de teclado, el enlace «Ir al contenido» calcula `outline: none`. Alguna
  regla gana a `:focus-visible` de `app.css`. El enlace sigue siendo visible porque solo aparece con foco
  y tiene un borde de 2 px, pero no muestra el recuadro de foco común. Hay que confirmarlo a mano.

### Comprobaciones manuales pendientes (quickstart.md §6)

- [ ] Solo con teclado en `/iniciar-sesion`, `/` y `/mis-avisos`: el primer Tab muestra «Ir al contenido»,
  el orden es lógico, el foco se ve siempre (también en el enlace de salto) y no hay trampas de foco.
- [ ] Intro en «Ir al contenido»: el siguiente Tab sigue desde el contenido principal, no desde la cabecera.
- [ ] Credenciales incorrectas con NVDA (o Narrador): el error se anuncia una vez, sin mover el ratón, y
  no se transmite solo con color (hay texto e icono).
- [ ] Enviar con los campos vacíos: los mensajes «Escribe tu…» se anuncian y quedan asociados a su campo.
- [ ] Al cambiar de página (menú), el lector lee el nuevo `<h1>`.
- [ ] `/mis-avisos`: el lector anuncia «No leído» (no basta el borde), y marcar como leído se anuncia.
- [ ] Contraste de los nodos que axe deja «para revisar» (texto sobre la barra superior y botones
  deshabilitados): con axe DevTools o el selector de color de DevTools, ≥ 4,5:1.
- [ ] Zoom al 200 % y ancho de 320 px: sin desplazamiento horizontal ni texto cortado.

## Para /speckit-converge (T076)

Hallazgos de esta validación. No se han corregido aquí: no se ha cambiado quickstart.md ni el código.

1. **Patrón de quickstart.md §7**: `git grep -nE "Password=|SI18_CLAVE_DEMO=.+[^>]$" -- ":!*.example"`
   nunca puede salir vacío, porque se encuentra a sí mismo en `quickstart.md:119` y coincide también con
   los marcadores de `tasks.md` y con los valores ficticios de las pruebas (ver T075).
2. **`dotnet user-secrets` (quickstart.md §2)**: falla con «Could not find the global property
   'UserSecretsId'» porque `fuentes/src/SI18.Api/SI18.Api.csproj` no declara `UserSecretsId`.
3. **`dotnet ef database update` (quickstart.md §3)**: necesita
   `--connection "<cadena>"`. `Si18DbContextDiseno` usa a propósito una cadena ficticia
   (`Database=si18_diseno`, sin contraseña), así que el comando tal como está escrito falla con «No password
   has been provided».
4. **Foco de «Ir al contenido» sin recuadro** (`MainLayout`, Artur): con foco de teclado
   (`:focus-visible` = true), el enlace `.salto-contenido` calcula `outline: none`. Probablemente una regla
   de MudBlazor pisa el `:focus-visible` de `app.css`. Se ve gracias a su borde, pero no lleva el recuadro
   de foco común.
