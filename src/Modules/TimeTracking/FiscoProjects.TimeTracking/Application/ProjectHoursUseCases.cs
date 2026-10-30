using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.TimeTracking.Application;

/// <summary>Where the contracted and additional hours are kept, and the sums of the logged hours.</summary>
public interface IProjectHoursStore
{
    /// <summary>The logged hours of each project that has any.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> LoggedByProjectAsync(CancellationToken cancellationToken);

    /// <summary>The contracted hours of each project that has them.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> ContractedByProjectAsync(CancellationToken cancellationToken);

    /// <summary>The sum of the additional hours of each project that has any.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> AdditionalByProjectAsync(CancellationToken cancellationToken);

    /// <summary>The logged hours of each person in the project.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> LoggedByPersonAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The first day with hours logged in the project; null when none.</summary>
    Task<DateOnly?> FirstDayAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The contracted hours of the project, tracked; null when it has none.</summary>
    Task<ContractedHours?> ContractedOfAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The additional hours of the project, not tracked.</summary>
    Task<IReadOnlyList<AdditionalHours>> AdditionalOfAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The additional hours, tracked.</summary>
    Task<AdditionalHours?> FindAdditionalAsync(Guid id, CancellationToken cancellationToken);

    void Add(ContractedHours contracted);

    void Remove(ContractedHours contracted);

    void Add(AdditionalHours additional);

    void Remove(AdditionalHours additional);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Which projects the list of the hours shows.</summary>
public enum ProjectHoursView
{
    /// <summary>Planning, in progress or paused.</summary>
    Open = 1,

    /// <summary>Completed or cancelled.</summary>
    Closed = 2,

    All = 3,
}

/// <summary>A project with the balance of its hours.</summary>
public sealed record ProjectHoursRow(ProjectCard Project, HoursBalance Balance);

/// <summary>Additional hours with the current name of who registered them.</summary>
public sealed record AdditionalHoursRow(AdditionalHours Hours, string CreatedByName);

/// <summary>
/// The hours a person logged in the project, the share of them in the logged hours of the project, rounded to a whole
/// percentage, and the current role of the person in it; null when the person left the project.
/// </summary>
public sealed record PersonHoursRow(Guid UserId, string Name, ProjectMemberRole? Role, decimal Hours, int Share);

/// <summary>The hours of a project: the balance, the first day logged, the additional hours and the hours of each person.</summary>
public sealed record ProjectHoursDetail(
    ProjectCard Project,
    HoursBalance Balance,
    DateOnly? FirstDay,
    IReadOnlyList<AdditionalHoursRow> Additional,
    IReadOnlyList<PersonHoursRow> People);

public enum ProjectHoursOutcome
{
    Done = 1,
    NotFound = 2,
    Invalid = 3,
}

/// <summary>What happened to contracted or additional hours: the id, or why nothing was saved.</summary>
public sealed record ProjectHoursResult(ProjectHoursOutcome Outcome, Guid? Id = null, IReadOnlyList<AdditionalHoursField>? Invalid = null);

/// <summary>Additional hours as the Administrator fills them in.</summary>
public sealed record AdditionalHoursRequest(DateOnly? Day, decimal? Hours, string? Reason);

/// <summary>The contracted hours of a project, or null to take them out.</summary>
public sealed record ContractedHoursRequest(decimal? Hours);

/// <summary>
/// The hours of the projects for the Administrator (ADR-0128): every project of the view and of the search, by code or
/// name with no difference of case or accents, with the balance, in the order of <see cref="HoursBalance.Order{T}"/>.
/// Only the Administrator reaches it, by the policy of the routes.
/// </summary>
public sealed class ListProjectHours(IProjectCatalog catalog, IProjectHoursStore store)
{
    public async Task<IReadOnlyList<ProjectHoursRow>> ExecuteAsync(
        ProjectHoursView view, string? search, CancellationToken cancellationToken)
    {
        var text = SearchText.Simplify(search);
        var projects = (await catalog.AllAsync(cancellationToken))
            .Where(project => view switch
            {
                ProjectHoursView.Open => !project.IsClosed,
                ProjectHoursView.Closed => project.IsClosed,
                _ => true,
            })
            .Where(project => SearchText.Finds(project.Code, text) || SearchText.Finds(project.Name, text))
            .ToList();
        var logged = await store.LoggedByProjectAsync(cancellationToken);
        var contracted = await store.ContractedByProjectAsync(cancellationToken);
        var additional = await store.AdditionalByProjectAsync(cancellationToken);
        var rows = projects.Select(project => new ProjectHoursRow(
            project,
            HoursBalance.Of(
                contracted.TryGetValue(project.Id, out var sold) ? sold : null,
                additional.GetValueOrDefault(project.Id),
                logged.GetValueOrDefault(project.Id),
                project.IsClosed)));
        return [.. HoursBalance.Order(rows, row => row.Balance, row => row.Project.Code)];
    }
}

/// <summary>
/// The hours of a project for the Administrator (ADR-0128): the balance, the first day logged, the additional hours,
/// the newest day first, and the hours of each person who logged, the most first, with the current role in the project
/// (none for whoever left it) and the current name (ADR-0110). Null when the project does not exist.
/// </summary>
public sealed class GetProjectHours(IProjectCatalog catalog, IProjectHoursStore store, IProjectAccess access, IUserDirectory people)
{
    public async Task<ProjectHoursDetail?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await catalog.FindAsync(projectId, cancellationToken) is not { } project)
        {
            return null;
        }

