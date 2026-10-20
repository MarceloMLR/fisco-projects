using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>The phases and the items of the schedule of a project, as the list of projects reads them.</summary>
public sealed record ScheduleParts(IReadOnlyList<SchedulePhase> Phases, IReadOnlyList<ScheduleItem> Items);

/// <summary>The first start and the last end of a schedule.</summary>
public sealed record SchedulePeriod(DateOnly Start, DateOnly End);

/// <summary>
/// Where the schedules are kept, in the same unit of work as the projects: a change is saved by
/// <see cref="IProjectStore.SaveChangesAsync"/>, in the transaction of <see cref="IProjectStore.InSerializableTransactionAsync"/>.
/// </summary>
public interface IScheduleStore
{
    /// <summary>The schedule with its phases, items and dependencies, tracked; null while the project has none.</summary>
    Task<Schedule?> FindAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The schedule, tracked, created now when the project has none yet.</summary>
    Task<Schedule> GetOrAddAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The phases and items of each project, without tracking, for the list of projects.</summary>
    Task<IReadOnlyDictionary<Guid, ScheduleParts>> OfProjectsAsync(
        IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);

    /// <summary>The dates of the schedule of a project; null without tasks and milestones.</summary>
    Task<SchedulePeriod?> PeriodAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The name of each phase found.</summary>
    Task<IReadOnlyDictionary<Guid, string>> PhaseNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>The name of each task or milestone found.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ItemNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>The changes a person keeps in the schedule of a project, tracked, in order (ADR-0091).</summary>
    Task<IReadOnlyList<ScheduleChange>> ChangesOfAsync(Guid projectId, Guid actorId, CancellationToken cancellationToken);

    void AddChange(ScheduleChange change);

    void RemoveChange(ScheduleChange change);
}

/// <summary>Today in the time zone of the installation (ADR-0013), for the delays and the completion of the tasks.</summary>
public sealed class InstallationClock(IInstallationSettings settings, TimeProvider clock)
{
    public async Task<DateOnly> TodayAsync(CancellationToken cancellationToken)
    {
        var installation = await settings.GetAsync(cancellationToken);
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(installation.TimeZone, out var found) ? found : TimeZoneInfo.Utc;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, zone));
    }
}

/// <summary>
/// A schedule as the Schedule tab shows it: the numbers of the day (<see cref="ScheduleFigures"/>), the names of the
/// owners, the items each item depends on, the tasks whose progress the person may update now (ADR-0043), the
/// non-business days around its period, for the timeline to shade (ADR-0051), the business days of delay of each
/// late item (<see cref="ScheduleFigures.LateDaysOf"/>), the items on the critical path and the baseline compared with
/// the schedule now, none until the manager saves one (ADR-0021), and, for who edits it, what undoing and redoing would
/// do (ADR-0091).
/// </summary>
public sealed record ScheduleView(
    ScheduleFigures Figures,
    IReadOnlyDictionary<Guid, string> OwnerNames,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Predecessors,
    IReadOnlySet<Guid> ProgressUpdatable,
    IReadOnlyList<DateOnly> NonWorkingDays,
    IReadOnlyDictionary<Guid, int> LateDays,
    IReadOnlySet<Guid> Critical,
    BaselineComparison? Baseline,
    ScheduleChangeLabel? Undo = null,
    ScheduleChangeLabel? Redo = null);

