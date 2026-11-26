using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Cancelaciones.Configuraciones;

/// <summary>Tabla <c>devoluciones</c> (data-model.md, «Devolucion»). Dueños: HU-03 y HU-07.</summary>
public sealed class DevolucionConfig : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> b)
    {
        // Una devolución por pago.
        b.HasIndex(d => d.PagoId).IsUnique();

        b.HasOne<Pago>().WithMany().HasForeignKey(d => d.PagoId).OnDelete(DeleteBehavior.Restrict);
    }
}
