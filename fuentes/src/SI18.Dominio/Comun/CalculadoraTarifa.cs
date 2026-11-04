namespace SI18.Dominio.Comun;

/// <summary>
/// Tipo de tarifa de un usuario (RF-007; data-model.md, «Usuario»). No se guarda: se calcula al
/// iniciar sesión y queda fijado en cada reserva.
/// </summary>
public static class CalculadoraTarifa
{
    /// <returns>
    /// <see cref="TipoTarifa.Club"/> para un representante (esté o no empadronado);
    /// <see cref="TipoTarifa.Empadronado"/> o <see cref="TipoTarifa.NoEmpadronado"/> para un ciudadano;
    /// <c>null</c> para el personal de centro y el técnico, que no reservan
    /// (igual que <c>UsuarioActual.tipoTarifa</c> en el OpenAPI).
    /// </returns>
    public static TipoTarifa? Para(Rol rol, bool empadronado) => rol switch
    {
        Rol.RepresentanteClub => TipoTarifa.Club,
        Rol.Ciudadano => empadronado ? TipoTarifa.Empadronado : TipoTarifa.NoEmpadronado,
        _ => null,
    };
}
