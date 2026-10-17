using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Hubs;
using SmartX.Api.Services;
using SmartX.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SmartXDatabase")
    ?? throw new InvalidOperationException("Connection string 'SmartXDatabase' not found.");

builder.Services.AddDbContext<SmartXDbContext>(options =>
    options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
/// Scoped
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<ITelemetryIngestionService, TelemetryIngestionService>();
builder.Services.AddScoped<ISensorService, SensorService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Migrate the database during startup.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<SmartXHub>("/smartxHub");

app.Run();