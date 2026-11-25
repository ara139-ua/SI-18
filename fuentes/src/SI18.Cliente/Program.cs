using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SI18.Cliente;
using SI18.Cliente.Comun;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// La API sirve el cliente: mismo origen, así la cookie de sesión viaja sola (research.md R10).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ClienteApi>();

// Estado de sesión (T056): AuthorizeView y el menú por rol lo leen en cascada.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<EstadoSesion>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<EstadoSesion>());

await builder.Build().RunAsync();
