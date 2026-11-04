using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>notificaciones</c> (data-model.md, «Notificacion»).</summary>
public sealed class NotificacionConfig : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> b)
    {
        b.Property(n => n.Texto).HasMaxLength(500);
        b.Property(n => n.Leida).HasDefaultValue(false);

        // «Mis avisos»: los más recientes primero.
        b.HasIndex(n => new { n.UsuarioId, n.FechaUtc }).IsDescending(false, true);

        b.HasOne<Usuario>().WithMany().HasForeignKey(n => n.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
