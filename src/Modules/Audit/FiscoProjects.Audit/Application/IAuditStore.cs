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

/// <summary>The audit records. They are only added, never changed or removed (ADR-0039).</summary>
public interface IAuditStore
{
    Task<AuditPage> ListAsync(AuditQuery query, CancellationToken cancellationToken);

    /// <summary>Adds a record and saves it at once.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);
}
