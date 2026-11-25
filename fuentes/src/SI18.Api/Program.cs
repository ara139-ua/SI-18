using SI18.Api.Infraestructura;
using SI18.Api.Modulos.Comun;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Modulos.Reservas;

// Solo composición (tasks.md T009, T072). Cada responsable registra lo suyo en su fichero de extensión:
// RegistroInfraestructura (Saul), RegistroErrores (Artur) y RegistroComun (Ambos). Cada módulo entra con dos
// líneas, Add<Modulo>() y Map<Modulo>(), en el orden fijo de plan.md, «Registro de los módulos en la API».
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraestructura(builder.Configuration);
builder.Services.AddErrores();
builder.Services.AddOpenApi();
builder.Services.AddComun();
builder.Services.AddDisponibilidad();
builder.Services.AddReservas();

var app = builder.Build();

app.UseErrores();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Documento OpenAPI en /openapi/v1.json, solo en desarrollo (quickstart.md §3).
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapComun();
app.MapDisponibilidad();
app.MapReservas();
app.MapFallbackToFile("index.html");

app.Run();

// Permite a WebApplicationFactory<Program> arrancar la API en las pruebas de integración.
public partial class Program;
