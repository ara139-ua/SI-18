<!--
Sync Impact Report
- Versión: 1.0.0 → 1.1.0 (2026-10-21)
- Idioma: el documento pasa de inglés a español (sin cambiar el sentido de I, II, IV, V y VI).
- Principio modificado: III. Las reglas viven en el backend. Se quita la lista parcial de reglas
  (7 días, 24 h, 15 min) y se remite a los valores de negocio de specs/000-core/spec.md para que
  haya una única fuente de verdad.
- Principios añadidos: VII. Base común en 000-core; VIII. Cero alcance fantasma;
  IX. Idioma, moneda y zona horaria.
- Secciones ampliadas: «Flujo de trabajo» (ciclo Spec Kit completo y obligatorio, conservación
  de iteraciones) y nueva sección «Acuerdos de colaboración» (ramas, dueño por módulo,
  trazabilidad, cambios en la base común por PR).
- Gobernanza: «Principios I–VI» → «Principios I–IX».
- Plantillas afectadas: ninguna requiere cambios (plan-template ya incluye «Constitution Check»).
- Pendientes: crear specs/000-core (lo exige el principio VII).
---
Informe anterior (1.0.0, 2026-10-21): adopción inicial. Principios I–VI, restricciones de pila
y seguridad, flujo de trabajo y gobernanza.
-->
# Constitución de SI-18 · Reserva de Instalaciones Deportivas Municipales

## Principios

### I. Accesibilidad WCAG 2.1 AA (NO NEGOCIABLE)
Toda pantalla y componente visible para el usuario DEBE cumplir WCAG 2.1 nivel AA. Los elementos
interactivos DEBEN poder usarse con teclado y con el foco visible, tener nombre y etiqueta
accesibles y cumplir el contraste AA. Los errores y cambios de estado (por ejemplo, una reserva
rechazada o un QR validado) NO DEBEN comunicarse solo con color y DEBEN anunciarse a las
tecnologías de apoyo. Una funcionalidad no está terminada hasta que su interfaz se ha comprobado
contra estos criterios.
Motivo: la aplicación da servicio a toda la ciudadanía de un municipio; la accesibilidad es una
obligación legal y social, no una mejora.

### II. La lógica de negocio está probada
Toda regla de negocio DEBE tener pruebas automáticas que cubran los dos lados de cada límite (por
ejemplo, exactamente 7 días, exactamente 24 h, exactamente 15 minutos). Toda corrección de un
fallo DEBE incluir una prueba de regresión. Las pruebas de negocio DEBEN ser deterministas: la hora
se obtiene siempre a través de una abstracción de reloj inyectada, nunca leyendo el reloj del
sistema directamente.
Motivo: las reglas deciden dinero y acceso; probarlas es barato y equivocarse sale caro.

### III. Las reglas de negocio viven en el backend
La API de ASP.NET Core es la única fuente de verdad de las reglas de negocio. Cada regla DEBE
implementarse y hacerse cumplir en el backend, en un único lugar. Los valores que las gobiernan
(antelación máxima, plazo de devolución, ventana del QR, duración de la franja, tiempo de bloqueo
durante el pago, etc.) se definen una sola vez en `specs/000-core/spec.md` y en el código DEBEN
ser constantes con nombre o configuración, nunca literales repetidos.
El cliente Blazor PUEDE replicar una regla para dar respuesta inmediata al usuario, pero NO DEBE
considerarse fiable y DEBE obtener el resultado definitivo de la API. Toda restricción que dependa
de la concurrencia (por ejemplo, que dos usuarios no confirmen la misma franja) DEBE garantizarse
también en la base de datos, no solo en el código.
Motivo: un único punto de aplicación evita que cliente y servidor diverjan y que la regla se
salte con una petición directa.

### IV. Errores de la API coherentes
Todos los errores de la API DEBEN usar un mismo formato (Problem Details, RFC 9457) con un código
de error estable legible por máquina, un mensaje en español apto para mostrarse al usuario y el
código HTTP correcto (400/422 validación o regla incumplida, 401/403 autenticación y permisos, 404
no encontrado, 409 conflicto). Las excepciones no controladas DEBEN convertirse en un manejador
global y NO DEBEN mostrar trazas ni detalles internos. Un rechazo por regla de negocio (por
ejemplo, reservar a más de 7 días) DEBE devolver un mensaje que explique el motivo.
Motivo: los clientes tratan los errores de forma uniforme y el usuario recibe explicaciones
comprensibles.

### V. Sin secretos en el repositorio (NO NEGOCIABLE)
Credenciales, cadenas de conexión, claves de API, claves de la pasarela de pago y claves de firma
NUNCA DEBEN subirse al repositorio. Se proporcionan mediante variables de entorno, user-secrets o
un almacén de secretos; el repositorio solo PUEDE contener ejemplos con valores de relleno
(`.env.example`). Un secreto subido por error DEBE considerarse comprometido y cambiarse.
Motivo: un secreto filtrado no puede borrarse de forma fiable del historial de git.

### VI. Datos de prueba ficticios
Los datos de inicialización, fixtures, pruebas y demos DEBEN ser ficticios. No DEBEN aparecer
datos personales reales (nombres, correos, teléfonos, DNI, datos de pago) en el repositorio, en
los registros ni en las bases de datos de prueba.
Motivo: protege la privacidad de la ciudadanía y cumple la normativa de protección de datos.