/// <summary>The schedule of a project, for the people in it and the Administrator (ADR-0043).</summary>
public sealed class GetSchedule(
    IProjectStore projects, IScheduleStore schedules, IUserDirectory users, IWorkCalendar calendar, InstallationClock clock)
{
    /// <returns>The schedule, empty while the project has none, or null when the project does not exist for the person.</returns>
    public async Task<ScheduleView?> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return null;
        }

        var schedule = await schedules.FindAsync(projectId, cancellationToken) ?? new Schedule(projectId);
        var today = await clock.TodayAsync(cancellationToken);
        var names = await PeopleNames.OfAsync(
            users, schedule.Items.Where(item => item.OwnerId is not null).Select(item => item.OwnerId!.Value), cancellationToken);
        var figures = schedule.Figures(today);
        // What the person who edits the schedule would undo and redo now (ADR-0091).
        var changes = permissions.CanEditSchedule && !project.IsClosed
            ? await schedules.ChangesOfAsync(projectId, viewer.UserId, cancellationToken)
            : [];
        // Without items nor a baseline, there is nothing to shade, to be late, on the path or to compare.
        var days = figures.Start is null && schedule.Baseline is null ? null : await calendar.GetAsync(cancellationToken);
        var nonWorkingDays = days is not null && figures is { Start: { } start, End: { } end }
            ? Schedule.NonWorkingDaysAround(start, end, days)
            : [];
        return new ScheduleView(
            figures,
            names,
            schedule.Dependencies
                .GroupBy(dependency => dependency.SuccessorId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)[.. group.Select(dependency => dependency.PredecessorId)]),
            project.IsClosed
                ? new HashSet<Guid>()
                : schedule.Items
                    .Where(item => item.Kind == ScheduleItemKind.Task && !figures.Summaries.Contains(item.Id)
                        && ScheduleEditing.MayUpdateProgress(permissions, item, viewer))
                    .Select(item => item.Id)
                    .ToHashSet(),
            nonWorkingDays,
            days is null ? new Dictionary<Guid, int>() : ScheduleFigures.LateDaysOf(schedule.Items, today, days),
            days is null ? new HashSet<Guid>() : ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, days),
            schedule.Baseline is { } baseline && days is not null ? baseline.CompareWith(schedule.Items, figures.End, days) : null,
            changes.LastOrDefault(change => !change.Undone)?.Label,
            changes.FirstOrDefault(change => change.Undone)?.Label);
    }
}

/// <summary>What happened to a change of the schedule.</summary>
public enum ScheduleOutcome
{
    Done = 1,

    /// <summary>The project, or the phase or item in it, does not exist for the person who asks.</summary>
    NotFound = 2,

    Forbidden = 3,
    Invalid = 4,
    Closed = 5,

    /// <summary>An item it would depend on already depends on it (ADR-0019).</summary>
    Cycle = 6,

    /// <summary>The person has no change of the schedule to undo, or to redo (ADR-0091).</summary>
    NothingToUndo = 7,

    /// <summary>Someone changed the records of the change after it: it is not undone, nor redone (ADR-0091).</summary>
    UndoBlocked = 8,
}

/// <summary>A field of the forms of the schedule.</summary>
public enum ScheduleField
{
    Name = 1,
    Phase = 2,
    Start = 3,
    Duration = 4,
    Progress = 5,
    Owner = 6,
    Predecessors = 7,

    /// <summary>Only a milestone is reached; a task has a progress (ADR-0020).</summary>
    Reached = 8,

    /// <summary>The end of a task in the timeline: on or after the start, up to 999 business days; a milestone has none.</summary>
    End = 9,

    /// <summary>The task the item goes into: a task right in the same phase, one level only (ADR-0088).</summary>
    Parent = 10,

    /// <summary>A baseline needs tasks or milestones in the schedule (ADR-0021).</summary>
    Baseline = 11,
}

/// <summary>What happened to a change: the id of what was created, how many items were pushed, or the invalid fields.</summary>
public sealed record ScheduleResult(ScheduleOutcome Outcome, Guid? Id, int Pushed, IReadOnlyList<ScheduleField> Invalid)
{
    /// <summary>What the change was, for the person to undo it (ADR-0091); none for what is not undone.</summary>
    public ScheduleChangeLabel? Label { get; init; }

    public ScheduleResult Labeled(ScheduleAction action, string name) => this with { Label = new ScheduleChangeLabel(action, name) };

    public static ScheduleResult Of(ScheduleOutcome outcome) => new(outcome, null, 0, []);

    public static ScheduleResult Done(Guid? id = null, int pushed = 0) => new(ScheduleOutcome.Done, id, pushed, []);

    public static ScheduleResult InvalidFields(IReadOnlyList<ScheduleField> fields) => new(ScheduleOutcome.Invalid, null, 0, fields);
}

