namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Avisos de «Mis avisos» (contracts/modulos.md, «Comun»). La implementación es de tasks.md T066.
/// </summary>
public interface IServicioAvisos
{
    /// <summary>
    /// Añade un aviso para <paramref name="usuarioId"/>. No llama a <c>SaveChanges</c>: se guarda con el del
    /// llamador, en la misma transacción que el cambio de estado que lo provoca (R18).
    /// </summary>
    void Avisar(int usuarioId, string texto);
}
