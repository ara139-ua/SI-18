using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>centros</c> (data-model.md, «Centro»).</summary>
public sealed class CentroConfig : IEntityTypeConfiguration<Centro>
{
    public void Configure(EntityTypeBuilder<Centro> b)
    {
        b.Property(c => c.Nombre).HasMaxLength(100);
        b.Property(c => c.Direccion).HasMaxLength(200);
        b.HasIndex(c => c.Nombre).IsUnique();
    }
}
