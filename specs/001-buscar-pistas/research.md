# Investigación y decisiones: 001 · Buscar pistas libres

**Entrada**: [spec.md](./spec.md) (con las aclaraciones del 2026-11-25) y la base común de
[`000-core`](../000-core/plan.md). Las decisiones de 000-core (R1–R19) **no se repiten**: aquí solo
figuran las que añade HU-01. Se numeran D1–D10 para no confundirlas con las de 000-core.

No queda ningún «NEEDS CLARIFICATION» en el contexto técnico: la pila, el almacenamiento, las pruebas
y el reloj ya los fija 000-core.

## D1. Un único endpoint de búsqueda

- **Decisión**: `GET /api/disponibilidad/franjas?deporteId={id}&centroId={id}&fecha=yyyy-MM-dd`,
  protegido con la política `EsCiudadanoORepresentante`. Devuelve las pistas del centro que admiten
  el deporte y son reservables por franjas, cada una con sus franjas **libres** y el precio para la
  tarifa del usuario, más el número total de esas pistas (`totalPistas`), para distinguir «no hay
  pistas de ese deporte» de «no quedan franjas libres» (RF-011 de la spec). Contrato:
  [contracts/api-disponibilidad.openapi.yaml](./contracts/api-disponibilidad.openapi.yaml).
- **Motivo**: es lo único que pide la spec. Va dentro del prefijo reservado `/api/disponibilidad`
  (contracts/README.md de 000-core). Un `GET` con parámetros de consulta se puede repetir y enlazar.
- **Alternativas descartadas**: un endpoint por pista (más peticiones para lo mismo); devolver las 14
  franjas con un estado (la aclaración 3 dice que solo se muestran las libres); `POST` de búsqueda (no
  modifica nada).

## D2. Validación de la petición

- **Decisión**: se valida en este orden, y gana el primero que falla:
  1. si falta algún parámetro o tiene un formato incorrecto → 400 `PETICION_INVALIDA`, con `errors`
     por campo;
  2. si la fecha está fuera de hoy..hoy + 7 (fecha de Madrid) → 422 `FUERA_DE_ANTELACION`, sin
     consultar la base de datos;
  3. si el deporte o el centro no existen → 404 `NO_ENCONTRADO`.

  Si el deporte existe pero el centro no tiene pistas reservables de ese deporte, la respuesta es 200
  con `totalPistas = 0`: no es un error, es un resultado vacío que el cliente explica (RF-011).
- **Motivo**: todos los códigos ya existen en el catálogo de 000-core (contracts/errores.md); no hace
  falta proponer ninguno nuevo (RF-003 de 000-core). El rango de fechas se valida con
  `HorarioMadrid.Hoy` y `ValoresNegocio.AntelacionMaximaDias`, la misma regla que
  `ReglaDisponibilidad`.
- **Alternativas descartadas**: devolver 200 vacío para una fecha fuera de rango (oculta el motivo y
  contradice el escenario 3 de la historia 3).

## D3. Cálculo: reutilizar las reglas puras de 000-core

- **Decisión**: un servicio `ServicioDisponibilidad` (módulo `Disponibilidad` de la API) hace, para
  cada pista candidata:
  1. `CalculadoraFranjas.FranjasDelDia(pista, fecha)` → las 14 franjas;
  2. lee, una vez por pista y para el día completo `[AUtc(fecha, 00:00), AUtc(fecha + 1, 00:00))`, los
     cierres (`IConsultaCierres`), las franjas fijas (`IConsultaFranjasFijas`) y las reservas activas
     (`IConsultaReservas`);
  3. aplica `ReglaDisponibilidad.MotivoNoDisponible` a cada franja con `ahora` de `TimeProvider`, y
     se queda con las que dan `null`;
  4. pone a cada una el precio con `CalculadoraPrecio.Precio(tipo, tarifa, tarifas)`; las tarifas se
     leen una sola vez por petición.

  Las cuatro piezas de dominio ya están probadas en `SI18.Dominio.Tests` (T042–T044 de 000-core).
- **Motivo**: principio III (regla única en el backend) y RF-013 de 000-core (buscar y reservar
  calculan igual). HU-01 no reescribe ninguna regla: solo compone las de 000-core.
- **Alternativas descartadas**: una consulta SQL que calcule las franjas libres (duplica
  `ReglaDisponibilidad` en SQL y rompe RF-013); guardar las franjas en una tabla (RF-010 de 000-core:
  se calculan, no se almacenan).

## D4. `IConsultaDisponibilidad` para HU-02