/// <summary>
/// The common part of the changes of a schedule: by default only the Administrator and the manager change it, while
/// the project is open (ADR-0016, ADR-0043). The change runs in a serializable transaction, so two changes at once never
/// break a chain of dependencies, and a person leaving the project never leaves an owner who is no longer in it. The
/// refusals come in the same order for every change: 404, 403, 409 and then the fields (400).
/// </summary>
internal static class ScheduleEditing
{
    /// <summary>A change only the Administrator and the manager make.</summary>
    public static Task<ScheduleResult> RunAsync(
        IProjectStore projects,
        IScheduleStore schedules,
        Guid projectId,
        ProjectViewer viewer,
        Func<Project, Schedule, ScheduleResult> change,
        CancellationToken cancellationToken) =>
        RunAsync(projects, schedules, projectId, viewer, mayChange: null, change, cancellationToken);

    /// <summary>
    /// A change that whoever <paramref name="mayChange"/> allows makes, looking at the schedule (the owner of a task).
    /// The check runs before the transaction, so a refusal opens none, and again in it, on what is in the database then.
    /// </summary>
    public static async Task<ScheduleResult> RunAsync(
        IProjectStore projects,
        IScheduleStore schedules,
        Guid projectId,
        ProjectViewer viewer,
        Func<ProjectPermissions, Schedule, bool>? mayChange,
        Func<Project, Schedule, ScheduleResult> change,
        CancellationToken cancellationToken)
    {
        var before = await projects.FindAsync(projectId, cancellationToken);
        var scheduleBefore = mayChange is null || before is null
            ? null
            : await schedules.FindAsync(projectId, cancellationToken) ?? new Schedule(projectId);
        if (Refusal(before, viewer, permissions => Allows(mayChange, permissions, scheduleBefore)) is { } refusal)
        {
            return refusal;
        }

        return await projects.InSerializableTransactionAsync(async token =>
        {
            var project = await projects.FindAsync(projectId, token);
            var schedule = project is null ? null : await schedules.GetOrAddAsync(projectId, token);
            if (Refusal(project, viewer, permissions => Allows(mayChange, permissions, schedule)) is { } late)
            {
                return late;
            }

            var before = schedule!.Snapshot();
            var result = change(project!, schedule);
            if (result.Outcome == ScheduleOutcome.Done)
            {
                if (result.Label is { } label && PermissionsOf(project!, viewer).CanEditSchedule)
                {
                    await RecordAsync(schedules, schedule, viewer, label, ScheduleDiff.Between(before, schedule.Snapshot()), token);
                }

                await projects.SaveChangesAsync(token);
            }

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Keeps a change of who edits the schedule, to undo it (ADR-0091): a new change takes away what the person could
    /// redo, and the person keeps the last <see cref="ScheduleChange.Kept"/> changes in the project.
    /// </summary>
    private static async Task RecordAsync(
        IScheduleStore schedules, Schedule schedule, ProjectViewer viewer, ScheduleChangeLabel label, ScheduleDiff diff, CancellationToken token)
    {
        if (diff.IsEmpty)
        {
            return;
        }

        var kept = (await schedules.ChangesOfAsync(schedule.Id, viewer.UserId, token)).ToList();
        foreach (var undone in kept.Where(change => change.Undone).ToList())
        {
            schedules.RemoveChange(undone);
            kept.Remove(undone);
        }

        foreach (var oldest in kept.Take(Math.Max(0, kept.Count - (ScheduleChange.Kept - 1))))
        {
            schedules.RemoveChange(oldest);
        }

        var number = kept.Count == 0 ? 1 : kept.Max(change => change.Number) + 1;
        schedules.AddChange(new ScheduleChange(schedule.Id, viewer.UserId, number, label, diff));
    }

    /// <summary>
    /// Undoes the last change of the person not undone yet, or redoes the last one undone (ADR-0091), with the refusals of
    /// every change: 404, 403 for who does not edit the schedule and 409 for a closed project. A change whose records
    /// someone changed after it is blocked and leaves the history of the person.
    /// </summary>
    public static async Task<ScheduleResult> StepAsync(
        IProjectStore projects, IScheduleStore schedules, Guid projectId, ProjectViewer viewer, bool undo, CancellationToken cancellationToken)
    {
        if (Refusal(await projects.FindAsync(projectId, cancellationToken), viewer, permissions => permissions.CanEditSchedule) is { } refusal)
        {
            return refusal;
        }

        return await projects.InSerializableTransactionAsync(async token =>
        {
            var project = await projects.FindAsync(projectId, token);
            if (Refusal(project, viewer, permissions => permissions.CanEditSchedule) is { } late)
            {
                return late;
            }

            var schedule = await schedules.GetOrAddAsync(projectId, token);
            var changes = await schedules.ChangesOfAsync(projectId, viewer.UserId, token);
            var next = undo ? changes.LastOrDefault(change => !change.Undone) : changes.FirstOrDefault(change => change.Undone);
            if (next is null)
            {
                return ScheduleResult.Of(ScheduleOutcome.NothingToUndo);
            }

            var done = undo ? next.UndoIn(schedule) : next.RedoIn(schedule);
            if (!done)
            {
                schedules.RemoveChange(next);
            }

            await projects.SaveChangesAsync(token);
            return done
                ? ScheduleResult.Done() with { Label = next.Label }
                : ScheduleResult.Of(ScheduleOutcome.UndoBlocked) with { Label = next.Label };
        }, cancellationToken);
    }

    private static ProjectPermissions PermissionsOf(Project project, ProjectViewer viewer) =>
        ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));

    /// <summary>A task is in charge of a person of the project who is not a viewer (ADR-0043).</summary>
    public static bool MayOwn(Project project, Guid? ownerId) =>
        ownerId is not { } id || project.RoleOf(id) is { } role && role != ProjectRole.Viewer;

    /// <summary>Whether the person may update the progress of the item, by being in charge of it or editing the schedule.</summary>
    public static bool MayUpdateProgress(ProjectPermissions permissions, ScheduleItem item, ProjectViewer viewer) =>
        permissions.MayUpdateProgress(item.OwnerId == viewer.UserId);

    private static bool Allows(Func<ProjectPermissions, Schedule, bool>? mayChange, ProjectPermissions permissions, Schedule? schedule) =>
        mayChange is null ? permissions.CanEditSchedule : mayChange(permissions, schedule!);

    private static ScheduleResult? Refusal(Project? project, ProjectViewer viewer, Func<ProjectPermissions, bool> allowed)
    {
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return ScheduleResult.Of(ScheduleOutcome.NotFound);
        }

        if (!allowed(permissions))
        {
            return ScheduleResult.Of(ScheduleOutcome.Forbidden);
        }

        return project.IsClosed ? ScheduleResult.Of(ScheduleOutcome.Closed) : null;
    }
}

