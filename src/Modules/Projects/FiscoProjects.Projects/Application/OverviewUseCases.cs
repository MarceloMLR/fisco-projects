using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>The progress of the project and how much should be done by today (none with no task to plan), and by what.</summary>
public sealed record OverviewProgress(int Actual, int? Planned, bool ByBaseline);

/// <summary>The end of the schedule and, with a baseline, its end and the business days of deviation.</summary>
public sealed record OverviewDeadline(DateOnly End, DateOnly? BaselineEnd, int? Deviation);

/// <summary>The next milestone not reached: its day, the business days left until it, or the business days it is late.</summary>
public sealed record OverviewMilestone(Guid Id, string Name, DateOnly Day, int BusinessDaysLeft, int? DaysLate);

/// <summary>What a line of the timeline of the overview is.</summary>
public enum OverviewRowKind
{
    Phase = 1,
    Milestone = 2,
}

/// <summary>
/// A line of the smaller timeline (ADR-0071): a phase with its dates, its progress, whether it is late and on the critical
/// path and its dates in the baseline; or a milestone on its day, reached or late.
/// </summary>
public sealed record OverviewRow(
    OverviewRowKind Kind,
    Guid Id,
    string Name,
    DateOnly Start,
    DateOnly End,
    int Progress,
    bool Late,
    bool Critical,
    bool Reached,
    DateOnly? BaselineStart,
    DateOnly? BaselineEnd);

/// <summary>The period of the schedule, its non-business days and its phases and milestones in order.</summary>
public sealed record OverviewTimeline(DateOnly Start, DateOnly End, IReadOnlyList<DateOnly> NonWorkingDays, IReadOnlyList<OverviewRow> Rows);

/// <summary>A late task: its owner, progress, end, business days of delay and the names of the items that depend on it.</summary>
public sealed record OverviewLateTask(
    Guid Id, string Name, Guid? OwnerId, int Progress, DateOnly End, int DaysLate, IReadOnlyList<string> Blocks);

/// <summary>A pending request of acceptance the person sees, with the milestone and its day.</summary>
public sealed record OverviewAcceptance(
    Guid Id, string Title, Guid RecipientId, DateTime RequestedAt, string? Milestone, DateOnly? MilestoneDay);

/// <summary>The teams of a project (ADR-0040, ADR-0068).</summary>
public enum OverviewTeam
{
    Management = 1,
    Functional = 2,
    Development = 3,
}

/// <summary>A team with its people and, for the teams that serve tickets, how many open tickets it has.</summary>
public sealed record OverviewTeamPeople(OverviewTeam Team, IReadOnlyList<ProjectPerson> People, int? OpenTickets);

/// <summary>
/// The overview of a project (ADR-0073): the numbers of the schedule (none without tasks and milestones), the tickets,
/// the next milestone, the timeline, what needs attention, the team and the names of the people it shows.
/// </summary>
public sealed record ProjectOverview(
    DateOnly Today,
    OverviewProgress? Progress,
    OverviewDeadline? Deadline,
    ProjectTicketOverview Tickets,
    OverviewMilestone? NextMilestone,
    OverviewTimeline? Timeline,
    IReadOnlyList<OverviewLateTask> LateTasks,
    IReadOnlyList<OverviewAcceptance> Acceptances,
    IReadOnlyList<OverviewTeamPeople> Teams,
    IReadOnlyDictionary<Guid, string> Names)
{
    public string NameOf(Guid id) => Names.TryGetValue(id, out var name) ? name : string.Empty;
}

