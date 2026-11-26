# Clarify sobre la versión funcional · 005-franjas-fijas (HU-05)

**Responsable**: Artur. **Revisor**: Saul (revisión cruzada). **Comando**: `/speckit-clarify` (Spec Kit 1.1.2.dev0)
sobre `specs/005-franjas-fijas`, después de probar la aplicación ya implementada (enunciado, 5.1). La diferencia de
la spec antes y después de esta pasada está en el apartado 5; la de la primera pasada, en [clarify-diff.md](./clarify-diff.md).

## 1. Tres situaciones probadas

**Cómo se probó**: contra la API local (`https://localhost:7180`, `dotnet run` con la siembra de demo en una base
propia, `si18_demo`, y PostgreSQL de `docker-compose`), con sesiones reales de los usuarios ficticios de la siembra,
el 26/11/2026, sobre `develop` en `027ab28`. Las peticiones las hizo un script de Artur fuera del repositorio
(`situaciones.mjs`, que reinicia los datos antes de empezar); aquí se copian la petición, los datos, el resultado
esperado según la spec antes de esta pasada y el observado. Las horas son de Madrid; «hoy» es el 26/11/2026.

Instalaciones: 5 = pabellón del Polideportivo Norte, 6 = sala del Norte, 12 = pabellón del Polideportivo Sur.
Temporada 1 = 2026-2027.

| Situación | Hora (Madrid) | Usuario | Petición | Datos | Esperado | Observado |
|-----------|---------------|---------|----------|-------|----------|-----------|
| S1 habitual | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"21:00"}` | 201 pendiente, a nombre del club Norte | **201** · `id`: 1; `estado`: pendiente; `clubNombre`: Club Deportivo Ficticio Norte; `instalacion`: Pabellón (Polideportivo Norte) |
| S1 habitual | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `GET /api/solicitudes-franjas-fijas` | — | 200: la ve en la lista de su club | **200** · 1 solicitud(es); Club Deportivo Ficticio Norte pendiente |
| S2 límite | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"21:00"}` | Sin decidir en la spec (pregunta 2) | **201** · `id`: 2; `estado`: pendiente; `clubNombre`: Club Deportivo Ficticio Norte; `instalacion`: Pabellón (Polideportivo Norte) |
| S2 entrada inválida | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,2],"horaInicio":"19:00","horaFin":"21:00"}` | 400 con diasSemana marcado | **400** · `codigo`: PETICION_INVALIDA; `detail`: Revisa los datos marcados.; `errors`: {"diasSemana":["Elige al menos un día, sin repetir."]} |
| S2 entrada inválida | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:30","horaFin":"21:00"}` | 400 con horaInicio marcado | **400** · `codigo`: PETICION_INVALIDA; `detail`: Revisa los datos marcados.; `errors`: {"horaInicio":["Elige una hora en punto entre las 08:00 y las 22:00."]} |
| S2 entrada inválida | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"23:00"}` | 400 con horaFin marcado | **400** · `codigo`: PETICION_INVALIDA; `detail`: Revisa los datos marcados.; `errors`: {"horaFin":["Elige una hora en punto entre las 08:00 y las 22:00."]} |
| S2 entrada inválida | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":6,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"21:00"}` | Rechazo explicando que no admite franjas fijas (esc. 3) | **400** · `codigo`: PETICION_INVALIDA; `detail`: Revisa los datos marcados.; `errors`: {"instalacionId":["Elige una pista o un pabellón."]} |
| S2 límite | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":12,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"21:00"}` | Sin decidir en la spec (pregunta 1) | **201** · `id`: 3; `estado`: pendiente; `clubNombre`: Club Deportivo Ficticio Norte; `instalacion`: Pabellón (Polideportivo Sur) |
| S3 permiso | 26/11/2026 22:33:45 | `ciudadana.empadronada@si18.test` | `POST /api/solicitudes-franjas-fijas` | `{"instalacionId":5,"temporadaId":1,"diasSemana":[2,4],"horaInicio":"19:00","horaFin":"21:00"}` | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `detail`: Tu perfil no permite realizar esta acción. |
| S3 permiso | 26/11/2026 22:33:45 | `representante.norte@si18.test` | `GET /api/solicitudes-franjas-fijas/1/conflictos` | — | 403 ROL_NO_PERMITIDO | **403** · `codigo`: ROL_NO_PERMITIDO; `detail`: Tu perfil no permite realizar esta acción. |
| S3 estado | 26/11/2026 22:33:45 | `tecnico@si18.test` | `POST /api/solicitudes-franjas-fijas/1/aprobacion` | — | 200 aprobada | **200** · `estado`: aprobada |
| S3 estado | 26/11/2026 22:33:45 | `tecnico@si18.test` | `POST /api/solicitudes-franjas-fijas/1/rechazo` | — | 409 SOLICITUD_YA_RESUELTA | **409** · `codigo`: SOLICITUD_YA_RESUELTA; `detail`: Esta solicitud ya está resuelta y no se puede volver a aprobar ni rechazar. |
| S3 estado | 26/11/2026 22:33:45 | `tecnico@si18.test` | `GET /api/solicitudes-franjas-fijas` | — | 200: todas, pendientes primero | **200** · 2:pendiente, 3:pendiente, 1:aprobada |
| criterio nuevo (pregunta 2) | 26/11/2026 22:41:06 | `tecnico@si18.test` | `POST /api/solicitudes-franjas-fijas/2/aprobacion` | — (la copia, con la 1 ya aprobada) | 409 FRANJA_FIJA_SOLAPADA | **409** · `codigo`: FRANJA_FIJA_SOLAPADA; `detail`: Se solapa con otra franja fija ya aprobada en esa instalación y temporada. |

