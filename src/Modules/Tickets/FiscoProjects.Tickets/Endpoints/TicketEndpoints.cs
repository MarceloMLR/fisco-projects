using System.ComponentModel;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Tickets.Endpoints;

/// <summary>A person of a ticket.</summary>
public sealed record TicketPersonResponse(Guid Id, string Name);

/// <summary>The project of a ticket: the screens show the code beside the number (ADR-0029).</summary>
public sealed record TicketProjectResponse(Guid Id, string Code, string Name);

/// <summary>A ticket of a list; without an assignee until someone of the team answers it (ADR-0068).</summary>
public sealed record TicketListItemResponse(
    Guid Id,
    int Number,
    string Title,
    TicketProjectResponse Project,
    TicketTeam Team,
    TicketPriority Priority,
    TicketStatus Status,
    TicketPersonResponse Requester,
    TicketPersonResponse? Assignee,
    DateTime OpenedAt,
    DateTime UpdatedAt);

public sealed record TicketPageResponse(IReadOnlyList<TicketListItemResponse> Items, int Total);

/// <summary>
/// How many tickets each list has: all of them, any status, and the open ones, the open ones without an assignee,
/// without an assignee in the teams of the person, in charge of the person and opened by the person.
/// </summary>
public sealed record TicketCountsResponse(int All, int Open, int Unassigned, int UnassignedInMyTeams, int AssignedToMe, int OpenedByMe);

/// <summary>
/// A project the person sees, for the filters and the opening: whether it is closed, whether the person may open
/// tickets in it and the teams with someone to serve them (ADR-0068).
/// </summary>
public sealed record TicketProjectChoiceResponse(
    Guid Id, string Code, string Name, bool IsClosed, bool CanOpen, IReadOnlyList<TicketTeam> Teams);

/// <summary>A new ticket: the project, the team, the priority, the title and the description, without an assignee (ADR-0068).</summary>
public sealed record NewTicketRequest(Guid? ProjectId, TicketTeam? Team, TicketPriority? Priority, string? Title, string? Description);

public sealed record TicketCreatedResponse(Guid Id, int Number);

/// <summary>A ticket as its page shows it, with the people: the requester, the assignee, the managers and the tech leads of the project.</summary>
public sealed record TicketResponse(
    Guid Id,
    int Number,
    string Title,
    string Description,
    TicketProjectResponse Project,
    TicketTeam Team,
    TicketPriority Priority,
    TicketStatus Status,
    TicketPersonResponse Requester,
    TicketPersonResponse? Assignee,
    IReadOnlyList<TicketPersonResponse> Managers,
    IReadOnlyList<TicketPersonResponse> TechLeads,
    DateTime OpenedAt,
    DateTime UpdatedAt);

/// <summary>Problem types of the tickets, so the screen can tell the reason.</summary>
public static class TicketProblems
{
    /// <summary>The project is completed or cancelled and takes no tickets until it is reopened (ADR-0022); status 409.</summary>
    public const string ProjectClosed = "urn:fiscoprojects:problem:project-closed";
}

/// <summary>
/// The tickets (ADR-0022, ADR-0029, ADR-0044, ADR-0068): everyone in a project sees its tickets and the Administrator
/// sees all; everyone in the project but the viewer opens them.
/// </summary>
public static class TicketEndpoints
{
    public static RouteGroupBuilder MapTicketsEndpoints(this RouteGroupBuilder api)
    {
        var tickets = api.MapGroup("/tickets")
            .WithTags("Tickets")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        tickets.MapGet("/", ListAsync).WithName("ListTickets");
        tickets.MapGet("/counts", CountAsync).WithName("CountTickets");
        tickets.MapGet("/choices", ChoicesAsync).WithName("ListTicketChoices");
        tickets.MapPost("/", OpenAsync).WithName("OpenTicket")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapGet("/{number:int}", GetAsync).WithName("GetTicket");
        return api;
    }

