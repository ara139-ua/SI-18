using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;
using SI18.Dominio.CursosInformes;

namespace SI18.Api.Modulos.CursosInformes.Configuraciones;

/// <summary>
/// Tabla <c>inscripciones</c> (data-model.md, «Inscripcion»). Dueño: HU-04.
/// Solo claves foráneas: el índice único por usuario y curso lo propondrá la spec-004 (principio VIII).
/// </summary>
public sealed class InscripcionConfig : IEntityTypeConfiguration<Inscripcion>
{
    public void Configure(EntityTypeBuilder<Inscripcion> b)
    {
        b.HasOne<Usuario>().WithMany().HasForeignKey(i => i.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Curso>().WithMany().HasForeignKey(i => i.CursoId).OnDelete(DeleteBehavior.Restrict);
    }
}
