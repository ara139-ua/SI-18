# Registro de `/speckit-analyze` y trazabilidad · 006-validar-qr

**Responsable**: Saul (módulo `Reservas`). **Informes**: [analyze-1.md](./analyze-1.md) (inicial, antes de
los ajustes) y [analyze-2.md](./analyze-2.md) (después de los ajustes).

## 1. Registro de hallazgos de analyze-1

Estados: **Resuelto** (corregido en el commit «spec-006: correcciones de analyze») · **Verificado**
(analyze-2 confirma que ya no aparece).

| ID | Historia / criterio | Gravedad | Evidencia (analyze-1) | Decisión | Responsable | Estado | Dónde se resuelve |
|----|---------------------|----------|-----------------------|----------|-------------|--------|-------------------|
| D1 | Constitución V; «Flujo de trabajo» | CRITICAL | T030 abría los PR sin revisar secretos | Revisión de secretos antes de abrir cada PR: `git grep` de quickstart §7 de 000-core y `git ls-files fuentes/.env`, con el resultado en el PR | Saul | Verificado (analyze-2) | tasks.md T030 |
| C1 | HU-1, HU-2, HU-3 (T007, T013, T018) | HIGH | Varias pruebas HTTP insertaban confirmadas en la misma pista e inicio → `23505` | Una sola reserva confirmada compartida, creada de forma **idempotente** en `IAsyncLifetime` (si ya existe, se reutiliza, también con `SI18_TEST_CONNECTION`); `git grep` repetido para la pista y el 21/12/2026 | Saul | Verificado (analyze-2) | tasks.md «Datos de las pruebas», T006 (`ConfirmadaCompartida`), T007, T013; research.md V11; plan.md «Riesgos» |
| C2 | HU-1 (RF-006a, CE-007), CE-002 (T008, T014) | HIGH | Los casos de T008 y T014 compartían reserva o chocaban | Una fecha por reserva dentro del 16–31/01/2027: T008 → 18, 20 y 22/01/2027; T014 → 21/01/2027 compartida de solo lectura; T018b → 25/01/2027 | Saul | Verificado (analyze-2) | tasks.md «Asignación de fechas», T008, T014; research.md V11 |
| U1 | HU-1 esc. 6; «Casos límite» (otra persona) | MEDIUM | La siembra tiene una sola persona del personal por centro; T008 usaba la misma | Validación aceptada insertada con `personal_id` de `personal.oeste` y validación posterior con `personal.este` | Saul | Verificado (analyze-2) | tasks.md T008 (22/01/2027) |
| U2 | CE-001 | MEDIUM | T028 aceptaba 9 de 10 (90 %) frente al 95 % | 20 mediciones; < 1 s en al menos 19 de 20 | Saul | Verificado (analyze-2) | tasks.md T028 |
| A1 | RF-001 (T005, T021) | MEDIUM | T005 sin entregable; T021 condicional | T005 y T021 retiradas (número conservado); política fijada en T004 y probada en T020 | Saul | Verificado (analyze-2) | tasks.md T004, T005, T020, T021, T030, «Dependencias» |
| A2 | RF-009 | MEDIUM | RF-009 solo tenía una revisión (T019), sin prueba | Prueba T018b: fallo de `SaveChanges` provocado dentro de la propia prueba (FK de `personal_id` con un `IUsuarioActual` de prueba), sin dejar filas en la base compartida; `Validar` lanza y no hay filas de esa reserva | Saul | Verificado (analyze-2) | tasks.md T006 (sobrecarga de `ConServicio`), T018b, T019 |
| F1 | CE-004 (T020) | LOW | La regla general no recogía la comprobación por `codigo_leido` único | Regla con las dos formas; `HayValidacionConCodigo` en `DatosValidaciones` | Saul | Verificado (analyze-2) | tasks.md «Datos de las pruebas», T006, T020 |
| F2 | C2 del plan | LOW | El árbol del plan no tenía `ClienteApiExtensionesTests.cs` | Añadido al árbol | Saul | Verificado (analyze-2) | plan.md «Código fuente» |
| U3 | C2 (T016) | LOW | No se decía cómo construir `ClienteApi` (necesita `NavigationManager`) | Subclase mínima `NavegacionDePrueba` en la prueba | Saul | Verificado (analyze-2) | tasks.md T016 |
| E1 | HU-4 esc. 3; RF-016 | LOW | Nadie comprobaba que los demás roles no ven «Validar acceso» | Comprobación con una ciudadana y con el técnico en la revisión manual | Saul | Verificado (analyze-2) | tasks.md T026 |

### Hallazgos nuevos de analyze-2

| ID | Historia / criterio | Gravedad | Evidencia (analyze-2) | Decisión | Responsable | Estado | Dónde se resuelve |
|----|---------------------|----------|-----------------------|----------|-------------|--------|-------------------|
| L1 | HU-1 esc. 1; RF-006a | LOW | Con la reserva HTTP compartida (C1), T007 no puede exigir `validacionAnteriorHora` nulo en el primer 201; «sin aviso en la primera» se prueba solo en el servicio (T008) | Pendiente de decidir; no bloquea. Propuesta: al escribir T007, comprobar que es nulo o igual a la hora de una aceptada anterior de esa reserva leída de la base | Saul | Abierto (no bloquea) | tasks.md T007 |

