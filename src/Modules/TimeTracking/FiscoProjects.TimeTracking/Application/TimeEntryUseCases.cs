using FiscoProjects.Administration.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Contracts;
using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Application;

/// <summary>Where the time entries are kept.</summary>
public interface ITimeEntryStore
{
    /// <summary>The entries of the person from a day to another, both included, not tracked.</summary>
    Task<IReadOnlyList<TimeEntry>> OfPersonAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>The hours of the person on the day, in every project, without the entry being changed.</summary>
    Task<decimal> HoursOfDayAsync(Guid userId, DateOnly day, Guid? except, CancellationToken cancellationToken);

    /// <summary>The entry, tracked.</summary>
    Task<TimeEntry?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(TimeEntry entry);

    void Remove(TimeEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>A time entry as the person fills it in.</summary>
public sealed record TimeEntryRequest(Guid? ProjectId, DateOnly? Day, decimal? Hours, string? Description, Guid? ScheduleItemId, Guid? TicketId);

public enum TimeEntryOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Invalid = 5,
}

/// <summary>What happened to a time entry: its id, or why nothing was saved, with the fields that are not valid.</summary>
public sealed record TimeEntryResult(TimeEntryOutcome Outcome, Guid? Id = null, IReadOnlyList<TimeEntryField>? Invalid = null)
{
    public static TimeEntryResult Of(TimeEntryRefusal refusal) => new(refusal switch
    {
        TimeEntryRefusal.NotSeen => TimeEntryOutcome.NotFound,
        TimeEntryRefusal.Forbidden => TimeEntryOutcome.Forbidden,
        _ => TimeEntryOutcome.ProjectClosed,
    });

    public static TimeEntryResult InvalidOf(params TimeEntryField[] fields) => new(TimeEntryOutcome.Invalid, null, fields);
}

/// <summary>A project as a time entry shows it.</summary>
public sealed record TimeEntryProject(Guid Id, string Code, string Name);

/// <summary>The task of a time entry, without the name when it left the schedule.</summary>
public sealed record TimeEntryTask(Guid Id, string? Name);

/// <summary>The ticket of a time entry.</summary>
public sealed record TimeEntryTicket(Guid Id, int Number, string Title);

/// <summary>A time entry with its project, its task or ticket and whether the person may change it.</summary>
public sealed record TimeEntryRow(TimeEntry Entry, TimeEntryProject Project, TimeEntryTask? Task, TimeEntryTicket? Ticket, bool CanChange);

/// <summary>The entries of a period, the newest day first, and their total.</summary>
public sealed record TimeEntryList(IReadOnlyList<TimeEntryRow> Items, decimal Total);

/// <summary>The tasks of the schedule and the tickets of a project, for a time entry.</summary>
public sealed record TimeEntryWork(IReadOnlyList<ScheduleItemInfo> Tasks, IReadOnlyList<ProjectTicketInfo> Tickets);

/// <summary>Today in the time zone of the installation (ADR-0013).</summary>
public sealed class TimeEntryClock(IInstallationSettings installation, TimeProvider clock)
{
    public DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<DateOnly> TodayAsync(CancellationToken cancellationToken) =>
        (await installation.GetAsync(cancellationToken)).DayOf(Now);
}

/// <summary>
/// The projects as a person sees them for the hours: any project, with the role of the person in it, so the entries
/// of a project the person left still show (ADR-0128).
/// </summary>
public sealed class TimeEntryProjects(IProjectAccess projects)
{
    /// <summary>The project with the role of the person, null for whoever is not in it; null when it does not exist.</summary>
    public Task<ProjectAccess?> FindAsync(Guid projectId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return projects.FindAsync(projectId, viewer.UserId, isAdministrator: true, cancellationToken);
    }

    /// <summary>Every project, by code, with the role of the person in it.</summary>
    public async Task<IReadOnlyList<ProjectAccess>> AllAsync(TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return await projects.VisibleAsync(viewer.UserId, isAdministrator: true, cancellationToken);
    }
}

/// <summary>
/// Checks the task and the ticket of an entry: the task a task of the schedule of the project (not a phase or a
/// milestone), the ticket a ticket of the project. A task kept from the entry being changed stays, even if it left the
/// schedule (ADR-0128).
/// </summary>
public sealed class TimeEntryTargets(IScheduleItems schedule, IProjectTicketList tickets)
{
    public async Task<TimeEntryField?> InvalidAsync(TimeEntryForm form, TimeEntry? changed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var sameProject = changed is not null && changed.ProjectId == form.ProjectId;
        if (form.ScheduleItemId is { } itemId && !(sameProject && changed!.ScheduleItemId == itemId))
        {
            var items = await schedule.ItemsAsync(form.ProjectId, cancellationToken);
            if (!items.Any(item => item.Id == itemId && item.Type == ScheduleItemType.Task))
            {
                return TimeEntryField.ScheduleItem;
            }
        }

        if (form.TicketId is { } ticketId)
        {
            var found = await tickets.FindAsync([ticketId], cancellationToken);
            if (!found.Any(ticket => ticket.ProjectId == form.ProjectId))
            {
                return TimeEntryField.Ticket;
            }
        }

        return null;
    }
}

/// <summary>
/// The entries of a person in a period of up to 93 days, the newest day first, with the total (ADR-0128): the person
/// sees only the own entries; the Administrator, the entries of anyone.
/// </summary>
public sealed class ListTimeEntries(ITimeEntryStore store, TimeEntryProjects projects, IScheduleItems schedule, IProjectTicketList tickets)
{
    /// <summary>The longest period a list takes, in days.</summary>
    public const int MaxDays = 93;

