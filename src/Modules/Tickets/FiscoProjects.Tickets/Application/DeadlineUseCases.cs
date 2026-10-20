using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>The day of today in the installation and its business days, read once for many tickets.</summary>
public sealed record TicketDay(IBusinessDays Days, TimeZoneInfo Zone, DateOnly Today)
{
    /// <summary>The day of a moment in the time zone of the installation (ADR-0013).</summary>
    public DateOnly DayOf(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));
}

/// <summary>Where a deadline stands: its state, the day it is due and, when overdue, by how many business days.</summary>
public sealed record TicketDue(TicketDueState State, DateOnly? DueOn, int? DaysLate);

/// <summary>
/// The deadlines of a ticket today (ADR-0025): the first answer (null once the ticket finished without one), the
/// resolution (null for a ticket closed without being resolved, as a cancelled one; once resolved, met or late by the
/// day it was resolved), the business days used of the total and the business days it waited for the requester.
/// </summary>
public sealed record TicketDeadlineFigures(
    TicketDue? FirstResponse, DateTime? FirstRespondedAt, TicketDue? Resolution, int UsedDays, int TotalDays, int PausedDays);

/// <summary>
/// The deadlines of the tickets in one place (ADR-0025): set at the opening and at a new priority, from the day of the
/// opening, in business days of the calendar of the installation; paused while the ticket waits for the requester and
/// pushed by the paused days when it stops waiting. Every change of status goes through <see cref="TakeAsync"/>.
/// </summary>
public sealed class TicketDeadlines(ITicketStore store, IWorkCalendar calendar, IInstallationSettings installation, TimeProvider clock)
{
    public async Task<TicketDay> TodayAsync(CancellationToken cancellationToken)
    {
        var info = await installation.GetAsync(cancellationToken);
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(info.TimeZone, out var found) ? found : TimeZoneInfo.Utc;
        var days = await calendar.GetAsync(cancellationToken);
        var day = new TicketDay(days, zone, default);
        return day with { Today = day.DayOf(clock.GetUtcNow().UtcDateTime) };
    }

