using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>What happened to a change of the people of a project.</summary>
public enum MemberOutcome
{
    Done = 1,

    /// <summary>The project, or the person in it, does not exist for the person who asks.</summary>
    NotFound = 2,

    Forbidden = 3,

    /// <summary>The person is not an active user nor a person of the directory.</summary>
    InvalidPerson = 4,

    /// <summary>An unknown role, or the manager role for someone who may not be a manager (ADR-0085).</summary>
    InvalidRole = 5,

    Closed = 6,
    MemberExists = 7,
    LastManager = 8,
}

/// <summary>
/// A person of a project as the Members tab shows it: the name, the e-mail, the role, when the person joined, the last
/// sign-in (null for who never signed in), whether the person may be a manager (ADR-0085) or a tech lead (ADR-0097) and
/// the absence of today or the next one (ADR-0141).
/// </summary>
public sealed record ProjectMemberItem(
    Guid Id, string Name, string Email, ProjectRole Role, DateTime AddedAt, DateTime? LastSignInAt, bool CanBeManager,
    bool CanBeTechLead = false, AbsencePeriod? NextAbsence = null);

/// <summary>The people of a project, by name, for whoever sees the project (ADR-0043).</summary>
public sealed class ListProjectMembers(IProjectStore store, IUserDirectory users, IAbsenceStore absences, InstallationClock clock)
{
    /// <returns>The people, or null when the project does not exist for the person who asks.</returns>
    public async Task<IReadOnlyList<ProjectMemberItem>?> ExecuteAsync(
        Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync(projectId, cancellationToken);
        if (project is null || !ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId)).CanView)
        {
            return null;
        }

        var people = (await users.PeopleAsync([.. project.Members.Select(member => member.UserId)], cancellationToken))
            .ToDictionary(person => person.Id);
        // The absence of today or the next one of each person, which the people of the project see (ADR-0141).
        var today = await clock.TodayAsync(cancellationToken);
        var next = (await absences.EndingFromAsync([.. project.Members.Select(member => member.UserId)], today, cancellationToken))
            .GroupBy(absence => absence.UserId)
            .ToDictionary(group => group.Key, group => group.MinBy(absence => absence.Start)!.Period);
        return [.. project.Members
            .Select(member => people.TryGetValue(member.UserId, out var person)
                ? new ProjectMemberItem(
                    member.UserId, person.Name, person.Email, member.Role, member.AddedAt, person.LastSignInAt, person.CanBeManager,
                    person.CanBeTechLead, next.GetValueOrDefault(member.UserId))
                : new ProjectMemberItem(
                    member.UserId, string.Empty, string.Empty, member.Role, member.AddedAt, null, false, NextAbsence: next.GetValueOrDefault(member.UserId)))
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)];
    }
}

/// <summary>The person to include: a user or a person of the company directory (ADR-0005), and the role.</summary>
public sealed record NewProjectMember(Guid? UserId, string? DirectoryId, ProjectRole Role);

/// <summary>What happened to the inclusion, and the user now in the project.</summary>
public sealed record MemberAdded(MemberOutcome Outcome, Guid? UserId = null);

/// <summary>The checks of the people of a project, shared by the inclusion, the change of the role and the removal.</summary>
internal static class MemberChecks
{
    /// <summary>
    /// Who may manage the person (<see cref="ProjectPermissions.MayManage"/>) in the open project, with the outcome to
    /// return otherwise; the person with <paramref name="current"/> null is being included.
    /// </summary>
    public static MemberOutcome? Refusal(
        Project? project, ProjectViewer viewer, ProjectRole? current, ProjectRole? next, bool personIsInIt = true)
    {
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true } || !personIsInIt)
        {
            return MemberOutcome.NotFound;
        }

        if (!permissions.MayManage(current, next))
        {
            return MemberOutcome.Forbidden;
        }

        return project.IsClosed ? MemberOutcome.Closed : null;
    }

    /// <summary>
    /// The tasks of a person who left the project or became a viewer lose the owner, in the same change (ADR-0042,
    /// ADR-0043).
    /// </summary>
    public static async Task UnassignAsync(
        IScheduleStore schedules, Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await schedules.FindAsync(projectId, cancellationToken) is { } schedule)
        {
            schedule.Unassign(userId);
        }
    }

    /// <summary>Only an active Administrator or Project manager is a manager of a project (ADR-0085).</summary>
    public static async Task<bool> MayBeManagerAsync(IUserAccess access, Guid userId, CancellationToken cancellationToken) =>
        await access.FindAsync(userId, cancellationToken) is { IsActive: true } person && SystemRoles.ManagesProjects(person.Role);

    /// <summary>Only an active Administrator or Tech lead is a tech lead of a project (ADR-0097).</summary>
    public static async Task<bool> MayBeTechLeadAsync(IUserAccess access, Guid userId, CancellationToken cancellationToken) =>
        await access.FindAsync(userId, cancellationToken) is { IsActive: true } person && SystemRoles.LeadsTechnically(person.Role);

    /// <summary>
    /// Whether the person may get the role in a project: a manager and a tech lead need the system role (ADR-0085,
    /// ADR-0097); the other roles, anyone.
    /// </summary>
    public static async Task<bool> MayHaveRoleAsync(
        IUserAccess access, Guid userId, ProjectRole role, CancellationToken cancellationToken) => role switch
        {
            ProjectRole.Manager => await MayBeManagerAsync(access, userId, cancellationToken),
            ProjectRole.TechLead => await MayBeTechLeadAsync(access, userId, cancellationToken),
            _ => true,
        };
}

