namespace FiscoProjects.Audit.Domain;

/// <summary>
/// A monthly cleanup of the audit (ADR-0058, ADR-0110): the month, in the time zone of the installation, so it runs once
/// in it; when it ran, the first day kept and how many records it removed.
/// </summary>
public sealed class AuditCleanupRun
{
    private AuditCleanupRun()
    {
    }

    public DateOnly Month { get; private init; }

    public DateTime RanAt { get; private init; }

    public DateOnly KeptFrom { get; private init; }

    public int Removed { get; private init; }

    public static AuditCleanupRun Of(DateOnly month, DateTime ranAt, DateOnly keptFrom, int removed) => new()
    {
        Month = new DateOnly(month.Year, month.Month, 1),
        RanAt = ranAt,
        KeptFrom = keptFrom,
        Removed = removed,
    };
}
