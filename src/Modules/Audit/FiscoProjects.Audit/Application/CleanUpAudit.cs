using FiscoProjects.Administration.Contracts;
using FiscoProjects.Audit.Domain;

namespace FiscoProjects.Audit.Application;

/// <summary>What a cleanup did: the first day it kept and how many records it removed.</summary>
public sealed record AuditCleanupResult(DateOnly KeptFrom, int Removed);

/// <summary>
/// The monthly cleanup of the audit (ADR-0058, ADR-0110): once in each month of the installation, it removes the
/// records older than the retention, counted from the start of today in its time zone, and, when it removed any,
/// records the cleanup in the audit, by the system. Nothing happens in a month that had its cleanup.
/// </summary>
public sealed class CleanUpAudit(IAuditStore store, IInstallationSettings installation, TimeProvider time)
{
    public async Task<AuditCleanupResult?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var info = await installation.GetAsync(cancellationToken);
        var now = time.GetUtcNow().UtcDateTime;
        var today = info.DayOf(now);
        var month = new DateOnly(today.Year, today.Month, 1);
        if (await store.HasCleanupAsync(month, cancellationToken))
        {
            return null;
        }

        var keptFrom = today.AddYears(-info.AuditRetentionYears);
        var removed = await store.RemoveBeforeAsync(info.StartOf(keptFrom), cancellationToken);
        var recorded = await store.RecordCleanupAsync(
            AuditCleanupRun.Of(month, now, keptFrom, removed),
            removed > 0 ? AuditEntry.Cleaned(now, keptFrom, removed) : null,
            cancellationToken);
        return recorded ? new AuditCleanupResult(keptFrom, removed) : null;
    }
}
