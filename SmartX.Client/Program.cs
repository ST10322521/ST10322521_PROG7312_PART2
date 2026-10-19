using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SmartX.Client;
using SmartX.Client.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var apiBaseAddress = builder.HostEnvironment.Environment == "Development" ? "http://localhost:5017/" : builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseAddress)
});

builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<SensorService>();
builder.Services.AddScoped<SensorAttachmentService>();

var app = builder.Build();

var signalRService = app.Services.GetRequiredService<SignalRService>();
await signalRService.StartAsync();

await app.RunAsync();