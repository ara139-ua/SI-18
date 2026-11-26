# Guía de validación: 006 · Validar el acceso con el código QR

Cómo comprobar de principio a fin que HU-06 funciona. El arranque del entorno (Docker, `.env`, base de
datos y siembra) es el de [`000-core/quickstart.md`](../000-core/quickstart.md) §1–§3; aquí no se
repite.

## 1. Requisitos previos

- El entorno de 000-core arrancado (PostgreSQL de docker-compose y la API con la siembra hecha).
- `develop` con spec-002 fusionada (reservar, pagar y «Mis reservas» con el código).
- La contraseña de demostración (`SI18_CLAVE_DEMO` de tu `.env`).
- Una reserva **confirmada** de Polideportivo Este que empiece dentro de unos minutos, hecha con
  spec-002 (quickstart §3 de 002): como `ciudadana2.empadronada@si18.test`, reserva «Pista de pádel 2»
  de Polideportivo Este en la próxima hora en punto y págala con `4000 0000 0000 0001`. Anota su
  `codigoAcceso` (22 caracteres) → `{código}`.

## 2. Pruebas automáticas

```powershell
cd fuentes
dotnet test SI18.slnx                                         # todo en verde, también las de 000-core, 001, 002 y 005
dotnet test SI18.slnx --filter-trait "Categoria=Limites"      # ventana del QR (15/14 min, 1 s antes, fin, 1 s y 1 min después) y los dos cambios de hora
```

Las pruebas de HU-06 están en `tests/SI18.Api.Tests/Reservas/` (`ValidarAccesoTests`,
`LimitesValidacionTests`, `AvisoValidacionAnteriorTests`; research.md V11) y necesitan Docker
(Testcontainers). Comprueba además que no se ha escrito la zona horaria a mano:

```powershell
git grep -n "Europe/Madrid" -- fuentes/src/SI18.Api/Modulos/Reservas fuentes/src/SI18.Cliente/Reservas   # sin resultados
```

## 3. Escenarios manuales contra la API

Con un cliente HTTP con cookies (o desde el navegador con la sesión iniciada). Contrato:
[contracts/api-validaciones-acceso.openapi.yaml](./contracts/api-validaciones-acceso.openapi.yaml). Las
filas registradas se ven con `SELECT * FROM validaciones_acceso ORDER BY id DESC LIMIT 5;` en `psql`.

| #   | Acción                                                                                         | Resultado esperado                                                                                                 | Spec |
| --- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ---- |
| Q1  | Sesión como `personal.este@si18.test`; antes de 15 min del inicio: `POST /api/validaciones-acceso` con `{ "codigo": "{código}" }` | 422 `QR_FUERA_DE_VENTANA`, con `validacionId` y la extensión `reserva` (instalación, centro, fecha, horas), **sin titular**; fila con `aceptada = false` | HU2 esc. 1 |
| Q2  | Repite Q1 dentro de la ventana (desde 15 min antes del inicio)                                 | 201 con titular «Carla Vecina», «Pista de pádel 2», «Polideportivo Este», fecha y horas; `validacionAnteriorHora` nulo; fila con `aceptada = true` | HU1 esc. 1 |
| Q3  | Repite Q2 unos minutos después                                                                 | 201 con `validacionAnteriorHora` = la hora de Q2 (no la de Q1, que se rechazó)                                       | HU1 esc. 6–7 |
| Q4  | Repite Q2 con espacios delante y detrás del código                                             | 201, igual que Q3                                                                                                  | Casos límite |
| Q5  | `{ "codigo": "   " }` y `{}`                                                                    | 400 `PETICION_INVALIDA` con `errors.codigo`; **ninguna fila nueva**                                                | RF-004 |
| Q6  | `{ "codigo": "noexiste" }`                                                                      | 422 `QR_DESCONOCIDO`, sin extensión `reserva`; fila con `reserva_id` nulo                                          | HU2 esc. 4 |
| Q7  | Sesión como `personal.oeste@si18.test`; `{ "codigo": "{código}" }`                              | 403 `QR_OTRO_CENTRO`, sin extensión `reserva`; fila con su `reserva_id` y `aceptada = false`                       | HU2 esc. 5 |
| Q8  | Sesión como `ciudadana2.empadronada@si18.test`, `representante.padel@si18.test` o `tecnico@si18.test` | 403 `ROL_NO_PERMITIDO`; **ninguna fila nueva**                                                               | HU4 esc. 1 |
| Q9  | Sin sesión                                                                                      | 401 `NO_AUTENTICADO`; **ninguna fila nueva**                                                                       | HU4 esc. 2 |
| Q10 | Pasada la hora de fin de la reserva, repite Q2                                                  | 422 `QR_FUERA_DE_VENTANA` con la extensión `reserva`                                                               | HU2 esc. 2 |

Una reserva **cancelada** (`QR_RESERVA_NO_CONFIRMADA`) todavía no se puede preparar a mano: cancelar es
de spec-003 y spec-007, que no están en `develop`. Se valida con `ValidarAccesoTests` (cancelada con
`Reserva.Cancelar` de 000-core) y, de punta a punta, cuando se fusionen esas specs.

## 4. Escenarios manuales en la pantalla

1. Inicia sesión como `personal.este@si18.test`. El menú muestra «Validar acceso» (y no «Buscar pistas»
   ni «Mis reservas»). *(Si el cambio de `MainLayout.razor` —punto a coordinar C1— aún no está
   fusionado, abre `/validar-acceso` a mano.)*
2. Pulsa «Validar» con el campo vacío: el campo se marca y se anuncia «Escribe el código de acceso.»; no
   se envía nada.
3. Escribe `{código}` dentro de la ventana y pulsa «Validar» (o Intro): aparece «Acceso permitido» con
   icono, titular, instalación, centro, fecha y horas; el lector de pantalla lo anuncia; el campo queda
   vacío y con el foco.
4. Valídalo otra vez: además aparece el aviso «Este código ya se validó hoy a las HH:mm» con icono.
5. Escribe `noexiste`: aparece «Acceso denegado» con icono distinto y «Código no válido.», sin datos de
   ninguna reserva.
6. Como `personal.oeste@si18.test`, valida `{código}`: «Acceso denegado» y «Este código pertenece a una
   reserva de otro centro.», sin titular ni franja.
7. Como `ciudadana2.empadronada@si18.test`, el menú no muestra «Validar acceso», y `/validar-acceso`
   escrita a mano muestra un aviso, sin formulario. Sin sesión, lleva a «Iniciar sesión».

## 5. Accesibilidad (quickstart.md §6 de 000-core, aplicado a `/validar-acceso`)

1. Valida un código aceptado y otro rechazado solo con el teclado, con el foco siempre visible; tras
   cada resultado el foco vuelve al campo (CE-006, RF-014).
2. Con un lector de pantalla (NVDA):
   - el campo se anuncia con su etiqueta, su ayuda y, si está vacío al enviar, con su error;
   - cada resultado se anuncia **una sola vez** con el texto completo (permitido o denegado, el motivo y
     los datos), incluido el aviso de validación anterior.
3. Ningún resultado se entiende solo por el color: «Acceso permitido» y «Acceso denegado» están escritos
   y tienen iconos distintos.
4. A 320 px de ancho (DevTools, modo móvil): campo, botón y resultado completos, sin desplazamiento
   horizontal (RF-015).
5. Lighthouse → Accesibilidad ≥ 95 y axe DevTools sin infracciones graves ni críticas, con el formulario
   vacío, con un resultado aceptado (con aviso) y con uno rechazado.
6. Anota el resultado en la descripción del PR.
