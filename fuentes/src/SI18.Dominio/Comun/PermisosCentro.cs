namespace SI18.Dominio.Comun;

/// <summary>
/// El personal de centro solo actúa sobre instalaciones de su centro (RF-009, D7; research.md R10).
/// </summary>
public static class PermisosCentro
{
    // TODO(T018): sustituir por CodigosError.Error(CodigosError.OtroCentro) cuando entre CodigosError.
    public const string CodigoOtroCentro = "OTRO_CENTRO";
    private const string DetalleOtroCentro = "Esta instalación pertenece a otro centro; solo puedes actuar en el tuyo.";

    /// <exception cref="ErrorNegocio">
    /// <c>OTRO_CENTRO</c> si <paramref name="centroPersonal"/> es null o distinto de <paramref name="centroInstalacion"/>.
    /// </exception>
    public static void ExigirMismoCentro(int? centroPersonal, int centroInstalacion)
    {
        if (centroPersonal != centroInstalacion)
        {
            throw new ErrorNegocio(CodigoOtroCentro, DetalleOtroCentro);
        }
    }
}
