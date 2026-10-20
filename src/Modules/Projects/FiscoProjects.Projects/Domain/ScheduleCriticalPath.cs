using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The critical path of a schedule (ADR-0021, ADR-0051): the tasks and milestones with dates of their own without float,
/// the ones that cannot end one business day later without the end of the project moving. It goes backwards from the
/// end of the project through the dependencies of the kind "finishes, then starts", on the dates the schedule already
/// has and in business days of the calendar of the installation (ADR-0018). A task with subtasks is out of it, and its
/// subtasks are in it as any item (ADR-0088).
/// </summary>
public static class ScheduleCriticalPath
{
    /// <summary>The ids of the tasks and milestones on the critical path; none without items.</summary>
    public static IReadOnlySet<Guid> Of(
        IEnumerable<ScheduleItem> items, IEnumerable<ScheduleDependency> dependencies, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(days);
        var all = items.ToList();
        var summaries = all.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
        var own = all.Where(item => !summaries.Contains(item.Id)).ToDictionary(item => item.Id);
        if (own.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var links = dependencies
            .Where(dependency => own.ContainsKey(dependency.PredecessorId) && own.ContainsKey(dependency.SuccessorId))
            .ToList();
        var successors = links.ToLookup(dependency => dependency.PredecessorId, dependency => dependency.SuccessorId);
        var predecessors = links.ToLookup(dependency => dependency.SuccessorId, dependency => dependency.PredecessorId);
        var finish = own.Values.Max(item => item.End);

        // Backwards, each item after all the items that come after it: the ones with no successors first.
        var waiting = own.Keys.ToDictionary(id => id, id => successors[id].Count());
        var ready = new Queue<Guid>(waiting.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var latestStarts = new Dictionary<Guid, DateOnly>();
        var critical = new HashSet<Guid>();
        while (ready.TryDequeue(out var id))
        {
            var item = own[id];
            var latestEnd = successors[id]
                .Select(successor => LatestEndBefore(own[successor], latestStarts[successor], days))
                .DefaultIfEmpty(finish)
                .Min();
            latestStarts[id] = item.Kind == ScheduleItemKind.Milestone
                ? latestEnd
                : days.AddBusinessDays(latestEnd, -(item.Duration - 1));
            if (latestEnd <= item.End)
            {
                critical.Add(id);
            }

            foreach (var predecessor in predecessors[id])
            {
                if (--waiting[predecessor] == 0)
                {
                    ready.Enqueue(predecessor);
                }
            }
        }

        return critical;
    }

    /// <summary>
    /// The latest an item may end for the one after it to start on its latest start: the business day before for a
    /// task, the same day for a milestone, the inverse of the rule that places them (Schedule.EarliestAfter).
    /// </summary>
    private static DateOnly LatestEndBefore(ScheduleItem successor, DateOnly latestStart, IBusinessDays days) =>
        successor.Kind == ScheduleItemKind.Milestone ? latestStart : days.AddBusinessDays(latestStart, -1);
}
