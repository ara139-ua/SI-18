using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>deportes</c> (data-model.md, «Deporte»).</summary>
public sealed class DeporteConfig : IEntityTypeConfiguration<Deporte>
{
    public void Configure(EntityTypeBuilder<Deporte> b)
    {
        b.Property(d => d.Nombre).HasMaxLength(50);
        b.HasIndex(d => d.Nombre).IsUnique();
    }
}
