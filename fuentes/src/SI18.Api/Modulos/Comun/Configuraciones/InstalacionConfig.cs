using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tablas <c>instalaciones</c> e <c>instalacion_deportes</c> (data-model.md, «Instalacion»).</summary>
public sealed class InstalacionConfig : IEntityTypeConfiguration<Instalacion>
{
    public void Configure(EntityTypeBuilder<Instalacion> b)
    {
        b.ToTable(t => t.HasCheckConstraint(
            "ck_instalaciones_reservable_por_franjas",
            "reservable_por_franjas = (tipo IN ('pista_tenis', 'pista_padel'))"));

        b.Property(i => i.Nombre).HasMaxLength(100);
        b.HasIndex(i => new { i.CentroId, i.Nombre }).IsUnique();

        b.HasOne<Centro>().WithMany().HasForeignKey(i => i.CentroId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(i => i.Deportes)
            .WithMany()
            .UsingEntity(
                "instalacion_deportes",
                r => r.HasOne(typeof(Deporte)).WithMany().HasForeignKey("DeporteId").OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne(typeof(Instalacion)).WithMany().HasForeignKey("InstalacionId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey("InstalacionId", "DeporteId"));
    }
}
