# Plan de implementación: 001 · Buscar pistas libres (HU-01)

**Rama**: `feature/001-buscar-pistas` | **Fecha**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/001-buscar-pistas/spec.md` (con las aclaraciones del
2026-11-25) y la base técnica común de [`000-core/plan.md`](../000-core/plan.md).

## Resumen

HU-01 añade una consulta: **qué pistas de un deporte tiene libres un centro en una fecha, y a qué
precio para quien busca**. No crea tablas ni cambia estados. Se apoya en la base de 000-core:

- **Backend** (módulo `Disponibilidad`):
  - un endpoint `GET /api/disponibilidad/franjas` (D1, D2);
  - un `ServicioDisponibilidad` que compone las reglas puras de 000-core (`CalculadoraFranjas`,
    `ReglaDisponibilidad`, `CalculadoraPrecio`) y también implementa `IConsultaDisponibilidad` para
    HU-02, con el mismo código (D3, D4);
  - tres consultas **provisionales** de solo lectura (`IConsultaReservas`, `IConsultaCierres`,
    `IConsultaFranjasFijas`) mientras sus dueños no publiquen las suyas (D5, RF-014a).
- **Cliente**: la pantalla `/buscar-pistas`, accesible, con los tres filtros obligatorios, las franjas
  libres agrupadas por pista con su precio y los mensajes anunciados (D7, D8).

Las decisiones y sus motivos están en [research.md](./research.md) (D1–D10).

## Contexto técnico

Todo es igual que en [000-core/plan.md](../000-core/plan.md#contexto-técnico); no hay dependencias nuevas.

- **Lenguaje y versión**: C# 14 sobre .NET 10 (`net10.0`).
- **Dependencias principales**: ASP.NET Core Minimal API, EF Core 10 con Npgsql, Blazor WebAssembly y
  MudBlazor 8.x. Ningún paquete nuevo.
- **Almacenamiento**: PostgreSQL 17. **Sin migraciones nuevas** (solo lectura; [data-model.md](./data-model.md)).
- **Pruebas**: xUnit v3, `FakeTimeProvider` y `FabricaApi` con Testcontainers (000-core, R15).
- **Plataforma**: navegadores actuales (WebAssembly) y una API en Kestrel.
- **Tipo de proyecto**: aplicación web (SPA servida por la API).
- **Objetivos de rendimiento**: búsqueda de un centro y un día < 500 ms p95 (000-core); CE-002 de la
  spec (< 2 s) queda holgado (D10).
- **Restricciones**:
  - reglas en el backend (principio III);
  - hora de `TimeProvider` y `HorarioMadrid` (principio IX);
  - importes `decimal`;
  - WCAG 2.1 AA;
  - ninguna escritura en tablas de otros módulos (RF-014).
- **Escala y alcance**: 4 centros; como mucho 2 pistas por deporte y centro en la siembra; 14 franjas
  por pista y día. Una pantalla y un endpoint.

## Comprobación de la constitución

*Puerta: debe superarse antes de la Fase 0 y volver a comprobarse tras la Fase 1.*

| Principio | Cómo lo cumple el diseño | Antes de F0 | Tras F1 |
| --------- | ------------------------ | ----------- | ------- |
| I. WCAG 2.1 AA | Filtros con `Label` visible, `MensajeAccesible` (`aria-live`) para el resultado y el «sin resultados», una sección por pista con `<h2>` y lista con `role="list"`, precio y horas en texto, y comprobación de quickstart §5 en el PR (D7) | ✅ | ✅ |
| II. Lógica probada | Las reglas de dominio ya tienen pruebas de límites en 000-core. HU-01 añade pruebas de integración de los escenarios, de los límites (7 días, inicio, 10 min, cambio de hora) y de las consultas provisionales (D9) | ✅ | ✅ |
| III. Reglas en el backend | El cálculo vive en `ServicioDisponibilidad`, que compone las reglas de `SI18.Dominio`. La API valida la fecha. El cliente solo muestra y limita el selector como ayuda (D3, D8) | ✅ | ✅ |
| IV. Errores coherentes | Solo códigos existentes del catálogo (`PETICION_INVALIDA`, `NO_AUTENTICADO`, `ROL_NO_PERMITIDO`, `NO_ENCONTRADO`, `FUERA_DE_ANTELACION`); ninguno nuevo (D2) | ✅ | ✅ |
| V. Sin secretos | Nada nuevo que configurar | ✅ | ✅ |
| VI. Datos ficticios | Usa la siembra de 000-core; las pruebas insertan reservas, cierres y solicitudes ficticias | ✅ | ✅ |
| VII. Base común en 000-core | Sin entidades, estados, valores ni códigos nuevos; implementa `IConsultaDisponibilidad` con la firma de 000-core ([contracts/modulos.md](./contracts/modulos.md)) | ✅ | ✅ |
| VIII. Cero alcance fantasma | Un endpoint y una pantalla. Sin caché, sin «todos los centros», sin botón «Reservar» (lo añade spec-002). Las provisionales tienen una justificación explícita (aclaración 1) y se retiran | ✅ | ✅ |
| IX. Idioma, moneda y zona | Textos en español; `horaInicio` y `horaFin` calculadas en Madrid por la API; precio con `FormatoEs.Importe`; «hoy» en Madrid | ✅ | ✅ |
| Pila y seguridad | La pila de 000-core, sin cambios; política `EsCiudadanoORepresentante` en el endpoint | ✅ | ✅ |
| Colaboración | Solo escribe en `*/Disponibilidad`. Las provisionales están en la carpeta de Disponibilidad y se avisan en las specs 002, 005 y 007. Las dos líneas de `Program.cs` se proponen como convención en el PR (D6) | ✅ | ✅ |

**Resultado**: se supera sin infracciones. «Seguimiento de la complejidad» queda vacío. Las dos
líneas en `Program.cs` (D6) no infringen la constitución: la regla de no tocarlo era una tarea de
000-core (T009), y se somete al grupo en el PR.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/001-buscar-pistas/
├── spec.md
├── plan.md              # este fichero
├── research.md          # Fase 0: decisiones D1–D10
├── data-model.md        # Fase 1: qué lee HU-01 y el modelo de lectura (sin tablas nuevas)
├── quickstart.md        # Fase 1: validación de principio a fin
├── contracts/
│   ├── api-disponibilidad.openapi.yaml   # GET /api/disponibilidad/franjas
│   └── modulos.md                        # qué interfaces de 000-core implementa y consume
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks; no la crea este comando)
```

