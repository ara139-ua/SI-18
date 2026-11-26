using SI18.Dominio.Comun;

namespace SI18.Dominio.CursosInformes;

/// <summary>
/// Inscripción de un usuario en un curso (data-model.md, «Inscripcion»; dueño: HU-04).
/// Estados: confirmada | lista_espera → baja; lista_espera → confirmada. La posición en la lista de
/// espera no se guarda: se calcula por (<see cref="LlegadaEnUtc"/>, <see cref="Id"/>).
/// </summary>
public class Inscripcion
{
    private Inscripcion() { }

    public Inscripcion(int usuarioId, int cursoId, EstadoInscripcion estado, DateTime llegadaEnUtc)
    {
        UsuarioId = usuarioId;
        CursoId = cursoId;
        Estado = estado;
        LlegadaEnUtc = llegadaEnUtc;
    }

    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public int CursoId { get; private set; }

    public EstadoInscripcion Estado { get; private set; }

    /// <summary>Ordena la lista de espera.</summary>
    public DateTime LlegadaEnUtc { get; private set; }
}
