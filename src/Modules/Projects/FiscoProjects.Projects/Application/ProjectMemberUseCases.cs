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
/// sign-in (null for who never signed in) and whether the person may be a manager (ADR-0085).
/// </summary>
public sealed record ProjectMemberItem(
    Guid Id, string Name, string Email, ProjectRole Role, DateTime AddedAt, DateTime? LastSignInAt, bool CanBeManager);

/// <summary>The people of a project, by name, for whoever sees the project (ADR-0043).</summary>
public sealed class ListProjectMembers(IProjectStore store, IUserDirectory users)
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
        return [.. project.Members
            .Select(member => people.TryGetValue(member.UserId, out var person)
                ? new ProjectMemberItem(
                    member.UserId, person.Name, person.Email, member.Role, member.AddedAt, person.LastSignInAt, person.CanBeManager)
                : new ProjectMemberItem(member.UserId, string.Empty, string.Empty, member.Role, member.AddedAt, null, false))
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

    /// <summary>Only an active Administrator or Project manager is a manager of a project (ADR-0085).</summary>
    public static async Task<bool> MayBeManagerAsync(IUserAccess access, Guid userId, CancellationToken cancellationToken) =>
        await access.FindAsync(userId, cancellationToken) is { IsActive: true } person && SystemRoles.ManagesProjects(person.Role);
}

/// <summary>
/// Includes a person in a project with a role (ADR-0042): the Administrator and the manager include anyone, the tech
/// lead anyone but managers (ADR-0043). A manager has the system role to be one (ADR-0085). A person of the directory
/// who never signed in becomes a user first (ADR-0005), only after the permission and the role are checked, so a
/// refused request creates nobody.
/// </summary>
public sealed class AddProjectMember(IProjectStore store, IUserAccess access, IUserProvisioning provisioning, TimeProvider clock)
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

            if (form.Role == ProjectRole.Manager && !await MemberChecks.MayBeManagerAsync(access, id, cancellationToken))
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
            // Who never signed in has the role User, so is never a manager.
            if (form.Role == ProjectRole.Manager)
            {
                return new(MemberOutcome.InvalidRole);
            }

            if (await provisioning.UserFromDirectoryAsync(form.DirectoryId!, cancellationToken) is not { } created)
            {
                return new(MemberOutcome.InvalidPerson);
            }

            userId = created;
        }

        return await store.InSerializableTransactionAsync<MemberAdded>(async token =>
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
            return new(MemberOutcome.Done, userId);
        }, cancellationToken);
    }
}

/// <summary>
/// Changes the role of a person in a project (ADR-0042): the tech lead does not change the role of a manager nor makes
/// someone a manager (ADR-0043); a manager has the system role to be one (ADR-0085); the last manager keeps the role.
/// Run in a serializable transaction, so two changes at once never leave the project without a manager.
/// </summary>
public sealed class ChangeProjectMemberRole(IProjectStore store, IUserAccess access)
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
            || (role == ProjectRole.Manager && currentRole != ProjectRole.Manager
                && !await MemberChecks.MayBeManagerAsync(access, userId, cancellationToken)))
        {
            return MemberOutcome.InvalidRole;
        }

        if (role == currentRole)
        {
            return MemberOutcome.Done;
        }

        return await store.InSerializableTransactionAsync(async token =>
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

            await store.SaveChangesAsync(token);
            return MemberOutcome.Done;
        }, cancellationToken);
    }
}

/// <summary>
/// Removes a person from a project, who stops seeing it from the next request on (ADR-0042); what the person did keeps
/// the name. The tech lead does not remove managers (ADR-0043), and the last manager stays. Run in a serializable
/// transaction, so two removals at once never leave the project without a manager.
/// </summary>
public sealed class RemoveProjectMember(IProjectStore store)
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

        return await store.InSerializableTransactionAsync(async token =>
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

            await store.SaveChangesAsync(token);
            return MemberOutcome.Done;
        }, cancellationToken);
    }
}
