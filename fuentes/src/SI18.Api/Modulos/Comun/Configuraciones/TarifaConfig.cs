using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SI18.Dominio.Comun;

namespace SI18.Api.Modulos.Comun.Configuraciones;

/// <summary>Tabla <c>tarifas</c> (data-model.md, «Tarifa»).</summary>
public sealed class TarifaConfig : IEntityTypeConfiguration<Tarifa>
{
    public void Configure(EntityTypeBuilder<Tarifa> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tarifas_precio_hora_positivo", "precio_hora > 0");
            t.HasCheckConstraint(
                "ck_tarifas_tipo_instalacion_pista",
                "tipo_instalacion IN ('pista_tenis', 'pista_padel')");
        });

        b.HasIndex(t => new { t.TipoInstalacion, t.TipoTarifa }).IsUnique();
    }
}