/// <summary>
/// Includes a person in a project with a role (ADR-0042): the Administrator and the manager include anyone, the tech
/// lead anyone but managers (ADR-0043). A manager and a tech lead have the system role to be one (ADR-0085, ADR-0097).
/// A person of the directory
/// who never signed in becomes a user first (ADR-0005), only after the permission and the role are checked, so a
/// refused request creates nobody.
/// </summary>
public sealed class AddProjectMember(
    IProjectStore store, IUserAccess access, IUserProvisioning provisioning, TimeProvider clock, ProjectNotices notices)
{
    public async Task<MemberAdded> ExecuteAsync(
        Guid projectId, NewProjectMember form, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var project = await store.FindAsync(projectId, cancellationToken);
        if (MemberChecks.Refusal(project, viewer, current: null, form.Role) is { } refusal)
        {
            return new(refusal);
        }

        if (!Enum.IsDefined(form.Role))
        {
            return new(MemberOutcome.InvalidRole);
        }

        if ((form.UserId is null) == string.IsNullOrWhiteSpace(form.DirectoryId))
        {
            return new(MemberOutcome.InvalidPerson);
        }

        Guid userId;
        if (form.UserId is { } id)
        {
            if (await access.FindAsync(id, cancellationToken) is not { IsActive: true })
            {
                return new(MemberOutcome.InvalidPerson);
            }

            if (!await MemberChecks.MayHaveRoleAsync(access, id, form.Role, cancellationToken))
            {
                return new(MemberOutcome.InvalidRole);
            }

            if (project!.RoleOf(id) is not null)
            {
                return new(MemberOutcome.MemberExists);
            }

            userId = id;
        }
        else
        {
            // Who never signed in has the role User, so is never a manager nor a tech lead.
            if (form.Role is ProjectRole.Manager or ProjectRole.TechLead)
            {
                return new(MemberOutcome.InvalidRole);
            }

            if (await provisioning.UserFromDirectoryAsync(form.DirectoryId!, cancellationToken) is not { } created)
            {
                return new(MemberOutcome.InvalidPerson);
            }

            userId = created;
        }

        Project? saved = null;
        var added = await store.InSerializableTransactionAsync<MemberAdded>(async token =>
        {
            var current = await store.FindAsync(projectId, token);
            if (MemberChecks.Refusal(current, viewer, current: null, form.Role) is { } late)
            {
                return new(late);
            }

            if (current!.RoleOf(userId) is not null)
            {
                return new(MemberOutcome.MemberExists);
            }

            current.AddMember(userId, form.Role, clock.GetUtcNow().UtcDateTime);
            await store.SaveChangesAsync(token);
            saved = current;
            return new(MemberOutcome.Done, userId);
        }, cancellationToken);
        if (added.Outcome == MemberOutcome.Done && saved is not null)
        {
            // The person hears about it in the daily summary (ADR-0037).
            await notices.IncludedAsync(saved, [(userId, form.Role)], viewer.UserId, cancellationToken);
        }

        return added;
    }
}

