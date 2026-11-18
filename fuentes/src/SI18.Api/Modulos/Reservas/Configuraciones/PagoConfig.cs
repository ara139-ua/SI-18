using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Reservas;

namespace SI18.Api.Modulos.Reservas.Configuraciones;

/// <summary>Tabla <c>pagos</c> (data-model.md, «Pago»). Dueño: HU-02. La relación con la reserva está en <see cref="ReservaConfig"/>.</summary>
public sealed class PagoConfig : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        b.ToTable(t => t.HasCheckConstraint(
            "ck_pagos_resuelto_en",
            "(estado <> 'pendiente') = (resuelto_en_utc IS NOT NULL)"));

        // Un único pago por reserva.
        b.HasIndex(p => p.ReservaId).IsUnique();
    }
}