## 2. Matriz de trazabilidad de HU-06

| Criterio | Qué exige (resumen) | Decisión del plan | Tareas | Prueba prevista |
|----------|---------------------|-------------------|--------|-----------------|
| RF-001 | Solo el personal de centro valida; otros roles y sin sesión, rechazo sin registro | V7 | T004, T020 | `ValidarAccesoTests`: 401 sin sesión; 403 `ROL_NO_PERMITIDO` con ciudadana, representante y técnico; ningún `codigo_leido` registrado |
| RF-002 | Centro del personal = el asignado en 000-core | V7 | T009, T013 | `ValidarAccesoTests`: `personal.oeste` sobre una reserva de Este → `QR_OTRO_CENTRO` |
| RF-003 | Solo introducción manual | V10 (aclaración 1) | T012 | Manual: quickstart §4 paso 1 y §5 (sin permiso de cámara) |
| RF-004 | Espacios de los extremos fuera; vacío no se envía | V6 | T004, T007, T012, T018 | `ValidarAccesoTests`: código con espacios → 201; `"   "` y `{}` → 400 sin registro |
| RF-005 | Decisión en el servidor y orden de motivos | V3 | T009, T013, T014, T015 | `ValidarAccesoTests` (cada motivo) y `LimitesValidacionTests` |
| RF-006 | Titular, instalación y franja si se acepta | V2, V8 | T007, T009, T012 | `ValidarAccesoTests`: 201 con titular, instalación, centro, fecha y horas de Madrid |
| RF-006a | Aviso con la hora de la aceptada anterior más reciente | V5 | T008, T010, T012 | `AvisoValidacionAnteriorTests` (18, 20 y 22/01/2027) |
| RF-007 | Datos en los rechazos, nunca el titular | V2, V9 | T001, T013, T015, T017 | `ValidarAccesoTests`: `QR_DESCONOCIDO` y `QR_OTRO_CENTRO` sin `reserva`; cancelada con `reserva` y sin titular; `LimitesValidacionTests`: `Reserva` informada fuera de ventana |
| RF-008 | Un registro por intento con todos sus datos | V4 | T007, T009, T013 | `ValidarAccesoTests`: fila por intento, con `reserva_id` nulo en `QR_DESCONOCIDO` y recorte a 64 |
| RF-009 | Sin registro no hay resultado | V4 | T009, T018b, T019 | `ValidarAccesoTests` (T018b): FK fallida → `Validar` lanza y no queda ninguna fila |
| RF-010 | La reserva no cambia | V4 | T009, T018 | `ValidarAccesoTests`: estado, código y `cancelada_en_utc` iguales tras validar |
| RF-011 | El código es el de HU-02 | V3 | T007 | Las reservas de prueba se crean con `DatosReservas.Confirmada` (código de `GeneradorCodigoAcceso`) |
| RF-012 | Una cancelación invalida el código sin más | V3; contracts/modulos.md | T013, T029 | `ValidarAccesoTests`: cancelada (`DatosReservas.Cancelada`) → `QR_RESERVA_NO_CONFIRMADA` |
| RF-013 | Texto e icono, anuncio único | V10 | T012, T017, T025, T026 | Manual: quickstart §5 (NVDA y colores) |
| RF-014 | Teclado, foco visible, foco al campo tras el resultado | V10 | T012, T025, T026 | Manual: quickstart §5 punto 1; foco visible en DevTools (T026) |
| RF-015 | Sin desplazamiento horizontal desde 320 px | V10 | T025, T026 | Manual: quickstart §5 punto 4; barra superior a 320 px (T026) |
| RF-016 | Menú solo para el personal | V10, C1 | T024, T026 | Manual (T026): menú con `personal.este`, con una ciudadana y con el técnico |
| RF-017 | Español y hora de Madrid | V8 | T003, T012, T027 | `ValidarAccesoTests` (horas de Madrid) y `git grep "Europe/Madrid"` sin resultados (T027) |
| CE-001 | < 1 s en el 95 % | V12 | T028 | Medición manual: 20 intentos, al menos 19 < 1 s |
| CE-002 | Límites y cambios de hora | V11 | T014 | `LimitesValidacionTests` (`Categoria=Limites`): 15/14 min, 1 s antes, fin, 1 s y 1 min después; 25/10/2026 y 28/03/2027 |
| CE-003 | Rechazos sin datos indebidos | V2, V9 | T001, T013, T015 | `ValidarAccesoTests` (extensión `reserva` según el motivo; `ReservaValidadaDto` sin titular) |
| CE-004 | Todo intento del personal registrado; nada de otros | V4, V7 | T007, T013, T018, T020 | `ValidarAccesoTests` (filas por `validacionId`, `UltimoId` y `HayValidacionConCodigo`) |
| CE-005 | < 15 s a mano | V10 | T028 | Medición manual con cronómetro |
| CE-006 | Teclado, 320 px, Lighthouse ≥ 95, axe, lector | V10 | T025, T026 | Manual: quickstart §5 (sin marcar hasta hacerla) |
| CE-007 | Aviso correcto en el 100 % | V5 | T008, T010 | `AvisoValidacionAnteriorTests` |
| (rendimiento 000-core) | < 300 ms p95 por validación | V12 | T028 | 20 llamadas con la API local |