- **Decisión**: `ServicioDisponibilidad` implementa también `IConsultaDisponibilidad` (contracts/
  modulos.md de 000-core), con la **misma** composición de D3 para una sola franja:
  - `MotivoNoDisponible(instalacionId, inicioUtc)`: localiza la franja de esa instalación que empieza
    en `inicioUtc` (con `HorarioMadrid.ALocal`) y le aplica `ReglaDisponibilidad`. El `string?` que
    devuelve es **solo** uno de los seis motivos de RF-011 de 000-core, o `null`. Las peticiones mal
    formadas **lanzan** `ErrorNegocio`, igual en los dos casos:
    - si `inicioUtc` no es el inicio de una franja válida (no está en punto o está fuera de
      08:00–21:00): `PETICION_INVALIDA`;
    - si la instalación no existe: `NO_ENCONTRADO`.

    Así HU-02 trata los motivos como respuesta y deja que los errores lleguen solos a
    `ManejadorErrores`.
  - `Precio(instalacionId, tarifa)`: `CalculadoraPrecio` con el tipo de la instalación.

  La búsqueda (D3) y `IConsultaDisponibilidad` comparten un único método privado que evalúa las
  franjas de una pista, así no pueden divergir.
- **Motivo**: RF-013 de la spec (HU-01 es la única fuente) y RF-013 de 000-core.
- **Alternativas descartadas**: que HU-02 llame al endpoint HTTP de búsqueda (acopla por HTTP dos
  módulos del mismo proceso).

## D5. Consultas provisionales de solo lectura (aclaración 1)

- **Decisión**: en `Modulos/Disponibilidad/Provisional/`, tres clases que implementan las interfaces
  de 000-core con lecturas `AsNoTracking` sobre `Si18DbContext`:

  | Clase                              | Interfaz                | Dueño real (spec) | Qué hace                                                                                     |
  | ---------------------------------- | ----------------------- | ----------------- | -------------------------------------------------------------------------------------------- |
  | `ConsultaReservasProvisional`      | `IConsultaReservas`     | Saul (002)        | Reservas de la instalación en `[desde, hasta)` con estado `confirmada`, o `pendiente_pago` y `creada_en_utc > ahora − TiempoBloqueoPago` (la misma vigencia que `Reserva.EstaVigente`) |
  | `ConsultaCierresProvisional`       | `IConsultaCierres`      | C (007)           | Cierres de la instalación que se solapan con `[desde, hasta)`                                 |
  | `ConsultaFranjasFijasProvisional`  | `IConsultaFranjasFijas` | Artur (005)       | Solicitudes `aprobada` de la instalación cuya temporada cubre cada fecha de `[desde, hasta]`, expandidas en franjas con `CalculadoraFranjas` para sus días de la semana y su horario. `RepresentantesAfectados` lanza `NotImplementedException`: solo lo usa HU-07 |

  - Cada clase lleva en su comentario «PROVISIONAL (spec-001, RF-014a): sustituir por la de spec-00X»
    y una prueba de integración (D9) que fija su comportamiento, para que la versión definitiva la
    pueda reutilizar.
  - Se registran con `TryAddScoped`: si el dueño ya registró la suya, la provisional no se añade; si
    la registra después, la suya gana (en el contenedor de .NET, el último registro es el que se
    resuelve). El día que el dueño fusione la suya, borra la provisional en el mismo PR.
  - `ConsultaFranjasFijasProvisional` no descuenta los periodos de cierre: para HU-01 da igual, porque
    `INSTALACION_CERRADA` se evalúa antes que `FRANJA_BLOQUEADA_CLUB` y la franja no aparece en ninguno
    de los dos casos (spec, «Casos límite»).
- **Motivo**: aclaración 1 y RF-014a. Permite construir y probar HU-01 ya, sin escribir nunca en datos
  ajenos. Leer no infringe la regla del dueño único (contracts/modulos.md de 000-core, «CursosInformes»).
- **Alternativas descartadas**: esperar a HU-02, HU-05 y HU-07 (bloquea HU-01); leer las tablas desde
  `ServicioDisponibilidad` sin interfaces (rompe contracts/modulos.md y obligaría a reescribirlo).

## D6. Registro del módulo en la API

- **Decisión**: `Modulos/Disponibilidad/RegistroDisponibilidad.cs` (dueño: Artur) con
  `AddDisponibilidad()` y `MapDisponibilidad()`. `Program.cs` necesita **dos líneas nuevas** para
  llamarlos, después de `AddComun()` y `MapComun()`.
- **Motivo**: 000-core dejó la regla de no tocar `Program.cs` después de T009, pero no previó cómo
  entra cada módulo. Esas dos líneas son el mínimo, y cada spec 002–008 necesitará las suyas. Se
  propone en el PR para que el grupo lo acepte como convención (una línea `Add<Modulo>()` y otra
  `Map<Modulo>()` por módulo), y se avisa a Saul, que revisa `Program.cs` en T072.
- **Alternativas descartadas**: registrar Disponibilidad desde `RegistroComun` (mezcla módulos y
  obliga a tocar un fichero de todos); descubrir los módulos por reflexión (complejidad que ninguna
  spec pide, principio VIII).

## D7. Pantalla de búsqueda

