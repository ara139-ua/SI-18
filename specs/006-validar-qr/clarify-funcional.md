# Clarify sobre la versión funcional · 006-validar-qr (HU-06)

**Responsable**: Saul. **Revisor**: Artur (revisión cruzada de RF-002, ver analyze-registro.md). **Comando**: `/speckit-clarify` (Spec Kit 1.1.2.dev0) sobre `specs/006-validar-qr`, después de probar la aplicación ya implementada (enunciado, 5.1). La diferencia de la spec antes y después está en el commit de esta pasada y la de la primera pasada, en [clarify-diff.md](./clarify-diff.md).

## 1. Tres situaciones probadas

**Cómo se probó**: contra la API local (`https://localhost:7180`, `dotnet run` con la siembra de demo y
PostgreSQL de `docker-compose`), con sesiones reales de los usuarios ficticios de la siembra, el 26/11/2026,
sobre el commit `32931fb` de `develop` (mismo código que el commit de referencia `36a3166`:
`docs/evidencias/commit-referencia.md`). Las peticiones las hizo un script de Saul fuera del repositorio; aquí
se copian la petición, los datos, el resultado esperado y el observado. Los códigos de acceso no se copian
(son datos de la base local). Las horas son de Madrid.

Preparación: `ciudadana2.empadronada@si18.test` reserva y paga la franja de las 21:00 del 26/11/2026 en una pista de pádel del Polideportivo Este (pasos «preparación»).

| Situación | Hora (Madrid) | Usuario | Petición | Datos | Esperado | Observado |
|-----------|---------------|---------|----------|-------|----------|-----------|
| preparación | 26/11/2026 20:16:49 | `ciudadana2.empadronada@si18.test` | `POST /api/reservas` | `{"instalacionId": 17, "inicioUtc": "2026-11-26T20:00:00Z", "importeMostrado": 6.0}` | 201 | **201** · `estado`: pendiente_pago; `importe`: 6.0; `horaLimitePago`: 20:26; `centro`: Polideportivo Este; `fecha`: 2026-11-26; `horaInicio`: 21:00; `horaFin`: 22:00 |
| preparación | 26/11/2026 20:16:49 | `ciudadana2.empadronada@si18.test` | `POST /api/reservas/48/pago` | `{"numeroTarjeta": "4000 0000 0000 0001"}` | 200 confirmada | **200** · `estado`: confirmada; `importe`: 6.0; `centro`: Polideportivo Este; `fecha`: 2026-11-26; `horaInicio`: 21:00; `horaFin`: 22:00; `codigoAcceso`: 22 caracteres |
| S2 límite | 26/11/2026 20:16:49 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(código de la reserva de prueba)"}` | Denegado: fuera de la ventana (antes de las 20:45), con datos de la reserva y sin titular | **422** · `codigo`: QR_FUERA_DE_VENTANA; `status`: 422; `detail`: El código solo es válido desde 15 minutos antes del inicio hasta el final de la reserva.; `reserva`: Pista de pádel 1 2026-11-26 21:00–22:00 (sin titular) |
| S2 entrada inválida | 26/11/2026 20:16:49 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "   "}` | 400 PETICION_INVALIDA, sin registro | **400** · `codigo`: PETICION_INVALIDA; `status`: 400; `detail`: Revisa los datos marcados.; `errors`: {"codigo": ["Escribe el código de acceso."]} |
| S2 entrada inválida | 26/11/2026 20:16:49 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "noexiste"}` | Denegado QR_DESCONOCIDO, sin datos de reserva | **422** · `codigo`: QR_DESCONOCIDO; `status`: 422; `detail`: Código no válido. |
| S3 permiso | 26/11/2026 20:16:49 | `ciudadana2.empadronada@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(código de la reserva de prueba)"}` | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `status`: 403; `detail`: Tu perfil no permite realizar esta acción. |
| S3 permiso | 26/11/2026 20:16:49 | `personal.oeste@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(código de la reserva de prueba)"}` | Denegado QR_OTRO_CENTRO, sin datos de reserva | **403** · `codigo`: QR_OTRO_CENTRO; `status`: 403; `detail`: Este código pertenece a una reserva de otro centro. |
| criterio nuevo (mayúsculas) | 26/11/2026 20:30:23 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(el código de la reserva con una letra en otra caja)"}` | 422 QR_DESCONOCIDO | **422** · `codigo`: QR_DESCONOCIDO; `status`: 422; `detail`: Código no válido. |
| S1 habitual | 26/11/2026 20:46:00 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(código de la reserva de prueba)"}` | 201 aceptado: titular, instalación, centro, fecha y horario; sin aviso | **201** · `titular`: Carla Vecina; `instalacion`: Pista de pádel 1; `centro`: Polideportivo Este; `fecha`: 2026-11-26; `horaInicio`: 21:00; `horaFin`: 22:00 |
| S1 habitual | 26/11/2026 20:46:00 | `personal.este@si18.test` | `POST /api/validaciones-acceso` | `{"codigo": "(código de la reserva de prueba)"}` | 201 aceptado con aviso de la validación anterior | **201** · `titular`: Carla Vecina; `instalacion`: Pista de pádel 1; `centro`: Polideportivo Este; `fecha`: 2026-11-26; `horaInicio`: 21:00; `horaFin`: 22:00; `validacionAnteriorHora`: 20:45 |

Todas dieron el resultado esperado.
La fila «criterio nuevo (mayúsculas)» comprueba la respuesta a la pregunta 2 después de integrarla.

## 2. Preguntas, respuestas y justificación

Integradas en spec.md, «Clarifications», sesión «2026-11-26 (sobre la versión funcional)».

| # | Pregunta | Respuesta | Justificación (resumen) | Historia y criterios afectados |
|---|----------|-----------|-------------------------|--------------------------------|
| 1 | ¿El acceso de un club a su franja fija aprobada lo valida el personal en esta historia? | No; fuera de alcance | La descripción solo habla de validar los QR de las reservas, y una franja fija no genera reserva ni código (principio VIII) | «Fuera de alcance» |
| 2 | ¿El código se compara distinguiendo mayúsculas y minúsculas? | Sí, exacto; otra caja → `QR_DESCONOCIDO` | Es el código que se ve en «Mis reservas»; ignorar la caja exigiría cambiar el índice único de 000-core y reduciría la aleatoriedad | RF-004, «Casos límite» |

Relacionada con spec-002 (pregunta 3 de su pasada): en las reservas con tarifa club, el titular que se muestra al aceptar es el representante que reservó. No cambia nada en HU-06.

## 3. Criterios afectados en «Dado… Cuando… Entonces…»

- **RF-004 (mayúsculas)** — **Dado** una reserva confirmada de su centro dentro de su ventana, **cuando** el personal introduce su código con una letra en otra caja, **entonces** se rechaza con `QR_DESCONOCIDO`, sin datos de la reserva, y el intento queda registrado.
- **Fuera de alcance (franjas fijas)** — **Dado** un club que llega a su franja fija aprobada, **cuando** el personal quiere comprobar su acceso, **entonces** HU-06 no ofrece ninguna validación: no hay código.

## 4. Qué queda

- RF-004 (mayúsculas) ya se cumplía (`ServicioValidarAcceso.cs:31` compara exacto); se comprobó a mano (tabla de arriba) y después se añadió la prueba automática en T034 (`ValidarAccesoTests.RF004_el_codigo_con_una_letra_en_otra_caja_es_desconocido_y_se_registra_sin_reserva`), con analyze ([analyze-3.md](./analyze-3.md)) y converge ([converge-2026-11-26-3.md](./converge-2026-11-26-3.md)) repetidos en esta spec.
