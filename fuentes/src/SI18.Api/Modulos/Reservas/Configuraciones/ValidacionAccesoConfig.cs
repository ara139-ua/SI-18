using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas.Configuraciones;

/// <summary>Tabla <c>validaciones_acceso</c> (data-model.md, «ValidacionAcceso»). Dueño: HU-06.</summary>
public sealed class ValidacionAccesoConfig : IEntityTypeConfiguration<ValidacionAcceso>
{
    public void Configure(EntityTypeBuilder<ValidacionAcceso> b)
    {
        b.ToTable(t => t.HasCheckConstraint(
            "ck_validaciones_acceso_motivo_rechazo",
            "(NOT aceptada) = (motivo_rechazo IS NOT NULL)"));

        b.Property(v => v.CodigoLeido).HasMaxLength(ValidacionAcceso.LongitudMaximaCodigo);
        b.Property(v => v.MotivoRechazo).HasMaxLength(40);

        b.HasOne<Reserva>().WithMany().HasForeignKey(v => v.ReservaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(v => v.PersonalId).OnDelete(DeleteBehavior.Restrict);
    }
}
