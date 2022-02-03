using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NoxConsole.Data;

public class ApplicationDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<Hello> Hello { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }


    public ApplicationDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            base.OnConfiguring(optionsBuilder);
            return;
        }

        if( _connectionString is not null)
        {
            optionsBuilder.UseSqlServer(_connectionString);
            return;
        }

        var configuration = ConfigurationHelper.GetApplicationConfiguration(new string[] {});

        optionsBuilder.UseSqlServer(configuration.GetConnectionString("ApplicationDbContext"));

        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure column attributes and model behaviour here
    }
}