/// <summary>Includes a phase in the schedule, after the others (ADR-0019).</summary>
public sealed class AddSchedulePhase(IProjectStore projects, IScheduleStore schedules)
{
    public Task<ScheduleResult> ExecuteAsync(Guid projectId, string? name, ProjectViewer viewer, CancellationToken cancellationToken) =>
        ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
            Schedule.ValidPhaseName(name) is { } valid
                ? ScheduleResult.Done(schedule.AddPhase(valid).Id).Labeled(ScheduleAction.PhaseAdded, valid)
                : ScheduleResult.InvalidFields([ScheduleField.Name]),
            cancellationToken);
}

/// <summary>Renames a phase of the schedule.</summary>
public sealed class RenameSchedulePhase(IProjectStore projects, IScheduleStore schedules)
{
    public Task<ScheduleResult> ExecuteAsync(
        Guid projectId, Guid phaseId, string? name, ProjectViewer viewer, CancellationToken cancellationToken) =>
        ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
        {
            if (schedule.PhaseOf(phaseId) is null)
            {
                return ScheduleResult.Of(ScheduleOutcome.NotFound);
            }

            return Schedule.ValidPhaseName(name) is { } valid && schedule.RenamePhase(phaseId, valid)
                ? ScheduleResult.Done().Labeled(ScheduleAction.PhaseRenamed, valid)
                : ScheduleResult.InvalidFields([ScheduleField.Name]);
        }, cancellationToken);
}