    public async Task<(TimeEntryList? List, TimeEntryOutcome Outcome)> ExecuteAsync(
        DateOnly from, DateOnly to, Guid? personId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (to < from || to.DayNumber - from.DayNumber + 1 > MaxDays)
        {
            return (null, TimeEntryOutcome.Invalid);
        }

        var person = personId ?? viewer.UserId;
        if (person != viewer.UserId && !viewer.IsAdministrator)
        {
            return (null, TimeEntryOutcome.Forbidden);
        }

        var entries = await store.OfPersonAsync(person, from, to, cancellationToken);
        var all = (await projects.AllAsync(viewer, cancellationToken)).ToDictionary(project => project.Id);
        var tasks = new Dictionary<Guid, string>();
        foreach (var projectId in entries.Where(entry => entry.ScheduleItemId is not null).Select(entry => entry.ProjectId).Distinct())
        {
            foreach (var item in await schedule.ItemsAsync(projectId, cancellationToken))
            {
                tasks[item.Id] = item.Name;
            }
        }

        var ticketIds = entries.Where(entry => entry.TicketId is not null).Select(entry => entry.TicketId!.Value).Distinct().ToList();
        var ticketsById = (await tickets.FindAsync(ticketIds, cancellationToken)).ToDictionary(ticket => ticket.Id);
        var rows = entries
            .Where(entry => all.ContainsKey(entry.ProjectId))
            .OrderByDescending(entry => entry.Day)
            .ThenBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .Select(entry =>
            {
                var project = all[entry.ProjectId];
                var task = entry.ScheduleItemId is { } itemId ? new TimeEntryTask(itemId, tasks.GetValueOrDefault(itemId)) : null;
                var ticket = entry.TicketId is { } ticketId && ticketsById.TryGetValue(ticketId, out var found)
                    ? new TimeEntryTicket(found.Id, found.Number, found.Title)
                    : null;
                return new TimeEntryRow(
                    entry,
                    new TimeEntryProject(project.Id, project.Code, project.Name),
                    task,
                    ticket,
                    TimeEntryAccess.RefusalToChange(viewer, entry, project) is null);
            })
            .ToList();
        return (new TimeEntryList(rows, rows.Sum(row => row.Entry.Hours)), TimeEntryOutcome.Done);
    }
}

/// <summary>
/// The projects where the person logs hours: the open ones the person is in, but as viewer, and every open one for
/// the Administrator (ADR-0128).
/// </summary>
public sealed class ListTimeEntryProjects(TimeEntryProjects projects)
{
    public async Task<IReadOnlyList<ProjectAccess>> ExecuteAsync(TimeEntryViewer viewer, CancellationToken cancellationToken) =>
        [.. (await projects.AllAsync(viewer, cancellationToken)).Where(project => TimeEntryAccess.RefusalToLog(viewer, project) is null)];
}

/// <summary>
/// The tasks of the schedule, in its order, and the tickets of a project the person sees, first the ones not closed,
/// for the entry; null when the person does not see the project (404).
/// </summary>
public sealed class ListTimeEntryWork(TimeEntryProjects projects, IScheduleItems schedule, IProjectTicketList tickets)
{
    public async Task<TimeEntryWork?> ExecuteAsync(Guid projectId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        if (await projects.FindAsync(projectId, viewer, cancellationToken) is not { } project || !TimeEntryAccess.Sees(viewer, project))
        {
            return null;
        }

        var items = await schedule.ItemsAsync(projectId, cancellationToken);
        return new TimeEntryWork(
            [.. items.Where(item => item.Type == ScheduleItemType.Task)],
            await tickets.OfProjectAsync(projectId, cancellationToken));
    }
}

/// <summary>
/// The person logs hours in a project (ADR-0128): the Administrator in any, the people of the project but the viewer,
/// while it is open; the day up to today, and up to 24 hours of the person in a day.
/// </summary>
public sealed class AddTimeEntry(ITimeEntryStore store, TimeEntryProjects projects, TimeEntryTargets targets, TimeEntryClock clock)
{
    public async Task<TimeEntryResult> ExecuteAsync(TimeEntryRequest request, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);
        if (request.ProjectId is not { } projectId || projectId == Guid.Empty)
        {
            return TimeEntryResult.InvalidOf(TimeEntryField.Project);
        }

