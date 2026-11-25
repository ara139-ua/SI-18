# Plan de implementación: 005 · Franjas fijas de temporada para clubes (HU-05)

**Rama**: `feature/005-franjas-fijas` | **Fecha**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/005-franjas-fijas/spec.md` (con las aclaraciones del 2026-11-25),
la base técnica de [`000-core/plan.md`](../000-core/plan.md) y lo construido en
[`001-buscar-pistas`](../001-buscar-pistas/plan.md).

## Resumen

HU-05 añade el ciclo de vida de una **solicitud de franjas fijas**: el representante la envía, el
técnico ve sus conflictos y la aprueba o la rechaza, y el club recibe un aviso. Una solicitud aprobada
bloquea sus franjas en la búsqueda (HU-01) y en la reserva (HU-02). No crea tablas ni estados: la entidad,
sus transiciones y los tres códigos de error ya están en `develop`. Las piezas son:

- **Dominio**: `ReglaFranjasFijas`, una función pura que decide solapes, si una solicitud cubre una hora
  y la expansión en franjas por temporada (F3). La usan todas las demás piezas.
- **Backend** (módulo `Disponibilidad`):
  - `ServicioSolicitudesFranjasFijas`: crear, listar, conflictos, aprobar y rechazar, con los avisos en
    la misma operación (F2, F4, F5);
  - seis endpoints en `/api/solicitudes-franjas-fijas` (F1);
  - `ConsultaFranjasFijas`, la **definitiva** de `IConsultaFranjasFijas`: descuenta los cierres y ofrece
    `RepresentantesAfectados` a HU-07. **Retira** la provisional de spec-001 (F6).
- **Cliente**: `/franjas-fijas` para el representante y `/franjas-fijas/revision` para el técnico,
  accesibles (F7, F8).

Las decisiones y sus motivos están en [research.md](./research.md) (F1–F11).

## Contexto técnico

Todo es igual que en [000-core/plan.md](../000-core/plan.md#contexto-técnico); no hay dependencias nuevas.

- **Lenguaje y versión**: C# 14 sobre .NET 10.
- **Dependencias**: ASP.NET Core Minimal API, EF Core 10 con Npgsql, Blazor WebAssembly y MudBlazor 8.x.
  Ningún paquete nuevo.
- **Almacenamiento**: PostgreSQL 17. **Sin migraciones**: escribe en `solicitudes_franjas_fijas` y
  `notificaciones`, que ya existen ([data-model.md](./data-model.md)).
- **Pruebas**: xUnit v3, `FakeTimeProvider`, `FabricaApi` con Testcontainers y el ayudante
  `DatosDisponibilidad` de spec-001.
- **Tipo de proyecto**: aplicación web (SPA servida por la API).
- **Rendimiento**: escala municipal. Un listado y unas decenas de solicitudes por temporada; los
  conflictos leen las reservas de una instalación para el resto de la temporada (como mucho unas
  pocas miles de filas). Objetivo: < 500 ms por petición, como en 000-core.
- **Restricciones**:
  - reglas en el backend (principio III);
  - hora de `TimeProvider` y `HorarioMadrid`;
  - el cambio de estado y los avisos se guardan juntos (R18 de 000-core);
  - sin datos personales del titular en los conflictos;
  - WCAG 2.1 AA;
  - solo escribe en las tablas de su módulo y en avisos a través de `IServicioAvisos`.
- **Escala y alcance**: 2 clubes y 2 representantes en la siembra, 1 técnico y 1 temporada. Dos pantallas
  y seis endpoints.

## Comprobación de la constitución

*Puerta: debe superarse antes de la Fase 0 y volver a comprobarse tras la Fase 1.*

| Principio | Cómo lo cumple el diseño | Antes de F0 | Tras F1 |
| --------- | ------------------------ | ----------- | ------- |
| I. WCAG 2.1 AA | Días en un `<fieldset>` con `<legend>` y casillas, horas en desplegables con etiqueta, estados en texto, resultados en `MensajeAccesible`, foco devuelto tras elegir y tras resolver, botón «Aprobar» desactivado con su explicación al lado (F7, F8) | ✅ | ✅ |
| II. Lógica probada | `ReglaFranjasFijas` con pruebas de límites (solapes en el borde, 21:00 fuera, temporada y cambio de hora). Integración de todos los escenarios, también la concurrencia lógica de dos pendientes solapadas (F10) | ✅ | ✅ |
| III. Reglas en el backend | Conflictos y solapes calculados en el servidor y **vueltos a comprobar** al aprobar; el cliente solo desactiva el botón como ayuda (F4) | ✅ | ✅ |
| IV. Errores coherentes | `PETICION_INVALIDA` con `errors` para el formulario; `SOLICITUD_CON_CONFLICTOS`, `FRANJA_FIJA_SOLAPADA` y `SOLICITUD_YA_RESUELTA` ya en el catálogo (PR #32); `ROL_NO_PERMITIDO` y `NO_ENCONTRADO` | ✅ | ✅ |
| V. Sin secretos | Nada nuevo que configurar | ✅ | ✅ |
| VI. Datos ficticios | Clubes, representantes y técnico de la siembra; los conflictos no muestran datos personales (aclaración 3) | ✅ | ✅ |
| VII. Base común en 000-core | Sin entidades, estados ni columnas nuevas. Los códigos nuevos se propusieron antes en 000-core. Implementa `IConsultaFranjasFijas` con su firma ([contracts/modulos.md](./contracts/modulos.md)) | ✅ | ✅ |
| VIII. Cero alcance fantasma | Sin retirar ni modificar solicitudes, sin motivo de rechazo, sin bloqueo serializable (riesgo aceptado en F4), sin política nueva en `Comun`. Las temporadas se publican aquí porque solo las usa esta historia | ✅ | ✅ |
| IX. Idioma, moneda y zona | Textos y avisos en español («martes y jueves»), horas en Madrid, expansión correcta en el cambio de hora | ✅ | ✅ |
| Pila y seguridad | La pila de 000-core. Políticas `EsRepresentante` y `EsTecnico`; el club sale de la sesión, nunca del cuerpo | ✅ | ✅ |
| Colaboración | Solo `*/Disponibilidad`, más dos huecos de menú en `MainLayout`. Retira **su** provisional y deja a Saul la suya. Se coordinan los ficheros compartidos (F11) | ✅ | ✅ |

**Resultado**: se supera sin infracciones. «Seguimiento de la complejidad» queda vacío.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/005-franjas-fijas/
├── spec.md
├── plan.md              # este fichero
├── research.md          # Fase 0: decisiones F1–F11
├── data-model.md        # Fase 1: qué escribe y lee, reglas calculadas y DTO (sin tablas nuevas)
├── quickstart.md        # Fase 1: validación de principio a fin
├── contracts/
│   ├── api-solicitudes-franjas-fijas.openapi.yaml   # seis endpoints
│   └── modulos.md                                   # IConsultaFranjasFijas definitiva y coordinación
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Código fuente

```text
fuentes/
├── src/
│   ├── SI18.Dominio/Disponibilidad/
│   │   └── ReglaFranjasFijas.cs                    # SeSolapan, Cubre, Expandir (F3)
│   ├── SI18.Contratos/Disponibilidad/
│   │   ├── PeticionSolicitudFranjasFijas.cs
│   │   ├── SolicitudFranjasFijasDto.cs
│   │   ├── ConflictosSolicitudDto.cs               # + ReservaEnConflictoDto, SolicitudSolapadaDto
│   │   ├── TemporadaDto.cs
│   │   └── EnumeracionesFranjasFijas.cs            # EstadoSolicitud, EstadoReservaEnConflicto
│   ├── SI18.Api/Modulos/Disponibilidad/
│   │   ├── ServicioSolicitudesFranjasFijas.cs      # F2, F4, F5
│   │   ├── EndpointsSolicitudesFranjasFijas.cs     # F1
│   │   ├── ConsultaFranjasFijas.cs                 # IConsultaFranjasFijas definitiva (F6)
│   │   ├── RegistroDisponibilidad.cs               # + servicio, endpoints; TryAdd provisional → AddScoped definitiva
│   │   └── Provisional/ConsultaFranjasFijasProvisional.cs   # se BORRA (F6)
│   └── SI18.Cliente/
│       ├── Layout/MainLayout.razor                 # + «Franjas fijas» (representante) y «Revisar franjas fijas» (técnico)
│       └── Disponibilidad/
│           ├── FranjasFijasClub.razor              # /franjas-fijas (F7)
│           └── RevisarFranjasFijas.razor           # /franjas-fijas/revision (F8)
└── tests/
    ├── SI18.Dominio.Tests/Disponibilidad/ReglaFranjasFijasTests.cs
    └── SI18.Api.Tests/
        ├── Comun/EnumeracionesEspejoTests.cs        # + EstadoSolicitud
        └── Disponibilidad/
            ├── SolicitarFranjasFijasTests.cs
            ├── RevisarFranjasFijasTests.cs
            ├── ConsultaFranjasFijasTests.cs         # + pruebas movidas de ConsultasProvisionalesTests
            ├── ConsultasProvisionalesTests.cs       # − las de franjas fijas
            └── DatosDisponibilidad.cs               # ConServicio con la definitiva; + ayudas de solicitudes
