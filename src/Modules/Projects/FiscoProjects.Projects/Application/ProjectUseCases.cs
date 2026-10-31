using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Contracts;
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

/// <summary>
/// A project of the list, with the numbers of its schedule (null without tasks and milestones) and the business days of
/// delay of its most late task or milestone (ADR-0096).
/// </summary>
public sealed record ProjectListItem(
    Guid Id,
    string Code,
    string Name,
    ProjectClient? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPerson> Managers,
    ScheduleFigures? Schedule,
    int LateDays = 0);

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
public sealed class ListProjects(
    IProjectStore store, IScheduleStore schedules, IUserDirectory users, InstallationClock clock, IWorkCalendar calendar)
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
        var days = await calendar.GetAsync(cancellationToken);
        return new ProjectListPage(
            [.. rows.Items.Select(row =>
            {
                var schedule = parts.TryGetValue(row.Id, out var found) && found.Items.Count > 0 ? found : null;
                return new ProjectListItem(
                    row.Id,
                    row.Code,
                    row.Name,
                    row.Client,
                    row.Status,
                    [.. row.ManagerIds
                        .Select(id => new ProjectPerson(id, PeopleNames.NameOf(names, id), ProjectRole.Manager))
                        .OrderBy(person => person.Name, StringComparer.CurrentCulture)],
                    schedule is null ? null : ScheduleFigures.Of(schedule.Phases, schedule.Items, today),
                    schedule is null ? 0 : ScheduleFigures.LateDaysOf(schedule.Items, today, days).Values.DefaultIfEmpty(0).Max());
            })],
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

    /// <summary>The folder chosen for the files may not be the folder of the project (ADR-0122).</summary>
    Folder = 9,

    /// <summary>
    /// The schedule converted from the MS Project does not hold together, comes with a model, or names people who may
    /// not be in the project (ADR-0135).
    /// </summary>
    ImportedSchedule = 10,
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
/// user with the system role to be one (<see cref="TechLeadId"/>, ADR-0097); a person of the directory who never signed
/// in (<see cref="TechLeadDirectoryId"/>) has the role User, so is refused.
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
    DateOnly? ScheduleStart = null,
    string? FolderId = null,
    ImportedSchedule? ImportedSchedule = null);

