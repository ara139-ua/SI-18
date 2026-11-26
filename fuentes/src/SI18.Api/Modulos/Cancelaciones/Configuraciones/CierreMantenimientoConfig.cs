using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Cancelaciones.Configuraciones;

/// <summary>Tabla <c>cierres_mantenimiento</c> (data-model.md, «CierreMantenimiento»). Dueño: HU-07.</summary>
public sealed class CierreMantenimientoConfig : IEntityTypeConfiguration<CierreMantenimiento>
{
    public void Configure(EntityTypeBuilder<CierreMantenimiento> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_cierres_mantenimiento_intervalo", "fin_utc > inicio_utc"));

        b.Property(c => c.Motivo).HasMaxLength(500);

        b.HasOne<Instalacion>().WithMany().HasForeignKey(c => c.InstalacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(c => c.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}
