using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.TimeTracking.Endpoints;

/// <summary>A project as a time entry shows it: its code and name.</summary>
public sealed record TimeEntryProjectResponse(Guid Id, string Code, string Name);

/// <summary>The task of a time entry; the name is null when the task left the schedule.</summary>
public sealed record TimeEntryTaskResponse(Guid Id, string? Name);

/// <summary>The ticket of a time entry, by its number and current title.</summary>
public sealed record TimeEntryTicketResponse(Guid Id, int Number, string Title);

/// <summary>Who logged a time entry, by the current name.</summary>
public sealed record TimeEntryPersonResponse(Guid Id, string Name);

/// <summary>
/// A time entry: the day, the hours, what was done, who logged it, the project, the task or the ticket, and whether the
/// person of the session may change it.
/// </summary>
public sealed record TimeEntryResponse(
    Guid Id,
    DateOnly Day,
    decimal Hours,
    string Description,
    TimeEntryPersonResponse Person,
    TimeEntryProjectResponse Project,
    TimeEntryTaskResponse? Task,
    TimeEntryTicketResponse? Ticket,
    bool CanChange);

/// <summary>The entries of the period, the newest day first, and their total in hours.</summary>
public sealed record TimeEntryListResponse(IReadOnlyList<TimeEntryResponse> Items, decimal Total);

/// <summary>A task of the schedule for a time entry.</summary>
public sealed record TimeEntryTaskOptionResponse(Guid Id, string Name);

/// <summary>A ticket of the project for a time entry.</summary>
public sealed record TimeEntryTicketOptionResponse(Guid Id, int Number, string Title, bool IsClosed);

/// <summary>The tasks, in the order of the schedule, and the tickets, first the ones not closed, of a project.</summary>
public sealed record TimeEntryWorkResponse(IReadOnlyList<TimeEntryTaskOptionResponse> Tasks, IReadOnlyList<TimeEntryTicketOptionResponse> Tickets);

public sealed record TimeEntryCreatedResponse(Guid Id);

/// <summary>Problem types of the hours, so the screen can tell the reason.</summary>
public static class TimeEntryProblems
{
    /// <summary>The project is completed or cancelled and is read-only (ADR-0016); status 409.</summary>
    public const string ProjectClosed = "urn:fiscoprojects:problem:project-closed";
}

