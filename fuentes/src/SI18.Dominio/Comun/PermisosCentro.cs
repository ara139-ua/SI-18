namespace SI18.Dominio.Comun;

/// <summary>
/// El personal de centro solo actúa sobre instalaciones de su centro (RF-009, D7; research.md R10).
/// </summary>
public static class PermisosCentro
{
    /// <exception cref="ErrorNegocio">
    /// <c>OTRO_CENTRO</c> si <paramref name="centroPersonal"/> es null o distinto de <paramref name="centroInstalacion"/>.
    /// </exception>
    public static void ExigirMismoCentro(int? centroPersonal, int centroInstalacion)
    {
        if (centroPersonal != centroInstalacion)
        {
            throw ErroresProvisionales.Error(ErroresProvisionales.OtroCentro);
        }
    }
}
