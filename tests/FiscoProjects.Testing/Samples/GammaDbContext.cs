using FiscoProjects.Persistence;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Testing.Samples;

/// <summary>Example module context with the audit markers: a label, a secret, a technical field and an item left out.</summary>
public sealed class GammaDbContext(DbContextOptions<GammaDbContext> options)
    : ModuleDbContext<GammaDbContext>(options), IModuleSchema
{
    public static string Schema => "gamma";

    public DbSet<GammaRecord> Records => Set<GammaRecord>();

    public DbSet<GammaNote> Notes => Set<GammaNote>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GammaRecord>(record =>
        {
            record.ToTable("records");
            record.HasAuditLabel(r => r.Name);
            record.Property(r => r.Name).HasMaxLength(20);
            record.Property(r => r.Password).HasMaxLength(100).IsAuditSecret();
            record.Property(r => r.LastSeenAt).ExcludeFromAudit();
        });

        modelBuilder.Entity<GammaNote>(note =>
        {
            note.ToTable("notes");
            note.Property(n => n.Text).HasMaxLength(100);
            note.ExcludeFromAudit();
        });
    }
}

public sealed class GammaDbContextFactory : ModuleDesignTimeDbContextFactory<GammaDbContext>
{
    protected override GammaDbContext CreateContext(DbContextOptions<GammaDbContext> options) => new(options);
}
