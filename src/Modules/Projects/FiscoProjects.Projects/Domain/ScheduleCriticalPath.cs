using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Domain;

/// <summary>The float of a task or milestone (GanttB-1): the business days it may end later and the latest it may end.</summary>
public sealed record ScheduleSlack(int Days, DateOnly LatestEnd);

/// <summary>What the backwards pass finds: the items on the critical path and the float of each item with dates of its own.</summary>
public sealed record ScheduleAnalysis(IReadOnlySet<Guid> Critical, IReadOnlyDictionary<Guid, ScheduleSlack> Slack);

/// <summary>
/// The critical path of a schedule (ADR-0021, ADR-0051): the tasks and milestones with dates of their own without float,
/// the ones that cannot end one business day later without the end of the project moving. It goes backwards from the
/// end of the project through the dependencies, by their kinds and lags (ADR-0137, <see cref="ScheduleLinks.LatestEnd"/>),
/// on the dates the schedule already has and in business days of the calendar of the installation (ADR-0018), and says
/// the float of each item: the business days after its end up to the latest it may end (GanttB-1). A task with
/// subtasks is out of it, and its subtasks are in it as any item (ADR-0088).
/// </summary>
public static class ScheduleCriticalPath
{
    /// <summary>The ids of the tasks and milestones on the critical path; none without items.</summary>
    public static IReadOnlySet<Guid> Of(
        IEnumerable<ScheduleItem> items, IEnumerable<ScheduleDependency> dependencies, IBusinessDays days) =>
        Analyze(items, dependencies, days).Critical;

    /// <summary>The critical path and the float of each task and milestone with dates of its own; both empty without items.</summary>
    public static ScheduleAnalysis Analyze(
        IEnumerable<ScheduleItem> items, IEnumerable<ScheduleDependency> dependencies, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(days);
        var all = items.ToList();
        var summaries = all.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
        var own = all.Where(item => !summaries.Contains(item.Id)).ToDictionary(item => item.Id);
        var critical = new HashSet<Guid>();
        var slack = new Dictionary<Guid, ScheduleSlack>();
        if (own.Count == 0)
        {
            return new ScheduleAnalysis(critical, slack);
        }

        var links = dependencies
            .Where(dependency => own.ContainsKey(dependency.PredecessorId) && own.ContainsKey(dependency.SuccessorId))
            .ToList();
        var successors = links.ToLookup(dependency => dependency.PredecessorId);
        var predecessors = links.ToLookup(dependency => dependency.SuccessorId, dependency => dependency.PredecessorId);
        var finish = own.Values.Max(item => item.End);

        // Backwards, each item after all the items that come after it: the ones with no successors first.
        var waiting = own.Keys.ToDictionary(id => id, id => successors[id].Count());
        var ready = new Queue<Guid>(waiting.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var latestStarts = new Dictionary<Guid, DateOnly>();
        while (ready.TryDequeue(out var id))
        {
            var item = own[id];
            var latestEnd = successors[id]
                .Select(link => ScheduleLinks.LatestEnd(
                    link.Type, link.Lag, item, own[link.SuccessorId], latestStarts[link.SuccessorId], days))
                .DefaultIfEmpty(finish)
                .Min();
            latestStarts[id] = item.Kind == ScheduleItemKind.Milestone
                ? latestEnd
                : days.AddBusinessDays(latestEnd, -(item.Duration - 1));
            if (latestEnd <= item.End)
            {
                critical.Add(id);
            }

            slack[id] = new ScheduleSlack(
                latestEnd > item.End ? days.CountBusinessDays(item.End.AddDays(1), latestEnd) : 0, latestEnd);
            foreach (var predecessor in predecessors[id])
            {
                if (--waiting[predecessor] == 0)
                {
                    ready.Enqueue(predecessor);
                }
            }
        }

        return new ScheduleAnalysis(critical, slack);
    }
}
