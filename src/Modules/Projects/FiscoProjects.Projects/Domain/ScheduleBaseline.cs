using FiscoProjects.Calendar.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The baseline of a project (ADR-0021): a copy of the start and the end of each task and milestone and of the
/// project, on the day it was saved, in the time zone of the installation. There is one per project, which the manager
/// saves again to update it. It is a snapshot: an item removed later stays in it unread, and an item included later is
/// not in it. Its id is the id of the project.
/// </summary>
public sealed class ScheduleBaseline : Entity
{
    private readonly List<ScheduleBaselineItem> _items = [];

    private ScheduleBaseline()
    {
    }

    internal ScheduleBaseline(Guid projectId)
    {
        Id = projectId;
    }

    /// <summary>The day it was saved, in the time zone of the installation.</summary>
    public DateOnly SavedOn { get; private set; }

    /// <summary>The start of the project on that day.</summary>
    public DateOnly Start { get; private set; }

    /// <summary>The end of the project on that day.</summary>
    public DateOnly End { get; private set; }

    public IReadOnlyList<ScheduleBaselineItem> Items => _items;

    /// <summary>The dates of an item in the baseline; none when it was not in the schedule then.</summary>
    public ScheduleBaselineItem? ItemOf(Guid itemId) => _items.FirstOrDefault(item => item.ItemId == itemId);

    /// <summary>
    /// The business days the end of an item or of the project is after (positive) or before (negative) its end in the
    /// baseline: the days from the day after the earlier one up to the later one, both included (ADR-0018).
    /// </summary>
    public static int DeviationOf(DateOnly end, DateOnly baselineEnd, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return end > baselineEnd
            ? days.CountBusinessDays(baselineEnd.AddDays(1), end)
            : end < baselineEnd ? -days.CountBusinessDays(end.AddDays(1), baselineEnd) : 0;
    }

    /// <summary>
    /// The baseline compared with the schedule as it is now: for each item in it, its saved dates and the deviation of
    /// its end, and the deviation of the end of the project, none without items now.
    /// </summary>
    public BaselineComparison CompareWith(IEnumerable<ScheduleItem> items, DateOnly? end, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(days);
        var saved = _items.ToDictionary(item => item.ItemId);
        var compared = new Dictionary<Guid, BaselineItemComparison>();
        foreach (var item in items)
        {
            if (saved.TryGetValue(item.Id, out var dates))
            {
                compared[item.Id] = new BaselineItemComparison(dates.Start, dates.End, DeviationOf(item.End, dates.End, days));
            }
        }

        return new BaselineComparison(SavedOn, Start, End, end is { } current ? DeviationOf(current, End, days) : null, compared);
    }

    /// <summary>Copies the dates of the items and of the project, on the day; the items it had are replaced.</summary>
    internal void Save(IReadOnlyCollection<ScheduleItem> items, DateOnly today)
    {
        SavedOn = today;
        Start = items.Min(item => item.Start);
        End = items.Max(item => item.End);
        var current = items.ToDictionary(item => item.Id);
        _items.RemoveAll(saved => !current.ContainsKey(saved.ItemId));
        foreach (var item in items)
        {
            if (ItemOf(item.Id) is { } saved)
            {
                saved.Set(item.Start, item.End);
            }
            else
            {
                _items.Add(new ScheduleBaselineItem(Id, item.Id, item.Start, item.End));
            }
        }
    }
}

/// <summary>The start and the end of a task or milestone in the baseline of its project.</summary>
public sealed class ScheduleBaselineItem : Entity
{
    private ScheduleBaselineItem()
    {
    }

    internal ScheduleBaselineItem(Guid projectId, Guid itemId, DateOnly start, DateOnly end)
    {
        ProjectId = projectId;
        ItemId = itemId;
        Start = start;
        End = end;
    }

    public Guid ProjectId { get; private init; }

    public Guid ItemId { get; private init; }

    public DateOnly Start { get; private set; }

    public DateOnly End { get; private set; }

    internal void Set(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>The saved dates of an item and how many business days its end is after (or before, negative) them.</summary>
public sealed record BaselineItemComparison(DateOnly Start, DateOnly End, int Deviation);

/// <summary>
/// The baseline against the schedule now: the day it was saved, the dates of the project then, the deviation of the end
/// of the project (none without items now) and the items that were in it.
/// </summary>
public sealed record BaselineComparison(
    DateOnly SavedOn,
    DateOnly Start,
    DateOnly End,
    int? Deviation,
    IReadOnlyDictionary<Guid, BaselineItemComparison> Items);
