using FiscoProjects.MyWork.Application;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.MyWork.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.MyWork.Endpoints;

/// <summary>A person an item shows, by the current name.</summary>
public sealed record MyWorkPersonResponse(Guid Id, string Name);

/// <summary>The project of an item: the id and the code.</summary>
public sealed record MyWorkProjectResponse(Guid Id, string Code);

/// <summary>A ticket of the queue: the number, the status, the team, who asked, who is in charge and which deadline counts.</summary>
public sealed record MyWorkTicketResponse(
    int Number,
    MyWorkTicketStatus Status,
    MyWorkTeam Team,
    MyWorkPersonResponse Requester,
    MyWorkPersonResponse? Assignee,
    int Interactions,
    MyWorkDeadline? Deadline);

/// <summary>A task of the queue: the phase and the progress.</summary>
public sealed record MyWorkTaskResponse(string Phase, int Progress);

/// <summary>
/// A request of acceptance of the queue: the names of the signers or, in a request asked before, the person of the
/// project; who asked, when and the milestone (ADR-0129).
/// </summary>
public sealed record MyWorkAcceptanceResponse(
    MyWorkPersonResponse? Recipient,
    IReadOnlyList<string> Signers,
    MyWorkPersonResponse RequestedBy,
    DateTime RequestedAt,
    string? Milestone);

/// <summary>
/// An item of the queue: the kind, the project, the title, the action of the line, the day it is due (none while it
/// waits for the person), the business days of delay, since when it waits for the person and what the kind tells.
/// </summary>
public sealed record MyWorkItemResponse(
    MyWorkKind Kind,
    Guid Id,
    MyWorkProjectResponse Project,
    string Title,
    MyWorkAction Action,
    DateOnly? DueOn,
    int? DaysLate,
    DateTime? WaitingSince,
    MyWorkTicketResponse? Ticket,
    MyWorkTaskResponse? Task,
    MyWorkAcceptanceResponse? Acceptance);

/// <summary>A group of a view, with its items in order.</summary>
public sealed record MyWorkGroupResponse(MyWorkGroup Group, IReadOnlyList<MyWorkItemResponse> Items);

/// <summary>A view of the queue: how many items it has and the groups with items, in the order of the screen.</summary>
public sealed record MyWorkViewResponse(MyWorkView View, int Count, IReadOnlyList<MyWorkGroupResponse> Groups);

/// <summary>Meu trabalho of the person: today and the next business day of the installation, and the three views.</summary>
public sealed record MyWorkResponse(DateOnly Today, DateOnly NextBusinessDay, IReadOnlyList<MyWorkViewResponse> Views);

/// <summary>Meu trabalho of the person in the session (ADR-0070, ADR-0096).</summary>
public static class MyWorkEndpoints
{
    public static RouteGroupBuilder MapMyWorkEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/my-work", GetAsync)
            .WithTags("MyWork")
            .WithName("GetMyWork")
            .WithDescription(
                "The tickets, tasks and requests of acceptance of the person by urgency: late, waiting for the person, "
                + "due up to the next business day and in the next 7 days, in the views for me, opened by me and followed.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return api;
    }

    private static async Task<Ok<MyWorkResponse>> GetAsync(
        HttpContext http, IUserAccess access, GetMyWork myWork, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("Meu trabalho is read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        var result = await myWork.ExecuteAsync(
            userId, person is { IsActive: true, Role: SystemRole.Administrator }, cancellationToken);
        return TypedResults.Ok(new MyWorkResponse(
            result.Window.Today,
            result.Window.NextBusinessDay,
            [.. result.Views.Select(view => new MyWorkViewResponse(
                view.View,
                view.Count,
                [.. view.Groups.Select(group => new MyWorkGroupResponse(
                    group.Group, [.. group.Entries.Select(entry => ItemOf(entry, result))]))]))]));
    }

    private static MyWorkItemResponse ItemOf(MyWorkEntry entry, MyWorkResult result)
    {
        var item = entry.Item;
        MyWorkPersonResponse Person(Guid id) => new(id, result.NameOf(id));
        return new MyWorkItemResponse(
            item.Kind,
            item.Id,
            new MyWorkProjectResponse(item.ProjectId, item.ProjectCode),
            item.Title,
            entry.Action,
            item.DueOn,
            item.DaysLate,
            item.WaitsForYou ? item.WaitingSince : null,
            item.Ticket is { } ticket
                ? new MyWorkTicketResponse(
                    ticket.Number,
                    ticket.Status,
                    ticket.Team,
                    Person(ticket.RequesterId),
                    ticket.AssigneeId is { } assignee ? Person(assignee) : null,
                    ticket.Interactions,
                    ticket.Deadline)
                : null,
            item.Task is { } task ? new MyWorkTaskResponse(task.Phase, task.Progress) : null,
            item.Acceptance is { } acceptance
                ? new MyWorkAcceptanceResponse(
                    acceptance.RecipientId is { } recipient ? Person(recipient) : null,
                    acceptance.Signers,
                    Person(acceptance.RequestedById),
                    acceptance.RequestedAt,
                    acceptance.Milestone)
                : null);
    }
}