```

**Decisión de estructura**: la de 000-core y spec-001, sin proyectos nuevos. Todo el código va en
las carpetas `Disponibilidad` (dueño: Artur). Fuera de ellas solo se tocan dos huecos de menú que
`MainLayout` reservó para el representante y el técnico (T056 de 000-core), y una línea en
`EnumeracionesEspejoTests`. `Program.cs` no cambia: el módulo ya está registrado.

## Flujos

```text
POST /api/solicitudes-franjas-fijas                       [EsRepresentante]
   ├─ formato (días, horas)              → 400 PETICION_INVALIDA (errors)
   ├─ instalación no pista/pabellón      → 400 (errors.instalacionId)
   ├─ temporada inexistente o terminada  → 400 (errors.temporadaId)
   └─ new SolicitudFranjasFijas(club de la sesión, …) → SaveChanges → 201 (pendiente)

POST /api/solicitudes-franjas-fijas/{id}/aprobacion        [EsTecnico]
   ├─ no existe                          → 404 NO_ENCONTRADO
   ├─ ya resuelta                        → 409 SOLICITUD_YA_RESUELTA
   ├─ Conflictos(): reservas             → 409 SOLICITUD_CON_CONFLICTOS
   ├─ Conflictos(): solapes con aprobadas→ 409 FRANJA_FIJA_SOLAPADA
   └─ Aprobar(ahora) + Avisar(cada representante) → un SaveChanges → 200