/// <summary>
/// Changes the role of a person in a project (ADR-0042): the tech lead does not change the role of a manager nor makes
/// someone a manager (ADR-0043); a manager has the system role to be one (ADR-0085); the last manager keeps the role; a
/// viewer is in charge of no task, so the tasks of a person who becomes one lose the owner. Run in a serializable
/// transaction, so two changes at once never leave the project without a manager.
/// </summary>
/// <summary>
/// Tells the other modules that a person left a project or got another role (ADR-0042), after the change is saved, as
/// the tickets of the person (Tickets).
/// </summary>
public interface IMemberChangeNotices
{
    /// <param name="projectId">The project.</param>
    /// <param name="userId">The person.</param>
    /// <param name="role">The role now; null when the person left.</param>
    /// <param name="actorId">Who made the change.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task NotifyAsync(Guid projectId, Guid userId, ProjectRole? role, Guid actorId, CancellationToken cancellationToken);
}

public sealed class ChangeProjectMemberRole(
    IProjectStore store, IScheduleStore schedules, IUserAccess access, IMemberChangeNotices notices, ProjectNotices projectNotices)
{
    public async Task<MemberOutcome> ExecuteAsync(
        Guid projectId, Guid userId, ProjectRole role, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync(projectId, cancellationToken);
        var currentRole = project?.RoleOf(userId);
        if (MemberChecks.Refusal(project, viewer, currentRole, role, personIsInIt: currentRole is not null) is { } refusal)
        {
            return refusal;
        }

        if (!Enum.IsDefined(role)
            || (role != currentRole && !await MemberChecks.MayHaveRoleAsync(access, userId, role, cancellationToken)))
        {
            return MemberOutcome.InvalidRole;
        }

        if (role == currentRole)
        {
            return MemberOutcome.Done;
        }

        Project? saved = null;
        var outcome = await store.InSerializableTransactionAsync(async token =>
        {
            var current = await store.FindAsync(projectId, token);
            var roleNow = current?.RoleOf(userId);
            if (MemberChecks.Refusal(current, viewer, roleNow, role, personIsInIt: roleNow is not null) is { } late)
            {
                return late;
            }

            try
            {
                current!.ChangeMemberRole(userId, role);
            }
            catch (LastManagerException)
            {
                return MemberOutcome.LastManager;
            }

            if (role == ProjectRole.Viewer)
            {
                await MemberChecks.UnassignAsync(schedules, projectId, userId, token);
            }

            await store.SaveChangesAsync(token);
            saved = current;
            return MemberOutcome.Done;
        }, cancellationToken);
        if (outcome == MemberOutcome.Done)
        {
            await notices.NotifyAsync(projectId, userId, role, viewer.UserId, cancellationToken);
            // The person hears about the new role in the daily summary (ADR-0037).
            await projectNotices.RoleChangedAsync(saved!, userId, role, viewer.UserId, cancellationToken);
        }

        return outcome;
    }
}

/// <summary>
/// Removes a person from a project, who stops seeing it from the next request on (ADR-0042); what the person did keeps
/// the name, and the tasks of the person lose the owner. The tech lead does not remove managers (ADR-0043), and the
/// last manager stays. Run in a serializable transaction, so two removals at once never leave the project without a
/// manager.
/// </summary>
public sealed class RemoveProjectMember(IProjectStore store, IScheduleStore schedules, IMemberChangeNotices notices)
{
    public async Task<MemberOutcome> ExecuteAsync(
        Guid projectId, Guid userId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync(projectId, cancellationToken);
        var currentRole = project?.RoleOf(userId);
        if (MemberChecks.Refusal(project, viewer, currentRole, next: null, personIsInIt: currentRole is not null) is { } refusal)
        {
            return refusal;
        }

        var outcome = await store.InSerializableTransactionAsync(async token =>
        {
            var current = await store.FindAsync(projectId, token);
            var roleNow = current?.RoleOf(userId);
            if (MemberChecks.Refusal(current, viewer, roleNow, next: null, personIsInIt: roleNow is not null) is { } late)
            {
                return late;
            }

            try
            {
                current!.RemoveMember(userId);
            }
            catch (LastManagerException)
            {
                return MemberOutcome.LastManager;
            }

            await MemberChecks.UnassignAsync(schedules, projectId, userId, token);
            await store.SaveChangesAsync(token);
            return MemberOutcome.Done;
        }, cancellationToken);
        if (outcome == MemberOutcome.Done)
        {
            // The tickets of the person in the project lose the assignee (ADR-0042), once the removal is saved.
            await notices.NotifyAsync(projectId, userId, null, viewer.UserId, cancellationToken);
        }

        return outcome;
    }
}
