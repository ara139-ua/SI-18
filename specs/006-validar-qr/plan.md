# Plan de implementación: 006 · Validar el acceso con el código QR (HU-06)

**Rama**: `feature/006-validar-qr` | **Fecha**: 2026-11-26 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/006-validar-qr/spec.md` (con las aclaraciones del 2026-11-26), la
base técnica común de [`000-core/plan.md`](../000-core/plan.md) y lo que spec-002 ya dejó en `develop`
([`002-reservar-pagar/plan.md`](../002-reservar-pagar/plan.md)).

## Resumen

HU-06 permite al **personal de un centro validar el código de acceso** de una reserva en la entrada. No
crea tablas, migraciones, estados ni códigos de error: `validaciones_acceso`, `ReglaAcceso`, los motivos
`QR_*` y el índice único de `codigo_acceso` ya están en 000-core. HU-06 los compone:

- **Backend** (módulo `Reservas`, dueño: Saul):
  - un endpoint, `POST /api/validaciones-acceso`, solo para el personal de centro (V1, V7);
  - `ServicioValidarAcceso`: busca la reserva por código, comprueba el centro, aplica `ReglaAcceso` y
    registra el intento en un solo `SaveChanges` (V3, V4);
  - si se acepta, devuelve titular, instalación y franja, y la hora de la validación aceptada anterior
    si la hay, calculada con `validaciones_acceso` (V5); si se rechaza, Problem Details del catálogo con
    la validación registrada y, solo con dos motivos, la instalación y la franja (V2).
- **Cliente**: la pantalla `/validar-acceso`, con entrada manual del código, resultado con texto e icono
  anunciado una vez y foco de vuelta al campo (V10); el enlace del menú y la lectura de las extensiones
  del Problem Details son cambios pequeños en código común (C1, C2).

Las decisiones y sus motivos están en [research.md](./research.md) (V1–V12).

## Contexto técnico

Todo es igual que en [000-core/plan.md](../000-core/plan.md#contexto-técnico); no hay dependencias nuevas.

- **Lenguaje y versión**: C# 14 sobre .NET 10 (`net10.0`).
- **Dependencias principales**: ASP.NET Core Minimal API, EF Core 10 con Npgsql, Blazor WebAssembly y
  MudBlazor 8.15. Ningún paquete nuevo (sin librería de cámara: aclaración 1).
- **Almacenamiento**: PostgreSQL 17. **Sin migraciones nuevas** ([data-model.md](./data-model.md)).
- **Pruebas**: xUnit v3, `FakeTimeProvider` y `FabricaApi` con Testcontainers (000-core, R15), y
  `DatosReservas` de spec-002 sin cambiarlo.
- **Plataforma**: navegadores actuales, sobre todo móviles (WebAssembly), y una API en Kestrel.
- **Tipo de proyecto**: aplicación web (SPA servida por la API).
- **Objetivos de rendimiento**: validación de QR < 300 ms p95 (000-core); CE-001 < 1 s (V12).
- **Restricciones**:
  - la decisión, en el backend y con la regla de 000-core (principio III);
  - hora de `TimeProvider`, leída una vez por operación; fechas y horas con `HorarioMadrid` (zona de
    `ValoresNegocio`), nunca `"Europe/Madrid"` a mano (principios II y IX);
  - una fila de `validaciones_acceso` por intento, en la misma operación que decide (RF-008, RF-009);
  - la reserva no se modifica (RF-010);
  - WCAG 2.1 AA, desde 320 px de ancho.
- **Escala y alcance**: 4 centros, una persona del personal por centro en la siembra, una validación por
  persona que entra. Una pantalla y un endpoint.

## Comprobación de la constitución

*Puerta: debe superarse antes de la Fase 0 y volver a comprobarse tras la Fase 1.*

| Principio | Cómo lo cumple el diseño | Antes de F0 | Tras F1 |
| --------- | ------------------------ | ----------- | ------- |
| I. WCAG 2.1 AA | Campo con etiqueta visible y ayuda; error de campo con el patrón `aria-errormessage` de spec-002 (MudBlazor 8.15); resultado con texto **e** icono, anunciado una sola vez con `MensajeAccesible`; foco de vuelta al campo; una columna sin desplazamiento a 320 px; estilos en `ValidarAcceso.razor.css`; comprobación de quickstart §5 en el PR (V10) | ✅ | ✅ |
| II. Lógica probada | `ReglaAcceso` ya tiene sus límites probados en 000-core. HU-06 añade pruebas de integración de cada motivo, del orden, del registro (y de su ausencia con 400/401/403), del aviso y, con `Categoria=Limites`, de la ventana (15 y 14 min antes, 1 s antes, fin, 1 s y 1 min después) y de los dos cambios de hora, con reloj propio (V11) | ✅ | ✅ |
| III. Reglas en el backend | La API decide con `ReglaAcceso` y la comprobación de centro en un único servicio; el cliente solo impide enviar el campo vacío y muestra lo que recibe (V3, V6) | ✅ | ✅ |
| IV. Errores coherentes | Solo códigos del catálogo de 000-core, **ninguno nuevo**, con su estado HTTP (`QR_OTRO_CENTRO` 403, el resto 422) y sus textos; las extensiones `validacionId` y `reserva` no cambian el formato (V2) | ✅ | ✅ |
| V. Sin secretos | Nada nuevo que configurar | ✅ | ✅ |
| VI. Datos ficticios | Usa la siembra de 000-core; las pruebas insertan sus reservas con usuarios ficticios | ✅ | ✅ |
| VII. Base común en 000-core | Sin entidades, estados, valores, índices ni códigos nuevos; `ReglaAcceso` y `ValidacionAcceso` se usan sin cambiarlos ([contracts/modulos.md](./contracts/modulos.md)). La comprobación de centro va en Reservas, como dice la propia `ReglaAcceso` (V3; C3 queda como propuesta no incluida) | ✅ | ✅ |
| VIII. Cero alcance fantasma | Sin cámara, sin historial en pantalla, sin estado «utilizada», sin límite de intentos, sin bloqueo para el aviso (aclaraciones 1 y 2; V5, V12) | ✅ | ✅ |
| IX. Idioma, moneda y zona | Textos en español; fechas y horas de Madrid calculadas por la API con `HorarioMadrid`; cambios de hora probados (V8) | ✅ | ✅ |
| Pila y seguridad | La pila de 000-core, sin cambios; política `EsPersonal`; el código viaja en el cuerpo de un `POST`, no en la URL; los rechazos no revelan el titular ni datos de otro centro (V1, V2, V7) | ✅ | ✅ |
| Colaboración | Escribe en `*/Reservas` (de Saul). Toca código común **solo** en lo pactado, con revisión de @ara139-ua: el enlace del menú (C1) y la lectura de extensiones en `ErrorApi`/`ClienteApi` (C2). `Program.cs` no cambia: `AddReservas()`/`MapReservas()` ya están. A C se le avisa de que no tiene que hacer nada para invalidar códigos (C4) | ✅ | ✅ |

**Resultado**: se supera sin infracciones. «Seguimiento de la complejidad» queda vacío.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/006-validar-qr/
├── spec.md
├── plan.md              # este fichero
├── research.md          # Fase 0: decisiones V1–V12
├── data-model.md        # Fase 1: qué lee y escribe HU-06 y DTO (sin tablas nuevas)
├── quickstart.md        # Fase 1: validación de principio a fin
├── contracts/
│   ├── api-validaciones-acceso.openapi.yaml   # POST /api/validaciones-acceso
│   └── modulos.md                             # qué usa de 000-core, qué no y cambios en código común
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks; no la crea este comando)
```

