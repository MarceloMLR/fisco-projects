using System.Globalization;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.TimeTracking.Application;

/// <summary>A week of the load: its Monday, its Sunday and its business days in the calendar of the installation.</summary>
public sealed record TeamLoadWeek(DateOnly Start, DateOnly End, int BusinessDays);

/// <summary>A task that weighs on a week of a person: the project, the task, its dates, its planned hours and the hours in the week.</summary>
public sealed record WeekTask(
    Guid ProjectId,
    string ProjectCode,
    string ProjectName,
    Guid ItemId,
    string Name,
    DateOnly Start,
    DateOnly End,
    decimal Planned,
    decimal Hours);

/// <summary>
/// A week of a person: the load and the capacity, in hours rounded to hundredths, the business days away, the state,
/// the tasks that weigh on it, the most hours first, and the absences that cross it.
/// </summary>
public sealed record PersonWeek(
    decimal Load,
    decimal Capacity,
    int AbsenceDays,
    LoadState State,
    IReadOnlyList<WeekTask> Tasks,
    IReadOnlyList<AbsentPeriod> Absences);

/// <summary>A person in the load, with each week and whether any of them is over the capacity.</summary>
public sealed record PersonLoad(Guid Id, string Name, string Email, bool Overloaded, IReadOnlyList<PersonWeek> Weeks);

/// <summary>The load of the team (ADR-0142): today, the hours of a working day, the weeks and the people, by name.</summary>
public sealed record TeamLoadView(DateOnly Today, decimal HoursPerDay, IReadOnlyList<TeamLoadWeek> Weeks, IReadOnlyList<PersonLoad> People);

/// <summary>
/// The load of each person in eight weeks from the week of the day asked (the current one, without it), across the
/// open projects, by the rule of the <see cref="TeamLoad"/> (ADR-0142): the active people of the open projects and
/// whoever has load, by name. Only the Administrator reaches it, by the policy of the route. Null when the week is out
/// of 2000 to 2099.
/// </summary>
public sealed class GetTeamLoad(
    ITeamWork work,
    IProjectHoursStore store,
    IWorkCalendar calendar,
    IInstallationSettings installation,
    IUserDirectory users,
    TimeEntryClock clock)
{
    private static readonly CompareInfo Names = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;

    public async Task<TeamLoadView?> ExecuteAsync(DateOnly? week, CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        var first = TeamLoad.WeekOf(week ?? today);
        var last = first.AddDays((7 * TeamLoad.Weeks) - 1);
        if (first.Year < 2000 || last.Year > 2099)
        {
            return null;
        }

        var days = await calendar.GetAsync(cancellationToken);
        var hoursPerDay = (await installation.GetAsync(cancellationToken)).WorkHoursPerDay;
        var tasks = await work.TasksAsync(first, last, cancellationToken);
        var planned = await store.PlannedOfItemsAsync([.. tasks.Select(task => task.ItemId)], cancellationToken);
        var weighing = tasks.Where(task => planned.ContainsKey(task.ItemId)).ToList();
        var absences = (await work.AbsencesAsync(first, last, cancellationToken)).ToLookup(absence => absence.UserId);
        var ids = (await work.PeopleAsync(cancellationToken)).Concat(weighing.Select(task => task.OwnerId)).ToHashSet();
        var people = (await users.PeopleAsync(ids, cancellationToken)).Where(person => person.IsActive);

        var weeks = Enumerable.Range(0, TeamLoad.Weeks)
            .Select(index => first.AddDays(7 * index))
            .Select(start => new TeamLoadWeek(start, start.AddDays(6), days.CountBusinessDays(start, start.AddDays(6))))
            .ToList();
        var byOwner = weighing.ToLookup(task => task.OwnerId);
        var loads = people
            .Select(person => LoadOf(person, weeks, byOwner[person.Id], planned, absences[person.Id], hoursPerDay, days))
            .OrderBy(person => person.Name, Comparer<string>.Create((a, b) => Names.Compare(a, b, CompareOptions.IgnoreCase)))
            .ToList();
        return new TeamLoadView(today, hoursPerDay, weeks, loads);
    }

    private static PersonLoad LoadOf(
        PersonInfo person,
        IReadOnlyList<TeamLoadWeek> weeks,
        IEnumerable<OwnedTask> tasks,
        IReadOnlyDictionary<Guid, decimal> planned,
        IEnumerable<AbsenceInfo> absences,
        decimal hoursPerDay,
        IBusinessDays days)
    {
        var periods = absences.Select(absence => new AbsentPeriod(absence.Start, absence.End)).ToList();
        var owned = tasks.ToList();
        var personWeeks = weeks.Select(week =>
        {
            var weekTasks = owned
                .Select(task => (Task: task, Hours: TeamLoad.HoursIn(
                    new LoadedTask(task.Start, task.End, planned[task.ItemId]), week.Start, week.End, days)))
                .Where(row => row.Hours > 0m)
                .Select(row => new WeekTask(
                    row.Task.ProjectId,
                    row.Task.ProjectCode,
                    row.Task.ProjectName,
                    row.Task.ItemId,
                    row.Task.Name,
                    row.Task.Start,
                    row.Task.End,
                    planned[row.Task.ItemId],
                    Math.Round(row.Hours, 2)))
                .OrderByDescending(task => task.Hours)
                .ThenBy(task => task.ProjectCode, StringComparer.Ordinal)
                .ToList();
            var away = periods.Where(period => period.Start <= week.End && period.End >= week.Start).ToList();
            var absenceDays = TeamLoad.AbsentBusinessDays(away, week.Start, week.End, days);
            var load = Math.Round(weekTasks.Sum(task => task.Hours), 2);
            var capacity = TeamLoad.CapacityOf(week.Start, week.End, absenceDays, hoursPerDay, days);
            return new PersonWeek(load, capacity, absenceDays, TeamLoad.StateOf(load, capacity), weekTasks, away);
        }).ToList();
        return new PersonLoad(
            person.Id, person.Name, person.Email, personWeeks.Any(week => week.State == LoadState.Over), personWeeks);
    }
}
