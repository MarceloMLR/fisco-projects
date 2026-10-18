using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Testing.Samples;

/// <summary>Second example module context, to check that modules share the database without mixing.</summary>
public sealed class BetaDbContext(DbContextOptions<BetaDbContext> options)
    : ModuleDbContext<BetaDbContext>(options), IModuleSchema
{
    public static string Schema => "beta";

    public DbSet<BetaRecord> Records => Set<BetaRecord>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<BetaRecord>(record =>
        {
            record.ToTable("records");
            record.Property(r => r.Title).HasMaxLength(100);
        });
}

public sealed class BetaDbContextFactory : ModuleDesignTimeDbContextFactory<BetaDbContext>
{
    protected override BetaDbContext CreateContext(DbContextOptions<BetaDbContext> options) => new(options);
}