### Código fuente

```text
fuentes/
├── src/
│   ├── SI18.Contratos/Reservas/                       # V9 (nuevos)
│   │   ├── PeticionValidacionAcceso.cs
│   │   ├── ValidacionAccesoDto.cs
│   │   └── ReservaValidadaDto.cs
│   ├── SI18.Api/Modulos/Reservas/
│   │   ├── RegistroReservas.cs                        # + AddScoped<ServicioValidarAcceso> y MapEndpointsValidaciones (V1)
│   │   ├── EndpointsValidacionesAcceso.cs             # POST /api/validaciones-acceso, EsPersonal (nuevo; V1, V2, V6, V7)
│   │   └── ServicioValidarAcceso.cs                   # decisión + registro + aviso (nuevo; V3–V5, V8)
│   └── SI18.Cliente/
│       ├── Layout/MainLayout.razor                    # + «Validar acceso» en el hueco del personal ⚠ C1 (común)
│       ├── Comun/ErrorApi.cs                          # + Extensiones ⚠ C2 (común)
│       ├── Comun/ClienteApi.cs                        # rellena Extensiones al leer el Problem Details ⚠ C2 (común)
│       └── Reservas/
│           ├── AccesoReservas.cs                      # + ComprobarPersonal (V10)
│           ├── ValidarAcceso.razor                    # /validar-acceso (nuevo; V10)
│           └── ValidarAcceso.razor.css                # estilos propios, sin tocar app.css (nuevo)
└── tests/SI18.Api.Tests/
    ├── Reservas/                                      # V11 (nuevos)
    │   ├── DatosValidaciones.cs                       # titular, personal, pista y fechas propios; ConServicio con reloj propio
    │   ├── ValidarAccesoTests.cs                      # HTTP: roles, 400, cada motivo, extensiones, filas registradas y RF-009
    │   ├── LimitesValidacionTests.cs                  # [Trait("Categoria","Limites")]: ventana y cambios de hora
    │   └── AvisoValidacionAnteriorTests.cs            # RF-006a
    └── Comun/ClienteApiExtensionesTests.cs            # C2: ErrorApi.Extensiones (nuevo; análisis F2)
```

