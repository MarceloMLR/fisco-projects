using FiscoProjects.Audit.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Audit.Infrastructure.Persistence;

/// <summary>
/// Context of the audit tables. It is not audited itself, and it only adds records: a save that would change or
/// remove one is refused (ADR-0039), but for the erasure of an anonymized person, which only changes records
/// (<see cref="SaveErasureAsync"/>); the retention removes the old ones straight in the database (ADR-0110).
/// </summary>
internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : ModuleDbContext<AuditDbContext>(options), IModuleSchema
{
    public static string Schema => "audit";

    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    /// <summary>The month of each cleanup by the retention of the installation (ADR-0110).</summary>
    public DbSet<AuditCleanupRun> Cleanups => Set<AuditCleanupRun>();

    protected override bool IsAudited => false;

    private bool _erasing;

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(entry =>
            entry.State is EntityState.Deleted
            || (entry.State is EntityState.Modified && !(_erasing && entry.Entity is AuditEntry))))
        {
            throw new InvalidOperationException("Audit records are only added; they are never changed or removed (ADR-0039).");
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Saves the records whose personal data the anonymization of a person erased (ADR-0110).</summary>
    public async Task SaveErasureAsync(CancellationToken cancellationToken)
    {
        _erasing = true;
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _erasing = false;
        }
    }

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class AuditDbContextFactory : ModuleDesignTimeDbContextFactory<AuditDbContext>
{
    protected override AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options) => new(options);
}