/// <summary>
/// Creates a project in Planning (ADR-0016) with its manager and, optionally, its tech lead (ADR-0072). The manager is
/// the person who creates it, or another active person with the system role Project manager or Administrator; then
/// the person who creates it does not join the project. The tech lead is another active person with the system role
/// Tech lead or Administrator (ADR-0097).
/// </summary>
public sealed class CreateProject(
    IProjectStore store,
    IClientStore clients,
    IUserAccess access,
    TimeProvider clock,
    ITemplateStore templates,
    IScheduleStore schedules,
    IWorkCalendar calendar,
    InstallationClock installationClock,
    ProjectNotices notices,
    IProjectSetupNotices setup,
    ProjectFolderChoices folders,
    IUserProvisioning provisioning)
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

        // Who never signed in has the role User, so is never a tech lead (ADR-0097).
        if (!string.IsNullOrWhiteSpace(form.TechLeadDirectoryId)
            || (form.TechLeadId is { } techLeadId
                && (techLeadId == managerId || !await MemberChecks.MayBeTechLeadAsync(access, techLeadId, cancellationToken))))
        {
            invalid.Add(ProjectField.TechLead);
        }

        // From a model, the schedule is made with the project (ADR-0017).
        var template = form.TemplateId is { } templateId ? await templates.FindAsync(templateId, cancellationToken) : null;
        if (form.TemplateId is not null && template is not { IsActive: true })
        {
            invalid.Add(ProjectField.Template);
        }

        var imported = form.ImportedSchedule;
        if ((form.TemplateId is not null || imported is not null) && (form.ScheduleStart is not { } day || !Schedule.IsValidDay(day)))
        {
            invalid.Add(ProjectField.ScheduleStart);
        }

        // From the MS Project, the schedule converted by the preview, instead of a model (ADR-0135).
        if (imported is not null
            && (form.TemplateId is not null || !await ImportedScheduleChecks.IsValidAsync(imported, form.ClientId, access, cancellationToken)))
        {
            invalid.Add(ProjectField.ImportedSchedule);
        }

        // A folder that already exists, instead of the one the system creates (ADR-0122).
        var folder = string.IsNullOrWhiteSpace(form.FolderId) ? null : await folders.CheckAsync(form.FolderId, null, cancellationToken);
        if (folder is { Refusal: not null })
        {
            invalid.Add(ProjectField.Folder);
        }

        var days = await calendar.GetAsync(cancellationToken);
        var today = await installationClock.TodayAsync(cancellationToken);
        // The outline as the schedule makes it, with someone in the place of each person, before anybody is created.
        if (imported is not null && invalid.Count == 0 && !Holds(imported, form.ScheduleStart!.Value, days, today))
        {
            invalid.Add(ProjectField.ImportedSchedule);
        }

        if (invalid.Count > 0)
        {
            return ProjectSaveResult.InvalidFields(invalid);
        }

        if (await store.NameOfProjectWithCodeAsync(data.Code, null, cancellationToken) is { } owner)
        {
            return ProjectSaveResult.CodeInUse(owner);
        }

        var users = imported is null ? [] : await UsersOfAsync(imported, cancellationToken);
        if (users.Count > 0 && imported!.Resources.Where((resource, index) => PersonOf(resource) && users[index] is null).Any())
        {
            return ProjectSaveResult.InvalidFields([ProjectField.ImportedSchedule]);
        }

        var techLead = form.TechLeadId;
        var now = clock.GetUtcNow().UtcDateTime;
        var project = Project.Create(data.Code, data.Name, data.Description, form.ClientId, managerId, techLead, now);
        if (folder is not null)
        {
            project.ChooseFolder(folder.Id, folder.Name);
        }

        if (imported is not null)
        {
            // Each person once, with the role of the first resource; the manager and the tech lead keep theirs.
            foreach (var (resource, userId) in imported.Resources.Zip(users))
            {
                if (userId is { } id && project.RoleOf(id) is null)
                {
                    project.AddMember(id, resource.Role ?? ProjectRole.Contributor, now);
                }
            }
        }

        store.Add(project);
        if (template is not null || imported is not null)
        {
            var schedule = await schedules.GetOrAddAsync(project.Id, cancellationToken);
            if (template is not null)
            {
                schedule.ApplyTemplate(template, form.ScheduleStart!.Value, days, today);
            }
            else
            {
                schedule.ApplyTemplate(ImportedScheduleChecks.OutlineOf(imported!, users), form.ScheduleStart!.Value, days, today);
            }
        }

        var saved = await ProjectSaving.SaveAsync(store, project, cancellationToken);
        if (saved.Outcome == ProjectSaveOutcome.Saved)
        {
            // The other modules set up what a new project has, as the folders of the documents (ADR-0092).
            await setup.ProjectCreatedAsync(project.Id, cancellationToken);

            // The manager and the tech lead chosen hear about it, unless they created it (ADR-0037, ADR-0072).
            await notices.IncludedAsync(
                project, [.. project.Members.Select(member => (member.UserId, member.Role))], creatorId, cancellationToken);
        }

        return saved;
    }

    private static bool PersonOf(ImportedResourceChoice resource) =>
        resource.UserId is not null || !string.IsNullOrWhiteSpace(resource.DirectoryId);

    /// <summary>Whether the outline makes a schedule, someone standing for each person.</summary>
    private static bool Holds(ImportedSchedule imported, DateOnly start, IBusinessDays days, DateOnly today)
    {
        var someone = Guid.CreateVersion7();
        try
        {
            new Schedule(Guid.CreateVersion7()).ApplyTemplate(
                ImportedScheduleChecks.OutlineOf(imported, [.. imported.Resources.Select(resource => PersonOf(resource) ? someone : (Guid?)null)]),
                start,
                days,
                today);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The user of each resource: the one chosen, or the person of the directory, who becomes a user (ADR-0005).</summary>
    private async Task<List<Guid?>> UsersOfAsync(ImportedSchedule imported, CancellationToken cancellationToken)
    {
        var users = new List<Guid?>();
        foreach (var resource in imported.Resources)
        {
            users.Add(resource.UserId
                ?? (string.IsNullOrWhiteSpace(resource.DirectoryId)
                    ? null
                    : await provisioning.UserFromDirectoryAsync(resource.DirectoryId, cancellationToken)));
        }

        return users;
    }
}

/// <summary>Tells the other modules that a project was created, after it is saved (<see cref="IProjectSetup"/>).</summary>
public interface IProjectSetupNotices
{
    Task ProjectCreatedAsync(Guid projectId, CancellationToken cancellationToken);
}

/// <summary>The data of a project that may change after its creation.</summary>
public sealed record ProjectChanges(string? Code, string? Name, string? Description, Guid? ClientId);

/// <summary>
/// Changes the code, the name, the description and the client: the Administrator, the manager and the tech lead of an
/// open project (ADR-0016, ADR-0043). A client that became inactive stays while it is not changed. After saving, the
/// folder of the project follows its code and name (ADR-0121).
/// </summary>
public sealed class ChangeProject(IProjectStore store, IClientStore clients, IProjectFolderRenaming folder)
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
        var saved = await ProjectSaving.SaveAsync(store, project, cancellationToken);
        if (saved.Outcome == ProjectSaveOutcome.Saved)
        {
            await folder.FollowAsync(project, cancellationToken);
        }

        return saved;
    }
}

