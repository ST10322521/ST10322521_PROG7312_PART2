using Microsoft.EntityFrameworkCore;
using SmartX.Shared.Models;

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.metadata.builders.propertybuilder.hasconversion?view=efcore-10.0

namespace SmartX.Api.Data;

public class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options) : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Sensor entity
        modelBuilder.Entity<Sensor>()
            .Property(s => s.Category)
            .HasConversion<string>();
        modelBuilder.Entity<Sensor>()
            .Property(s => s.TelemetryType)
            .HasConversion<string>();
    }
}