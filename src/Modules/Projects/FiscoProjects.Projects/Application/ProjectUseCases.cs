using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>Which projects the list shows: the open ones, the closed ones (completed or cancelled) or all.</summary>
public enum ProjectSituation
{
    Active = 1,
    Closed = 2,
    All = 3,
}

/// <summary>
/// Filters of the list of projects: part of the code or of the name, the client, the situation, only the projects the
/// person is in, and the page, from 1.
/// </summary>
public sealed record ProjectListQuery(
    string? Search, Guid? ClientId, ProjectSituation Situation, bool Mine, int Page, int PageSize)
{
    public const int MaxPageSize = 100;
}

/// <summary>The client of a project, as the screens show it: the name and when the logo changed (null without one).</summary>
public sealed record ProjectClient(Guid Id, string Name, DateTime? LogoChangedAt);

/// <summary>A person in a project, by name, with the role in it.</summary>
public sealed record ProjectPerson(Guid Id, string Name, ProjectRole Role);

/// <summary>A project of the list, with the numbers of its schedule (null without tasks and milestones).</summary>
public sealed record ProjectListItem(
    Guid Id,
    string Code,
    string Name,
    ProjectClient? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPerson> Managers,
    ScheduleFigures? Schedule);

/// <summary>A page of the list, with today in the time zone of the installation, the day the schedules are seen on.</summary>
public sealed record ProjectListPage(IReadOnlyList<ProjectListItem> Items, int Total, DateOnly Today);

/// <summary>A project of the list as the store reads it: the managers by id.</summary>
public sealed record ProjectListRow(
    Guid Id, string Code, string Name, ProjectClient? Client, ProjectStatus Status, IReadOnlyList<Guid> ManagerIds);

public sealed record ProjectListRows(IReadOnlyList<ProjectListRow> Items, int Total);

/// <summary>Another project already has the code: the unique index refused the save.</summary>
public sealed class ProjectCodeInUseException(Exception inner) : Exception("Another project has the code.", inner);

/// <summary>Where the projects and the people in them are kept.</summary>
public interface IProjectStore
{
    /// <summary>
    /// The projects the person sees, in alphabetical order: the Administrator sees all, except with "only mine", and
    /// everyone else only the projects they are in.
    /// </summary>
    Task<ProjectListRows> ListAsync(ProjectListQuery query, ProjectViewer viewer, CancellationToken cancellationToken);

    /// <summary>The project with the people in it, tracked, so a change is saved by <see cref="SaveChangesAsync"/>.</summary>
    Task<Project?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The name of the project with the code, other than the one given.</summary>
    Task<string?> NameOfProjectWithCodeAsync(string code, Guid? except, CancellationToken cancellationToken);

    /// <summary>Whether any project, open or closed, has the client.</summary>
    Task<bool> AnyWithClientAsync(Guid clientId, CancellationToken cancellationToken);

    /// <summary>The name of each project found.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(Project project);

    /// <exception cref="ProjectCodeInUseException">Another project got the same code meanwhile.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs the work in a serializable transaction, repeated from scratch when the database picks it as a deadlock
    /// victim, so checks such as "another manager stays" hold against concurrent changes.
    /// </summary>
    Task<TOutcome> InSerializableTransactionAsync<TOutcome>(
        Func<CancellationToken, Task<TOutcome>> work, CancellationToken cancellationToken);
}

/// <summary>The names of the people of the projects, from the Users module.</summary>
internal static class PeopleNames
{
    public static async Task<IReadOnlyDictionary<Guid, string>> OfAsync(
        IUserDirectory users, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        return distinct.Count == 0 ? new Dictionary<Guid, string>() : await users.NamesAsync(distinct, cancellationToken);
    }

    public static string NameOf(IReadOnlyDictionary<Guid, string> names, Guid id) =>
        names.TryGetValue(id, out var name) ? name : string.Empty;
}

