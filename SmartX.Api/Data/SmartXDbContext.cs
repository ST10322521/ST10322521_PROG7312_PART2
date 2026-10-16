using Microsoft.EntityFrameworkCore;

namespace SmartX.Api.Data;

public class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options) : base(options)
    {
    }
}