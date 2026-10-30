using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>A person of the overview, by the current name.</summary>
public sealed record OverviewPersonResponse(Guid Id, string Name);

/// <summary>The progress of the project and how much should be done by today (null with no task to plan), and by what.</summary>
public sealed record OverviewProgressResponse(int Actual, int? Planned, bool ByBaseline);

/// <summary>The end of the schedule and, with a baseline, its end and the business days of deviation.</summary>
public sealed record OverviewDeadlineResponse(DateOnly End, DateOnly? BaselineEnd, int? Deviation);

/// <summary>The tickets of the project: the open ones of all, the late ones and the ones waiting for the requester.</summary>
public sealed record OverviewTicketsResponse(int Open, int Total, int Overdue, int WaitingForRequester);

/// <summary>The next milestone not reached: its day, the business days left, or the business days it is late.</summary>
public sealed record OverviewMilestoneResponse(Guid Id, string Name, DateOnly Day, int BusinessDaysLeft, int? DaysLate);

/// <summary>A phase or a milestone of the smaller timeline (ADR-0071).</summary>
public sealed record OverviewRowResponse(
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
public sealed record OverviewTimelineResponse(
    DateOnly Start, DateOnly End, IReadOnlyList<DateOnly> NonWorkingDays, IReadOnlyList<OverviewRowResponse> Rows);

/// <summary>A late task: its owner, progress, end, business days of delay and what depends on it.</summary>
public sealed record OverviewLateTaskResponse(
    Guid Id, string Name, OverviewPersonResponse? Owner, int Progress, DateOnly End, int DaysLate, IReadOnlyList<string> Blocks);

/// <summary>A ticket that needs attention: late, due up to the next business day or without an assignee.</summary>
public sealed record OverviewTicketResponse(
    int Number,
    string Title,
    ProjectTicketTeam Team,
    OverviewPersonResponse? Assignee,
    int Interactions,
    ProjectTicketDeadline? Deadline,
    ProjectTicketDueState? DueState,
    DateOnly? DueOn,
    int? DaysLate);

/// <summary>
/// A pending request of acceptance, for who manages the terms: the names of the signers or, in a request asked before,
/// the person of the project (ADR-0129).
/// </summary>
public sealed record OverviewAcceptanceResponse(
    Guid Id,
    string Title,
    OverviewPersonResponse? Recipient,
    IReadOnlyList<string> Signers,
    DateTime RequestedAt,
    string? Milestone,
    DateOnly? MilestoneDay);

/// <summary>A ticket at a vendor without an update for the business days of the setting (ADR-0028).</summary>
public sealed record OverviewVendorTicketResponse(int TicketNumber, string Vendor, string Code, int StaleDays);

/// <summary>What needs attention (ADR-0073), in the order of the list.</summary>
public sealed record OverviewAttentionResponse(
    IReadOnlyList<OverviewLateTaskResponse> Tasks,
    IReadOnlyList<OverviewTicketResponse> Tickets,
    IReadOnlyList<OverviewAcceptanceResponse> Acceptances,
    IReadOnlyList<OverviewVendorTicketResponse> Vendors);

/// <summary>A team of the project with its people and, for the teams that serve tickets, the open tickets.</summary>
public sealed record OverviewTeamResponse(OverviewTeam Team, IReadOnlyList<ProjectPersonResponse> People, int? OpenTickets);

/// <summary>
/// The overview of a project (ADR-0073): today, the progress, the final deadline and the next milestone (null without a
/// schedule), the tickets, the timeline, what needs attention and the team.
/// </summary>
public sealed record ProjectOverviewResponse(
    DateOnly Today,
    OverviewProgressResponse? Progress,
    OverviewDeadlineResponse? Deadline,
    OverviewTicketsResponse Tickets,
    OverviewMilestoneResponse? NextMilestone,
    OverviewTimelineResponse? Timeline,
    OverviewAttentionResponse Attention,
    IReadOnlyList<OverviewTeamResponse> Teams);

/// <summary>The overview of a project, which answers "está no prazo?" (ADR-0073).</summary>
internal static class OverviewEndpoints
{
    public static void MapOverviewEndpoints(this RouteGroupBuilder projects) =>
        projects.MapGet("/{id:guid}/overview", GetAsync).WithName("GetProjectOverview")
            .WithDescription("The numbers, the timeline, what needs attention and the team of the project (ADR-0073).")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<ProjectOverviewResponse>, NotFound>> GetAsync(
        Guid id, HttpContext http, IUserAccess access, GetProjectOverview overview, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await overview.ExecuteAsync(id, viewer, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        OverviewPersonResponse Person(Guid personId) => new(personId, result.NameOf(personId));
        var tickets = result.Tickets;
        return TypedResults.Ok(new ProjectOverviewResponse(
            result.Today,
            result.Progress is { } progress ? new OverviewProgressResponse(progress.Actual, progress.Planned, progress.ByBaseline) : null,
            result.Deadline is { } deadline ? new OverviewDeadlineResponse(deadline.End, deadline.BaselineEnd, deadline.Deviation) : null,
            new OverviewTicketsResponse(tickets.Open, tickets.Total, tickets.Overdue, tickets.WaitingForRequester),
            result.NextMilestone is { } milestone
                ? new OverviewMilestoneResponse(milestone.Id, milestone.Name, milestone.Day, milestone.BusinessDaysLeft, milestone.DaysLate)
                : null,
            result.Timeline is { } timeline
                ? new OverviewTimelineResponse(
                    timeline.Start,
                    timeline.End,
                    timeline.NonWorkingDays,
                    [.. timeline.Rows.Select(row => new OverviewRowResponse(
                        row.Kind, row.Id, row.Name, row.Start, row.End, row.Progress, row.Late, row.Critical, row.Reached,
                        row.BaselineStart, row.BaselineEnd))])
                : null,
            new OverviewAttentionResponse(
                [.. result.LateTasks.Select(task => new OverviewLateTaskResponse(
                    task.Id,
                    task.Name,
                    task.OwnerId is { } owner ? Person(owner) : null,
                    task.Progress,
                    task.End,
                    task.DaysLate,
                    task.Blocks))],
                [.. tickets.Attention.Select(ticket => new OverviewTicketResponse(
                    ticket.Number,
                    ticket.Title,
                    ticket.Team,
                    ticket.AssigneeId is { } assignee ? Person(assignee) : null,
                    ticket.Interactions,
                    ticket.Deadline,
                    ticket.DueState,
                    ticket.DueOn,
                    ticket.DaysLate))],
                [.. result.Acceptances.Select(term => new OverviewAcceptanceResponse(
                    term.Id,
                    term.Title,
                    term.RecipientId is { } recipient ? Person(recipient) : null,
                    term.Signers,
                    term.RequestedAt,
                    term.Milestone,
                    term.MilestoneDay))],
                [.. tickets.Vendors.Select(vendor => new OverviewVendorTicketResponse(
                    vendor.TicketNumber, vendor.Vendor, vendor.Code, vendor.StaleDays))]),
            [.. result.Teams.Select(team => new OverviewTeamResponse(
                team.Team,
                [.. team.People.Select(person => new ProjectPersonResponse(person.Id, person.Name, person.Role))],
                team.OpenTickets))]));
    }
}
