using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Testing.Samples;

/// <summary>Example module context used by the tests, with its own schema and migrations.</summary>
public sealed class AlphaDbContext(DbContextOptions<AlphaDbContext> options)
    : ModuleDbContext<AlphaDbContext>(options), IModuleSchema
{
    public static string Schema => "alpha";

    public DbSet<AlphaRecord> Records => Set<AlphaRecord>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<AlphaRecord>(record =>
        {
            record.ToTable("records");
            record.Property(r => r.Name).HasMaxLength(100);
        });
}

public sealed class AlphaDbContextFactory : ModuleDesignTimeDbContextFactory<AlphaDbContext>
{
    protected override AlphaDbContext CreateContext(DbContextOptions<AlphaDbContext> options) => new(options);
}
