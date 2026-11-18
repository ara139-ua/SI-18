using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>temporadas</c> (data-model.md, «Temporada»).</summary>
public sealed class TemporadaConfig : IEntityTypeConfiguration<Temporada>
{
    public void Configure(EntityTypeBuilder<Temporada> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_temporadas_fechas", "fecha_fin > fecha_inicio"));

        b.Property(t => t.Nombre).HasMaxLength(20);
        b.HasIndex(t => t.Nombre).IsUnique();
    }
}
