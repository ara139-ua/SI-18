namespace SI18.Contratos.Disponibilidad;

// Enumerados de franjas fijas para el cliente. En JSON viajan en snake_case. EstadoSolicitud debe coincidir con
// SI18.Dominio.Comun.EstadoSolicitud (lo comprueba EnumeracionesEspejoTests).

/// <summary>Estado de una solicitud de franjas fijas.</summary>
public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada,
}

/// <summary>Estado de una reserva en conflicto: solo cuentan las activas.</summary>
public enum EstadoReservaEnConflicto
{
    Confirmada,
    PendientePago,
}
