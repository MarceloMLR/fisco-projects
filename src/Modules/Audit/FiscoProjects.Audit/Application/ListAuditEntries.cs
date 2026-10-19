using FiscoProjects.Audit.Domain;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Audit.Application;

/// <summary>
/// A record as the Audit screen shows it: the author's name comes from the Users module, and the label and the
/// fields that point to another item come with the item's current name, or none when it no longer exists (ADR-0084).
/// </summary>
public sealed record AuditEntryView(
    AuditEntry Entry, string? ActorName, string? ItemLabel, IReadOnlyList<AuditFieldView> Changes);

/// <summary>A field as the Audit screen shows it: a reference comes with the names of the items before and after.</summary>
public sealed record AuditFieldView(string Field, string? Before, string? After, bool Secret, string? Reference);

public sealed record AuditEntriesPage(IReadOnlyList<AuditEntryView> Items, int Total);

/// <summary>Why a listing was not run.</summary>
public sealed record AuditQueryError(string Field, string Message);

/// <summary>Lists the audit records for the Audit screen (ADR-0039), with the name of each author.</summary>
public sealed class ListAuditEntries(
    IAuditStore store, IUserDirectory users, IEnumerable<IAuditReferenceNames> referenceNames)
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
        var references = await ReferenceNamesAsync(page.Items, cancellationToken);

        return new AuditEntriesPage(
            [.. page.Items.Select(entry => new AuditEntryView(
                entry,
                entry.ActorId is { } actorId && names.TryGetValue(actorId, out var name) ? name : null,
                entry.ItemLabelReference is { } kind ? NameOf(references, kind, entry.ItemLabel) : entry.ItemLabel,
                [.. entry.Changes.Select(field => field.Reference is { } fieldKind
                    ? new AuditFieldView(field.Field, NameOf(references, fieldKind, field.Before), NameOf(references, fieldKind, field.After), field.Secret, fieldKind)
                    : new AuditFieldView(field.Field, field.Before, field.After, field.Secret, null))]))],
            page.Total);
    }

    /// <summary>The names of every item the page points to, asked once per type to the module that owns it.</summary>
    private async Task<Dictionary<string, IReadOnlyDictionary<Guid, string>>> ReferenceNamesAsync(
        IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken)
    {
        var idsByKind = new Dictionary<string, HashSet<Guid>>();
        void Add(string kind, string? value)
        {
            if (Guid.TryParse(value, out var id))
            {
                if (!idsByKind.TryGetValue(kind, out var ids))
                {
                    idsByKind[kind] = ids = [];
                }

                ids.Add(id);
            }
        }

        foreach (var entry in entries)
        {
            if (entry.ItemLabelReference is { } kind)
            {
                Add(kind, entry.ItemLabel);
            }

            foreach (var field in entry.Changes.Where(field => field.Reference is not null))
            {
                Add(field.Reference!, field.Before);
                Add(field.Reference!, field.After);
            }
        }

        var names = new Dictionary<string, IReadOnlyDictionary<Guid, string>>();
        foreach (var (kind, ids) in idsByKind)
        {
            // A type without a module to name it stays without names.
            var source = referenceNames.FirstOrDefault(candidate => candidate.Kind == kind);
            if (source is not null)
            {
                names[kind] = await source.NamesAsync(ids, cancellationToken);
            }
        }

        return names;
    }

    private static string? NameOf(Dictionary<string, IReadOnlyDictionary<Guid, string>> references, string kind, string? value) =>
        Guid.TryParse(value, out var id) && references.TryGetValue(kind, out var names) && names.TryGetValue(id, out var name)
            ? name
            : null;
}
