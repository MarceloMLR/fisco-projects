using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;

namespace GerenWare.Projects.Application;

/// <summary>An open ticket that threatens a milestone (ADR-0164), through the items linked to it that lead there.</summary>
public sealed record ScheduleThreat(ProjectTicketImpact Ticket, IReadOnlyList<ScheduleItem> Through);

/// <summary>
/// The open tickets of a project on its schedule (ADR-0164): the ones linked to each task and milestone, and the ones that
/// threaten each milestone not reached, by the rule of <see cref="ScheduleImpacts"/>. Nothing here changes a date.
/// </summary>
public sealed record ScheduleTickets(
    IReadOnlyDictionary<Guid, IReadOnlyList<ProjectTicketImpact>> OfItems,
    IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleThreat>> Threats)
{
    public static ScheduleTickets None { get; } = new(
        new Dictionary<Guid, IReadOnlyList<ProjectTicketImpact>>(), new Dictionary<Guid, IReadOnlyList<ScheduleThreat>>());

    /// <summary>The open tickets of the project, from every module that has them; none without any.</summary>
    public static async Task<IReadOnlyList<ProjectTicketImpact>> ImpactsAsync(
        IEnumerable<IProjectTickets> sources, Guid projectId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var impacts = new List<ProjectTicketImpact>();
        foreach (var source in sources)
        {
            impacts.AddRange(await source.ImpactsAsync(projectId, cancellationToken));
        }

        return impacts;
    }

    /// <summary>
    /// The tickets of each item and the threats of each milestone, by the number of the ticket; a milestone linked to a
    /// ticket has it among its tickets, not among its threats.
    /// </summary>
    public static ScheduleTickets Of(Schedule schedule, IReadOnlyList<ProjectTicketImpact> impacts)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(impacts);
        if (impacts.Count == 0)
        {
            return None;
        }

        var ofItems = new Dictionary<Guid, List<ProjectTicketImpact>>();
        var threats = new Dictionary<Guid, List<ScheduleThreat>>();
        foreach (var impact in impacts.OrderBy(impact => impact.Number))
        {
            var linked = impact.ItemIds
                .Select(schedule.ItemOf)
                .OfType<ScheduleItem>()
                .OrderBy(item => item.Start)
                .ThenBy(item => item.Name, StringComparer.CurrentCulture)
                .ToList();
            var through = new Dictionary<Guid, List<ScheduleItem>>();
            foreach (var item in linked)
            {
                Add(ofItems, item.Id, impact);
                foreach (var milestone in ScheduleImpacts.MilestonesAfter(schedule, item.Id).Where(id => !impact.ItemIds.Contains(id)))
                {
                    if (!through.TryGetValue(milestone, out var items))
                    {
                        through[milestone] = items = [];
                    }

                    items.Add(item);
                }
            }

            foreach (var (milestone, items) in through)
            {
                Add(threats, milestone, new ScheduleThreat(impact, items));
            }
        }

        return new ScheduleTickets(
            ofItems.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ProjectTicketImpact>)pair.Value),
            threats.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ScheduleThreat>)pair.Value));
    }

    private static void Add<T>(Dictionary<Guid, List<T>> map, Guid key, T value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = list = [];
        }

        list.Add(value);
    }
}
