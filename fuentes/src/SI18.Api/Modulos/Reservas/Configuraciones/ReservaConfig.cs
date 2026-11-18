using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas.Configuraciones;

/// <summary>Tabla <c>reservas</c> (data-model.md, «Reserva»). Dueño: HU-02.</summary>
public sealed class ReservaConfig : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_reservas_duracion", "fin_utc = inicio_utc + interval '1 hour'");
            t.HasCheckConstraint("ck_reservas_importe_positivo", "importe > 0");
            t.HasCheckConstraint(
                "ck_reservas_codigo_acceso",
                "(estado IN ('pendiente_pago', 'caducada')) = (codigo_acceso IS NULL)");
            t.HasCheckConstraint(
                "ck_reservas_cancelada_en",
                "(estado = 'cancelada') = (cancelada_en_utc IS NOT NULL)");
            t.HasCheckConstraint(
                "ck_reservas_motivo_cancelacion",
                "(estado = 'cancelada') = (motivo_cancelacion IS NOT NULL)");
        });

        b.Property(r => r.CodigoAcceso).HasMaxLength(32);
        b.HasIndex(r => r.CodigoAcceso).IsUnique();

        // Una sola reserva activa por instalación y franja (RF-014, R7).
        b.HasIndex(r => new { r.InstalacionId, r.InicioUtc })
            .IsUnique()
            .HasDatabaseName("ux_reservas_activa")
            .HasFilter("estado IN ('pendiente_pago', 'confirmada')");

        // «Mis reservas» y barrido de caducidades (R8).
        b.HasIndex(r => new { r.UsuarioId, r.InicioUtc });
        b.HasIndex(r => new { r.Estado, r.CreadaEnUtc });

        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Instalacion>().WithMany().HasForeignKey(r => r.InstalacionId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(r => r.Pago)
            .WithOne()
            .HasForeignKey<Pago>(p => p.ReservaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
