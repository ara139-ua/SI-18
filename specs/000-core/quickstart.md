# Guía rápida y validación: 000-core

**Fecha**: 2026-10-28 · Detalles en [plan.md](./plan.md), [data-model.md](./data-model.md) y
[contracts/](./contracts/README.md).

Esta guía sirve para arrancar la base común y comprobar que funciona de extremo a extremo. No contiene
código de implementación.

## 1. Requisitos previos

| Herramienta                   | Versión                         | Comprobación               |
| ----------------------------- | ------------------------------- | -------------------------- |
| SDK de .NET                   | **10.0.x (LTS)**: ver la nota   | `dotnet --list-sdks`       |
| Docker Desktop (con Compose)  | reciente                        | `docker compose version`   |
| Git                           | —                               | `git --version`            |

> **Nota**: el equipo de referencia solo tiene el SDK 9.0.312 (STS, sin soporte desde el
> 10/11/2026). Instala el SDK de .NET 10, por ejemplo con
> `winget install Microsoft.DotNet.SDK.10`. `fuentes/global.json` exige la banda `10.0.x`.

## 2. Configuración local (sin secretos en el repositorio)

```powershell
cd fuentes
Copy-Item .env.example .env          # rellena los valores; .env está en .gitignore
docker compose up -d                 # PostgreSQL 17 con healthcheck
docker compose ps                    # el servicio «postgres» debe estar «healthy»

dotnet user-secrets --project src/SI18.Api set "ConnectionStrings:Si18" "Host=localhost;Port=5432;Database=si18;Username=si18;Password=<la de .env>"
dotnet user-secrets --project src/SI18.Api set "SI18_CLAVE_DEMO" "<contraseña de demo de .env>"
```

Las variables de `.env.example` son:

- `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` y `POSTGRES_PORT`: para docker-compose.
- `ConnectionStrings__Si18`: para la API, como alternativa a user-secrets.
- `SI18_CLAVE_DEMO`: contraseña común de los usuarios de demo.
- `SI18_SEMBRAR_DATOS=true`: siembra los datos de demo.
- `SI18_TEST_CONNECTION`: opcional; hace que las pruebas usen esta base de datos en lugar de
  Testcontainers.

## 3. Compilar, migrar, sembrar y arrancar

```powershell
dotnet build SI18.slnx                                        # sin errores ni advertencias tratadas como error
dotnet tool restore                                           # dotnet-ef fijado en .config/dotnet-tools.json
dotnet ef database update --project src/SI18.Api              # aplica 0001_NucleoComun
$env:SI18_SEMBRAR_DATOS = "true"; dotnet run --project src/SI18.Api
```

**Resultado esperado**:
- el registro muestra «Datos de demostración sembrados» la primera vez y «ya existen» las siguientes
  (idempotente);
- en `https://localhost:7180` se abre el cliente Blazor, servido por la API;
- en `https://localhost:7180/openapi/v1.json` se ve el documento OpenAPI generado.

## 4. Escenarios de validación (HU-C1…HU-C4 de la spec)

Son manuales y van contra la API en marcha. Cada uno tiene también su prueba automática (sección 5).

| # | Paso | Resultado esperado | Spec |
|---|------|--------------------|------|
| V1 | `POST /api/sesion` con `ciudadana.empadronada@si18.test` + `SI18_CLAVE_DEMO` | 200; `rol = ciudadano`, `tipoTarifa = empadronado`; cookie `si18.sesion` HttpOnly | HU-C3 esc. 1 |
| V2 | Igual con `ciudadano.noempadronado@si18.test` | `tipoTarifa = no_empadronado` | HU-C3 esc. 2 |
| V3 | Igual con `representante.padel@si18.test` (no empadronado) | `tipoTarifa = club`, `clubNombre` informado | HU-C3 esc. 3 |
| V4 | Igual con `personal.norte@si18.test` | `rol = personal_centro`, `centroNombre = Polideportivo Norte` | HU-C3, RF-008 |
| V5 | `POST /api/sesion` con contraseña incorrecta, y otra vez con un correo inexistente | Las dos dan 401 `CREDENCIALES_INVALIDAS` con el mismo `detail` | HU-C3 esc. 5 |
| V6 | `GET /api/avisos` sin cookie | 401 Problem Details `NO_AUTENTICADO` en español, sin traza | RF-028, principio IV |
| V7 | `GET /api/centros` | 4 centros, cada uno con `horaApertura 08:00` y `horaCierre 22:00` | Valores de negocio |
| V8 | `GET /api/instalaciones?reservablePorFranjas=true` | Solo pistas de tenis y pádel (16) | RF-012 |
| V9 | En `psql`: insertar dos filas en `reservas` con la misma instalación e inicio y estado `confirmada` | La segunda falla con `23505` en `ux_reservas_activa` | RF-014, principio III |
| V10 | En `psql`: `SELECT hash_contrasena FROM usuarios LIMIT 1` | Hash PBKDF2 en Base64; nunca la contraseña en claro | RF-005 |

## 5. Pruebas automáticas

```powershell
dotnet test SI18.slnx                                           # todas (Testcontainers arranca PostgreSQL)
dotnet test tests/SI18.Dominio.Tests                            # solo unitarias, sin Docker
dotnet test SI18.slnx --filter "Categoria=Limites"              # límites a los dos lados (CE-006)
```

Lo que DEBE estar en verde al cerrar 000-core:

- **`SI18.Dominio.Tests/Comun`**:
  - `HorarioMadrid`: conversión de franjas los días 25/10/2026 y 28/03/2027 (cambio de hora); las
    14 franjas siguen existiendo.
  - `CalculadoraTarifa`: los tres casos y el representante empadronado.
  - `PoliticaDevolucion`: 24 h + 1 s → sí; 24 h exactas → no; misma comparación cruzando el cambio de
    hora; cierre → siempre.
  - `Reserva`: todas las transiciones válidas; `pendiente_pago → cancelada` y cambios desde
    estados finales → `TRANSICION_NO_PERMITIDA`; `EstaVigente` a 9:59 → sí y a 10:00 → no.
  - Ventana de antelación: hoy + 7 → sí; hoy + 8 → no; franja empezada hace 1 min → no.
  - `ReglaAcceso`: −15 min → válido; −16 min → no; fin → válido; fin + 1 min → no.
- **`SI18.Api.Tests/Comun`**:
  - migraciones aplicadas sobre una base de datos vacía;
  - siembra idempotente (ejecutar dos veces no duplica);
  - V1–V6 contra HTTP;
  - 20 inserciones concurrentes en la misma franja → exactamente 1 éxito y 19 `FRANJA_OCUPADA`;
  - excepción no controlada → 500 `ERROR_INTERNO` sin trazas.

## 6. Comprobación de accesibilidad (cada PR de interfaz)

1. Recorre la pantalla de inicio de sesión y el layout solo con el teclado: el orden es lógico, el
   foco siempre está visible y «Ir al contenido» funciona.
2. Introduce credenciales incorrectas: el mensaje de error se anuncia (región `aria-live`) y no se
   transmite solo con color.
3. Lighthouse → Accesibilidad ≥ 95 y axe DevTools sin infracciones graves ni críticas.
4. Anota el resultado en la descripción del PR (constitución, «Flujo de trabajo»).

## 7. Revisión de secretos antes de fusionar

```powershell
git grep -nE "Password=|SI18_CLAVE_DEMO=.+[^>]$" -- ":!*.example"   # no debe devolver nada
```
