# T030 (spec-005): prueba del fallo previo y del resultado posterior

Evidencia del punto 4 de la actividad de converge del enunciado (5.3) para la única corrección de converge de
spec-005: **F1 / T030**, «los conflictos empezaban en "ahora" aunque la temporada no hubiera empezado»
(RF-008, CE-003). Corrección en el commit `11d4b36` (PR #34). Reproducida por Artur el 26/11/2026 sobre
`develop` en `79de901`.

## Prueba

`ConflictosFranjasFijasTests.Antes_de_empezar_la_temporada_solo_cuentan_las_reservas_de_dentro_de_ella`
(`fuentes/tests/SI18.Api.Tests/Disponibilidad/`), con reloj propio, sin depender del reloj compartido:

| Dato | Valor |
| ---- | ----- |
| Ahora | martes 04/08/2026 10:00 (Madrid): la temporada 2026-2027 empieza el 01/09/2026 |
| Instalación | Polideportivo Oeste · Pista de pádel 1 |
| Reservas | confirmada el martes 25/08/2026 a las 19:00 (antes de la temporada) y confirmada el martes 01/09/2026 a las 19:00 (primer día) |
| Solicitud | pendiente, martes de 19:00 a 21:00, temporada 2026-2027 |
| Esperado | la reserva del 25/08 **no** es un conflicto; la del 01/09 **sí** |

## Antes (código anterior a la corrección)

Se repuso en `ServicioSolicitudesFranjasFijas.Conflictos` la línea anterior a `11d4b36` (`var desde = Ahora;`)
con `git diff 11d4b36 11d4b36^ -- …ServicioSolicitudesFranjasFijas.cs | git apply`, sin tocar la prueba.

```text
26/11/2026 22:54:32 · dotnet test --filter Antes_de_empezar_la_temporada_solo_cuentan_las_reservas_de_dentro_de_ella
con errores ConflictosFranjasFijasTests.Antes_de_empezar_la_temporada_solo_cuentan_las_reservas_de_dentro_de_ella
  Assert.DoesNotContain() Failure: Filter matched in collection
  Collection: [ReservaEnConflictoDto { Fecha = 25/08/2026, HoraInicio = 19:00, HoraFin = 20:00, Estado = Confirmada },
               ReservaEnConflictoDto { Fecha = 01/09/2026, HoraInicio = 19:00, HoraFin = 20:00, Estado = Confirmada }]
total: 1 · error: 1 · correcto: 0
```

**Observado**: la reserva del 25/08, anterior a la temporada, sale como conflicto. Con ella, la solicitud no se
habría podido aprobar (RF-009) por una reserva que no es de la temporada.

## Después (código actual, con la corrección)

Se restauró el fichero (`git checkout -- …`) y se repitió la misma prueba.

```text
26/11/2026 22:55:10 · dotnet test --filter Antes_de_empezar_la_temporada_solo_cuentan_las_reservas_de_dentro_de_ella
total: 1 · error: 0 · correcto: 1
```

**Observado**: solo sale la reserva del 01/09. Coincide con lo esperado. La batería completa sigue en verde
(410 pruebas, 26/11/2026).
