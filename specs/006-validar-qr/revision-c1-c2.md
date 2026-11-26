# Revisión cruzada de C1 y C2 · 006-validar-qr (HU-06)

**Revisor**: Artur. **Autor**: Saul. **Fecha**: 26/11/2026. **Qué se revisa**: los dos cambios de código común que
pacta el plan (plan.md, tabla «Código común», C1 y C2), fusionados en el #46 (`a043dbd`) sin esta revisión por la
entrega. Cierra P1 de analyze-3 y F1 de converge (`converge-2026-11-26-3.md`). Revisado sobre `develop` en `87fd32c`;
desde el #46 nadie más ha cambiado estos tres ficheros.

**Resultado**: ✅ **Aprobados sin cambios.** Los dos cambios se ciñen a lo pactado, no cambian nada de lo que ya
usaban las pantallas de HU-01, HU-02 y HU-05, y se comportan como se espera en la app.

## C1 · Enlace «Validar acceso» en el menú (`SI18.Cliente/Layout/MainLayout.razor`)

| Comprobación | Resultado |
|--------------|-----------|
| Es lo pactado en el plan: un `MudButton` a `/validar-acceso` en el hueco `personal_centro`, igual que los demás enlaces | Sí; una línea, con el mismo `Color` y `Variant` que el resto del menú |
| Deja sitio a los cierres de spec-007 | Sí; el comentario del hueco queda debajo («Hueco de Cancelaciones (spec-007): cierres») |
| No toca los huecos de otros roles ni el resto del layout | Sí; el diff solo cambia ese hueco |
| Menú por rol en la app (26/11/2026 22:47, `si18_demo`) | `personal.este`: «Validar acceso · Mis avisos». Ciudadana: «Buscar pistas · Mis reservas · Mis avisos». Representante: «Buscar pistas · Mis reservas · Franjas fijas · Mis avisos». Técnico: «Revisar franjas fijas · Mis avisos» |
| A 320 px de ancho | El enlace se ve y la página no se desplaza en horizontal (RF-015) |

**Texto y posición (T023, F2 de converge)**: confirmo «Validar acceso» y su posición, primero en el hueco del
personal y antes de los cierres. Es el nombre de la pantalla y de la acción en la spec (RF-016).

## C2 · Extensiones del Problem Details en el cliente (`ErrorApi.cs`, `ClienteApi.cs`)

| Comprobación | Resultado |
|--------------|-----------|
| Es lo pactado: `ErrorApi.Extensiones` (`IReadOnlyDictionary<string, JsonElement>`, vacío por defecto), rellenado por `ClienteApi.LeerProblemaAsync` | Sí |
| Aditivo: `Codigo`, `Detalle`, `Estado` y `Errores` salen igual que antes | Sí; mismas opciones JSON y mismos valores por defecto. Lo fija la prueba `Codigo_detalle_estado_y_errores_no_cambian` |
| Solo pasan a extensiones las propiedades que no son del Problem Details ni del contrato (`type`, `title`, `status`, `detail`, `instance`, `codigo`, `traceId`, `errors`) | Sí; la comparación no distingue mayúsculas, y los valores se clonan (`JsonElement.Clone()`) antes de liberar el `JsonDocument` |
| Respuestas que no son JSON, o JSON inválido | Extensiones vacías y el mismo `HTTP_nnn` de antes (`Una_respuesta_que_no_es_JSON_deja_las_extensiones_vacias`) |
| Pruebas | `ClienteApiExtensionesTests`: 4 de 4 en verde; batería completa en verde (410) |
| En la app, con `personal.este` (26/11/2026 22:47) | Fuera de ventana (`QR_FUERA_DE_VENTANA`) y reserva cancelada (`QR_RESERVA_NO_CONFIRMADA`): la pantalla muestra la instalación, el centro, la fecha y el horario que llegan en la extensión `reserva`, sin titular. Código desconocido (`QR_DESCONOCIDO`): sin datos de reserva, porque la API no manda la extensión |

**Observación menor, sin cambio**: `LeerProblemaAsync` lee el cuerpo dos veces (`JsonSerializer.Deserialize` y
`JsonDocument.Parse`). Se podría deserializar desde el propio `JsonDocument`, pero los Problem Details son
pequeños y el código es más fácil de seguir así. No hace falta tocarlo.

## Cómo se comprobó

- Lectura del diff del #46 en los tres ficheros comunes (`git diff a043dbd^1 a043dbd -- …`) y de su estado actual.
- `dotnet test --filter ClienteApiExtensionesTests` y la batería completa.
- Un script de Playwright de Artur, fuera del repositorio, sobre la app local con la siembra de demo (`si18_demo`):
  sesión con los cuatro roles, menú leído del `nav` «Menú principal», prueba a 320 px y tres códigos
  introducidos en «Validar acceso» (dos reservas de prueba de mañana en Polideportivo Este, pádel 1:
  una confirmada a las 11:00 y otra cancelada a las 12:00).
