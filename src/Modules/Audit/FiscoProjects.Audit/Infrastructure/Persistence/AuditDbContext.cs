using FiscoProjects.Audit.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Audit.Infrastructure.Persistence;

/// <summary>
/// Context of the audit tables. It is not audited itself, and it only adds records: a save that would change or
/// remove one is refused (ADR-0039).
/// </summary>
internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : ModuleDbContext<AuditDbContext>(options), IModuleSchema
{
    public static string Schema => "audit";

    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    protected override bool IsAudited => false;

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Audit records are only added; they are never changed or removed (ADR-0039).");
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class AuditDbContextFactory : ModuleDesignTimeDbContextFactory<AuditDbContext>
{
    protected override AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options) => new(options);
}
