# Clarify sobre la versión funcional · 002-reservar-pagar (HU-02)

**Responsable**: Saul. **Revisor**: Artur (revisión cruzada de CE-005, ver analyze-registro.md). **Comando**: `/speckit-clarify` (Spec Kit 1.1.2.dev0) sobre `specs/002-reservar-pagar`, después de probar la aplicación ya implementada (enunciado, 5.1). La diferencia de la spec antes y después está en el commit de esta pasada (`git diff` de `spec.md`) y la de la primera pasada, en [clarify-diff.md](./clarify-diff.md).

## 1. Tres situaciones probadas

**Cómo se probó**: contra la API local (`https://localhost:7180`, `dotnet run` con la siembra de demo y
PostgreSQL de `docker-compose`), con sesiones reales de los usuarios ficticios de la siembra, el 26/11/2026,
sobre el commit `32931fb` de `develop` (mismo código que el commit de referencia `36a3166`:
`docs/evidencias/commit-referencia.md`). Las peticiones las hizo un script de Saul fuera del repositorio; aquí
se copian la petición, los datos, el resultado esperado y el observado. Los códigos de acceso no se copian
(son datos de la base local). Las horas son de Madrid.

| Situación | Hora (Madrid) | Usuario | Petición | Datos | Esperado | Observado |
|-----------|---------------|---------|----------|-------|----------|-----------|
| S1 habitual | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `GET /api/disponibilidad/franjas?deporteId=2&centroId=1&fecha=2026-11-27` | — | 200 con franjas libres | **200** · `fecha`: 2026-11-27 |
| S1 habitual | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `GET /api/reservas/resumen?instalacionId=3&inicioUtc=2026-11-27T20:00:00Z` | — | 200, importe 6.00 | **200** · `importe`: 6.0; `centro`: Polideportivo Norte; `fecha`: 2026-11-27; `horaInicio`: 21:00; `horaFin`: 22:00 |
| S1 habitual | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas` | `{"instalacionId": 3, "inicioUtc": "2026-11-27T20:00:00Z", "importeMostrado": 6.0}` | 201 pendiente_pago con horaLimitePago | **201** · `estado`: pendiente_pago; `importe`: 6.0; `horaLimitePago`: 20:26; `centro`: Polideportivo Norte; `fecha`: 2026-11-27; `horaInicio`: 21:00; `horaFin`: 22:00 |
| S1 habitual | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas/46/pago` | `{"numeroTarjeta": "4000 0000 0000 0001"}` | 200 confirmada con codigoAcceso | **200** · `estado`: confirmada; `importe`: 6.0; `centro`: Polideportivo Norte; `fecha`: 2026-11-27; `horaInicio`: 21:00; `horaFin`: 22:00; `codigoAcceso`: 22 caracteres |
| S2 límite | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas` | `{"instalacionId": 4, "inicioUtc": "2026-12-03T20:00:00Z", "importeMostrado": 6.0}` | Día 7 (21:00, la última franja): dentro de la antelación | **201** · `estado`: pendiente_pago; `importe`: 6.0; `horaLimitePago`: 20:26; `centro`: Polideportivo Norte; `fecha`: 2026-12-03; `horaInicio`: 21:00; `horaFin`: 22:00 |
| S2 límite | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas` | `{"instalacionId": 4, "inicioUtc": "2026-12-04T09:00:00Z", "importeMostrado": 6.0}` | 422 FUERA_DE_ANTELACION | **422** · `codigo`: FUERA_DE_ANTELACION; `status`: 422; `detail`: Solo puedes reservar desde hoy hasta dentro de 7 días. |
| S2 entrada inválida | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas/47/pago` | `{"numeroTarjeta": "4000 0000 0000 001"}` | 400 PETICION_INVALIDA con errors.numeroTarjeta | **400** · `codigo`: PETICION_INVALIDA; `status`: 400; `detail`: Revisa los datos marcados.; `errors`: {"numeroTarjeta": ["Escribe los 16 dígitos de la tarjeta."]} |
| S2 entrada inválida | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `GET /api/reservas/47` | — | Sigue pendiente_pago | **200** · `estado`: pendiente_pago; `importe`: 6.0; `horaLimitePago`: 20:26; `centro`: Polideportivo Norte; `fecha`: 2026-12-03; `horaInicio`: 21:00; `horaFin`: 22:00 |
| S3 permiso | 26/11/2026 20:16:48 | `personal.norte@si18.test` | `POST /api/reservas` | `{"instalacionId": 3, "inicioUtc": "2026-11-27T20:00:00Z", "importeMostrado": 6.0}` | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `status`: 403; `detail`: Tu perfil no permite realizar esta acción. |
| S3 estado | 26/11/2026 20:16:48 | `ciudadana.empadronada@si18.test` | `POST /api/reservas/46/pago` | `{"numeroTarjeta": "4000 0000 0000 0001"}` | 409 TRANSICION_NO_PERMITIDA (ya confirmada) | **409** · `codigo`: TRANSICION_NO_PERMITIDA; `status`: 409; `detail`: Esta reserva ya no se puede modificar en su estado actual. |

Todas dieron el resultado esperado. Al probarlas se vio además un caso que la spec no decidía: pagar una reserva cuya franja ya ha empezado (pregunta 1).

## 2. Fallo previo de la pregunta 1 (prueba reproducible, enunciado 5.3)

### Reproducción en vivo (interrumpida)

El 26/11/2026 se intentó reproducir el fallo contra la API local: `ciudadana.empadronada@si18.test` reservó a las 20:56 la franja de las 21:00 de «Pista de tenis 1» del Polideportivo Norte (201, `pendiente_pago`) para pagarla a las 21:01:30, con la franja ya empezada y dentro de los 10 minutos. **Antes del pago, Claude Code paró la API y el script por falta de memoria en el equipo**, así que el pago no se llegó a hacer y la reproducción en vivo quedó interrumpida. No se volvió a arrancar la API (decisión de Saul), y la reserva de prueba (id 49) y su pago se borraron de la base local.

### Evidencia del fallo previo: la prueba automática de T056, en rojo

Por eso la evidencia del fallo previo es la prueba automática de T056, escrita **antes** de tocar el código y ejecutada sobre el código sin corregir (`develop` en `32931fb`, igual que el commit de referencia `36a3166`) el 26/11/2026 con `dotnet test SI18.slnx --no-build --filter-method "*RF009a*" --filter-method "*Confirmar_en_el_inicio_de_la_franja*" --filter-method "*Confirmar_1_s_antes_del_inicio*"` (base de datos de Testcontainers, no la local). Datos: Polideportivo Este, «Pista de tenis 1», 27/12/2026; reserva creada 5 minutos antes del inicio; reloj propio de la prueba.

| Prueba | Datos | Esperado (RF-009a) | Observado con el código anterior |
|--------|-------|--------------------|----------------------------------|
| `ReservaTests.Confirmar_en_el_inicio_de_la_franja_lanza_FRANJA_YA_EMPEZADA_y_no_cambia_nada` | `Confirmar` en el inicio | `FRANJA_YA_EMPEZADA`; sigue pendiente | ❌ No lanza nada: la reserva se confirma |
| `PagarReservaTests.RF009a_pagar_en_el_inicio_dentro_de_los_10_minutos_caduca_sin_cobrar_con_FRANJA_YA_EMPEZADA` | Pago a las 11:00 de una reserva de las 11:00 creada a las 10:55 | `FRANJA_YA_EMPEZADA`; caducada, pago fallido, sin código y 0 cobros | ❌ No lanza nada: se cobra y queda confirmada |
| `PagarReservaTests.RF009a_despues_de_FRANJA_YA_EMPEZADA_un_segundo_intento_es_TRANSICION_NO_PERMITIDA_sin_cobro` | Pagos a las 12:01 y 12:02 de una reserva de las 12:00 | `FRANJA_YA_EMPEZADA` y luego `TRANSICION_NO_PERMITIDA`; 0 cobros | ❌ El primer pago se acepta |
| `ReservaTests.Confirmar_1_s_antes_del_inicio_confirma` | `Confirmar` 1 s antes del inicio | Confirmada | ✅ (lado permitido del límite) |
| `PagarReservaTests.RF009a_pagar_1_s_antes_del_inicio_cobra_y_confirma` | Pago a las 09:59:59 de una reserva de las 10:00 | Confirmada, 1 cobro | ✅ (lado permitido) |
| `PagarReservaTests.RF009a_con_10_minutos_y_la_franja_empezada_responde_RESERVA_CADUCADA_porque_RF009_va_primero` | Reserva creada 10 min antes del inicio, pagada en el inicio | `RESERVA_CADUCADA`, 0 cobros | ✅ (ya se cumplía) |

Resumen de la ejecución: `total: 6 · error: 3 · correcto: 3`. Los tres fallos dicen `Assert.Throws() Failure: No exception was thrown` (se esperaba `ErrorNegocio`): es exactamente el defecto de la pregunta 1.

La salida en verde, después de implementar T056, se añadirá aquí con el commit de la corrección.

## 3. Preguntas, respuestas y justificación

Las tres están integradas en spec.md, «Clarifications», sesión «2026-11-26 (sobre la versión funcional)», con su justificación y las secciones afectadas.

| # | Pregunta | Respuesta | Justificación (resumen) | Historia y criterios afectados |
|---|----------|-----------|-------------------------|--------------------------------|
| 1 | ¿Se puede pagar una reserva pendiente cuando su franja ya ha empezado, aunque no hayan pasado 10 minutos? | No: caduca, pago fallido, sin cobro, `FRANJA_YA_EMPEZADA`; hora límite = la más temprana entre creación + 10 min e inicio | Reservar una franja empezada ya está prohibido; la versión funcional lo permitía al pagar (sección 2) y no se acepta solo porque ya funcione así | HU-2 esc. 6, HU-5 esc. 3, «Casos límite», RF-008a, RF-009a (nuevo), RF-011, RF-017 |
| 2 | ¿Puede un representante reservar una franja bloqueada por una franja fija de su propio club? | No: `FRANJA_BLOQUEADA_CLUB`, como a cualquiera | Las franjas fijas no se cobran: el club ya la tiene | HU-4 esc. 3, «Casos límite» |
| 3 | Las reservas con tarifa club, ¿son del representante o del club? | Del representante que reserva | En 000-core la reserva es de un usuario; hacerla del club cambiaría el modelo común | HU-1 esc. 4, RF-013, «Entidades clave» |

## 4. Criterios afectados en «Dado… Cuando… Entonces…»

- **HU-2 esc. 6** — **Dado** que Ana creó a las 20:55 una reserva pendiente para la franja de las 21:00, **cuando** intenta pagarla a las 21:00:00 o después (aunque no hayan pasado 10 minutos), **entonces** se rechaza con `FRANJA_YA_EMPEZADA`, la reserva pasa a caducada, su pago a fallido y no se cobra nada.
- **HU-5 esc. 3** — **Dado** que Ana tiene una reserva pendiente de pago vigente, **cuando** abre «Mis reservas», **entonces** la ve como «Pendiente de pago» y puede pagarla mientras siga vigente y su franja no haya empezado.
- **HU-4 esc. 3** — **Dado** una franja bloqueada por una franja fija aprobada del club de Elena, **cuando** Elena (representante de ese club) intenta reservarla, **entonces** se rechaza con `FRANJA_BLOQUEADA_CLUB` y no se crea ninguna reserva.
- **HU-1 esc. 4** — **Dado** que Elena, representante de club, reserva y paga con tarifa club, **cuando** otra persona (aunque sea de su club) intenta ver o pagar esa reserva, **entonces** se rechaza con `NO_ENCONTRADO`: la reserva es de Elena.

## 5. Qué se hace después

- [analyze-final-2026-11-26.md](./analyze-final-2026-11-26.md): D1 (la causa nueva de caducidad debe proponerse antes en 000-core) y C1–C2 (criterios nuevos sin tarea).
- [converge-2026-11-26-2.md](./converge-2026-11-26-2.md): tareas T055–T060 (fase 12 de tasks.md), pendientes de aprobación.
