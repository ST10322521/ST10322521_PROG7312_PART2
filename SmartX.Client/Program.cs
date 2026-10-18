using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SmartX.Client;
using SmartX.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add services to the container.
builder.Services.AddMudServices();
/// Scoped
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<SensorService>();

var app = builder.Build();

var signalRService = app.Services.GetRequiredService<SignalRService>();
await signalRService.StartAsync();

await app.RunAsync();