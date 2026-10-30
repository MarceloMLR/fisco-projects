using System.ComponentModel;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.TimeTracking.Endpoints;

/// <summary>
/// The balance of the hours of a project (ADR-0128): the contracted hours (null without them), the sum of the additional
/// ones, the logged ones, the total, the balance (negative when over), the share used in whole percentage and the state.
/// </summary>
public sealed record HoursBalanceResponse(
    decimal? Contracted, decimal Additional, decimal Logged, decimal? Total, decimal? Balance, int? Consumption, HoursBalanceState State);

/// <summary>A project in the hours of the projects: the code, the name, the status, the client (null when internal) and the balance.</summary>
public sealed record ProjectHoursResponse(
    Guid Id, string Code, string Name, ProjectCardStatus Status, string? ClientName, HoursBalanceResponse Balance);

/// <summary>Additional hours: the day, the hours, the reason and who registered them, by the current name.</summary>
public sealed record AdditionalHoursResponse(Guid Id, DateOnly Day, decimal Hours, string Reason, TimeEntryPersonResponse CreatedBy);

/// <summary>
/// The hours a person logged in the project, the share of them, in whole percentage, and the current role in the
/// project; null when the person left it.
/// </summary>
public sealed record PersonHoursResponse(TimeEntryPersonResponse Person, ProjectMemberRole? Role, decimal Hours, int Share);

/// <summary>
/// The hours of a project: the project with the balance, the first day with hours logged (null when none), the
/// additional hours, the newest day first, and the hours of each person, the most first.
/// </summary>
public sealed record ProjectHoursDetailResponse(
    ProjectHoursResponse Project,
    DateOnly? FirstDay,
    IReadOnlyList<AdditionalHoursResponse> Additional,
    IReadOnlyList<PersonHoursResponse> People);

public sealed record AdditionalHoursCreatedResponse(Guid Id);

/// <summary>
/// The contracted and additional hours of the projects and their balance, only for the Administrator (ADR-0046,
/// ADR-0128). Only hours, never values in money.
/// </summary>
public static class ProjectHoursEndpoints
{
    public static RouteGroupBuilder MapProjectHoursEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var hours = api.MapGroup("/admin/project-hours")
            .WithTags("TimeTracking")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        hours.MapGet("/", ListAsync).WithName("ListProjectHours");
        hours.MapGet("/{projectId:guid}", DetailAsync).WithName("GetProjectHours");
        hours.MapPut("/{projectId:guid}/contracted", ChangeContractedAsync).WithName("ChangeContractedHours")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        hours.MapPost("/{projectId:guid}/additional", AddAdditionalAsync).WithName("AddAdditionalHours")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        hours.MapPut("/{projectId:guid}/additional/{id:guid}", ChangeAdditionalAsync).WithName("ChangeAdditionalHours")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        hours.MapDelete("/{projectId:guid}/additional/{id:guid}", DeleteAdditionalAsync).WithName("DeleteAdditionalHours");
        return api;
    }

    private static async Task<Ok<IReadOnlyList<ProjectHoursResponse>>> ListAsync(
        ListProjectHours list,
        CancellationToken cancellationToken,
        [Description("Open (the default: planning, in progress or paused), Closed (completed or cancelled) or All.")] ProjectHoursView view = ProjectHoursView.Open,
        [Description("Part of the code or of the name, without telling case or accents apart.")] string? search = null)
    {
        var rows = await list.ExecuteAsync(view, search, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<ProjectHoursResponse>>([.. rows.Select(row => ResponseOf(row.Project, row.Balance))]);
    }

    private static async Task<Results<Ok<ProjectHoursDetailResponse>, NotFound>> DetailAsync(
        Guid projectId, GetProjectHours get, CancellationToken cancellationToken)
    {
        if (await get.ExecuteAsync(projectId, cancellationToken) is not { } detail)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ProjectHoursDetailResponse(
            ResponseOf(detail.Project, detail.Balance),
            detail.FirstDay,
            [.. detail.Additional.Select(row => new AdditionalHoursResponse(
                row.Hours.Id,
                row.Hours.Day,
                row.Hours.Hours,
                row.Hours.Reason,
                new TimeEntryPersonResponse(row.Hours.CreatedBy, row.CreatedByName)))],
            [.. detail.People.Select(person => new PersonHoursResponse(
                new TimeEntryPersonResponse(person.UserId, person.Name), person.Role, person.Hours, person.Share))]));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> ChangeContractedAsync(
        Guid projectId, ContractedHoursRequest request, ChangeContractedHours change, CancellationToken cancellationToken)
    {
        var result = await change.ExecuteAsync(projectId, request, cancellationToken);
        return result.Outcome switch
        {
            ProjectHoursOutcome.Done => TypedResults.NoContent(),
            ProjectHoursOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["hours"] = [$"The contracted hours are more than zero and up to {ContractedHours.Most:0}, in quarters of an hour."],
            }),
        };
    }

    private static async Task<Results<Created<AdditionalHoursCreatedResponse>, ValidationProblem, NotFound>> AddAdditionalAsync(
        Guid projectId, AdditionalHoursRequest request, HttpContext http, AddAdditionalHours add, CancellationToken cancellationToken)
    {
        var administratorId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The additional hours are registered in a session.");
        var result = await add.ExecuteAsync(projectId, request, administratorId, cancellationToken);
        return result.Outcome switch
        {
            ProjectHoursOutcome.Done => TypedResults.Created((string?)null, new AdditionalHoursCreatedResponse(result.Id!.Value)),
            ProjectHoursOutcome.NotFound => TypedResults.NotFound(),
            _ => InvalidOf(result),
        };
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> ChangeAdditionalAsync(
        Guid projectId, Guid id, AdditionalHoursRequest request, ChangeAdditionalHours change, CancellationToken cancellationToken)
    {
        var result = await change.ExecuteAsync(projectId, id, request, cancellationToken);
        return result.Outcome switch
        {
            ProjectHoursOutcome.Done => TypedResults.NoContent(),
            ProjectHoursOutcome.NotFound => TypedResults.NotFound(),
            _ => InvalidOf(result),
        };
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAdditionalAsync(
        Guid projectId, Guid id, DeleteAdditionalHours delete, CancellationToken cancellationToken)
    {
        var result = await delete.ExecuteAsync(projectId, id, cancellationToken);
        return result.Outcome == ProjectHoursOutcome.Done ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static ProjectHoursResponse ResponseOf(ProjectCard project, HoursBalance balance) => new(
        project.Id,
        project.Code,
        project.Name,
        project.Status,
        project.ClientName,
        new HoursBalanceResponse(
            balance.Contracted, balance.Additional, balance.Logged, balance.Total, balance.Balance, balance.Consumption, balance.State));

    private static ValidationProblem InvalidOf(ProjectHoursResult result) =>
        TypedResults.ValidationProblem((result.Invalid ?? []).Distinct().ToDictionary(
            field => field switch
            {
                AdditionalHoursField.Day => "day",
                AdditionalHoursField.Hours => "hours",
                _ => "reason",
            },
            field => new[]
            {
                field switch
                {
                    AdditionalHoursField.Day => "The day is needed and is not after today.",
                    AdditionalHoursField.Hours => $"The hours go from 0.25 to {AdditionalHours.Most:0}, in quarters of an hour.",
                    _ => $"The reason is needed and has up to {AdditionalHours.ReasonMaxLength} characters.",
                },
            }));
}
