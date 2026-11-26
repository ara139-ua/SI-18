namespace SI18.Contratos.Comun;

// Copias para el cliente de los enumerados de SI18.Dominio.Comun que aparecen en la API
// (contracts/api-comun.openapi.yaml). El cliente no referencia el dominio. Los nombres deben coincidir
// con los del dominio: lo comprueba EnumeracionesEspejoTests (tasks.md T059). En JSON viajan en snake_case.

/// <summary>Rol único de cada usuario (RF-006).</summary>
public enum Rol
{
    Ciudadano,
    RepresentanteClub,
    PersonalCentro,
    TecnicoConcejalia,
}

/// <summary>Tipo de tarifa que se aplica al reservar (RF-007).</summary>
public enum TipoTarifa
{
    Empadronado,
    NoEmpadronado,
    Club,
}

/// <summary>Tipo de instalación de un centro.</summary>
public enum TipoInstalacion
{
    PistaTenis,
    PistaPadel,
    Pabellon,
    Sala,
    Piscina,
}
