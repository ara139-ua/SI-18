using Microsoft.EntityFrameworkCore;
using SI18.Dominio.Cancelaciones;
using SI18.Dominio.Comun;
using SI18.Dominio.CursosInformes;
using SI18.Dominio.Disponibilidad;
using SI18.Dominio.Reservas;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Contexto único de SI-18 (data-model.md). Cada módulo configura sus tablas en
/// <c>Modulos/{Modulo}/Configuraciones/</c>; aquí solo van las convenciones comunes.
/// </summary>
public class Si18DbContext(DbContextOptions<Si18DbContext> options) : DbContext(options)
{
    // Comun
    public DbSet<Centro> Centros => Set<Centro>();
    public DbSet<Deporte> Deportes => Set<Deporte>();
    public DbSet<Instalacion> Instalaciones => Set<Instalacion>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Club> Clubes => Set<Club>();
    public DbSet<Temporada> Temporadas => Set<Temporada>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    // Disponibilidad
    public DbSet<SolicitudFranjasFijas> SolicitudesFranjasFijas => Set<SolicitudFranjasFijas>();

    // Reservas
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<ValidacionAcceso> ValidacionesAcceso => Set<ValidacionAcceso>();

    // Cancelaciones
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<CierreMantenimiento> CierresMantenimiento => Set<CierreMantenimiento>();

    // CursosInformes
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Importes: numeric(8,2), nunca coma flotante.
        configurationBuilder.Properties<decimal>().HavePrecision(8, 2);

        // Enumerados: texto en snake_case (también sus versiones anulables).
        Enumerado<Rol>(configurationBuilder);
        Enumerado<TipoTarifa>(configurationBuilder);
        Enumerado<TipoInstalacion>(configurationBuilder);
        Enumerado<EstadoReserva>(configurationBuilder);
        Enumerado<MotivoCancelacion>(configurationBuilder);
        Enumerado<EstadoPago>(configurationBuilder);
        Enumerado<MotivoDevolucion>(configurationBuilder);
        Enumerado<EstadoSolicitud>(configurationBuilder);
        Enumerado<EstadoInscripcion>(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Si18DbContext).Assembly);
        AgregarChecksDeEnumerados(modelBuilder);
    }

    private static void Enumerado<TEnum>(ModelConfigurationBuilder builder)
        where TEnum : struct, Enum
    {
        builder.Properties<TEnum>().HaveConversion<ConversorEnumSnakeCase<TEnum>>();
        builder.Properties<TEnum?>().HaveConversion<ConversorEnumSnakeCase<TEnum>>();
    }

    /// <summary>
    /// Para cada columna de enumerado, un <c>CHECK (columna IN (...))</c> con los valores permitidos
    /// (data-model.md, «Convenciones»). Se nombra <c>ck_{tabla}_{columna}_valores</c>.
    /// </summary>
    private static void AgregarChecksDeEnumerados(ModelBuilder modelBuilder)
    {
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            var tabla = entidad.GetTableName();
            if (tabla is null)
            {
                continue;
            }

            foreach (var propiedad in entidad.GetProperties())
            {
                var tipo = Nullable.GetUnderlyingType(propiedad.ClrType) ?? propiedad.ClrType;
                if (!tipo.IsEnum)
                {
                    continue;
                }

                var columna = propiedad.GetColumnName();
                var valores = string.Join(", ", ValoresEnum(tipo).Select(v => $"'{v}'"));
                modelBuilder.Entity(entidad.ClrType)
                    .ToTable(t => t.HasCheckConstraint($"ck_{tabla}_{columna}_valores", $"{columna} IN ({valores})"));
            }
        }
    }

    private static IReadOnlyList<string> ValoresEnum(Type tipoEnum) =>
        (IReadOnlyList<string>)typeof(ConversorEnumSnakeCase<>)
            .MakeGenericType(tipoEnum)
            .GetProperty(nameof(ConversorEnumSnakeCase<Rol>.ValoresPermitidos))!
            .GetValue(null)!;
}