/// <summary>
/// The overview of a project, which answers "está no prazo?" (ADR-0073), for whom sees the project (ADR-0043): the
/// progress against the planned one (<see cref="SchedulePlan"/>), the end against the baseline, the tickets
/// (<see cref="IProjectTickets"/>), the next milestone, the phases and the milestones, the late tasks, the pending
/// requests of acceptance the person sees (<see cref="IProjectAcceptances"/>) and the team.
/// </summary>
public sealed class GetProjectOverview(
    IProjectStore projects,
    IScheduleStore schedules,
    IWorkCalendar calendar,
    InstallationClock clock,
    IUserDirectory users,
    IEnumerable<IProjectTickets> tickets,
    IEnumerable<IProjectAcceptances> acceptances)
{
    /// <returns>The overview, or null when the project does not exist for the person.</returns>
    public async Task<ProjectOverview?> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null || !ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId)).CanView)
        {
            return null;
        }

        var today = await clock.TodayAsync(cancellationToken);
        var days = await calendar.GetAsync(cancellationToken);
        var schedule = await schedules.FindAsync(projectId, cancellationToken) ?? new Schedule(projectId);
        var figures = schedule.Figures(today);
        var ticketOverview = ProjectTicketOverview.None;
        foreach (var module in tickets)
        {
            ticketOverview = await module.OverviewAsync(projectId, cancellationToken);
        }

        var pending = new List<PendingProjectAcceptance>();
        foreach (var module in acceptances)
        {
            pending.AddRange(await module.PendingForAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken));
        }

        var items = schedule.Items.ToDictionary(item => item.Id);
        var lateTasks = LateTasksOf(schedule, figures, today, days);
        var teams = TeamsOf(project, ticketOverview);
        var people = lateTasks.Where(task => task.OwnerId is not null).Select(task => task.OwnerId!.Value)
            .Concat(ticketOverview.Attention.Where(ticket => ticket.AssigneeId is not null).Select(ticket => ticket.AssigneeId!.Value))
            .Concat(pending.Select(term => term.RecipientId))
            .Concat(teams.SelectMany(team => team.People).Select(person => person.Id));
        var names = await PeopleNames.OfAsync(users, people, cancellationToken);

        return new ProjectOverview(
            today,
            figures.Start is null
                ? null
                : new OverviewProgress(
                    figures.Progress,
                    SchedulePlan.PlannedProgress(schedule.Items, schedule.Baseline, today, days),
                    schedule.Baseline is not null),
            figures.End is not { } end
                ? null
                : new OverviewDeadline(
                    end,
                    schedule.Baseline?.End,
                    schedule.Baseline is { } baseline ? ScheduleBaseline.DeviationOf(end, baseline.End, days) : null),
            ticketOverview,
            NextMilestoneOf(schedule, today, days),
            TimelineOf(schedule, figures, days),
            lateTasks,
            [.. pending.Select(term =>
            {
                var milestone = items.GetValueOrDefault(term.MilestoneId);
                return new OverviewAcceptance(term.Id, term.Title, term.RecipientId, term.RequestedAt, milestone?.Name, milestone?.Start);
            })],
            [.. teams.Select(team => team with
            {
                People = [.. team.People.Select(person => person with { Name = PeopleNames.NameOf(names, person.Id) })],
            })],
            names);
    }

    /// <summary>
    /// The first milestone not reached by its day, a late one first: the business days left until it, from the day
    /// after today (none on the day itself), or the business days it is late (ADR-0086).
    /// </summary>
    private static OverviewMilestone? NextMilestoneOf(Schedule schedule, DateOnly today, IBusinessDays days) =>
        schedule.Items
            .Where(item => item.Kind == ScheduleItemKind.Milestone && item.ReachedOn is null)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => new OverviewMilestone(
                item.Id,
                item.Name,
                item.Start,
                item.Start > today ? days.CountBusinessDays(today.AddDays(1), item.Start) : 0,
                item.IsLate(today) ? item.DaysLate(today, days) : null))
            .FirstOrDefault();

    /// <summary>The late tasks with dates of their own, the most late first, with the names of what depends on them.</summary>
    private static List<OverviewLateTask> LateTasksOf(Schedule schedule, ScheduleFigures figures, DateOnly today, IBusinessDays days)
    {
        var names = schedule.Items.ToDictionary(item => item.Id, item => item.Name);
        var successors = schedule.Dependencies.ToLookup(dependency => dependency.PredecessorId, dependency => dependency.SuccessorId);
        return
        [
            .. schedule.Items
                .Where(item => item.Kind == ScheduleItemKind.Task && !figures.Summaries.Contains(item.Id) && item.IsLate(today))
                .Select(item => new OverviewLateTask(
                    item.Id,
                    item.Name,
                    item.OwnerId,
                    item.Progress,
                    item.End,
                    item.DaysLate(today, days),
                    [.. successors[item.Id].Where(names.ContainsKey).Select(id => names[id])]))
                .OrderByDescending(task => task.DaysLate)
                .ThenBy(task => task.End)
                .ThenBy(task => task.Name, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    /// <summary>
    /// The phases with dates and the milestones of the schedule, by start, a phase before a milestone of the same day;
    /// with the critical path and the baseline (ADR-0021).
    /// </summary>
    private static OverviewTimeline? TimelineOf(Schedule schedule, ScheduleFigures figures, IBusinessDays days)
    {
        if (figures is not { Start: { } start, End: { } end })
        {
            return null;
        }

        var critical = ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, days);
        var baseline = schedule.Baseline;
        var rows = new List<OverviewRow>();
        foreach (var phase in figures.Phases.Where(phase => phase.Start is not null))
        {
            var saved = baseline is null
                ? []
                : phase.Items.Select(item => baseline.ItemOf(item.Id)).OfType<ScheduleBaselineItem>().ToList();
            rows.Add(new OverviewRow(
                OverviewRowKind.Phase,
                phase.Phase.Id,
                phase.Phase.Name,
                phase.Start!.Value,
                phase.End!.Value,
                phase.Progress,
                phase.IsLate,
                phase.Items.Any(item => critical.Contains(item.Id)),
                false,
                saved.Count == 0 ? null : saved.Min(item => item.Start),
                saved.Count == 0 ? null : saved.Max(item => item.End)));
        }

        foreach (var milestone in figures.Phases.SelectMany(phase => phase.Items).Where(item => item.Kind == ScheduleItemKind.Milestone))
        {
            var saved = baseline?.ItemOf(milestone.Id);
            rows.Add(new OverviewRow(
                OverviewRowKind.Milestone,
                milestone.Id,
                milestone.Name,
                milestone.Start,
                milestone.End,
                0,
                figures.LateItems.Contains(milestone.Id),
                critical.Contains(milestone.Id),
                milestone.ReachedOn is not null,
                saved?.Start,
                saved?.End));
        }

        return new OverviewTimeline(
            start,
            end,
            Schedule.NonWorkingDaysAround(start, end, days),
            [.. rows.OrderBy(row => row.Start).ThenBy(row => row.Kind).ThenBy(row => row.End)]);
    }

    /// <summary>
    /// The teams (ADR-0040, ADR-0068): Gestão, the manager and the tech lead; Funcional, the contributors;
    /// Desenvolvimento, the tech lead and the developers; the two teams that serve tickets with their open tickets.
    /// </summary>
    private static List<OverviewTeamPeople> TeamsOf(Project project, ProjectTicketOverview tickets)
    {
        List<ProjectPerson> With(params ProjectRole[] roles) =>
            [.. project.Members
                .Where(member => roles.Contains(member.Role))
                .OrderBy(member => member.Role)
                .Select(member => new ProjectPerson(member.UserId, string.Empty, member.Role))];
        return
        [
            new(OverviewTeam.Management, With(ProjectRole.Manager, ProjectRole.TechLead), null),
            new(OverviewTeam.Functional, With(ProjectRole.Contributor), tickets.OpenFunctional),
            new(OverviewTeam.Development, With(ProjectRole.TechLead, ProjectRole.Developer), tickets.OpenDevelopment),
        ];
    }
}
