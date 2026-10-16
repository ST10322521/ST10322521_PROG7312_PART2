using Microsoft.EntityFrameworkCore;
using SmartX.Shared.Models;

namespace SmartX.Api.Data;

public class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options) : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();
}