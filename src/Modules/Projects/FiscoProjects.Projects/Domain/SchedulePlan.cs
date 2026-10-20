using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// How much of the project should be done by the end of a day (ADR-0073): the average, weighted by the duration, of the
/// part of each task with dates of its own that should be done, in business days of the calendar of the installation;
/// with a baseline, by its dates (ADR-0021).
/// </summary>
public static class SchedulePlan
{
    /// <summary>
    /// The planned progress on the day, rounded as the progress of the project; none without tasks to plan (none in the
    /// baseline, when there is one).
    /// </summary>
    public static int? PlannedProgress(
        IEnumerable<ScheduleItem> items, ScheduleBaseline? baseline, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(days);
        var all = items.ToList();
        var summaries = all.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
        var spans = new List<(DateOnly Start, DateOnly End, int Duration)>();
        foreach (var task in all.Where(item => item.Kind == ScheduleItemKind.Task && !summaries.Contains(item.Id)))
        {
            if (baseline is null)
            {
                spans.Add((task.Start, task.End, task.Duration));
            }
            else if (baseline.ItemOf(task.Id) is { } saved)
            {
                spans.Add((saved.Start, saved.End, Math.Max(1, days.CountBusinessDays(saved.Start, saved.End))));
            }
        }

        var total = spans.Sum(span => (long)span.Duration);
        if (total == 0)
        {
            return null;
        }

        // Each task: the business days from its start up to today, out of its duration, between none and all of it.
        var planned = spans.Sum(span => today < span.Start
            ? 0L
            : Math.Min(span.Duration, days.CountBusinessDays(span.Start, today < span.End ? today : span.End)) * 100L);
        return (int)(((2 * planned) + total) / (2 * total));
    }
}