**Decisión de estructura**: la de 000-core, sin proyectos nuevos. El código nuevo va en las carpetas
`Reservas` de cada proyecto (dueño: Saul). Fuera de ellas se toca solo el código común de C1 y C2, con
la revisión de @ara139-ua. No se añaden pruebas en `SI18.Dominio.Tests`: `ReglaAcceso` y
`ValidacionAcceso` ya están probadas en 000-core.

## Flujo de una validación

```text
/validar-acceso (personal de centro) ──[Validar]──►
POST /api/validaciones-acceso { codigo }                                       [EsPersonal]
   ├─ sin sesión → 401 · otro rol → 403 ROL_NO_PERMITIDO                       (sin registro)
   ├─ codigo.Trim() vacío → 400 PETICION_INVALIDA errors.codigo                 (sin registro)
   ├─ ahora ← TimeProvider (una vez)
   ├─ reserva ← reservas WHERE codigo_acceso = codigo (+ instalación, centro, titular)   AsNoTracking
   ├─ motivo:  null → QR_DESCONOCIDO
   │           centro ≠ IUsuarioActual.CentroId → QR_OTRO_CENTRO
   │           ReglaAcceso.MotivoRechazo(reserva, ahora) → QR_RESERVA_NO_CONFIRMADA | QR_FUERA_DE_VENTANA | null
   ├─ aceptada: anterior ← MAX(fecha_utc) validaciones_acceso WHERE reserva_id AND aceptada
   ├─ INSERT validaciones_acceso · SaveChanges (uno)                            (RF-008, RF-009)
   ├─ aceptada  → 201 ValidacionAccesoDto (+ validacionAnteriorHora)
   └─ rechazada → Problem Details del catálogo (403/422) + validacionId (+ reserva sin titular con dos motivos)
cliente: resultado con texto e icono · MensajeAccesible (una vez) · campo vacío y con el foco
```

## Puntos a coordinar

