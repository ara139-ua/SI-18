using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Api.Modulos.Disponibilidad.Configuraciones;

/// <summary>Tabla <c>solicitudes_franjas_fijas</c> (data-model.md, «SolicitudFranjasFijas»). Dueño: HU-05.</summary>
public sealed class SolicitudFranjasFijasConfig : IEntityTypeConfiguration<SolicitudFranjasFijas>
{
    public void Configure(EntityTypeBuilder<SolicitudFranjasFijas> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_solicitudes_franjas_fijas_resuelta",
                "(estado <> 'pendiente') = (resuelta_en_utc IS NOT NULL)");
            t.HasCheckConstraint(
                "ck_solicitudes_franjas_fijas_dias_semana",
                "cardinality(dias_semana) > 0 AND dias_semana <@ ARRAY[1,2,3,4,5,6,7]::smallint[]");
            t.HasCheckConstraint(
                "ck_solicitudes_franjas_fijas_horas",
                "hora_fin > hora_inicio");
        });

        b.Property(s => s.DiasSemana).HasColumnType("smallint[]");

        b.HasOne<Club>().WithMany().HasForeignKey(s => s.ClubId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Instalacion>().WithMany().HasForeignKey(s => s.InstalacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Temporada>().WithMany().HasForeignKey(s => s.TemporadaId).OnDelete(DeleteBehavior.Restrict);
    }
}
