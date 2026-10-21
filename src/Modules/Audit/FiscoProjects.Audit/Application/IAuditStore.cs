using FiscoProjects.Audit.Domain;

namespace FiscoProjects.Audit.Application;

/// <summary>
/// Which records a listing asks for: by author, by item type and by period (start included, end excluded),
/// one page at a time, newest first.
/// </summary>
public sealed record AuditQuery(
    Guid? ActorId,
    string? ItemType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);

/// <summary>A page of records and how many records match the query in all pages.</summary>
public sealed record AuditPage(IReadOnlyList<AuditEntry> Items, int Total);

/// <summary>
/// The audit records. They are only added (ADR-0039); the retention of the installation removes the old ones, a month
/// at a time (ADR-0110).
/// </summary>
public interface IAuditStore
{
    Task<AuditPage> ListAsync(AuditQuery query, CancellationToken cancellationToken);

    /// <summary>Adds a record and saves it at once.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);

    /// <summary>Whether the month, its first day, already had its cleanup.</summary>
    Task<bool> HasCleanupAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>Removes the records before the moment, in batches; gives how many.</summary>
    Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken);

    /// <summary>
    /// Keeps the cleanup of the month and its record, if any; false when another process kept the month first, and
    /// then nothing is added.
    /// </summary>
    Task<bool> RecordCleanupAsync(AuditCleanupRun cleanup, AuditEntry? entry, CancellationToken cancellationToken);
}
