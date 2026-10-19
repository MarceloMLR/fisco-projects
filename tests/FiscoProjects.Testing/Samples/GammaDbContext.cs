using FiscoProjects.Persistence;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Testing.Samples;

/// <summary>
/// Example module context with the audit markers: a label, a secret, a technical field, an item left out and an item
/// that points to others, one of them where the item is (a context field).
/// </summary>
public sealed class GammaDbContext(DbContextOptions<GammaDbContext> options)
    : ModuleDbContext<GammaDbContext>(options), IModuleSchema
{
    public static string Schema => "gamma";

    public DbSet<GammaRecord> Records => Set<GammaRecord>();

    public DbSet<GammaNote> Notes => Set<GammaNote>();

    public DbSet<GammaLink> Links => Set<GammaLink>();

    /// <summary>The types the links point to, as the modules would name them.</summary>
    public const string PersonReference = "Person";
    public const string RecordReference = "Record";

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

        modelBuilder.Entity<GammaLink>(link =>
        {
            link.ToTable("links");
            link.HasAuditLabelReference(l => l.PersonId, PersonReference);
            link.Property(l => l.PersonId).IsAuditReference(PersonReference);
            link.Property(l => l.RecordId).IsAuditReference(RecordReference).IsAuditContext();
            link.Property(l => l.Kind).HasMaxLength(20);
        });
    }
}

public sealed class GammaDbContextFactory : ModuleDesignTimeDbContextFactory<GammaDbContext>
{
    protected override GammaDbContext CreateContext(DbContextOptions<GammaDbContext> options) => new(options);
}