/// <summary>
/// The projects the person sees, with the names of the managers (ADR-0016, ADR-0043) and the numbers of their schedules
/// for the strip of phases and the final deadline (ADR-0071).
/// </summary>
public sealed class ListProjects(IProjectStore store, IScheduleStore schedules, IUserDirectory users, InstallationClock clock)
{
    public async Task<ProjectListPage> ExecuteAsync(
        ProjectListQuery query, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await store.ListAsync(
            query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() },
            viewer,
            cancellationToken);
        var names = await PeopleNames.OfAsync(users, rows.Items.SelectMany(row => row.ManagerIds), cancellationToken);
        var parts = await schedules.OfProjectsAsync([.. rows.Items.Select(row => row.Id)], cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        return new ProjectListPage(
            [.. rows.Items.Select(row => new ProjectListItem(
                row.Id,
                row.Code,
                row.Name,
                row.Client,
                row.Status,
                [.. row.ManagerIds
                    .Select(id => new ProjectPerson(id, PeopleNames.NameOf(names, id), ProjectRole.Manager))
                    .OrderBy(person => person.Name, StringComparer.CurrentCulture)],
                parts.TryGetValue(row.Id, out var schedule) && schedule.Items.Count > 0
                    ? ScheduleFigures.Of(schedule.Phases, schedule.Items, today)
                    : null))],
            rows.Total,
            today);
    }
}

/// <summary>
/// A project as its page shows it: the client, the people by name, what the person may do and the dates of the schedule
/// (null without tasks and milestones).
/// </summary>
public sealed record ProjectDetails(
    Project Project,
    ProjectClient? Client,
    IReadOnlyList<ProjectPerson> Members,
    ProjectPermissions Permissions,
    SchedulePeriod? Schedule);

/// <summary>One project, for the people in it and the Administrator; for anyone else, it does not exist.</summary>
public sealed class GetProject(IProjectStore store, IClientStore clients, IScheduleStore schedules, IUserDirectory users)
{
    public async Task<ProjectDetails?> ExecuteAsync(Guid id, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync(id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var permissions = ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (!permissions.CanView)
        {
            return null;
        }

        var client = project.ClientId is { } clientId ? await clients.FindAsync(clientId, cancellationToken) : null;
        var names = await PeopleNames.OfAsync(users, project.Members.Select(member => member.UserId), cancellationToken);
        return new ProjectDetails(
            project,
            client is null ? null : new ProjectClient(client.Id, client.Name, client.HasLogo ? client.LogoChangedAt : null),
            [.. project.Members
                .Select(member => new ProjectPerson(member.UserId, PeopleNames.NameOf(names, member.UserId), member.Role))
                .OrderBy(person => person.Role)
                .ThenBy(person => person.Name, StringComparer.CurrentCulture)],
            permissions,
            await schedules.PeriodAsync(id, cancellationToken));
    }
}

/// <summary>A field of the project form.</summary>
public enum ProjectField
{
    Code = 1,
    Name = 2,
    Description = 3,
    Client = 4,
    Manager = 5,
    TechLead = 6,

    /// <summary>The model is an active model of project (ADR-0017).</summary>
    Template = 7,