/// <summary>Removes a phase with its tasks and milestones and their dependencies; no other item moves (ADR-0019).</summary>
public sealed class RemoveSchedulePhase(IProjectStore projects, IScheduleStore schedules)
{
    public Task<ScheduleResult> ExecuteAsync(Guid projectId, Guid phaseId, ProjectViewer viewer, CancellationToken cancellationToken) =>
        ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
            schedule.PhaseOf(phaseId) is { } phase && schedule.RemovePhase(phaseId)
                ? ScheduleResult.Done().Labeled(ScheduleAction.PhaseRemoved, phase.Name)
                : ScheduleResult.Of(ScheduleOutcome.NotFound),
            cancellationToken);
}

/// <summary>
/// The form of a task or milestone: the phase, the name, the start, the duration in business days (zero for a
/// milestone), the owner (none for a milestone), the progress (zero for a milestone; none keeps it as it is, zero in a
/// new item), the items it depends on and the task it is in (ADR-0088). A task with subtasks takes only the phase, the
/// name and the owner; its start and duration are not looked at.
/// </summary>
public sealed record ScheduleItemForm(
    Guid PhaseId,
    string? Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int? Progress,
    IReadOnlyCollection<Guid>? PredecessorIds,
    Guid? ParentId = null);

/// <summary>The checks of the form of an item against the schedule and the people of the project.</summary>
internal static class ScheduleItemChecks
{
    /// <summary>The plan the form asks for, or the invalid fields.</summary>
    public static (ScheduleItemPlan? Plan, List<ScheduleField> Invalid) Check(
        Project project, Schedule schedule, ScheduleItemKind kind, ScheduleItemForm form, Guid? itemId)
    {
        var invalid = new List<ScheduleField>();
        // A task with subtasks takes its dates and progress from them (ADR-0088).
        var summary = itemId is { } id && schedule.IsSummary(id);
        var name = Schedule.ValidItemName(form.Name);
        if (name is null)
        {
            invalid.Add(ScheduleField.Name);
        }

        var phase = schedule.PhaseOf(form.PhaseId);
        if (phase is null)
        {
            invalid.Add(ScheduleField.Phase);
        }

        if (!summary && !Schedule.IsValidDay(form.Start))
        {
            invalid.Add(ScheduleField.Start);
        }

        if (!summary && !Schedule.IsValidDuration(kind, form.Duration))
        {
            invalid.Add(ScheduleField.Duration);
        }

        if (form.Progress is { } progress && (summary || !Schedule.IsValidProgress(kind, progress)))
        {
            invalid.Add(ScheduleField.Progress);
        }

        if ((kind == ScheduleItemKind.Milestone && form.OwnerId is not null) || !ScheduleEditing.MayOwn(project, form.OwnerId))
        {
            invalid.Add(ScheduleField.Owner);
        }

        if (phase is not null && form.ParentId is { } parentId && !schedule.MayHold(parentId, itemId, phase.Id))
        {
            invalid.Add(ScheduleField.Parent);
        }

        // Only the items with dates of their own have dependencies (ADR-0088).
        var predecessors = form.PredecessorIds ?? [];
        if (predecessors.Any(id => id == itemId || id == form.ParentId || schedule.ItemOf(id) is null || schedule.IsSummary(id))
            || (summary && predecessors.Count > 0))
        {
            invalid.Add(ScheduleField.Predecessors);
        }

        return invalid.Count > 0
            ? (null, invalid)
            : (new ScheduleItemPlan(
                form.PhaseId, name!, form.Start, form.Duration, form.OwnerId, form.Progress, [.. predecessors.Distinct()], form.ParentId), invalid);
    }
}