    private static async Task<Results<Ok<TicketPageResponse>, ValidationProblem>> ListAsync(
        HttpContext http,
        IUserAccess access,
        ListTickets listTickets,
        CancellationToken cancellationToken,
        [Description("All (the default), AssignedToMe, UnassignedInMyTeams, Unassigned or OpenedByMe.")] TicketView view = TicketView.All,
        [Description("Only the tickets of this project.")] Guid? projectId = null,
        [Description("Only the tickets of this team.")] TicketTeam? team = null,
        [Description("Open (the default: not resolved nor closed), Finished (resolved and closed) or All.")] TicketSituation situation = TicketSituation.Open,
        [Description("The number, with or without #, or part of the title or of the description, without telling case or accents apart.")] string? search = null,
        [Description("Priority (the default, the oldest first in each) or Number.")] TicketSort sort = TicketSort.Priority,
        [Description("From the highest priority or number to the lowest (the default).")] bool descending = true,
        int page = 1,
        int pageSize = 25)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Must be 1 or more."];
        }

        if (pageSize is < 1 or > TicketQuery.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"Must be between 1 and {TicketQuery.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await listTickets.ExecuteAsync(
            new TicketQuery(view, projectId, team, situation, search, sort, descending, page, pageSize), viewer, cancellationToken);
        return TypedResults.Ok(new TicketPageResponse([.. result.Items.Select(ItemOf)], result.Total));
    }

    private static async Task<Results<Ok<TicketCountsResponse>, NotFound>> CountAsync(
        HttpContext http,
        IUserAccess access,
        CountTickets countTickets,
        CancellationToken cancellationToken,
        [Description("Only the tickets of this project.")] Guid? projectId = null)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await countTickets.ExecuteAsync(projectId, viewer, cancellationToken) is { } counts
            ? TypedResults.Ok(new TicketCountsResponse(
                counts.All, counts.Open, counts.Unassigned, counts.UnassignedInMyTeams, counts.AssignedToMe, counts.OpenedByMe))
            : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<TicketProjectChoiceResponse>>> ChoicesAsync(
        HttpContext http, IUserAccess access, ListTicketChoices listChoices, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var choices = await listChoices.ExecuteAsync(viewer, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TicketProjectChoiceResponse>>([.. choices.Select(choice =>
            new TicketProjectChoiceResponse(choice.Id, choice.Code, choice.Name, choice.IsClosed, choice.CanOpen, choice.Teams))]);
    }

    private static async Task<Results<Created<TicketCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> OpenAsync(
        HttpContext http, NewTicketRequest request, IUserAccess access, OpenTicket openTicket, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await openTicket.ExecuteAsync(
            new NewTicket(request.ProjectId, request.Team, request.Priority, request.Title, request.Description), viewer, cancellationToken);
        return result.Outcome switch
        {
            OpenTicketOutcome.Opened => TypedResults.Created(
                (string?)null, new TicketCreatedResponse(result.Id!.Value, result.Number!.Value)),
            OpenTicketOutcome.NotFound => TypedResults.NotFound(),
            OpenTicketOutcome.Forbidden => TypedResults.Forbid(),
            OpenTicketOutcome.Closed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project takes no tickets until it is reopened.",
                type: TicketProblems.ProjectClosed),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<Ok<TicketResponse>, NotFound>> GetAsync(
        int number, HttpContext http, IUserAccess access, GetTicket getTicket, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await getTicket.ExecuteAsync(number, viewer, cancellationToken) is not { } details)
        {
            return TypedResults.NotFound();
        }

        var ticket = details.Ticket;
        return TypedResults.Ok(new TicketResponse(
            ticket.Id,
            ticket.Number,
            ticket.Title,
            ticket.Description,
            ProjectOf(details.Project),
            ticket.Team,
            ticket.Priority,
            ticket.Status,
            PersonOf(details.Requester),
            details.Assignee is { } assignee ? PersonOf(assignee) : null,
            [.. details.Managers.Select(PersonOf)],
            [.. details.TechLeads.Select(PersonOf)],
            ticket.OpenedAt,
            ticket.UpdatedAt));
    }

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    private static async Task<TicketViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The tickets are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new TicketViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }

    private static TicketListItemResponse ItemOf(TicketListItem item) =>
        new(
            item.Ticket.Id,
            item.Ticket.Number,
            item.Ticket.Title,
            ProjectOf(item.Project),
            item.Ticket.Team,
            item.Ticket.Priority,
            item.Ticket.Status,
            PersonOf(item.Requester),
            item.Assignee is { } assignee ? PersonOf(assignee) : null,
            item.Ticket.OpenedAt,
            item.Ticket.UpdatedAt);

    private static TicketProjectResponse ProjectOf(TicketProject project) => new(project.Id, project.Code, project.Name);

    private static TicketPersonResponse PersonOf(TicketPerson person) => new(person.Id, person.Name);

    private static ValidationProblem Invalid(IReadOnlyList<TicketField> fields) =>
        TypedResults.ValidationProblem(fields.Distinct().ToDictionary(
            field => field switch
            {
                TicketField.Project => "projectId",
                TicketField.Team => "team",
                TicketField.Priority => "priority",
                TicketField.Title => "title",
                _ => "description",
            },
            field => new[]
            {
                field switch
                {
                    TicketField.Project => "The ticket needs a project.",
                    TicketField.Team => "The team is Functional or Development, with someone in the project to serve it.",
                    TicketField.Priority => "The priority is Low, Medium, High or Critical.",
                    TicketField.Title => "The title is needed and has up to 200 characters.",
                    _ => "The description is needed and has up to 10,000 characters.",
                },
            }));
}
