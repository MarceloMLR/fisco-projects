using FiscoProjects.Audit.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Audit.Application;

/// <summary>A record as the Audit screen shows it: the author's name comes from the Users module.</summary>
public sealed record AuditEntryView(AuditEntry Entry, string? ActorName);

public sealed record AuditEntriesPage(IReadOnlyList<AuditEntryView> Items, int Total);

/// <summary>Why a listing was not run.</summary>
public sealed record AuditQueryError(string Field, string Message);

/// <summary>Lists the audit records for the Audit screen (ADR-0039), with the name of each author.</summary>
public sealed class ListAuditEntries(IAuditStore store, IUserDirectory users)
{
    public const int MaxPageSize = 100;

    /// <summary>The query's problems: page from 1, page size from 1 to <see cref="MaxPageSize"/>, period start before its end.</summary>
    public static IReadOnlyList<AuditQueryError> Validate(AuditQuery query)
    {
        var errors = new List<AuditQueryError>();
        if (query.Page < 1)
        {
            errors.Add(new("page", "The page starts at 1."));
        }

        if (query.PageSize is < 1 or > MaxPageSize)
        {
            errors.Add(new("pageSize", $"The page size goes from 1 to {MaxPageSize}."));
        }

        if (query.From is not null && query.To is not null && query.From >= query.To)
        {
            errors.Add(new("to", "The end of the period comes after its start."));
        }

        return errors;
    }

    public async Task<AuditEntriesPage> ExecuteAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        if (Validate(query).Count > 0)
        {
            throw new ArgumentException("Invalid audit query; validate it first.", nameof(query));
        }

        var page = await store.ListAsync(query, cancellationToken);
        var actorIds = page.Items.Where(entry => entry.ActorId is not null).Select(entry => entry.ActorId!.Value).Distinct().ToList();
        var names = actorIds.Count == 0 ? new Dictionary<Guid, string>() : await users.NamesAsync(actorIds, cancellationToken);

        return new AuditEntriesPage(
            [.. page.Items.Select(entry => new AuditEntryView(
                entry, entry.ActorId is { } actorId && names.TryGetValue(actorId, out var name) ? name : null))],
            page.Total);
    }
}
