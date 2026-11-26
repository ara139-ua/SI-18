namespace SI18.Dominio.Comun;

// Enumerados comunes de SI-18 (data-model.md, «Enumerados»).
// En la base de datos se guardan como texto en snake_case con CHECK sobre los valores permitidos.

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

/// <summary>Estado de una reserva (spec, «Estados»).</summary>
public enum EstadoReserva
{
    PendientePago,
    Confirmada,
    Caducada,
    Cancelada,
}

/// <summary>Motivo por el que se cancela una reserva confirmada.</summary>
public enum MotivoCancelacion
{
    Usuario,
    CierreMantenimiento,
}

/// <summary>Estado del único pago de una reserva.</summary>
public enum EstadoPago
{
    Pendiente,
    Completado,
    Fallido,
}

/// <summary>Motivo de una devolución.</summary>
public enum MotivoDevolucion
{
    CancelacionUsuario,
    CierreMantenimiento,
}

/// <summary>Estado de una solicitud de franjas fijas de un club.</summary>
public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada,
}

/// <summary>Estado de una inscripción en un curso.</summary>
public enum EstadoInscripcion
{
    Confirmada,
    ListaEspera,
    Baja,
}