### VII. Base común en 000-core
Las specs 001 a 008 se apoyan en el plan técnico (`specs/000-core/plan.md`) y en el modelo de
datos (`specs/000-core/data-model.md`) de la spec `000-core`. No los repiten ni los redefinen: los
referencian. Si una spec necesita una entidad, un estado o un valor de negocio nuevo o distinto,
el cambio se propone primero en `000-core` y solo después se usa en la spec. Si una spec
contradice a `000-core`, manda `000-core`.
Motivo: cuatro personas trabajan en paralelo sobre las mismas entidades (reserva, pago,
instalación, franja, curso); sin un modelo común la integración falla.

### VIII. Cero alcance fantasma
No se implementa ninguna funcionalidad que no esté escrita en una especificación. Si surge una
idea nueva, se propone en la spec correspondiente; no se construye hasta que forme parte de ella.
Entre dos soluciones que cumplen la spec, se elige la más simple: no se añaden capas, opciones ni
flexibilidad «por si acaso».
Motivo: mantiene trazable cada línea de código hasta una historia y evita que `converge` detecte
código sin justificación.

### IX. Idioma, moneda y zona horaria
Todo el producto (pantallas, mensajes, especificaciones y documentación) está en español de
España. Los importes se expresan en euros (€) con el formato habitual en España. Las reglas que
dependen de la hora (7 días, 24 h, 15 minutos, franjas de 1 hora) se evalúan en la zona horaria
`Europe/Madrid`; las fechas se guardan en UTC y se convierten solo al mostrarlas o al evaluar una
regla, teniendo en cuenta el cambio de horario de verano.
Motivo: el servicio es de un ayuntamiento español, y una regla de 24 h mal calculada en el cambio
de hora decide si se devuelve o no el dinero.

## Restricciones de pila y seguridad

- Frontend: Blazor WebAssembly con componentes MudBlazor, usados de forma accesible (principio I).
- Backend: ASP.NET Core Minimal API; las reglas de negocio en una capa independiente de los
  endpoints HTTP para poder probarlas de forma unitaria.
- Base de datos: PostgreSQL, con los cambios de esquema aplicados mediante migraciones versionadas.
- Los importes DEBEN usar tipos decimales exactos (nunca coma flotante). Las franjas de reserva
  son de 1 hora.
- Cualquier desviación de esta pila DEBE justificarse en el plan y aprobarse según la Gobernanza.

## Flujo de trabajo y controles de calidad

- Cada spec sigue el ciclo completo de Spec Kit: specify → clarify → plan → tasks → analyze →
  implement → converge. No se implementa una funcionalidad sin su spec, su plan y sus tareas.
- Se conservan todos los ficheros generados por el ciclo (`spec.md`, `plan.md`, `research.md`,
  `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`, checklists e informes de analyze y
  converge) y sus iteraciones: no se borran versiones anteriores de un informe ni tareas añadidas
  por `converge`.
- Todo pull request DEBE compilar y pasar las pruebas, y quien revisa DEBE confirmar que cumple los
  principios I–IX.
- Un pull request que añade o cambia una regla de negocio DEBE incluir pruebas (principio II) y
  situar la regla en el backend (principio III).
- Un pull request que cambia la interfaz DEBE indicar cómo se ha comprobado WCAG AA (principio I).
- Antes de fusionar se DEBE revisar que no se suben secretos, con herramienta si está disponible
  (principio V).

## Acuerdos de colaboración

- El equipo son cuatro personas y cada una es responsable de dos historias; el reparto está en
  `docs/agrupamiento.md` y no cambia sin acuerdo del grupo.
- Cada spec se trabaja en su propia rama (`spec-001` a `spec-008`); la base común (`000-core`, la
  constitución y la estructura del proyecto) se trabaja en `develop` y se lleva a `main` por pull
  request.
- Cada módulo de la API y del frontend tiene un único dueño, que es el único que escribe en sus
  tablas; los demás usan su interfaz pública. Los cambios en el modelo de datos compartido o en
  los valores de negocio van por pull request revisado por al menos otra persona del grupo.
- Todo commit indica la spec o tarea a la que pertenece (por ejemplo, `spec-002 T014: ...`) para
  que la autoría de cada historia sea trazable.
- Cuando una historia dependa de otra persona (por ejemplo, la cancelación invalida el QR), la
  dependencia se anota en las dos specs y se avisa al grupo.

## Gobernanza

Esta constitución prevalece sobre cualquier otra práctica del repositorio. Se modifica mediante un
pull request que explique el cambio, el motivo y su impacto en las specs, planes y código
existentes, y que el equipo apruebe antes de fusionarlo.

Las versiones siguen versionado semántico: MAYOR si se elimina o redefine un principio de forma
incompatible; MENOR si se añade un principio o sección o se amplía su alcance; PARCHE si solo se
aclara la redacción.

El cumplimiento se revisa en cada pull request y en la fase de planificación de cada spec
(«Constitution Check» del plan). Los incumplimientos DEBEN corregirse o justificarse y aprobarse
explícitamente antes de fusionar. La guía de uso para quien contribuye está en
[README.md](../../README.md).

**Versión**: 1.1.0 | **Ratificada**: 2026-10-21 | **Última modificación**: 2026-10-21