Todas dieron el resultado esperado salvo dos puntos que la spec no decidía: la solicitud repetida mientras la
primera sigue pendiente y el pabellón de otro centro. Son las dos preguntas de abajo. La sala dio 400
`PETICION_INVALIDA` con `instalacionId` marcado («Elige una pista o un pabellón.»), que es lo que piden el
escenario 3 y RF-005; el 422 que preveía el script era un error del script, no de la aplicación.
La fila «criterio nuevo» comprueba la respuesta a la pregunta 2 después de integrarla.

## 2. Preguntas, respuestas y justificación

Integradas en spec.md, «Clarifications», sesión «2026-11-26 (sobre la versión funcional, enunciado 5.1)».

| # | Pregunta | Respuesta | Justificación (resumen) | Historia y criterios afectados |
|---|----------|-----------|-------------------------|--------------------------------|
| 1 | ¿Un club puede pedir franjas fijas en una instalación de cualquier centro, o solo de uno «suyo»? | De cualquier centro | En el README un club no pertenece a ningún centro, y en 000-core `Club` solo tiene nombre. Limitarlo exigiría cambiar el modelo común y un error nuevo sin que la descripción lo pida | RF-001, «Casos límite» (Instalación de otro centro) |
| 2 | Si un club envía una solicitud idéntica a otra suya que sigue pendiente, ¿se acepta? | Sí; el técnico rechaza la sobrante y, si aprueba una, la otra da `FRANJA_FIJA_SOLAPADA` | Coherente con «un club puede tener varias solicitudes pendientes»: solo las aprobadas no pueden solaparse, y eso ya se comprueba al aprobar (RF-009). Rechazar el duplicado exigiría otro código de error en 000-core | «Casos límite» (Representante con varias solicitudes), RF-009 |

## 3. Criterios afectados en «Dado… Cuando… Entonces…»

- **RF-001 (otro centro)**: **Dado** Diego, representante del club Norte, **cuando** pide el pabellón del
  Polideportivo Sur los martes y jueves de 19:00 a 21:00, **entonces** la solicitud queda **pendiente** a
  nombre de su club, igual que si pidiera el del Norte.
- **Varias solicitudes (duplicado)**: **Dado** una solicitud pendiente del club Norte, **cuando** Diego envía
  otra idéntica, **entonces** también queda **pendiente**; y **dado** que el técnico aprueba una de las dos,
  **cuando** intenta aprobar la otra, **entonces** se rechaza con `FRANJA_FIJA_SOLAPADA` y sigue pendiente
  (el técnico puede rechazarla).

## 4. Qué queda

