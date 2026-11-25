# Guía de validación: 001 · Buscar pistas libres

Cómo comprobar de principio a fin que HU-01 funciona. El arranque del entorno (Docker, `.env`, base de
datos y siembra) es el de [`000-core/quickstart.md`](../000-core/quickstart.md) §1–§3; aquí no se
repite.

## 1. Requisitos previos

- El entorno de 000-core arrancado (PostgreSQL de docker-compose y la API con la siembra hecha).
- `develop` con los endpoints de catálogo de 000-core (T054: `/api/centros`, `/api/deportes`,
  `/api/instalaciones`), que la pantalla usa para los filtros.
- La contraseña de demostración (`SI18_CLAVE_DEMO` de tu `.env`).

## 2. Pruebas automáticas

```powershell
cd fuentes
dotnet test SI18.slnx                                         # todo en verde
dotnet test SI18.slnx --filter-trait "Categoria=Limites"      # límites (7 días, inicio, 10 min, cambio de hora)
```

Las pruebas de integración de HU-01 están en `tests/SI18.Api.Tests/Disponibilidad/` (research.md D9).
Necesitan Docker arrancado (Testcontainers).

## 3. Escenarios manuales contra la API

Con un cliente HTTP con cookies (o desde el navegador con la sesión iniciada). `{hoy}` es la fecha de
hoy en Madrid; los identificadores salen de `GET /api/centros` y `GET /api/deportes`.

| #   | Acción                                                                                                   | Resultado esperado                                                                                     | Spec |
| --- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ---- |
| B1  | Sesión como `ciudadana.empadronada@si18.test`; `GET /api/disponibilidad/franjas?deporteId={pádel}&centroId={Norte}&fecha={hoy+1}` | 200; `totalPistas = 2`; dos pistas con 14 franjas cada una, de `08:00` a `21:00`; `precio = 6.00`    | HU1 esc. 1, HU2 esc. 1 |
| B2  | Igual con `ciudadano.noempadronado@si18.test`                                                            | Las mismas franjas con `precio = 10.00`                                                                | HU2 esc. 1 |
| B3  | Igual con `representante.padel@si18.test`                                                                | `precio = 4.00`                                                                                        | HU2 esc. 2 |
| B4  | Igual con `fecha={hoy}`                                                                                   | Solo franjas con `horaInicio` posterior a la hora actual de Madrid                                     | HU1 esc. 6 |
| B5  | Igual con `fecha={hoy+7}` y con `fecha={hoy+8}`                                                           | 200 con franjas; y 422 `FUERA_DE_ANTELACION`                                                           | HU3 esc. 3 |
| B6  | `deporteId={baloncesto}`                                                                                  | 200 con `totalPistas = 0` y `pistas = []`                                                              | HU3 esc. 2 |
| B7  | Sin el parámetro `fecha`                                                                                  | 400 `PETICION_INVALIDA` con `errors.fecha`                                                             | RF-003 |
| B8  | `centroId=999999`                                                                                         | 404 `NO_ENCONTRADO`                                                                                    | D2 |
| B9  | Sesión como `personal.norte@si18.test` o `tecnico@si18.test`                                              | 403 `ROL_NO_PERMITIDO`                                                                                 | RF-001 |
| B10 | Sin sesión                                                                                                | 401 `NO_AUTENTICADO`                                                                                   | RF-001 |

Para ver franjas ocupadas, cerradas o bloqueadas sin las specs 002, 005 y 007, inserta en la base de
datos local una reserva confirmada, un cierre o una solicitud aprobada (las pruebas de integración lo
hacen así). Después repite B1: esas franjas ya no aparecen (HU1 esc. 2–5).

## 4. Escenarios manuales en la pantalla (`/buscar-pistas`)

1. Inicia sesión como `ciudadana.empadronada@si18.test`. El menú muestra «Buscar pistas».
2. Los filtros: el deporte ofrece solo «Tenis» y «Pádel»; el centro, los cuatro centros; la fecha
   propone hoy y no deja elegir antes de hoy ni después de hoy + 7.
3. Busca «Pádel», «Polideportivo Norte» y mañana: aparece una sección por pista con «08:00–09:00 ·
   6,00 €» y las demás franjas, y el mensaje «Hay 28 franjas libres en 2 pistas.».
4. Busca «Pádel» en un centro y un día sin franjas (por ejemplo, hoy a partir de las 21:00): aparece el
   mensaje «No hay pistas libres…».
5. Como `personal.norte@si18.test`, el menú no muestra «Buscar pistas», y `/buscar-pistas` muestra
   el rechazo `ROL_NO_PERMITIDO`.

## 5. Accesibilidad (quickstart.md §6 de 000-core, aplicado a `/buscar-pistas`)

1. Completa la búsqueda solo con el teclado: filtros, botón y resultado en orden lógico, con el foco
   siempre visible.
2. Con un lector de pantalla: los filtros se anuncian con su etiqueta; al buscar se anuncia «Hay N
   franjas libres…» o el mensaje sin resultados, sin que el foco salte.
3. Cada franja se entiende sin color: pista (encabezado), horas y precio en texto.
4. Lighthouse → Accesibilidad ≥ 95 y axe DevTools sin infracciones graves ni críticas.
5. Anota el resultado en la descripción del PR.