        var additional = await store.AdditionalOfAsync(projectId, cancellationToken);
        var byPerson = await store.LoggedByPersonAsync(projectId, cancellationToken);
        var logged = byPerson.Values.Sum();
        var contracted = await store.ContractedOfAsync(projectId, cancellationToken);
        var balance = HoursBalance.Of(contracted?.Hours, additional.Sum(hours => hours.Hours), logged, project.IsClosed);
        var roles = (await access.MembersAsync([projectId], cancellationToken)).ToDictionary(member => member.UserId, member => member.Role);
        var names = await people.NamesAsync([.. byPerson.Keys.Concat(additional.Select(hours => hours.CreatedBy)).Distinct()], cancellationToken);
        return new ProjectHoursDetail(
            project,
            balance,
            await store.FirstDayAsync(projectId, cancellationToken),
            [.. additional
                .OrderByDescending(hours => hours.Day)
                .ThenByDescending(hours => hours.CreatedAt)
                .Select(hours => new AdditionalHoursRow(hours, names.GetValueOrDefault(hours.CreatedBy) ?? string.Empty))],
            [.. byPerson
                .Select(person => new PersonHoursRow(
                    person.Key,
                    names.GetValueOrDefault(person.Key) ?? string.Empty,
                    roles.TryGetValue(person.Key, out var role) ? role : null,
                    person.Value,
                    (int)Math.Round(person.Value * 100m / logged, MidpointRounding.AwayFromZero)))
                .OrderByDescending(person => person.Hours)
                .ThenBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)]);
    }
}

/// <summary>
/// The Administrator registers, changes or takes out the contracted hours of a project of any status (ADR-0128):
/// without them, the project has no balance.
/// </summary>
public sealed class ChangeContractedHours(IProjectCatalog catalog, IProjectHoursStore store, TimeEntryClock clock)
{
    public async Task<ProjectHoursResult> ExecuteAsync(Guid projectId, ContractedHoursRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await catalog.FindAsync(projectId, cancellationToken) is null)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.NotFound);
        }

        if (request.Hours is { } hours && !ContractedHours.IsValid(hours))
        {
            return new ProjectHoursResult(ProjectHoursOutcome.Invalid, null, [AdditionalHoursField.Hours]);
        }

        var current = await store.ContractedOfAsync(projectId, cancellationToken);
        switch (request.Hours, current)
        {
            case (null, null):
                return new ProjectHoursResult(ProjectHoursOutcome.Done);
            case (null, { } existing):
                store.Remove(existing);
                break;
            case ({ } value, null):
                store.Add(new ContractedHours(projectId, value, clock.Now));
                break;
            case ({ } value, { } existing):
                if (existing.Hours == value)
                {
                    return new ProjectHoursResult(ProjectHoursOutcome.Done, existing.Id);
                }

                existing.Change(value, clock.Now);
                break;
        }

        await store.SaveChangesAsync(cancellationToken);
        return new ProjectHoursResult(ProjectHoursOutcome.Done);
    }
}

/// <summary>
/// The Administrator adds hours to a project of any status, with the day, the hours and the reason (ADR-0128); who
/// added them is kept.
/// </summary>
public sealed class AddAdditionalHours(IProjectCatalog catalog, IProjectHoursStore store, TimeEntryClock clock)
{
    public async Task<ProjectHoursResult> ExecuteAsync(
        Guid projectId, AdditionalHoursRequest request, Guid administratorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await catalog.FindAsync(projectId, cancellationToken) is null)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.NotFound);
        }

        var (form, invalid) = AdditionalHoursForm.Of(request.Day, request.Hours, request.Reason, await clock.TodayAsync(cancellationToken));
        if (form is null)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.Invalid, null, invalid);
        }

        var additional = new AdditionalHours(projectId, administratorId, form, clock.Now);
        store.Add(additional);
        await store.SaveChangesAsync(cancellationToken);
        return new ProjectHoursResult(ProjectHoursOutcome.Done, additional.Id);
    }
}

/// <summary>The Administrator corrects additional hours of a project (ADR-0128).</summary>
public sealed class ChangeAdditionalHours(IProjectHoursStore store, TimeEntryClock clock)
{
    public async Task<ProjectHoursResult> ExecuteAsync(
        Guid projectId, Guid id, AdditionalHoursRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await store.FindAdditionalAsync(id, cancellationToken) is not { } additional || additional.ProjectId != projectId)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.NotFound);
        }

        var (form, invalid) = AdditionalHoursForm.Of(request.Day, request.Hours, request.Reason, await clock.TodayAsync(cancellationToken));
        if (form is null)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.Invalid, null, invalid);
        }

        additional.Change(form, clock.Now);
        await store.SaveChangesAsync(cancellationToken);
        return new ProjectHoursResult(ProjectHoursOutcome.Done, additional.Id);
    }
}

/// <summary>The Administrator removes additional hours of a project (ADR-0128).</summary>
public sealed class DeleteAdditionalHours(IProjectHoursStore store)
{
    public async Task<ProjectHoursResult> ExecuteAsync(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        if (await store.FindAdditionalAsync(id, cancellationToken) is not { } additional || additional.ProjectId != projectId)
        {
            return new ProjectHoursResult(ProjectHoursOutcome.NotFound);
        }

        store.Remove(additional);
        await store.SaveChangesAsync(cancellationToken);
        return new ProjectHoursResult(ProjectHoursOutcome.Done, additional.Id);
    }
}
