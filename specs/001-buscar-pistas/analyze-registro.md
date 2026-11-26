# Registro de `/speckit-analyze` y trazabilidad · 001-buscar-pistas

**Responsable**: Artur (módulo `Disponibilidad`). **Informes**: analyze inicial (2026-11-25, antes de implementar;
sus hallazgos están copiados en la tabla 1 del informe que se dio en la sesión de Claude Code, que no se guardó como fichero) y
[analyze-final.md](./analyze-final.md) (2026-11-26, después de implementar y converger). Converge:
[converge-2026-11-26.md](./converge-2026-11-26.md).

> Este registro sustituye a la reconstrucción que hizo Saul el 26/11/2026 (#51) a partir de los commits de correcciones, porque el informe original sí se ha recuperado. Los identificadores y las decisiones coinciden con los de esa reconstrucción; aquí se añaden la gravedad, la evidencia de cada hallazgo, la decisión sobre C1 y el analyze final. Los PR de las correcciones (#18 y #19) los fusionó Artur sin revisión registrada en GitHub.

## 1. Registro de hallazgos del analyze inicial (2026-11-25)

Estados: **Resuelto** (corregido en el commit indicado) · **Verificado** (el analyze final confirma que ya no
aparece).

| ID | Historia / criterio | Gravedad | Evidencia (analyze inicial) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|-----------------------------|----------|-------------|---------|--------|-------------------|
| G1 | plan.md D6; T001 · regla de 000-core «Program.cs no se toca después de T009» | ALTA | La spec 001 añadía por su cuenta dos líneas a `Program.cs`, compartido, y la convención para 002–008 se aprobaría de rebote en el PR de HU-01 | Proponerlo **antes** en 000-core con un PR propio que corrija la regla de T009; T001 espera a ese PR. Convención: dos líneas por módulo, `Add<Modulo>()` y `Map<Modulo>()`, en orden fijo | Artur | Fusionado por Artur; sin revisión registrada | Verificado (analyze final) | PR #18 (`521e812`, 25/11 15:59, antes de la primera tarea de la 001) · 000-core plan.md «Registro de los módulos en la API» · tasks.md T024 |
| C1 | Constitución, «Acuerdos de colaboración» | MEDIA | La constitución pide ramas `spec-001` … `spec-008`; el grupo usa `feature/NNN-…` desde 000-core | Corregir la constitución con un **parche** (1.2.1) que escriba la práctica real, en lugar de renombrar ramas ya fusionadas. Decidido por Artur y Saul el 26/11/2026 | Artur (propuesta), Saul (aprobación) | Saul (aprueba el PR de la 1.2.1) | Resuelto al fusionar `feature/constitucion-1.2.1` | `.specify/memory/constitution.md` 1.2.1 |
| U1 | «Casos límite» (sin sesión) · T012 | MEDIA | Sin sesión se veía el formulario y solo al buscar se pedía iniciar sesión | Comprobar la sesión al entrar en `/buscar-pistas` y llevar a «Iniciar sesión» | Artur | Sin revisión registrada (#19) | Verificado (analyze final) | `8c1c27f` · tasks.md T012 · `BuscarPistas.razor` |
| U2 | RF-001 · quickstart §4 paso 5 | MEDIA | Un rol sin permiso que escribía la URL veía el formulario | Unido a U1: mensaje «La búsqueda de pistas es para ciudadanos y representantes de club.» sin formulario | Artur | Sin revisión registrada (#19) | Verificado (analyze final) | `8c1c27f` · tasks.md T012 |
| I1 | research.md D4 · contracts/modulos.md · T015 | MEDIA | `MotivoNoDisponible` devolvía `PETICION_INVALIDA` pero lanzaba `NO_ENCONTRADO` | Lanzar `ErrorNegocio` en los dos casos y reservar el `string?` para los seis motivos de RF-011 | Artur | Sin revisión registrada (#19) | Verificado (analyze final; reforzado por T027 de converge) | `8c1c27f` · research.md D4 · contracts/modulos.md · T015, T027 |
| E1 | CE-002 · research.md D10 | BAJA | Ninguna tarea medía el tiempo de respuesta | Medir B1 con `Measure-Command` en T022 | Artur | Sin revisión registrada (#19) | Verificado (30–44 ms) | `8c1c27f` · tasks.md T022 |
| I2 | research.md D2 · plan.md «Flujo» · T009 | BAJA | Orden de validaciones distinto en research y en plan | Orden 400 → 422 → 404 (el de T009) y research.md alineado | Artur | Sin revisión registrada (#19) | Verificado (analyze final) | `8c1c27f` · research.md D2 |
| T1 | T018 | BAJA | T018 cambiaba el reloj compartido sin restaurarlo | Usar un reloj propio (`DatosDisponibilidad.ConServicio`) | Artur | Sin revisión registrada (#19) | Verificado (analyze final) | `8c1c27f` · tasks.md T018 |
| T2 | «Casos límite» (franja fija y cierre a la vez) | BAJA | Ninguna prueba combinaba cierre y franja fija | Añadir el caso a T011 | Artur | Sin revisión registrada (#19) | Verificado (analyze final) | `8c1c27f` · tasks.md T011 · `BuscarFranjasTests.Una_franja_fija_con_un_cierre_encima_tampoco_aparece` |
| C2 | Constitución («la dependencia se anota en las dos specs») · T023 | BAJA | Las specs 002, 005 y 007 no existían | T023 pide a cada dueño anotar la provisional que sustituye al crear su spec | Artur | Sin revisión registrada (#19) | Verificado: 002 y 005 lo anotan; 007 fuera del alcance comprometido | `8c1c27f` · tasks.md T023 |

### Hallazgos nuevos del analyze final (2026-11-26)

| ID | Historia / criterio | Gravedad | Evidencia (analyze final) | Decisión | Responsable | Revisor | Estado | Dónde se resuelve |
|----|---------------------|----------|---------------------------|----------|-------------|---------|--------|-------------------|
| I1 | RF-014a · «Supuestos» · `ConsultaCierresProvisional` | MEDIA | La provisional de cierres «la sustituirá spec-007 (C)», pero HU-07 está fuera del alcance comprometido (constitución 1.2.0) | Nota de alcance: la provisional se queda como implementación de `IConsultaCierres` y base para trabajo futuro | Saul | Artur (revisa el cambio de Saul) | Resuelto (por Saul, #50) | contracts/modulos.md «Alcance (26/11/2026)» |
| I2 | plan.md «Código fuente» | BAJA | Lista dos provisionales ya retiradas (#31, #33) | Nota en plan.md | Artur | — | Pendiente (documentación) | plan.md |
| I3 | plan.md D6 | BAJA | Dice que la convención de `Program.cs` entra en el PR de HU-01; entró en el #18 | Nota en plan.md | Artur | — | Pendiente (documentación) | plan.md |
| E1 | CE-001 | BAJA | Sin tarea que mida «menos de 1 minuto» | Se da por cumplido con el recorrido de T021 (búsqueda completa con teclado en unos segundos) | Artur | — | Aceptado | tasks.md T021 |
| U1 | Cabecera de spec.md | BAJA | «Estado: Borrador» | Cambiar a «Implementada» | Saul | Artur (revisa el cambio de Saul) | Resuelto (por Saul, #51) | spec.md «Estado: Implementada» |

## 2. Matriz de trazabilidad de HU-01

| Criterio | Qué exige (resumen) | Decisión del plan | Tareas | Prueba |
|----------|---------------------|-------------------|--------|--------|
| RF-001 | Solo ciudadano y representante; sin sesión, 401 | D2 | T010, T011, T012 | `BuscarFranjasTests`: 401 sin sesión, 403 con personal y técnico; pantalla con mensaje para otros roles (T012) |
| RF-002 | Menú solo para quien puede buscar | D7 | T013 | Manual (T021): menú con ciudadana, representante y técnico |
| RF-003 | Tres filtros obligatorios | D2, D8 | T010, T012 | `BuscarFranjasTests.Sin_fecha_da_400_con_el_campo_marcado` |
| RF-004 | Deportes reservables por franjas y los cuatro centros | D8 | T012 | Manual (T021): desplegables «Tenis» y «Pádel», cuatro centros |
| RF-005 | Hoy por defecto; hoy..hoy+7 en Madrid; `FUERA_DE_ANTELACION` | D2, D8 | T010, T011, T012, T018 | `BuscarSinResultadosTests` (`Categoria=Limites`): `Hoy_mas_7_tiene_franjas_y_hoy_mas_8_esta_fuera_de_antelacion`, `Ayer_esta_fuera_de_antelacion` |
| RF-006 | Solo pistas del centro, reservables y del deporte | D3 | T009, T011 | `BuscarFranjasTests.Esc1_…` (solo las dos pistas del deporte); `ConsultaDisponibilidadTests.El_pabellon_no_es_reservable_por_franjas` |
| RF-007 | Solo franjas libres según RF-011 de 000-core | D3 | T008, T009, T011 | `BuscarFranjasTests` Esc2–Esc6: ocupadas, pendientes vigentes y caducadas, cerradas, bloqueadas y ya empezadas |
| RF-008 | Pista, horas de Madrid y precio en euros | D3, D8 | T009, T017 | `BuscarFranjasTests` (horas de pared); `PrecioPorTarifaTests` |
| RF-009 | Orden por pista y hora | D3 | T009, T011 | `BuscarFranjasTests` |
| RF-010 | Pista sin franjas libres no aparece | D3 | T009, T011 | `BuscarFranjasTests` |
| RF-011 | Mensaje sin resultados y sin pistas del deporte | D8 | T018, T019 | `BuscarSinResultadosTests`; manual en la pantalla |
| RF-012 | Precio con la tarifa de la sesión y el mismo cálculo que HU-02 | D4 | T014, T015 | `PrecioPorTarifaTests` |
| RF-013 | `IConsultaDisponibilidad` para HU-02, misma regla | D4 | T015, T016, T027 | `ConsultaDisponibilidadTests`; `ReservarComoEnLaBusquedaTests` (spec-002) |
| RF-014 | Leer solo por las consultas de otras historias | D5 | T003–T006, T008 | Revisión de código; ninguna escritura fuera de `Disponibilidad` |
| RF-014a | Provisionales de solo lectura, retiradas sin cambiar la búsqueda | D5, D9 | T003–T007 | `ConsultasProvisionalesTests`; todas las pruebas de HU-01 en verde tras el #31 y el #33 |
| RF-015 | WCAG 2.1 AA | D7, D8 | T012, T019, T020, T021, T026 | Manual (T021): teclado, anuncio, contraste, 320 px; Lighthouse 100 y axe sin graves |
| RF-016 | Español, Madrid, euros | D3, D8 | T012, T017, T020 | `BuscarFranjasTests` (cambio de hora del 25/10/2026); `FormatoEs` |
| CE-001 | Encontrar una franja en < 1 min | — | T021 | Manual (E1 del analyze final) |
| CE-002 | Resultado en < 2 s | D10 | T022 | Medición: 30–44 ms en 10 peticiones locales |
| CE-003 | Franjas mostradas = franjas libres en el 100 % | D3, D9 | T011, T018 | `BuscarFranjasTests` y `BuscarSinResultadosTests` (`Categoria=Limites`) |
| CE-004 | Precio = el que cobra HU-02 | D4 | T016 | `ConsultaDisponibilidadTests`; `ReservarComoEnLaBusquedaTests` (spec-002) |
| CE-005 | Empadronado y no empadronado ven precios distintos | D4 | T014 | `PrecioPorTarifaTests` |
| CE-006 | Teclado, Lighthouse ≥ 95, axe sin graves | D7 | T020, T021 | Manual (T021, 26/11/2026) |
| CE-007 | Ninguna búsqueda sin resultados deja la pantalla vacía | D8 | T018, T019 | `BuscarSinResultadosTests`; manual |

## 3. Revisión entre compañeros (enunciado 5.2)

| Criterio revisado | Revisor | Fecha | Qué comprobó | Resultado |
|-------------------|---------|-------|--------------|-----------|
| RF-005 (solo se busca desde hoy hasta hoy + 7 en hora de Madrid; fuera, `FUERA_DE_ANTELACION`) | Saul | 26/11/2026 | Las pruebas `BuscarSinResultadosTests.Hoy_mas_7_tiene_franjas_y_hoy_mas_8_esta_fuera_de_antelacion` (hoy + 7 → 200 con pistas; hoy + 8 → 422 con el texto del catálogo) y `Ayer_esta_fuera_de_antelacion`, y que `ServicioDisponibilidad.Buscar` (`ServicioDisponibilidad.cs:35–38`) compara la fecha con `HorarioMadrid.Hoy(Ahora)` y `ValoresNegocio.AntelacionMaximaDias`, sin escribir la cifra a mano | ✅ Cubierto; sin incidencias |