| # | Con quién | Qué | Propuesta | Cuándo |
| - | --------- | --- | --------- | ------ |
| C1 | **@ara139-ua** (revisión de código común) | Enlace del menú | Una línea en el hueco del personal de `MainLayout.razor`: `<MudButton Href="/validar-acceso" Color="Color.Inherit" Variant="Variant.Text">Validar acceso</MudButton>` dentro de `<AuthorizeView Roles="personal_centro">`, como «Mis reservas» en spec-002. Deja sitio para el enlace de cierres de spec-007 | PR de pantallas |
| C2 | **@ara139-ua** (revisión de código común) | Leer las extensiones de un Problem Details en el cliente | `ErrorApi.Extensiones` (`IReadOnlyDictionary<string, JsonElement>`, vacío por defecto) que rellena `ClienteApi.LeerProblemaAsync`. Aditivo: las pantallas existentes no cambian. Con una prueba que lo fije (research.md V2) | PR de pantallas, antes de `ValidarAcceso.razor` |
| C3 | Grupo (000-core) | Comprobación de centro dentro de `ReglaAcceso` | **No se propone ahora** (principio VIII): queda anotado por si otra historia necesita la misma regla (V3) | — |
| C4 | **C** (HU-03, HU-07) | Invalidación del código | Nada que hacer: una reserva cancelada se rechaza sola (RF-012). Aviso al grupo de la dependencia ([contracts/modulos.md](./contracts/modulos.md)) | Al publicar este plan |

## Reparto en dos PR (como spec-002)

| PR | Contenido | Revisión |
| -- | --------- | -------- |
| **PR de API** | DTO (`SI18.Contratos/Reservas`), `ServicioValidarAcceso`, `EndpointsValidacionesAcceso`, registro en `RegistroReservas`, y las pruebas `DatosValidaciones`, `ValidarAccesoTests`, `LimitesValidacionTests` y `AvisoValidacionAnteriorTests`. Sin código común | @ara139-ua (principios I–IX) |
| **PR de pantallas** | C2 (`ErrorApi`/`ClienteApi`, con su prueba), `AccesoReservas.ComprobarPersonal`, `ValidarAcceso.razor` y `.razor.css`, C1 (`MainLayout.razor`), con la comprobación de accesibilidad de quickstart §5 (Lighthouse, axe, teclado, NVDA y 320 px) en la descripción | @ara139-ua (código común C1 y C2) |

El PR de API se puede fusionar antes: el endpoint se prueba con quickstart §3 sin pantalla.

## Dependencias y riesgos

| Dependencia o riesgo | Mitigación |
| -------------------- | ---------- |
| Las pruebas pisan datos de otras specs (lo del #37) | Titular, personal, pista y fechas que no usa ninguna otra prueba (research.md V11); se repite el `git grep` antes de escribirlas; ninguna prueba adelanta `fabrica.Reloj` ni cuenta filas globales |
| El reloj compartido de `FabricaApi` deja una reserva fuera de su ventana | Los límites usan el servicio con reloj propio; las pruebas HTTP con reserva en ventana comparten una sola reserva, creada de forma idempotente en la hora en punto actual de la fábrica (analyze-1, C1) |
| C2 (código común) se retrasa | El PR de API no depende de C2; la pantalla puede mostrar solo el `detail` hasta que se fusione, y se completa después |
| Se muestra el titular en un rechazo por error | `ReservaValidadaDto` no tiene campo de titular; prueba de que `QR_OTRO_CENTRO` y `QR_DESCONOCIDO` no llevan la extensión `reserva` (CE-003) |
| Zona horaria escrita a mano | Fechas y horas con `HorarioMadrid` en la API; `git grep "Europe/Madrid"` en quickstart §2 |
| Doble anuncio del resultado con lector de pantalla | `MensajeAccesible` como única región viva; el panel del resultado no es región viva; prueba con NVDA en quickstart §5 |
| Dos validaciones simultáneas del mismo código sin aviso | Aceptado: el aviso es una ayuda (aclaración 2, V5) |

## Seguimiento de la complejidad

No aplica: no hay infracciones de la constitución que justificar.
