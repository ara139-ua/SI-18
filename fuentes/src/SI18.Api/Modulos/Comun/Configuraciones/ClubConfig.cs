using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>clubes</c> (data-model.md, «Club»).</summary>
public sealed class ClubConfig : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> b)
    {
        b.Property(c => c.Nombre).HasMaxLength(100);
        b.HasIndex(c => c.Nombre).IsUnique();
    }
}