### Código fuente (solo las carpetas de Disponibilidad, más dos líneas en `Program.cs` y una entrada de menú)

```text
fuentes/
├── src/
│   ├── SI18.Contratos/Disponibilidad/
│   │   ├── ResultadoBusquedaDto.cs
│   │   ├── PistaLibreDto.cs
│   │   └── FranjaLibreDto.cs
│   ├── SI18.Api/
│   │   ├── Program.cs                          # + AddDisponibilidad() y MapDisponibilidad() (D6)
│   │   └── Modulos/Disponibilidad/
│   │       ├── RegistroDisponibilidad.cs       # AddDisponibilidad / MapDisponibilidad
│   │       ├── ServicioDisponibilidad.cs       # búsqueda + IConsultaDisponibilidad (D3, D4)
│   │       ├── EndpointsDisponibilidad.cs      # GET /api/disponibilidad/franjas (D1, D2)
│   │       └── Provisional/                    # D5 · RF-014a · se retiran cuando cada dueño publique la suya
│   │           ├── ConsultaReservasProvisional.cs
│   │           ├── ConsultaCierresProvisional.cs
│   │           └── ConsultaFranjasFijasProvisional.cs
│   └── SI18.Cliente/
│       ├── Layout/MainLayout.razor             # + «Buscar pistas» en el hueco ciudadano/representante
│       └── Disponibilidad/
│           └── BuscarPistas.razor              # /buscar-pistas (D7, D8)
└── tests/SI18.Api.Tests/Disponibilidad/
    ├── BuscarFranjasTests.cs
    ├── PrecioPorTarifaTests.cs
    ├── ConsultaDisponibilidadTests.cs
    └── ConsultasProvisionalesTests.cs
```

