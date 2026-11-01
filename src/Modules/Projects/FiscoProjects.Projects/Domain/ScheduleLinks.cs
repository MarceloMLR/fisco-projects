using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The kind of a dependency (ADR-0137): the next item starts after the one before ends, starts with it, or ends with it.
/// </summary>
public enum ScheduleLinkType
{
    /// <summary>The next item starts on the business day after the one before ends (a milestone, on that day).</summary>
    FinishToStart = 1,

    /// <summary>The next item starts on the day the one before starts.</summary>
    StartToStart = 2,

    /// <summary>The next item ends on the day the one before ends.</summary>
    FinishToFinish = 3,
}

/// <summary>The kind and the lag of a dependency on an item, as a plan asks for it (ADR-0137).</summary>
public sealed record ScheduleLink(Guid PredecessorId, ScheduleLinkType Type = ScheduleLinkType.FinishToStart, int Lag = 0);

/// <summary>
/// The rules of the dependencies in one place (ADR-0019, ADR-0137): where an item may start at the earliest after the
/// one before, by the kind and the lag in business days of the calendar of the installation (ADR-0018), and the inverse
/// for the critical path. The lag is counted from a business day, so a negative one mirrors a positive one.
/// </summary>
public static class ScheduleLinks
{
    /// <summary>The lag goes from 99 business days before to 999 after.</summary>
    public const int MinLag = -99;

    public const int MaxLag = 999;

    public static bool IsValid(ScheduleLinkType type, int lag) => Enum.IsDefined(type) && lag is >= MinLag and <= MaxLag;

    /// <summary>The earliest start of the next item by one dependency, maybe on a day off (the caller moves it).</summary>
    public static DateOnly EarliestStart(
        ScheduleLinkType type, int lag, ScheduleItem before, ScheduleItem next, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(days);
        return type switch
        {
            ScheduleLinkType.StartToStart => days.AddBusinessDays(OnABusinessDay(before.Start, days), lag),
            ScheduleLinkType.FinishToFinish => StartFor(days.AddBusinessDays(OnABusinessDay(before.End, days), lag), next, days),
            _ => days.AddBusinessDays(
                next.Kind == ScheduleItemKind.Milestone ? OnABusinessDay(before.End, days) : days.AddBusinessDays(before.End, 1),
                lag),
        };
    }

    /// <summary>
    /// The latest the item before may end for the next one to start on its latest start, by one dependency: the inverse
    /// of <see cref="EarliestStart"/>, for the critical path.
    /// </summary>
    public static DateOnly LatestEnd(
        ScheduleLinkType type, int lag, ScheduleItem before, ScheduleItem next, DateOnly nextLatestStart, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(days);
        switch (type)
        {
            case ScheduleLinkType.StartToStart:
                var latestStart = days.AddBusinessDays(nextLatestStart, -lag);
                return before.Kind == ScheduleItemKind.Milestone ? latestStart : days.AddBusinessDays(latestStart, before.Duration - 1);
            case ScheduleLinkType.FinishToFinish:
                var nextLatestEnd = next.Kind == ScheduleItemKind.Milestone
                    ? nextLatestStart
                    : days.AddBusinessDays(nextLatestStart, next.Duration - 1);
                return days.AddBusinessDays(nextLatestEnd, -lag);
            default:
                var end = days.AddBusinessDays(nextLatestStart, -lag);
                return next.Kind == ScheduleItemKind.Milestone ? end : days.AddBusinessDays(end, -1);
        }
    }

    /// <summary>The start of an item that ends on the day given, by its duration in business days.</summary>
    private static DateOnly StartFor(DateOnly end, ScheduleItem item, IBusinessDays days) =>
        item.Kind == ScheduleItemKind.Milestone ? end : days.AddBusinessDays(end, -(item.Duration - 1));

    private static DateOnly OnABusinessDay(DateOnly day, IBusinessDays days) =>
        days.IsBusinessDay(day) ? day : days.AddBusinessDays(day, 1);
}
