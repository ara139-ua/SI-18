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

### Comprobaciones manuales (quickstart.md §6) · resultado de Saul, 2026-11-25

Resumen: **pasan 2** (2 y 7), **fallan 2** (1 y 8, código de Artur) y **4 no se han podido verificar**
(3–6: el lector de pantalla no emite voz en esta máquina). T073 sigue sin marcar.

- [ ] ❌ **(1) FALLA** · Solo con teclado en `/iniciar-sesion`, `/` y `/mis-avisos`: el primer Tab muestra
  «Ir al contenido», el orden es lógico, el foco se ve siempre (también en el enlace de salto) y no hay
  trampas de foco. **Resultado manual (Saul, 2026-11-25)**: el foco **no se ve en ningún elemento
  interactivo de ninguna pantalla**, no solo en «Ir al contenido». Causa y propuesta: hallazgo 5 de
  «Para /speckit-converge».
- [x] ✅ **(2)** Intro en «Ir al contenido»: el siguiente Tab sigue desde el contenido principal, no desde la cabecera.
- [ ] **(3)** Credenciales incorrectas con NVDA (o Narrador): el error se anuncia una vez, sin mover el ratón, y
  no se transmite solo con color (hay texto e icono). ⚠️ **No verificada**: el lector de pantalla no emite voz en esta máquina (Saul, 2026-11-25). No se atribuye a la aplicación; hay que repetirla en otra máquina o con Narrador.
- [ ] **(4)** Enviar con los campos vacíos: los mensajes «Escribe tu…» se anuncian y quedan asociados a su campo.
  ⚠️ **No verificada**: el lector de pantalla no emite voz en esta máquina (Saul, 2026-11-25). No se atribuye a la aplicación; hay que repetirla en otra máquina o con Narrador.
- [ ] **(5)** Al cambiar de página (menú), el lector lee el nuevo `<h1>`. ⚠️ **No verificada**: el lector de pantalla no emite voz en esta máquina (Saul, 2026-11-25). No se atribuye a la aplicación; hay que repetirla en otra máquina o con Narrador.
- [ ] **(6)** `/mis-avisos`: el lector anuncia «No leído» (no basta el borde), y marcar como leído se anuncia.
  ⚠️ **No verificada**: el lector de pantalla no emite voz en esta máquina (Saul, 2026-11-25). No se atribuye a la aplicación; hay que repetirla en otra máquina o con Narrador.
- [x] ✅ **(7)** Contraste de los nodos que axe deja «para revisar» (texto sobre la barra superior y botones
  deshabilitados): con axe DevTools o el selector de color de DevTools, ≥ 4,5:1.
- [ ] ❌ **(8) FALLA** · Zoom al 200 % y ancho de 320 px: sin desplazamiento horizontal ni texto cortado.
  **Resultado manual (Saul, 2026-11-25)**: en `/mis-avisos` y `/buscar-pistas`, con zoom al 200 % y a
  320 px, **el menú de la barra superior se corta** (no es una barra de desplazamiento horizontal): el
  nombre y «Cerrar sesión» quedan fuera. Sigue igual en la segunda revisión. Causa y propuesta: hallazgo
  6 de «Para /speckit-converge».

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
   de foco común. **Ampliado y confirmado en el hallazgo 5.**
