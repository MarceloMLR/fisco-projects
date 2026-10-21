using System.Globalization;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Audit.Application;

/// <summary>What to export of the audit: the days of the period, in the time zone of the installation, both included, and the optional item type and author.</summary>
public sealed record AuditExportQuery(DateOnly? From, DateOnly? To, string? ItemType, Guid? ActorId);

/// <summary>The records of an export, newest first, or none when there are more than the limit; with how many match.</summary>
public sealed record AuditExportResult(IReadOnlyList<AuditEntryView>? Items, int Total);

/// <summary>
/// The export of the audit to a spreadsheet, only by the Administrator (ADR-0099): every record of the filter, up to
/// <see cref="MaxRows"/>, with the current names, as the screen shows them; each export becomes a record of the audit,
/// with the filters and how many records left. A secret value never left the change, so it never leaves here.
/// </summary>
public sealed class ExportAuditEntries(
    IAuditStore store, ListAuditEntries list, IInstallationSettings settings, TimeProvider time)
{
    public const int MaxRows = 10_000;
    public const int MaxDays = 366;

    /// <summary>The fields of the record of an export.</summary>
    public static class Fields
    {
        public const string From = "From";
        public const string To = "To";
        public const string ItemType = "ItemType";
        public const string Person = "Person";
        public const string Rows = "Rows";
    }

    /// <summary>The query's problems: the period is needed, starts before or on its end and has up to <see cref="MaxDays"/> days.</summary>
    public static IReadOnlyList<AuditQueryError> Validate(AuditExportQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var errors = new List<AuditQueryError>();
        if (query.From is null)
        {
            errors.Add(new("from", "The first day of the period is needed."));
        }

        if (query.To is null)
        {
            errors.Add(new("to", "The last day of the period is needed."));
        }

        if (query.From is { } from && query.To is { } to)
        {
            if (to < from)
            {
                errors.Add(new("to", "The last day of the period comes on or after its first day."));
            }
            else if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            {
                errors.Add(new("to", $"The period has up to {MaxDays} days."));
            }
        }

        return errors;
    }

    public async Task<AuditExportResult> ExecuteAsync(
        AuditExportQuery query, Guid administratorId, CancellationToken cancellationToken)
    {
        if (Validate(query).Count > 0)
        {
            throw new ArgumentException("Invalid audit export; validate it first.", nameof(query));
        }

        var from = query.From!.Value;
        var to = query.To!.Value;
        var installation = await settings.GetAsync(cancellationToken);
        var itemType = string.IsNullOrWhiteSpace(query.ItemType) ? null : query.ItemType.Trim();
        var page = await store.ListAsync(
            new AuditQuery(
                query.ActorId,
                itemType,
                new DateTimeOffset(installation.StartOf(from), TimeSpan.Zero),
                new DateTimeOffset(installation.StartOf(to.AddDays(1)), TimeSpan.Zero),
                1,
                MaxRows),
            cancellationToken);
        if (page.Total > MaxRows)
        {
            return new AuditExportResult(null, page.Total);
        }

        var views = await list.ViewsAsync(page.Items, cancellationToken);
        List<AuditField> filters =
        [
            new(Fields.From, null, from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Secret: false),
            new(Fields.To, null, to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Secret: false),
        ];
        if (itemType is not null)
        {
            filters.Add(new(Fields.ItemType, null, itemType, Secret: false));
        }

        if (query.ActorId is { } actorId)
        {
            filters.Add(new(Fields.Person, null, actorId.ToString(), Secret: false, UserAudit.Reference));
        }

        filters.Add(new(Fields.Rows, null, page.Total.ToString(CultureInfo.InvariantCulture), Secret: false));
        await store.AddAsync(AuditEntry.Exported(time.GetUtcNow().UtcDateTime, administratorId, filters), cancellationToken);
        return new AuditExportResult(views, page.Total);
    }
}