    /// <summary>A project made from a model needs the day its schedule starts.</summary>
    ScheduleStart = 8,
}

public enum ProjectSaveOutcome
{
    Saved = 1,
    Invalid = 2,
    CodeInUse = 3,
    NotFound = 4,
    Forbidden = 5,
    Closed = 6,
}

/// <summary>What happened to the form: the id of the project saved, the invalid fields or who has the code.</summary>
public sealed record ProjectSaveResult(
    ProjectSaveOutcome Outcome, Guid? Id, IReadOnlyList<ProjectField> Invalid, string? CodeInUseBy)
{
    public static ProjectSaveResult Saved(Guid id) => new(ProjectSaveOutcome.Saved, id, [], null);

    public static ProjectSaveResult Of(ProjectSaveOutcome outcome) => new(outcome, null, [], null);

    public static ProjectSaveResult InvalidFields(IReadOnlyList<ProjectField> fields) =>
        new(ProjectSaveOutcome.Invalid, null, fields, null);

    public static ProjectSaveResult CodeInUse(string projectName) =>
        new(ProjectSaveOutcome.CodeInUse, null, [], projectName);
}

/// <summary>The data of the project form checked: the values as they are kept, and the invalid fields.</summary>
internal sealed record CheckedProject(string Code, string Name, string? Description, List<ProjectField> Invalid)
{
    public static CheckedProject Of(string? code, string? name, string? description)
    {
        var invalid = new List<ProjectField>();
        var normalizedCode = ProjectCode.Normalize(code);
        if (normalizedCode is null)
        {
            invalid.Add(ProjectField.Code);
        }

        var validName = Project.ValidName(name);
        if (validName is null)
        {
            invalid.Add(ProjectField.Name);
        }

        if (!Project.TryNormalizeDescription(description, out var normalizedDescription))
        {
            invalid.Add(ProjectField.Description);
        }

        return new CheckedProject(normalizedCode ?? string.Empty, validName ?? string.Empty, normalizedDescription, invalid);
    }
}

/// <summary>Saves the project, or tells who has the code when the database refuses it at the last moment.</summary>
internal static class ProjectSaving
{
    public static async Task<ProjectSaveResult> SaveAsync(IProjectStore store, Project project, CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return ProjectSaveResult.Saved(project.Id);
        }
        catch (ProjectCodeInUseException)
        {
            var owner = await store.NameOfProjectWithCodeAsync(project.Code, project.Id, cancellationToken);
            return ProjectSaveResult.CodeInUse(owner ?? string.Empty);
        }
    }

    /// <summary>Whether the client may be chosen: it exists and is active, or it is the one the project already has.</summary>
    public static async Task<bool> ClientMayBeChosenAsync(
        IClientStore clients, Guid? clientId, Guid? current, CancellationToken cancellationToken)
    {
        if (clientId is not { } id || id == current)
        {
            return true;
        }

        return await clients.FindAsync(id, cancellationToken) is { IsActive: true };
    }
}

/// <summary>
/// The project form of the creation: the manager is the person who creates it when none is chosen. The tech lead is a
/// user (<see cref="TechLeadId"/>) or a person of the directory who never signed in (<see cref="TechLeadDirectoryId"/>).
/// </summary>
public sealed record NewProject(
    string? Code,
    string? Name,
    string? Description,
    Guid? ClientId,
    Guid? ManagerId,
    Guid? TechLeadId,
    string? TechLeadDirectoryId = null,
    Guid? TemplateId = null,
    DateOnly? ScheduleStart = null);

/// <summary>
/// Creates a project in Planning (ADR-0016) with its manager and, optionally, its tech lead (ADR-0072). The manager is
/// the person who creates it, or another active person with the system role Project manager or Administrator; then
/// the person who creates it does not join the project. The tech lead is another active person.
/// </summary>
public sealed class CreateProject(
    IProjectStore store,
    IClientStore clients,
    IUserAccess access,
    IUserProvisioning provisioning,
    TimeProvider clock,
    ITemplateStore templates,
    IScheduleStore schedules,
    IWorkCalendar calendar,
    InstallationClock installationClock)
{
    public async Task<ProjectSaveResult> ExecuteAsync(NewProject form, Guid creatorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var data = CheckedProject.Of(form.Code, form.Name, form.Description);
        var invalid = data.Invalid;
        if (!await ProjectSaving.ClientMayBeChosenAsync(clients, form.ClientId, null, cancellationToken))
        {
            invalid.Add(ProjectField.Client);
        }

        var managerId = form.ManagerId ?? creatorId;
        if (await access.FindAsync(managerId, cancellationToken) is not
            { IsActive: true, Role: SystemRole.Administrator or SystemRole.ProjectManager })
        {
            invalid.Add(ProjectField.Manager);
        }

        var fromDirectory = !string.IsNullOrWhiteSpace(form.TechLeadDirectoryId);
        if ((form.TechLeadId is not null && fromDirectory)
            || (form.TechLeadId is { } techLeadId
                && (techLeadId == managerId || await access.FindAsync(techLeadId, cancellationToken) is not { IsActive: true })))
        {
            invalid.Add(ProjectField.TechLead);
        }

        // From a model, the schedule is made with the project (ADR-0017).
        var template = form.TemplateId is { } templateId ? await templates.FindAsync(templateId, cancellationToken) : null;
        if (form.TemplateId is not null && template is not { IsActive: true })
        {
            invalid.Add(ProjectField.Template);
        }

        if (form.TemplateId is not null && (form.ScheduleStart is not { } day || !Schedule.IsValidDay(day)))
        {
            invalid.Add(ProjectField.ScheduleStart);
        }

        if (invalid.Count > 0)
        {
            return ProjectSaveResult.InvalidFields(invalid);
        }

        if (await store.NameOfProjectWithCodeAsync(data.Code, null, cancellationToken) is { } owner)
        {
            return ProjectSaveResult.CodeInUse(owner);
        }

        // A tech lead of the directory becomes a user only when everything else is right (ADR-0005).
        var techLead = form.TechLeadId;
        if (fromDirectory)
        {
            techLead = await provisioning.UserFromDirectoryAsync(form.TechLeadDirectoryId!, cancellationToken);
            if (techLead is null || techLead == managerId)
            {
                return ProjectSaveResult.InvalidFields([ProjectField.TechLead]);
            }
        }

        var project = Project.Create(
            data.Code, data.Name, data.Description, form.ClientId, managerId, techLead, clock.GetUtcNow().UtcDateTime);
        store.Add(project);
        if (template is not null)
        {
            var schedule = await schedules.GetOrAddAsync(project.Id, cancellationToken);
            schedule.ApplyTemplate(
                template,
                form.ScheduleStart!.Value,
                await calendar.GetAsync(cancellationToken),
                await installationClock.TodayAsync(cancellationToken));
        }

        return await ProjectSaving.SaveAsync(store, project, cancellationToken);
    }
}