- **Decisión**: `SI18.Cliente/Disponibilidad/BuscarPistas.razor`, ruta `/buscar-pistas`:
  - **Filtros**: `MudSelect` de deporte y de centro, y `MudDatePicker` de fecha, todos con `Label`
    visible y `Required`.
    - Deportes: solo los que aparecen en `deporteIds` de `GET /api/instalaciones?reservablePorFranjas=true`,
      con el nombre de `GET /api/deportes` (RF-004).
    - Centros: de `GET /api/centros`.
    - Fecha: hoy por defecto, con `MinDate`/`MaxDate` en hoy..hoy + 7 (fecha de Madrid). Es solo una
      ayuda: la regla la aplica la API (D2).
  - **Botón** «Buscar pistas» (`type=submit`), que también se lanza con Enter.
  - **Resultado**: una sección por pista con un `<h2>` con su nombre y una lista (`role="list"`) de
    franjas, cada una con el texto «18:00–19:00 · 6,00 €». La hora sale de `FormatoEs` y la tarifa no
    se muestra con color.
  - **Mensajes** en `MensajeAccesible`:
    - con resultados: «Hay N franjas libres en M pistas.»;
    - sin franjas: «No hay pistas libres de {deporte} en {centro} el {fecha}. Prueba otra fecha u otro
      centro.»;
    - sin pistas: «{centro} no tiene pistas de {deporte}.»;
    - con error: el `detail` de la API.

    El foco no se mueve: lo anuncia la región `aria-live`.
  - **Menú**: entrada «Buscar pistas» en el hueco «ciudadano, representante_club» de `MainLayout`
    (RF-002).
  - **Acceso directo por URL**: la página comprueba la sesión al cargarse, porque los filtros salen de
    endpoints anónimos y si no nadie la pararía hasta pulsar «Buscar».
    - Sin sesión, lleva a `/iniciar-sesion`.
    - Con un rol sin permiso, no muestra el formulario y explica para quién es la búsqueda.

    Se hace en la propia página y no con `AuthorizeRouteView` en `App.razor`, que es un fichero común
    de todos los módulos.
- **Motivo**: RF-003–RF-011 y RF-015 de la spec, con los componentes y el patrón accesible que ya
  fijó 000-core (R16, `MensajeAccesible`, `FormatoEs`).
- **Alternativas descartadas**: una tabla de 14 columnas por hora (se lee peor con lector de pantalla
  y en móvil); un calendario semanal (alcance que la spec no pide).

## D8. Fecha de hoy en el cliente

- **Decisión**: el cliente calcula «hoy en Madrid» solo para proponer la fecha y limitar el
  selector, con `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")`, que funciona en WebAssembly
  porque `BlazorEnableTimeZoneSupport` sigue activo (T048 de 000-core). La validación de verdad es la
  de la API (D2).
- **Motivo**: principio III (la regla vive en el backend); el cliente solo ayuda a no equivocarse.
- **Alternativas descartadas**: un endpoint que devuelva el rango permitido (una petición más para una
  ayuda visual).

## D9. Pruebas

- **Decisión**:
  - **Dominio**: las reglas (franjas, precio, disponibilidad) ya tienen pruebas de límites en 000-core.
    No se duplican.
  - **Integración** (`SI18.Api.Tests/Disponibilidad/`, con `FabricaApi`, `FakeTimeProvider` y datos
    insertados directamente en la base de pruebas):
    - `BuscarFranjasTests`: los escenarios de aceptación de las historias 1–3 (reserva confirmada,
      pendiente vigente y caducada, cierre de 10:00 a 12:00, franja fija de club, hoy a las 18:00,
      hoy + 7 / + 8, deporte sin pistas, 403 con personal y técnico, 401 sin sesión, 400 y 404).
    - `PrecioPorTarifaTests`: 6,00 / 10,00 / 4,00 para empadronado, no empadronado y representante.
    - `ConsultaDisponibilidadTests`: para varias franjas, `MotivoNoDisponible` y `Precio` dan lo mismo
      que la búsqueda (CE-004; base de la prueba de extremo a extremo que añadirá spec-002).
    - `ConsultasProvisionalesTests`: la vigencia de 10 minutos (9:59 → activa; 10:00 → no), el solape
      semiabierto de los cierres y la expansión de una franja fija aprobada (sí en temporada, no en
      julio).
    - Cambio de hora: búsqueda el 25/10/2026 con 14 franjas de 1 h real.
  - **Accesibilidad**: comprobación manual de quickstart §6 de 000-core sobre `/buscar-pistas`
    (teclado, anuncio, Lighthouse ≥ 95, axe).
- **Motivo**: principio II y CE-003–CE-006 de la spec.
- **Alternativas descartadas**: pruebas E2E con navegador (000-core, R16: no se piden).

## D10. Rendimiento

- **Decisión**: sin caché. Por petición: 1 consulta de pistas, 1 de tarifas y 3 por pista (como mucho
  2 pistas por deporte y centro con la siembra; en general ≤ 4). El objetivo es el de 000-core
  (búsqueda de un centro y un día < 500 ms p95), muy por debajo de CE-002 (< 2 s).
- **Motivo**: escala municipal (000-core, «Escala y alcance»); principio VIII.
- **Alternativas descartadas**: cachear la disponibilidad (riesgo de mostrar como libre algo
  ocupado); una sola consulta para todas las pistas (optimización innecesaria con ≤ 4 pistas).