    /// <summary>The deadlines of the priority of the ticket, as the installation has them now, from the opening.</summary>
    public async Task StartAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var day = await TodayAsync(cancellationToken);
        var setting = (await store.DeadlinesAsync(cancellationToken)).Single(item => item.Priority == ticket.Priority);
        var opened = day.DayOf(ticket.OpenedAt);
        ticket.UseDeadlines(
            setting.FirstResponseDays,
            setting.ResolutionDays,
            day.Days.AddBusinessDays(opened, setting.FirstResponseDays),
            ticket.PausedSince is null
                ? day.Days.AddBusinessDays(opened, setting.ResolutionDays + ticket.PausedBusinessDays)
                : null);
    }

    /// <summary>
    /// Takes a transition of the flow and records it in the conversation; leaving Aguardando solicitante pushes the
    /// resolution by the business days it waited.
    /// </summary>
    public async Task TakeAsync(Ticket ticket, TicketTransition transition, Guid? actorId, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(transition);
        if (ticket.PausedSince is { } since && transition.To != TicketStatus.WaitingForRequester)
        {
            var day = await TodayAsync(cancellationToken);
            var paused = PausedDays(day, since, day.DayOf(now));
            ticket.Resume(paused, day.Days.AddBusinessDays(
                day.DayOf(ticket.OpenedAt), ticket.ResolutionDays + ticket.PausedBusinessDays + paused));
        }

        ticket.Apply(transition, now);
        store.AddEvent(TicketEvent.StatusChanged(ticket.Id, actorId, transition, now));
    }

    /// <summary>
    /// Closes the ticket because its project was cancelled (ADR-0043), and records it in the conversation: a ticket
    /// waiting for the requester stops waiting first, so its paused days do not grow afterwards.
    /// </summary>
    public async Task CloseWithProjectAsync(Ticket ticket, Guid actorId, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var from = ticket.Status;
        if (ticket.PausedSince is { } since)
        {
            var day = await TodayAsync(cancellationToken);
            var paused = PausedDays(day, since, day.DayOf(now));
            ticket.Resume(paused, day.Days.AddBusinessDays(
                day.DayOf(ticket.OpenedAt), ticket.ResolutionDays + ticket.PausedBusinessDays + paused));
        }

        ticket.CloseWithProject(now);
        store.AddEvent(TicketEvent.ClosedWithProject(ticket.Id, actorId, from, now));
    }

    /// <summary>The deadlines of the ticket today; null for a ticket opened before there were deadlines.</summary>
    public static TicketDeadlineFigures? FiguresOf(Ticket ticket, TicketDay day)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(day);
        if (ticket.FirstResponseDueOn is not { } firstDue)
        {
            return null;
        }

        var finished = ticket.Status is TicketStatus.Resolved or TicketStatus.Closed;
        var end = finished ? day.DayOf(ticket.ResolvedAt ?? ticket.ClosedAt ?? ticket.UpdatedAt) : day.Today;
        var pausedNow = ticket.PausedSince is { } since ? PausedDays(day, since, day.Today) : 0;
        var paused = ticket.PausedBusinessDays + pausedNow;
        var firstResponse = ticket.FirstRespondedAt is not null
            ? new TicketDue(TicketDueState.Met, firstDue, null)
            : finished ? null : StateOf(firstDue, day);
        var resolution = finished
            ? ResolvedDue(ticket, day)
            : ticket.Status == TicketStatus.WaitingForRequester || ticket.ResolutionDueOn is null
                ? new TicketDue(TicketDueState.Paused, null, null)
                : StateOf(ticket.ResolutionDueOn.Value, day);
        var used = day.Days.CountBusinessDays(day.DayOf(ticket.OpenedAt).AddDays(1), end) - paused;
        return new TicketDeadlineFigures(
            firstResponse, ticket.FirstRespondedAt, resolution, Math.Clamp(used, 0, ticket.ResolutionDays), ticket.ResolutionDays, paused);
    }

    /// <summary>The resolution of a finished ticket: met, or late by the business days after it was due.</summary>
    private static TicketDue? ResolvedDue(Ticket ticket, TicketDay day)
    {
        if (ticket.ResolvedAt is not { } resolvedAt)
        {
            return null;
        }

        var resolved = day.DayOf(resolvedAt);
        return ticket.ResolutionDueOn is { } due && resolved > due
            ? new TicketDue(TicketDueState.Overdue, due, Math.Max(1, day.Days.CountBusinessDays(due.AddDays(1), resolved)))
            : new TicketDue(TicketDueState.Met, ticket.ResolutionDueOn, null);
    }

    /// <summary>The business days after the day the waiting started, up to the given day.</summary>
    private static int PausedDays(TicketDay day, DateTime since, DateOnly until) =>
        day.Days.CountBusinessDays(day.DayOf(since).AddDays(1), until);

    private static TicketDue StateOf(DateOnly due, TicketDay day)
    {
        if (day.Today > due)
        {
            // A weekend after the day it was due still counts as late.
            return new TicketDue(TicketDueState.Overdue, due, Math.Max(1, day.Days.CountBusinessDays(due.AddDays(1), day.Today)));
        }

        return new TicketDue(due <= day.Days.AddBusinessDays(day.Today, 1) ? TicketDueState.Soon : TicketDueState.OnTrack, due, null);
    }
}

public enum PriorityChangeOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,

    /// <summary>The ticket is resolved or closed.</summary>
    NotAllowedNow = 4,

    Invalid = 5,
}

/// <summary>
/// Who serves the ticket changes its priority while it is open (ADR-0025, ADR-0044): the deadlines start again from the
/// opening, with the days of the new priority, and the conversation records the change.
/// </summary>
public sealed class ChangeTicketPriority(IProjectAccess projects, ITicketStore store, TicketDeadlines deadlines, TimeProvider clock)
{
    public async Task<PriorityChangeOutcome> ExecuteAsync(
        int number, TicketPriority? priority, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (priority is not { } chosen || !Enum.IsDefined(chosen))
        {
            return PriorityChangeOutcome.Invalid;
        }

        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return PriorityChangeOutcome.NotFound;
        }

        if ((TicketAccess.ActorsOf(viewer, project.Role, ticket) & TicketActors.Attendant) == 0)
        {
            return PriorityChangeOutcome.Forbidden;
        }

        if (!ticket.IsOpen)
        {
            return PriorityChangeOutcome.NotAllowedNow;
        }

