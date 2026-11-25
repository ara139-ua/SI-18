using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using Testcontainers.PostgreSql;

namespace SI18.Api.Tests.Infraestructura;

/// <summary>
/// API completa contra un PostgreSQL real (research.md R15). Arranca un contenedor <c>postgres:17-alpine</c>
/// con Testcontainers, salvo que esté definida <c>SI18_TEST_CONNECTION</c> (por ejemplo, la base de
/// docker-compose). Las migraciones y la siembra las aplica el propio arranque (<c>InicializadorDatos</c>).
/// Úsala con <c>[Collection(ColeccionApi.Nombre)]</c>.
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string VariableConexionPruebas = "SI18_TEST_CONNECTION";
    public const string ClaveDemo = "clave-de-prueba";

    /// <summary>Instante inicial del reloj: lunes 21/12/2026, 18:00 en Madrid (17:00 UTC).</summary>
    public static readonly DateTimeOffset InicioReloj = new(2026, 12, 21, 17, 0, 0, TimeSpan.Zero);

    private PostgreSqlContainer? _contenedor;
    private string? _cadenaConexion;

    public FabricaApi()
    {
        // Con http://localhost el HttpClient de prueba no reenviaría la cookie Secure «si18.sesion».
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    /// <summary>Reloj de la API. Adelántalo con <c>Reloj.Advance(...)</c> o fíjalo con <c>Reloj.SetUtcNow(...)</c>.</summary>
    public FakeTimeProvider Reloj { get; } = new(InicioReloj);

    /// <summary>Cadena de conexión de la base de datos de pruebas.</summary>
    public string CadenaConexion => _cadenaConexion ?? throw new InvalidOperationException("La fábrica aún no se ha inicializado.");

    public virtual async ValueTask InitializeAsync()
    {
        _cadenaConexion = Environment.GetEnvironmentVariable(VariableConexionPruebas);
        if (string.IsNullOrWhiteSpace(_cadenaConexion))
        {
            // La misma imagen que docker-compose (Testcontainers 4.16 exige la imagen en el constructor).
            _contenedor = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _contenedor.StartAsync();
            _cadenaConexion = _contenedor.GetConnectionString();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Si18", CadenaConexion);
        builder.UseSetting(InicializadorDatos.VariableClaveDemo, ClaveDemo);
        builder.UseSetting(InicializadorDatos.VariableSembrar, "true");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);
        });
    }

    /// <summary>
    /// Cliente con la sesión iniciada como <paramref name="correo"/> (uno de los usuarios <c>@si18.test</c> de la
    /// siembra). Guarda la cookie <c>si18.sesion</c> para las siguientes peticiones.
    /// </summary>
    public async Task<HttpClient> CrearClienteConSesion(string correo)
    {
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new PeticionInicioSesion { Correo = correo, Contrasena = ClaveDemo },
            OpcionesJson.Predeterminadas);

        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            cliente.Dispose();
            throw new InvalidOperationException($"No se pudo iniciar sesión como {correo}: {(int)respuesta.StatusCode} {cuerpo}");
        }

        return cliente;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_contenedor is not null)
        {
            await _contenedor.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
