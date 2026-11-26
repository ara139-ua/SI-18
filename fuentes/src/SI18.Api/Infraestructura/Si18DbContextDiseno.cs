using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SI18.Api.Infraestructura;

/// <summary>
/// Solo para <c>dotnet ef migrations add</c>: crea el contexto sin arrancar la API, así que no hace
/// falta la cadena de conexión real ni se ejecuta el inicializador. Generar una migración no conecta
/// con la base de datos. Para aplicar migraciones se usa la API (InicializadorDatos) o
/// <c>dotnet ef database update --connection ...</c>.
/// </summary>
public sealed class Si18DbContextDiseno : IDesignTimeDbContextFactory<Si18DbContext>
{
    public Si18DbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<Si18DbContext>()
            .UseNpgsql("Host=localhost;Database=si18_diseno")
            .UseSnakeCaseNamingConvention()
            .Options);
}
