using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>usuarios</c> (data-model.md, «Usuario»).</summary>
public sealed class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_usuarios_club_si_representante",
                "(rol = 'representante_club') = (club_id IS NOT NULL)");
            t.HasCheckConstraint(
                "ck_usuarios_centro_si_personal",
                "(rol = 'personal_centro') = (centro_id IS NOT NULL)");
        });

        b.Property(u => u.Nombre).HasMaxLength(100);
        b.Property(u => u.Correo).HasMaxLength(254);
        b.Property(u => u.HashContrasena).HasMaxLength(200);
        b.HasIndex(u => u.Correo).IsUnique();

        b.HasOne<Club>().WithMany().HasForeignKey(u => u.ClubId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Centro>().WithMany().HasForeignKey(u => u.CentroId).OnDelete(DeleteBehavior.Restrict);
    }
}
