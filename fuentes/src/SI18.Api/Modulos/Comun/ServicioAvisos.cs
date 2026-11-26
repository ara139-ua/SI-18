using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun;

/// <summary>
/// Crea los avisos de «Mis avisos» (contracts/modulos.md, «Comun»; research.md R18). Dueño: Ambos (tasks.md T066).
/// </summary>
public sealed class ServicioAvisos(Si18DbContext db, TimeProvider reloj) : IServicioAvisos
{
    /// <summary>
    /// Añade el aviso al <c>Si18DbContext</c> de la petición sin guardarlo: lo guarda el <c>SaveChanges</c> del
    /// llamador, en la misma transacción que el cambio de estado que lo provoca.
    /// </summary>
    public void Avisar(int usuarioId, string texto) =>
        db.Notificaciones.Add(new Notificacion(usuarioId, texto, reloj.GetUtcNow().UtcDateTime));
}
