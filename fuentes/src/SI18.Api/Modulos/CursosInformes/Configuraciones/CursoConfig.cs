using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;
using SI18.Dominio.CursosInformes;

namespace SI18.Api.Modulos.CursosInformes.Configuraciones;

/// <summary>Tabla <c>cursos</c> (data-model.md, «Curso»). Dueño: HU-04.</summary>
public sealed class CursoConfig : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_cursos_plazas_maximas", "plazas_maximas > 0");
            t.HasCheckConstraint("ck_cursos_fechas", "fecha_fin > fecha_inicio");
            t.HasCheckConstraint("ck_cursos_horas", "hora_fin > hora_inicio");
        });

        b.Property(c => c.Nombre).HasMaxLength(100);
        b.Property(c => c.Trimestre).HasMaxLength(30);
        b.Property(c => c.DiasSemana).HasColumnType("smallint[]");

        b.HasOne<Deporte>().WithMany().HasForeignKey(c => c.DeporteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Instalacion>().WithMany().HasForeignKey(c => c.InstalacionId).OnDelete(DeleteBehavior.Restrict);
    }
}