public enum ProjectStatusOutcome
{
    Saved = 1,
    NotFound = 2,
    Forbidden = 3,

    /// <summary>The project has tickets not closed, which keep it from being completed (ADR-0043).</summary>
    HasOpenTickets = 4,

    /// <summary>Someone changed a ticket while the cancellation closed them; nothing changed.</summary>
    TicketsChanged = 5,
}

/// <summary>What happened to a change of status, with the tickets not closed that kept the project from being completed.</summary>
public sealed record ProjectStatusResult(ProjectStatusOutcome Outcome, int OpenTickets = 0);

/// <summary>
/// Moves a project to another status, including reopening a closed one: the Administrator and the manager of the
/// project (ADR-0016, ADR-0043). A project is completed only with all its tickets closed; cancelling it closes the open
/// tickets first, so a failure of the project is settled by cancelling it again.
/// </summary>
public sealed class ChangeProjectStatus(IProjectStore store, IEnumerable<IProjectClosing> closing, ProjectNotices notices)
{
    public async Task<ProjectStatusResult> ExecuteAsync(
        Guid id, ProjectStatus status, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return new ProjectStatusResult(ProjectStatusOutcome.NotFound);
        }

        if (!permissions.CanChangeStatus)
        {
            return new ProjectStatusResult(ProjectStatusOutcome.Forbidden);
        }

        if (status == ProjectStatus.Completed && project.Status != ProjectStatus.Completed)
        {
            var open = 0;
            foreach (var module in closing)
            {
                open += await module.OpenTicketsAsync(id, cancellationToken);
            }

            if (open > 0)
            {
                return new ProjectStatusResult(ProjectStatusOutcome.HasOpenTickets, open);
            }
        }

        if (status == ProjectStatus.Cancelled)
        {
            // Also when it is already cancelled: a cancellation that closed the tickets and failed to save the project
            // is settled by cancelling it again.
            foreach (var module in closing)
            {
                if (!await module.CloseOpenTicketsAsync(id, viewer.UserId, cancellationToken))
                {
                    return new ProjectStatusResult(ProjectStatusOutcome.TicketsChanged);
                }
            }
        }

        if (project.Status != status)
        {
            project.ChangeStatus(status);
            await store.SaveChangesAsync(cancellationToken);
            // The people of the project hear about it in the daily summary (ADR-0037).
            await notices.StatusChangedAsync(project, viewer.UserId, cancellationToken);
        }

        return new ProjectStatusResult(ProjectStatusOutcome.Saved);
    }
}

/// <summary>
/// What closing a project would leave behind: the tickets not closed, the tasks below 100% and the requests of
/// acceptance waiting for the answer (ADR-0043).
/// </summary>
public sealed record ProjectClosing(int OpenTickets, int IncompleteTasks, int PendingAcceptances);

/// <summary>
/// The summary of the closing of a project, for who changes its status (ADR-0043): null for whom does not see the
/// project, and forbidden for the others.
/// </summary>
public sealed class GetProjectClosing(
    IProjectStore store, IScheduleStore schedules, IEnumerable<IProjectClosing> closing, IEnumerable<IProjectAcceptances> acceptances)
{
    public async Task<(ProjectStatusOutcome Outcome, ProjectClosing? Closing)> ExecuteAsync(
        Guid id, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return (ProjectStatusOutcome.NotFound, null);
        }

        if (!permissions.CanChangeStatus)
        {
            return (ProjectStatusOutcome.Forbidden, null);
        }

        var open = 0;
        foreach (var module in closing)
        {
            open += await module.OpenTicketsAsync(id, cancellationToken);
        }

        // The tasks with dates of their own: a summary task follows its subtasks.
        var schedule = await schedules.FindAsync(id, cancellationToken);
        var incomplete = schedule?.Items.Count(item =>
            item.Kind == ScheduleItemKind.Task && !schedule.IsSummary(item.Id) && item.Progress < 100) ?? 0;
        var pending = 0;
        foreach (var module in acceptances)
        {
            pending += await module.PendingAsync(id, cancellationToken);
        }

        return (ProjectStatusOutcome.Saved, new ProjectClosing(open, incomplete, pending));
    }
}