        var project = await projects.FindAsync(projectId, viewer, cancellationToken);
        if (TimeEntryAccess.RefusalToLog(viewer, project) is { } refusal)
        {
            return TimeEntryResult.Of(refusal);
        }

        var (form, invalid) = TimeEntryForm.Of(
            projectId, request.Day, request.Hours, request.Description, request.ScheduleItemId, request.TicketId,
            await clock.TodayAsync(cancellationToken));
        if (form is null)
        {
            return new TimeEntryResult(TimeEntryOutcome.Invalid, null, invalid);
        }

        if (await targets.InvalidAsync(form, null, cancellationToken) is { } target)
        {
            return TimeEntryResult.InvalidOf(target);
        }

        if (!TimeEntryForm.FitsInDay(form.Hours, await store.HoursOfDayAsync(viewer.UserId, form.Day, null, cancellationToken)))
        {
            return TimeEntryResult.InvalidOf(TimeEntryField.Hours);
        }

        var entry = new TimeEntry(viewer.UserId, form, clock.Now);
        store.Add(entry);
        await store.SaveChangesAsync(cancellationToken);
        return new TimeEntryResult(TimeEntryOutcome.Done, entry.Id);
    }
}

/// <summary>
/// The person corrects an own entry, and the Administrator anyone's (ADR-0128), with the rules of logging in the
/// project of the entry and, when the project changes, in the new one.
/// </summary>
public sealed class ChangeTimeEntry(ITimeEntryStore store, TimeEntryProjects projects, TimeEntryTargets targets, TimeEntryClock clock)
{
    public async Task<TimeEntryResult> ExecuteAsync(
        Guid entryId, TimeEntryRequest request, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);
        if (await store.FindAsync(entryId, cancellationToken) is not { } entry || !TimeEntryAccess.SeesEntry(viewer, entry))
        {
            return new TimeEntryResult(TimeEntryOutcome.NotFound);
        }

        var current = await projects.FindAsync(entry.ProjectId, viewer, cancellationToken);
        if (TimeEntryAccess.RefusalToChange(viewer, entry, current) is { } refusal)
        {
            return TimeEntryResult.Of(refusal);
        }

        if (request.ProjectId is not { } projectId || projectId == Guid.Empty)
        {
            return TimeEntryResult.InvalidOf(TimeEntryField.Project);
        }

        if (projectId != entry.ProjectId
            && TimeEntryAccess.RefusalToLog(viewer, await projects.FindAsync(projectId, viewer, cancellationToken)) is { } moving)
        {
            return TimeEntryResult.Of(moving);
        }

        var (form, invalid) = TimeEntryForm.Of(
            projectId, request.Day, request.Hours, request.Description, request.ScheduleItemId, request.TicketId,
            await clock.TodayAsync(cancellationToken));
        if (form is null)
        {
            return new TimeEntryResult(TimeEntryOutcome.Invalid, null, invalid);
        }

        if (await targets.InvalidAsync(form, entry, cancellationToken) is { } target)
        {
            return TimeEntryResult.InvalidOf(target);
        }

        if (!TimeEntryForm.FitsInDay(form.Hours, await store.HoursOfDayAsync(entry.UserId, form.Day, entry.Id, cancellationToken)))
        {
            return TimeEntryResult.InvalidOf(TimeEntryField.Hours);
        }

        entry.Change(form, clock.Now);
        await store.SaveChangesAsync(cancellationToken);
        return new TimeEntryResult(TimeEntryOutcome.Done, entry.Id);
    }
}

/// <summary>The person removes an own entry, and the Administrator anyone's, while the project is open (ADR-0128).</summary>
public sealed class DeleteTimeEntry(ITimeEntryStore store, TimeEntryProjects projects)
{
    public async Task<TimeEntryResult> ExecuteAsync(Guid entryId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await store.FindAsync(entryId, cancellationToken) is not { } entry || !TimeEntryAccess.SeesEntry(viewer, entry))
        {
            return new TimeEntryResult(TimeEntryOutcome.NotFound);
        }

        var project = await projects.FindAsync(entry.ProjectId, viewer, cancellationToken);
        if (TimeEntryAccess.RefusalToChange(viewer, entry, project) is { } refusal)
        {
            return TimeEntryResult.Of(refusal);
        }

        store.Remove(entry);
        await store.SaveChangesAsync(cancellationToken);
        return new TimeEntryResult(TimeEntryOutcome.Done, entry.Id);
    }
}