        if (ticket.Priority == chosen)
        {
            return PriorityChangeOutcome.Done;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var before = ticket.Priority;
        ticket.ChangePriority(chosen, now);
        await deadlines.StartAsync(ticket, cancellationToken);
        store.AddEvent(TicketEvent.PriorityChanged(ticket.Id, viewer.UserId, before, chosen, now));
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (TicketChangedException)
        {
            return PriorityChangeOutcome.NotAllowedNow;
        }

        return PriorityChangeOutcome.Done;
    }
}

/// <summary>The deadlines of each priority, for the Administration (ADR-0025, ADR-0046).</summary>
public sealed class GetTicketDeadlines(ITicketStore store)
{
    public Task<IReadOnlyList<TicketDeadlineSetting>> ExecuteAsync(CancellationToken cancellationToken) =>
        store.DeadlinesAsync(cancellationToken);
}

/// <summary>The deadlines of one priority, as the Administrator sets them.</summary>
public sealed record TicketDeadlineChange(TicketPriority? Priority, int FirstResponseDays, int ResolutionDays);

/// <summary>
/// The Administrator changes the deadlines of the priorities (ADR-0025): all four at once, each from 1 to 365 business
/// days; they count for the tickets opened afterwards. Returns the priorities with a value out of range, or none.
/// </summary>
public sealed class ChangeTicketDeadlines(ITicketStore store)
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(IReadOnlyList<TicketDeadlineChange> changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var invalid = new List<string>();
        var priorities = Enum.GetValues<TicketPriority>();
        if (changes.Count != priorities.Length
            || changes.Select(change => change.Priority).Distinct().Count() != priorities.Length
            || changes.Any(change => change.Priority is not { } priority || !Enum.IsDefined(priority)))
        {
            return ["deadlines"];
        }

        foreach (var change in changes)
        {
            if (!TicketDeadlineSetting.IsValid(change.FirstResponseDays) || !TicketDeadlineSetting.IsValid(change.ResolutionDays))
            {
                invalid.Add(change.Priority!.Value.ToString());
            }
        }

        if (invalid.Count > 0)
        {
            return invalid;
        }

        var settings = await store.DeadlinesAsync(cancellationToken);
        foreach (var setting in settings)
        {
            var change = changes.Single(item => item.Priority == setting.Priority);
            setting.Change(change.FirstResponseDays, change.ResolutionDays);
        }

        await store.SaveChangesAsync(cancellationToken);
        return [];
    }
}

/// <summary>
/// Tells the deadlines of the open tickets that became due soon (today or on the next business day) or overdue
/// (ADR-0025, ADR-0037), each state once per deadline, to the assignee or, without one, to who serves the team. A
/// paused or met deadline tells nothing.
/// </summary>
public sealed class AlertTicketDeadlines(ITicketStore store, TicketDeadlines deadlines, TicketNotices notices)
{
    /// <summary>How many alerts were told.</summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tickets = await store.OpenWithDeadlinesAsync(cancellationToken);
        if (tickets.Count == 0)
        {
            return 0;
        }

        var day = await deadlines.TodayAsync(cancellationToken);
        var told = (await store.AlertsAsync([.. tickets.Select(ticket => ticket.Id)], cancellationToken))
            .ToDictionary(alert => (alert.TicketId, alert.Deadline));
        var alerts = new List<TicketDueAlert>();
        foreach (var ticket in tickets)
        {
            if (TicketDeadlines.FiguresOf(ticket, day) is not { } figures)
            {
                continue;
            }

            foreach (var (deadline, due) in new[] { (TicketDeadline.FirstResponse, figures.FirstResponse), (TicketDeadline.Resolution, figures.Resolution) })
            {
                var last = told.GetValueOrDefault((ticket.Id, deadline));
                if (due is not { DueOn: { } dueOn } || !TicketDeadlineAlert.IsNews(last, due.State, dueOn))
                {
                    continue;
                }

                if (last is null)
                {
                    store.AddAlert(new TicketDeadlineAlert(ticket.Id, deadline, due.State, dueOn));
                }
                else
                {
                    last.Told(due.State, dueOn);
                }

                alerts.Add(new TicketDueAlert(ticket, deadline, due.State, dueOn));
            }
        }

        if (alerts.Count == 0)
        {
            return 0;
        }

        await store.SaveChangesAsync(cancellationToken);
        await notices.DueAsync(alerts, cancellationToken);
        return alerts.Count;
    }
}