/// <summary>
/// Includes a task or a milestone (ADR-0018, ADR-0019): it starts on a business day, after the items it depends on, and
/// a task ends as many business days later as its duration says. The first subtask of a task takes its dependencies
/// (ADR-0088).
/// </summary>
public sealed class AddScheduleItem(IProjectStore projects, IScheduleStore schedules, IWorkCalendar calendar, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(
        Guid projectId, ScheduleItemKind kind, ScheduleItemForm form, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var days = await calendar.GetAsync(cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (project, schedule) =>
        {
            var (plan, invalid) = ScheduleItemChecks.Check(project, schedule, kind, form, itemId: null);
            if (plan is null)
            {
                return ScheduleResult.InvalidFields(invalid);
            }

            try
            {
                var item = schedule.AddItem(kind, plan, days, today);
                return ScheduleResult.Done(item.Id)
                    .Labeled(kind == ScheduleItemKind.Task ? ScheduleAction.TaskAdded : ScheduleAction.MilestoneAdded, item.Name);
            }
            catch (ScheduleCycleException)
            {
                return ScheduleResult.Of(ScheduleOutcome.Cycle);
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Changes a task or a milestone and pushes, in chain, the items after it that would start before it ends; an earlier
/// start pulls nobody (ADR-0019). A task that reaches 100% keeps today as the day it was completed (ADR-0020).
/// </summary>
public sealed class ChangeScheduleItem(IProjectStore projects, IScheduleStore schedules, IWorkCalendar calendar, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(
        Guid projectId, Guid itemId, ScheduleItemForm form, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var days = await calendar.GetAsync(cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (project, schedule) =>
        {
            if (schedule.ItemOf(itemId) is not { } item)
            {
                return ScheduleResult.Of(ScheduleOutcome.NotFound);
            }

            var (plan, invalid) = ScheduleItemChecks.Check(project, schedule, item.Kind, form, itemId);
            if (plan is null)
            {
                return ScheduleResult.InvalidFields(invalid);
            }

            try
            {
                return ScheduleResult.Done(pushed: schedule.ChangeItem(itemId, plan, days, today) ?? 0)
                    .Labeled(item.Kind == ScheduleItemKind.Task ? ScheduleAction.TaskChanged : ScheduleAction.MilestoneChanged, item.Name);
            }
            catch (ScheduleCycleException)
            {
                return ScheduleResult.Of(ScheduleOutcome.Cycle);
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Updates the progress of a task (ADR-0020): the Administrator and the manager of any task, the tech lead, the
/// contributor and the developer of the ones they are in charge of (ADR-0043). At 100% the task keeps today as the day
/// it was completed; below it, the day goes away.
/// </summary>
public sealed class UpdateScheduleItemProgress(IProjectStore projects, IScheduleStore schedules, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(
        Guid projectId, Guid itemId, int progress, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(
            projects,
            schedules,
            projectId,
            viewer,
            // An item that is not there passes, to get its 404 below.
            (permissions, schedule) =>
                schedule.ItemOf(itemId) is not { } item || ScheduleEditing.MayUpdateProgress(permissions, item, viewer),
            (_, schedule) =>
            {
                if (schedule.ItemOf(itemId) is not { } item)
                {
                    return ScheduleResult.Of(ScheduleOutcome.NotFound);
                }

                // A task with subtasks takes its progress from them (ADR-0088).
                if (!Schedule.IsValidProgress(item.Kind, progress) || item.Kind != ScheduleItemKind.Task || schedule.IsSummary(itemId))
                {
                    return ScheduleResult.InvalidFields([ScheduleField.Progress]);
                }

                schedule.UpdateProgress(itemId, progress, today);
                return ScheduleResult.Done().Labeled(ScheduleAction.ProgressUpdated, item.Name);
            },
            cancellationToken);
    }
}

/// <summary>
/// Marks a milestone as reached, keeping today as the day, or as not reached (ADR-0020, ADR-0086); only the
/// Administrator and the manager do it (ADR-0043).
/// </summary>
public sealed class SetMilestoneReached(IProjectStore projects, IScheduleStore schedules, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(
        Guid projectId, Guid itemId, bool reached, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
        {
            if (schedule.ItemOf(itemId) is not { } item)
            {
                return ScheduleResult.Of(ScheduleOutcome.NotFound);
            }

            if (item.Kind != ScheduleItemKind.Milestone)
            {
                return ScheduleResult.InvalidFields([ScheduleField.Reached]);
            }

            schedule.SetReached(itemId, reached, today);
            return ScheduleResult.Done()
                .Labeled(reached ? ScheduleAction.MilestoneReached : ScheduleAction.MilestoneUnmarked, item.Name);
        }, cancellationToken);
    }
}

/// <summary>
/// Saves the baseline of the project, or updates it, on the day in the time zone of the installation (ADR-0021): only
/// the Administrator and the manager, while the project is open (ADR-0043), and only with tasks or milestones.
/// </summary>
public sealed class SaveScheduleBaseline(IProjectStore projects, IScheduleStore schedules, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
            schedule.SaveBaseline(today) ? ScheduleResult.Done() : ScheduleResult.InvalidFields([ScheduleField.Baseline]),
            cancellationToken);
    }
}

/// <summary>The new dates of a task or milestone moved in the timeline: the start and, for a task, maybe the end.</summary>
public sealed record ScheduleItemDates(DateOnly Start, DateOnly? End);

/// <summary>
/// Moves a task or milestone in the timeline (ADR-0019, ADR-0051): the start keeps the duration; with an end, a task
/// lasts the business days between them, counted by the calendar of the installation. The items after it are pushed.
/// </summary>
public sealed class RescheduleScheduleItem(
    IProjectStore projects, IScheduleStore schedules, IWorkCalendar calendar, InstallationClock clock)
{
    public async Task<ScheduleResult> ExecuteAsync(
        Guid projectId, Guid itemId, ScheduleItemDates dates, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dates);
        var days = await calendar.GetAsync(cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
        {
            if (schedule.ItemOf(itemId) is not { } item)
            {
                return ScheduleResult.Of(ScheduleOutcome.NotFound);
            }

            var invalid = new List<ScheduleField>();
            // A task with subtasks takes its dates from them (ADR-0088).
            if (!Schedule.IsValidDay(dates.Start) || schedule.IsSummary(itemId))
            {
                invalid.Add(ScheduleField.Start);
            }

            if (dates.End is { } end
                && (item.Kind != ScheduleItemKind.Task || Schedule.DurationBetween(dates.Start, end, days) is null))
            {
                invalid.Add(ScheduleField.End);
            }

            return invalid.Count > 0
                ? ScheduleResult.InvalidFields(invalid)
                : ScheduleResult.Done(pushed: schedule.Reschedule(itemId, dates.Start, dates.End, days, today) ?? 0)
                    .Labeled(ScheduleAction.DatesChanged, item.Name);
        }, cancellationToken);
    }
}

/// <summary>
/// Removes a task or milestone and its dependencies; no other item moves (ADR-0019). A task with subtasks goes with
/// them, and the last subtask of a task leaves its dependencies to it (ADR-0088).
/// </summary>
public sealed class RemoveScheduleItem(IProjectStore projects, IScheduleStore schedules, IWorkCalendar calendar)
{
    public async Task<ScheduleResult> ExecuteAsync(Guid projectId, Guid itemId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var days = await calendar.GetAsync(cancellationToken);
        return await ScheduleEditing.RunAsync(projects, schedules, projectId, viewer, (_, schedule) =>
            schedule.ItemOf(itemId) is { } item && schedule.RemoveItem(itemId, days)
                ? ScheduleResult.Done().Labeled(
                    item.Kind == ScheduleItemKind.Task ? ScheduleAction.TaskRemoved : ScheduleAction.MilestoneRemoved, item.Name)
                : ScheduleResult.Of(ScheduleOutcome.NotFound),
            cancellationToken);
    }
}

/// <summary>Undoes the last change of the schedule the person made and has not undone (ADR-0021, ADR-0091).</summary>
public sealed class UndoScheduleChange(IProjectStore projects, IScheduleStore schedules)
{
    public Task<ScheduleResult> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken) =>
        ScheduleEditing.StepAsync(projects, schedules, projectId, viewer, undo: true, cancellationToken);
}

/// <summary>Redoes the last change of the schedule the person undid (ADR-0071, ADR-0091).</summary>
public sealed class RedoScheduleChange(IProjectStore projects, IScheduleStore schedules)
{
    public Task<ScheduleResult> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken) =>
        ScheduleEditing.StepAsync(projects, schedules, projectId, viewer, undo: false, cancellationToken);
}