- Las dos respuestas ya se cumplían en el código (tabla de arriba). La 1 ya tenía prueba automática
  (`SolicitarFranjasFijasTests.Esc1`: el club Norte pide el pabellón del Sur); la 2 se añadió en T031
  (`ConflictosFranjasFijasTests.Una_solicitud_identica_a_otra_pendiente_del_club_se_acepta_y_solo_puede_aprobarse_una`).
- `plan.md` no cambia. `tasks.md` solo gana T031.
- Analyze repetido: [analyze-funcional.md](./analyze-funcional.md) (E3, resuelto con T031). Converge:
  [converge-2026-11-26-2.md](./converge-2026-11-26-2.md). Batería completa en verde después del cambio (410 pruebas).

## 5. Diferencia de spec.md

```diff
diff --git a/specs/005-franjas-fijas/spec.md b/specs/005-franjas-fijas/spec.md
index 4c1a2f4..6e5bed3 100644
--- a/specs/005-franjas-fijas/spec.md
+++ b/specs/005-franjas-fijas/spec.md
@@ -56,6 +56,15 @@ ya enviada o aprobada, cobrar las franjas fijas (no se cobran, D9 de 000-core) y
   como conflicto que impide aprobar? → A: Sí. Cualquier reserva confirmada o pendiente de pago vigente
   es un conflicto, sea de quien sea.
 
+### Session 2026-11-26 (sobre la versión funcional, enunciado 5.1)
+
+- Q: ¿Un club puede pedir franjas fijas en una instalación de cualquier centro, o solo de uno «suyo»?
+  → A: De cualquier centro. Un club no pertenece a ningún centro (en `000-core` solo tiene nombre), así
+  que la solicitud no se limita por centro.
+- Q: Si un club envía una solicitud idéntica (misma instalación, días, horario y temporada) a otra suya
+  que sigue pendiente, ¿se acepta? → A: Sí, queda pendiente como cualquier otra. El técnico rechaza la
+  que sobre; si aprueba una, la otra ya no puede aprobarse porque se solapa (`FRANJA_FIJA_SOLAPADA`).
+
 ## Escenarios de usuario y pruebas *(obligatorio)*
 
 ### Historia de usuario 1 - Solicitar franjas fijas para mi club (Prioridad: P1)
@@ -243,7 +252,11 @@ pantalla, y se pasa la comprobación de quickstart.md §6 de `000-core`.
 - **Mismo horario, otro día**: martes 19:00–21:00 y miércoles 19:00–21:00 no se solapan.
 - **Otra instalación u otra temporada**: no hay solape.
 - **Representante con varias solicitudes**: un club puede tener varias solicitudes pendientes o
-  aprobadas a la vez, siempre que las aprobadas no se solapen entre sí.
+  aprobadas a la vez, siempre que las aprobadas no se solapen entre sí. Esto incluye una solicitud
+  idéntica a otra suya pendiente: se acepta, y en cuanto una se aprueba, la otra se rechaza al intentar
+  aprobarla con `FRANJA_FIJA_SOLAPADA`.
+- **Instalación de otro centro**: un club no pertenece a ningún centro; su representante puede pedir
+  una pista o un pabellón de cualquier centro.
 - **Dos representantes del mismo club**: ven las mismas solicitudes (son del club) y los dos reciben
   los avisos.
 - **Reserva del propio club**: si un representante del club tiene una reserva suelta en una de esas
@@ -264,7 +277,7 @@ pantalla, y se pasa la comprobación de quickstart.md §6 de `000-core`.
 **Solicitar (representante de club)**
 
 - **RF-001**: Solo un representante de club DEBE poder enviar solicitudes, siempre a nombre de su
-  propio club (el de su sesión, no uno elegido).
+  propio club (el de su sesión, no uno elegido), y para una instalación de cualquier centro.
 - **RF-002**: La solicitud DEBE indicar instalación, días de la semana, hora de inicio, hora de fin y
   temporada, con las reglas de `000-core` (data-model.md § SolicitudFranjasFijas): instalación pista o
   pabellón (D2); al menos un día, sin repetir, de lunes a domingo; horas en punto, inicio ≥ 08:00, fin ≤
```