/// <summary>The data of a project that may change after its creation.</summary>
public sealed record ProjectChanges(string? Code, string? Name, string? Description, Guid? ClientId);

/// <summary>
/// Changes the code, the name, the description and the client: the Administrator, the manager and the tech lead of an
/// open project (ADR-0016, ADR-0043). A client that became inactive stays while it is not changed.
/// </summary>
public sealed class ChangeProject(IProjectStore store, IClientStore clients)
{
    public async Task<ProjectSaveResult> ExecuteAsync(
        Guid id, ProjectChanges changes, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return ProjectSaveResult.Of(ProjectSaveOutcome.NotFound);
        }

        if (!permissions.CanEditData)
        {
            return ProjectSaveResult.Of(ProjectSaveOutcome.Forbidden);
        }

        if (project.IsClosed)
        {
            return ProjectSaveResult.Of(ProjectSaveOutcome.Closed);
        }

        var data = CheckedProject.Of(changes.Code, changes.Name, changes.Description);
        var invalid = data.Invalid;
        if (!await ProjectSaving.ClientMayBeChosenAsync(clients, changes.ClientId, project.ClientId, cancellationToken))
        {
            invalid.Add(ProjectField.Client);
        }

        if (invalid.Count > 0)
        {
            return ProjectSaveResult.InvalidFields(invalid);
        }

        if (await store.NameOfProjectWithCodeAsync(data.Code, id, cancellationToken) is { } owner)
        {
            return ProjectSaveResult.CodeInUse(owner);
        }

        project.Change(data.Code, data.Name, data.Description, changes.ClientId);
        return await ProjectSaving.SaveAsync(store, project, cancellationToken);
    }
}

public enum ProjectStatusOutcome
{
    Saved = 1,
    NotFound = 2,
    Forbidden = 3,
}

/// <summary>
/// Moves a project to another status, including reopening a closed one: the Administrator and the manager of the
/// project (ADR-0016, ADR-0043).
/// </summary>
public sealed class ChangeProjectStatus(IProjectStore store)
{
    public async Task<ProjectStatusOutcome> ExecuteAsync(
        Guid id, ProjectStatus status, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return ProjectStatusOutcome.NotFound;
        }

        if (!permissions.CanChangeStatus)
        {
            return ProjectStatusOutcome.Forbidden;
        }

        if (project.Status != status)
        {
            project.ChangeStatus(status);
            await store.SaveChangesAsync(cancellationToken);
        }

        return ProjectStatusOutcome.Saved;
    }
}