ServicioDisponibilidad (HU-01/HU-02) ──► IConsultaFranjasFijas.FranjasBloqueadas
                                         = Expandir(aprobadas) − franjas afectadas por cierres
HU-07 ──► IConsultaFranjasFijas.RepresentantesAfectados(instalación, cierre)
```

## Dependencias y riesgos

| Dependencia o riesgo | Mitigación |
| -------------------- | ---------- |
| Una reserva o una aprobación solapada se cuela entre comprobar y guardar (F4) | Ventana de milisegundos a escala municipal. Tras guardar, HU-01 y HU-02 ya no ofrecen ni aceptan esas franjas, y la doble aprobación de la misma solicitud la para el dominio. Riesgo aceptado (principio VIII); se revisa si aparece en la práctica |
| Saul (spec-002) quita su provisional en los mismos ficheros (F11) | Cambios de líneas contiguas; el segundo PR hace `rebase`. Aviso cruzado en los dos PR |
| HU-02 y HU-07 aún sin consultas definitivas | Se usan las provisionales de spec-001 a través de las interfaces; nada cambia cuando lleguen las definitivas |
| La búsqueda cambia al quitar la provisional | CE-005: las pruebas de HU-01 deben seguir en verde, y `ConsultaFranjasFijasTests` hereda las que fijaban la provisional |
| Errores del constructor del dominio (`ArgumentException`) | Validación previa en el servicio (F2), con una prueba por cada caso |

## Seguimiento de la complejidad

No aplica: no hay infracciones de la constitución que justificar.