**Decisión de estructura**: la de 000-core, sin proyectos nuevos. Todo el código nuevo va en las
carpetas `Disponibilidad` de cada proyecto (dueño: Artur). Fuera de ellas solo se tocan dos líneas de
`Program.cs` (D6) y una entrada en el hueco de menú que `MainLayout` reservó para este rol (T056 de
000-core). No se añaden pruebas en `SI18.Dominio.Tests`: las reglas que usa HU-01 ya están probadas.

## Flujo de una búsqueda

```text
GET /api/disponibilidad/franjas?deporteId&centroId&fecha   [EsCiudadanoORepresentante]
   ├─ falta un parámetro / formato incorrecto                  → 400 PETICION_INVALIDA
   ├─ fecha ∉ [hoyMadrid, hoyMadrid + 7]                       → 422 FUERA_DE_ANTELACION
   ├─ deporte o centro inexistente                             → 404 NO_ENCONTRADO
   ├─ pistas = instalaciones(centro, reservables, con deporte) → totalPistas
   ├─ tarifas = todas (1 consulta) · tarifa = IUsuarioActual.TipoTarifa
   └─ por pista:
        franjas  = CalculadoraFranjas.FranjasDelDia(pista, fecha)
        cierres  = IConsultaCierres.Cierres(pista, díaUtc)
        bloqueos = IConsultaFranjasFijas.FranjasBloqueadas(pista, fecha, fecha)
        ocupadas = IConsultaReservas.Activas(pista, díaUtc).InicioUtc
        libres   = franjas donde ReglaDisponibilidad.MotivoNoDisponible(…, ahora) == null
        precio   = CalculadoraPrecio.Precio(pista.Tipo, tarifa, tarifas)
   → 200 ResultadoBusquedaDto (pistas sin franjas libres omitidas; orden por nombre y hora)

HU-02 ──► IConsultaDisponibilidad.MotivoNoDisponible / Precio ──► mismo «por pista» para una franja
```

## Dependencias y riesgos

| Dependencia o riesgo | Mitigación |
| -------------------- | ---------- |
| Los filtros necesitan `/api/centros`, `/api/deportes` e `/api/instalaciones` (T054 de 000-core, de Saul), que aún no están en `develop` | La API de HU-01 no depende de T054 y se construye y prueba antes. La pantalla se termina cuando T054 esté fusionado; si se retrasa, se avisa a Saul o se acuerda que Artur la haga |
| `Program.cs` no tiene punto de entrada para los módulos | Dos líneas (`AddDisponibilidad`, `MapDisponibilidad`) propuestas como convención para 002–008 en el PR; revisa Saul (T072) (D6) |
| Las consultas provisionales se quedan para siempre o divergen de las definitivas | Comentario «PROVISIONAL» y pruebas de integración que fijan su comportamiento (D9). Aviso en las specs 002, 005 y 007. `/speckit-converge` las detecta si siguen ahí |
| Divergencia entre lo que muestra la búsqueda y lo que cobra HU-02 | Un único código para la búsqueda y para `IConsultaDisponibilidad` (D4), y `ConsultaDisponibilidadTests` que lo comprueba (CE-004) |
| Cambio de hora | `HorarioMadrid` para el día de Madrid y las horas de pared; prueba del 25/10/2026 (D9) |

## Seguimiento de la complejidad

No aplica: no hay infracciones de la constitución que justificar.
