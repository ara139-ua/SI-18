using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SI18.Dominio.Comun;
using SI18.Dominio.CursosInformes;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Aplica las migraciones y siembra los datos de demostración ficticios (research.md R13;
/// data-model.md, «Datos de inicialización»). Solo actúa si <c>SI18_SEMBRAR_DATOS=true</c>, y solo
/// inserta si la tabla <c>centros</c> está vacía, así que reiniciar la API no duplica nada.
/// </summary>
public sealed class InicializadorDatos(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<InicializadorDatos> logger) : IHostedService
{
    public const string VariableSembrar = "SI18_SEMBRAR_DATOS";
    public const string VariableClaveDemo = "SI18_CLAVE_DEMO";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>(VariableSembrar))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Si18DbContext>();
        await SembrarAsync(db, configuration[VariableClaveDemo], logger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Aplica migraciones y, si no hay centros, inserta los datos. Público para las pruebas (T040).</summary>
    public static async Task SembrarAsync(
        Si18DbContext db, string? claveDemo, ILogger logger, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Centros.AnyAsync(ct))
        {
            logger.LogInformation("Los datos de demostración ya existen");
            return;
        }

        if (string.IsNullOrWhiteSpace(claveDemo))
        {
            throw new InvalidOperationException("Falta SI18_CLAVE_DEMO para sembrar los datos de demostración");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Catálogo sin dependencias.
        var deportes = new Dictionary<string, Deporte>
        {
            ["tenis"] = new("Tenis"),
            ["padel"] = new("Pádel"),
            ["baloncesto"] = new("Baloncesto"),
            ["futbol_sala"] = new("Fútbol sala"),
            ["natacion"] = new("Natación"),
            ["yoga"] = new("Yoga"),
            ["pilates"] = new("Pilates"),
        };
        var centros = new Dictionary<string, Centro>
        {
            ["Norte"] = new("Polideportivo Norte", "Calle Ficticia 1"),
            ["Sur"] = new("Polideportivo Sur", "Calle Ficticia 2"),
            ["Este"] = new("Polideportivo Este", "Calle Ficticia 3"),
            ["Oeste"] = new("Polideportivo Oeste", "Calle Ficticia 4"),
        };
        var clubNorte = new Club("Club Deportivo Ficticio Norte");
        var clubPadel = new Club("Club de Pádel Ejemplo");

        db.Deportes.AddRange(deportes.Values);
        db.Centros.AddRange(centros.Values);
        db.Clubes.AddRange(clubNorte, clubPadel);
        db.Temporadas.Add(new Temporada("2026-2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)));
        db.Tarifas.AddRange(
            new Tarifa(TipoInstalacion.PistaTenis, TipoTarifa.Empadronado, 6.00m),
            new Tarifa(TipoInstalacion.PistaTenis, TipoTarifa.NoEmpadronado, 10.00m),
            new Tarifa(TipoInstalacion.PistaTenis, TipoTarifa.Club, 4.00m),
            new Tarifa(TipoInstalacion.PistaPadel, TipoTarifa.Empadronado, 6.00m),
            new Tarifa(TipoInstalacion.PistaPadel, TipoTarifa.NoEmpadronado, 10.00m),
            new Tarifa(TipoInstalacion.PistaPadel, TipoTarifa.Club, 4.00m));
        await db.SaveChangesAsync(ct);

        // Instalaciones: 6 por centro y piscina en Norte y Sur (26).
        var salas = new Dictionary<string, Instalacion>();
        var piscinas = new Dictionary<string, Instalacion>();
        foreach (var (clave, centro) in centros)
        {
            db.Instalaciones.AddRange(
                new Instalacion(centro.Id, "Pista de tenis 1", TipoInstalacion.PistaTenis, [deportes["tenis"]]),
                new Instalacion(centro.Id, "Pista de tenis 2", TipoInstalacion.PistaTenis, [deportes["tenis"]]),
                new Instalacion(centro.Id, "Pista de pádel 1", TipoInstalacion.PistaPadel, [deportes["padel"]]),
                new Instalacion(centro.Id, "Pista de pádel 2", TipoInstalacion.PistaPadel, [deportes["padel"]]),
                new Instalacion(centro.Id, "Pabellón", TipoInstalacion.Pabellon,
                    [deportes["baloncesto"], deportes["futbol_sala"]]));

            salas[clave] = new Instalacion(centro.Id, "Sala polivalente", TipoInstalacion.Sala,
                [deportes["yoga"], deportes["pilates"]]);
            db.Instalaciones.Add(salas[clave]);

            if (clave is "Norte" or "Sur")
            {
                piscinas[clave] = new Instalacion(centro.Id, "Piscina", TipoInstalacion.Piscina, [deportes["natacion"]]);
                db.Instalaciones.Add(piscinas[clave]);
            }
        }

        // Usuarios (10), todos con la misma contraseña de demo y su propia sal.
        var hasher = new PasswordHasher<Usuario>();
        // PasswordHasher no usa el usuario para calcular el hash.
        string Hash() => hasher.HashPassword(null!, claveDemo);

        db.Usuarios.AddRange(
            new Usuario("Ana Ciudadana", "ciudadana.empadronada@si18.test", Hash(), Rol.Ciudadano, empadronado: true),
            new Usuario("Bruno Visitante", "ciudadano.noempadronado@si18.test", Hash(), Rol.Ciudadano, empadronado: false),
            new Usuario("Carla Vecina", "ciudadana2.empadronada@si18.test", Hash(), Rol.Ciudadano, empadronado: true),
            new Usuario("Diego Representante", "representante.norte@si18.test", Hash(), Rol.RepresentanteClub,
                empadronado: true, clubId: clubNorte.Id),
            new Usuario("Elena Representante", "representante.padel@si18.test", Hash(), Rol.RepresentanteClub,
                empadronado: false, clubId: clubPadel.Id),
            new Usuario("Fernando Personal", "personal.norte@si18.test", Hash(), Rol.PersonalCentro,
                empadronado: false, centroId: centros["Norte"].Id),
            new Usuario("Gloria Personal", "personal.sur@si18.test", Hash(), Rol.PersonalCentro,
                empadronado: false, centroId: centros["Sur"].Id),
            new Usuario("Héctor Personal", "personal.este@si18.test", Hash(), Rol.PersonalCentro,
                empadronado: false, centroId: centros["Este"].Id),
            new Usuario("Irene Personal", "personal.oeste@si18.test", Hash(), Rol.PersonalCentro,
                empadronado: false, centroId: centros["Oeste"].Id),
            new Usuario("Javier Técnico", "tecnico@si18.test", Hash(), Rol.TecnicoConcejalia, empadronado: false));
        await db.SaveChangesAsync(ct);

        // Cursos «Invierno 2027» (3).
        const string trimestre = "Invierno 2027";
        var inicio = new DateOnly(2027, 1, 11);
        var fin = new DateOnly(2027, 3, 26);
        db.Cursos.AddRange(
            new Curso("Natación adultos", deportes["natacion"].Id, piscinas["Norte"].Id, trimestre, inicio, fin,
                [1, 3], new TimeOnly(19, 0), new TimeOnly(20, 0), plazasMaximas: 12),
            new Curso("Yoga", deportes["yoga"].Id, salas["Sur"].Id, trimestre, inicio, fin,
                [2, 4], new TimeOnly(10, 0), new TimeOnly(11, 0), plazasMaximas: 2),
            new Curso("Pilates", deportes["pilates"].Id, salas["Este"].Id, trimestre, inicio, fin,
                [5], new TimeOnly(18, 0), new TimeOnly(19, 0), plazasMaximas: 10));
        await db.SaveChangesAsync(ct);

        await transaccion.CommitAsync(ct);
        logger.LogInformation("Datos de demostración sembrados");
    }
}