5. **❌ El foco no se ve en ningún control** (T073, comprobación manual 1; WCAG 2.4.7; principio I).
   Código de Artur: **no se ha corregido**, se decide con él.
   - **Fichero y regla**: `fuentes/src/SI18.Cliente/wwwroot/css/app.css`, líneas 109–112:
     `:focus-visible { outline: 3px solid #0B4F8A; outline-offset: 2px; }`. Su especificidad es (0,1,0) y
     `MudBlazor.min.css` (que se carga antes, `index.html`) tiene reglas más específicas que la anulan.
     Comprobado en el CSS que sirve la API (`/_content/MudBlazor/MudBlazor.min.css`):

     | Regla de MudBlazor | Especificidad | Qué elementos pierden el recuadro |
     | ------------------ | ------------- | --------------------------------- |
     | `a:focus-visible{outline:none}` | (0,1,1) | Enlaces: «Ir al contenido», el título de la barra y los `MudButton` con `Href` («Buscar pistas», «Mis avisos») |
     | `button:focus{outline:none}` | (0,1,1) | Todos los `MudButton` y `MudIconButton`: «Cerrar sesión», «Iniciar sesión», «Buscar pistas», «Marcar como leído», el icono del calendario |
     | `.mud-input>input.mud-input-root:focus{…outline:none}` y `div.mud-input-slot.mud-input-root:focus{…}` | (0,3,1) y (0,3,1) | Campos de texto (correo, contraseña, fecha) y `MudSelect` (deporte, centro) |

     En el navegador, con el primer Tab sobre «Ir al contenido», el estilo calculado es `outline: none`
     aunque `:focus-visible` es verdadero (ver hallazgo 4). MudBlazor solo cambia el **fondo** de algunos
     botones con foco (`.mud-button:focus-visible`), con un contraste muy bajo; en los botones de la barra
     (texto blanco sobre `#0B4F8A`) apenas se nota.
   - **Problema añadido**: aunque se arregle la especificidad, el recuadro es `#0B4F8A`, el mismo color que
     el fondo de la barra superior (`Tema.cs`, `AppbarBackground`). En los botones del menú y en «Ir al
     contenido» (cuyo recuadro, con `outline-offset: 2px`, cae sobre la barra) seguiría sin verse.
   - **Cambio mínimo propuesto** (solo `app.css`):

     ```css
     /* Foco siempre visible (quickstart.md §6): !important para ganar a MudBlazor
        (a:focus-visible, button:focus, .mud-input-root:focus). */
     :focus-visible {
         outline: 3px solid #0B4F8A !important;
         outline-offset: 2px !important;
     }

     /* Sobre la barra superior (#0B4F8A) el recuadro va en blanco (contraste 8,3:1). */
     .mud-appbar :focus-visible,
     .salto-contenido:focus-visible {
         outline-color: #FFFFFF !important;
     }

     h1:focus,
     main:focus,
     .contenedor-avisos:focus {
         outline: none !important;   /* se mantiene la excepción actual: (0,1,1) > (0,1,0) */
     }
     ```

     Las dos últimas reglas ya existen (líneas 116–119 y 137–139); solo se les añade `!important` para
     que la excepción siga ganando a la nueva regla. Después, repetir la comprobación manual 1 en todas
     las pantallas.
6. **❌ La barra superior se sale de la pantalla a 320 px** (T073, comprobación manual 8; WCAG 1.4.10).
   Código de Artur (`MainLayout.razor` y `app.css`): **no se ha corregido**, se decide con él.
   - **Qué se ha medido** (Chrome, iframes de 320 y 640 px de ancho con sesión de
     `ciudadana.empadronada@si18.test`):
     - a **320 px**, en `/mis-avisos` y `/buscar-pistas`, `nav.menu-principal` mide 338 px y su borde
       derecho queda en 459 px: «Ana Ciudadana…» y «Cerrar sesión» quedan fuera de la pantalla y el título
       «SI-18 · Reservas deportivas» se parte en tres líneas recortadas por la altura de la barra;
     - el contenido de `<main>` cabe en los dos anchos: ningún elemento pasa del borde derecho;
     - a **640 px** (200 % sobre 1280 px) no se desborda nada en ninguna de las dos pantallas.
   - **Fichero y regla**:
     - `MainLayout.razor`: `MudAppBar` (fija, `position: fixed`, con la altura fija de `.mud-toolbar`)
       contiene en una sola fila el `MudLink` del título (`Typo.h6`) y `<nav class="menu-principal">`
       con tres botones, el nombre y «Cerrar sesión»;
     - `app.css`, líneas 122–126: `.menu-principal { display: flex; align-items: center; gap: 0.5rem; }`,
       **sin `flex-wrap`**, así que la fila no se parte y desborda la barra.
   - **Nota**: como la barra es `position: fixed`, el documento no llega a desplazarse
     (`scrollWidth = clientWidth`): lo que desborda queda cortado e inalcanzable, que incumple 1.4.10.
     La revisión manual lo confirma: con zoom al 200 % el menú se corta, sin barra de desplazamiento. En la
     medición a 640 px no se cortaba, así que depende del ancho real de la ventana al 200 %.
   - **Cambio mínimo propuesto** (a validar con Artur en el navegador, porque toca la altura de la barra):

     ```css
     /* Menú principal (T056): en pantallas estrechas, la barra crece y el menú pasa a otra línea (WCAG 1.4.10). */
     .menu-principal {
         display: flex;
         flex-wrap: wrap;
         justify-content: flex-end;
         align-items: center;
         gap: 0.5rem;
     }

     @media (max-width: 40em) {
         .mud-appbar { position: static; }               /* deja de tapar el contenido al crecer */
         .mud-appbar .mud-toolbar { height: auto; flex-wrap: wrap; padding-block: 0.5rem; }
         .mud-main-content { padding-top: 0; }           /* el hueco de la barra fija ya no hace falta */
     }
     ```

     `.mud-main-content` siempre reserva arriba la altura de la barra (`padding-top` en
     `MudBlazor.min.css`), por eso se anula en la media query. Alternativa en `MainLayout.razor`:
     `<MudAppBar Fixed="false">` (barra no fija en todos los anchos), con las mismas reglas de `flex-wrap`,
     `height: auto` y `padding-top: 0`. En los dos casos, repetir la comprobación manual 8 a 320 px y con
     zoom al 200 %.
