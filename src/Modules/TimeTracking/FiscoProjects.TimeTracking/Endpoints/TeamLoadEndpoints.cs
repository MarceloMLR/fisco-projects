using System.ComponentModel;
using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.TimeTracking.Endpoints;

/// <summary>A week of the load: its Monday, its Sunday and its business days in the calendar of the installation.</summary>
public sealed record TeamLoadWeekResponse(DateOnly Start, DateOnly End, int BusinessDays);

/// <summary>A task that weighs on a week: the project, the task, its dates, its planned hours and the hours in the week.</summary>
public sealed record WeekTaskResponse(
    Guid ProjectId,
    string ProjectCode,
    string ProjectName,
    Guid ItemId,
    string Name,
    DateOnly Start,
    DateOnly End,
    decimal Planned,
    decimal Hours);

/// <summary>A period a person does not work, both days included (ADR-0141).</summary>
public sealed record LoadAbsenceResponse(DateOnly Start, DateOnly End);

/// <summary>
/// A week of a person: the load and the capacity in hours, the business days away, the state, the tasks that weigh on
/// it, the most hours first, and the absences that cross it.
/// </summary>
public sealed record PersonWeekResponse(
    decimal Load,
    decimal Capacity,
    int AbsenceDays,
    LoadState State,
    IReadOnlyList<WeekTaskResponse> Tasks,
    IReadOnlyList<LoadAbsenceResponse> Absences);

/// <summary>A person in the load: the name, the e-mail, whether any week is over the capacity and each week.</summary>
public sealed record PersonLoadResponse(Guid Id, string Name, string Email, bool Overloaded, IReadOnlyList<PersonWeekResponse> Weeks);

/// <summary>The load of the team (ADR-0142): today, the hours of a working day, the eight weeks and the people, by name.</summary>
public sealed record TeamLoadResponse(
    DateOnly Today, decimal HoursPerDay, IReadOnlyList<TeamLoadWeekResponse> Weeks, IReadOnlyList<PersonLoadResponse> People);

/// <summary>The load of the team by week, from the planned hours and the absences, only for the Administrator (ADR-0142).</summary>
public static class TeamLoadEndpoints
{
    public static RouteGroupBuilder MapTeamLoadEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        api.MapGet("/admin/team-load", GetAsync)
            .WithTags("TimeTracking")
            .RequireAuthorization(SystemPolicies.Administrator)
            .WithName("GetTeamLoad")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return api;
    }

    private static async Task<Results<Ok<TeamLoadResponse>, ValidationProblem>> GetAsync(
        GetTeamLoad get,
        CancellationToken cancellationToken,
        [Description("A day of the first week, which starts on its Monday; the current week without it.")] DateOnly? week = null)
    {
        if (await get.ExecuteAsync(week, cancellationToken) is not { } load)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["week"] = ["Choose a week from 2000 to 2099."],
            });
        }

        return TypedResults.Ok(new TeamLoadResponse(
            load.Today,
            load.HoursPerDay,
            [.. load.Weeks.Select(week => new TeamLoadWeekResponse(week.Start, week.End, week.BusinessDays))],
            [.. load.People.Select(person => new PersonLoadResponse(
                person.Id,
                person.Name,
                person.Email,
                person.Overloaded,
                [.. person.Weeks.Select(week => new PersonWeekResponse(
                    week.Load,
                    week.Capacity,
                    week.AbsenceDays,
                    week.State,
                    [.. week.Tasks.Select(task => new WeekTaskResponse(
                        task.ProjectId,
                        task.ProjectCode,
                        task.ProjectName,
                        task.ItemId,
                        task.Name,
                        task.Start,
                        task.End,
                        task.Planned,
                        task.Hours))],
                    [.. week.Absences.Select(absence => new LoadAbsenceResponse(absence.Start, absence.End))]))]))]));
    }
}
