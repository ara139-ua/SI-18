# Evidencias de la entrega

Evidencias que no viven en ninguna spec concreta. Las de cada spec (clarify, analyze y converge) están
en su carpeta de `specs/`.

| Fichero | Qué es | Origen |
| ------- | ------ | ------ |
| [commit-referencia.md](./commit-referencia.md) | Commit de referencia del refinamiento y versión de la herramienta, **identificados a posteriori** | Historial de git y `.specify/init-options.json` |
| [auditorias/000-core-t073/](./auditorias/000-core-t073/) | Informe de Lighthouse (*user flow*, accesibilidad) de InicioSesion, MainLayout y MisAvisos, y el script que lo generó (`t073.mjs`) | 000-core T073 (#42); resultados resumidos en el [comentario del #41](https://github.com/ara139-ua/SI-18/pull/41#issuecomment-7195036538) |
| [auditorias/002-pantallas/](./auditorias/002-pantallas/) | Informe de Lighthouse de las pantallas de reservar y pagar, y `resultados.json` (puntuaciones, nombres accesibles y foco) | spec-002 T042–T043 (#36); el script no se conservó |
| [t058-revision-de-marcado.md](./t058-revision-de-marcado.md) | Revisión del marcado de los dos estados nuevos del cliente de spec-002 (excepción justificada de T058) | Lectura del código, 26/11/2026 |
| [t030-antes-despues.md](./t030-antes-despues.md) | Prueba del fallo previo y del resultado posterior de la corrección de converge T030 de spec-005 (enunciado 5.3, punto 4) | `dotnet test` con el código anterior y con el actual, 26/11/2026 |

## Cómo se copiaron

Las auditorías se generaron el 26/11/2026 en la máquina de Saul, fuera del repositorio
(`Proyecto SDD/auditoria-000core` y `Proyecto SDD/auditoria-002`), y se copian aquí sin cambios. Antes
de añadirlas se ha comprobado que no contienen la contraseña de demo ni la de PostgreSQL de `fuentes/.env`
(el script lee la clave de la variable `SI18_CLAVE_DEMO`). Los datos que aparecen (correos `@si18.test`,
pistas y un código de acceso) son los ficticios de la siembra local (principio VI).

La auditoría de spec-006 (T026) no se guardó como fichero: sus resultados están en la descripción del
#46.
