using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;

// Solo composición (tasks.md T009). Cada responsable registra lo suyo en su fichero de extensión:
// RegistroInfraestructura (Saul), RegistroErrores (Artur) y RegistroComun (Ambos).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraestructura(builder.Configuration);
builder.Services.AddErrores();
builder.Services.AddComun();

var app = builder.Build();

app.UseErrores();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapComun();
app.MapFallbackToFile("index.html");

app.Run();

// Permite a WebApplicationFactory<Program> arrancar la API en las pruebas de integración.
public partial class Program;