/// <summary>
/// The hours each person works on the projects (ADR-0128): the person logs, sees and corrects only the own hours; the
/// Administrator, the hours of anyone. Only hours, never values in money.
/// </summary>
public static class TimeEntryEndpoints
{
    public static RouteGroupBuilder MapTimeTrackingEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var hours = api.MapGroup("/time-entries")
            .WithTags("TimeTracking")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        hours.MapGet("/", ListAsync).WithName("ListTimeEntries")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        hours.MapGet("/projects", ProjectsAsync).WithName("ListTimeEntryProjects");
        hours.MapGet("/projects/{projectId:guid}/work", WorkAsync).WithName("ListTimeEntryWork");
        hours.MapPost("/", AddAsync).WithName("AddTimeEntry")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        hours.MapPut("/{entryId:guid}", ChangeAsync).WithName("ChangeTimeEntry")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        hours.MapDelete("/{entryId:guid}", DeleteAsync).WithName("DeleteTimeEntry")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return api;
    }

    private static async Task<Results<Ok<TimeEntryListResponse>, ValidationProblem, ForbidHttpResult, NotFound>> ListAsync(
        DateOnly from,
        DateOnly to,
        Guid? personId,
        Guid? projectId,
        HttpContext http,
        IUserAccess access,
        ListTimeEntries list,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var (entries, outcome) = await list.ExecuteAsync(from, to, personId, projectId, viewer, cancellationToken);
        return outcome switch
        {
            TimeEntryOutcome.Forbidden => TypedResults.Forbid(),
            TimeEntryOutcome.NotFound => TypedResults.NotFound(),
            TimeEntryOutcome.Invalid => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = [$"The period goes from the first day to the last, with up to {ListTimeEntries.MaxDays} days."],
            }),
            _ => TypedResults.Ok(new TimeEntryListResponse([.. entries!.Items.Select(ResponseOf)], entries.Total)),
        };
    }

    private static async Task<Ok<IReadOnlyList<TimeEntryProjectResponse>>> ProjectsAsync(
        HttpContext http, IUserAccess access, ListTimeEntryProjects projects, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var found = await projects.ExecuteAsync(viewer, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TimeEntryProjectResponse>>(
            [.. found.Select(project => new TimeEntryProjectResponse(project.Id, project.Code, project.Name))]);
    }

    private static async Task<Results<Ok<TimeEntryWorkResponse>, NotFound>> WorkAsync(
        Guid projectId, HttpContext http, IUserAccess access, ListTimeEntryWork work, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await work.ExecuteAsync(projectId, viewer, cancellationToken) is not { } found)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new TimeEntryWorkResponse(
            [.. found.Tasks.Select(task => new TimeEntryTaskOptionResponse(task.Id, task.Name))],
            [.. found.Tickets.Select(ticket => new TimeEntryTicketOptionResponse(ticket.Id, ticket.Number, ticket.Title, ticket.IsClosed))]));
    }

    private static async Task<Results<Created<TimeEntryCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AddAsync(
        TimeEntryRequest request, HttpContext http, IUserAccess access, AddTimeEntry add, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await add.ExecuteAsync(request, viewer, cancellationToken);
        return result.Outcome == TimeEntryOutcome.Done
            ? TypedResults.Created((string?)null, new TimeEntryCreatedResponse(result.Id!.Value))
            : Refusal<Created<TimeEntryCreatedResponse>>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeAsync(
        Guid entryId, TimeEntryRequest request, HttpContext http, IUserAccess access, ChangeTimeEntry change, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await change.ExecuteAsync(entryId, request, viewer, cancellationToken);
        return result.Outcome == TimeEntryOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> DeleteAsync(
        Guid entryId, HttpContext http, IUserAccess access, DeleteTimeEntry delete, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await delete.ExecuteAsync(entryId, viewer, cancellationToken);
        return result.Outcome == TimeEntryOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static TimeEntryResponse ResponseOf(TimeEntryRow row) => new(
        row.Entry.Id,
        row.Entry.Day,
        row.Entry.Hours,
        row.Entry.Description,
        new TimeEntryPersonResponse(row.Person.Id, row.Person.Name),
        new TimeEntryProjectResponse(row.Project.Id, row.Project.Code, row.Project.Name),
        row.Task is { } task ? new TimeEntryTaskResponse(task.Id, task.Name) : null,
        row.Ticket is { } ticket ? new TimeEntryTicketResponse(ticket.Id, ticket.Number, ticket.Title) : null,
        row.CanChange);

    private static Results<T, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Refusal<T>(TimeEntryResult result)
        where T : IResult =>
        result.Outcome switch
        {
            TimeEntryOutcome.NotFound => TypedResults.NotFound(),
            TimeEntryOutcome.Forbidden => TypedResults.Forbid(),
            TimeEntryOutcome.ProjectClosed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: TimeEntryProblems.ProjectClosed),
            _ => TypedResults.ValidationProblem((result.Invalid ?? []).Distinct().ToDictionary(FieldName, field => new[] { MessageOf(field) })),
        };

    private static string FieldName(TimeEntryField field) => field switch
    {
        TimeEntryField.Day => "day",
        TimeEntryField.Hours => "hours",
        TimeEntryField.Description => "description",
        TimeEntryField.Project => "projectId",
        TimeEntryField.ScheduleItem => "scheduleItemId",
        _ => "ticketId",
    };

    private static string MessageOf(TimeEntryField field) => field switch
    {
        TimeEntryField.Day => "The day is needed and is not after today.",
        TimeEntryField.Hours => "The hours go from 0.25 to 24, in quarters of an hour, and the hours of a person in a day are up to 24.",
        TimeEntryField.Description => $"The description is needed and has up to {TimeEntry.DescriptionMaxLength} characters.",
        TimeEntryField.Project => "The project is needed.",
        TimeEntryField.ScheduleItem => "The task is a task of the schedule of the project.",
        _ => "The ticket is a ticket of the project, and an entry has a task or a ticket, not both.",
    };

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    private static async Task<TimeEntryViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The hours are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new TimeEntryViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }
}
