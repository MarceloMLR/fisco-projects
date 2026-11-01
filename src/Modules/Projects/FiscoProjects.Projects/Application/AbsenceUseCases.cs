using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>Where the absences of the people are kept (ADR-0141).</summary>
public interface IAbsenceStore
{
    /// <summary>The absences of the person, not tracked.</summary>
    Task<IReadOnlyList<PersonAbsence>> OfPersonAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The absences of the people that end on the day given or after it, not tracked.</summary>
    Task<IReadOnlyList<PersonAbsence>> EndingFromAsync(IReadOnlyCollection<Guid> userIds, DateOnly day, CancellationToken cancellationToken);

    /// <summary>The absence of the person, tracked; null when the person has none with the id.</summary>
    Task<PersonAbsence?> FindAsync(Guid userId, Guid absenceId, CancellationToken cancellationToken);

    /// <summary>Removes the absences whose last day is before the day given, and says how many.</summary>
    Task<int> RemoveEndedBeforeAsync(DateOnly day, CancellationToken cancellationToken);

    void Add(PersonAbsence absence);

    void Remove(PersonAbsence absence);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>What happened to a request about the absences of a person of a project.</summary>
public enum AbsenceOutcome
{
    Done = 1,

    /// <summary>The project, or the person in it, does not exist for the person who asks.</summary>
    NotFound = 2,

    Forbidden = 3,

    /// <summary>The dates are not of a valid absence (<see cref="PersonAbsence.IsValid"/>).</summary>
    Invalid = 4,

    /// <summary>The person already has an absence on a day of the period.</summary>
    Overlap = 5,

    /// <summary>The project is completed or cancelled: its people are not managed through it (ADR-0016).</summary>
    Closed = 6,
}

/// <summary>The absences of a person, the latest end first, and whether the person who asks registers and removes them.</summary>
public sealed record MemberAbsences(IReadOnlyList<PersonAbsence> Absences, bool CanManage);

/// <summary>What happened to an absence, and its id when it was included.</summary>
public sealed record AbsenceResult(AbsenceOutcome Outcome, Guid? Id = null);

/// <summary>The project and the person of it an absence request is about, with what the person who asks may do.</summary>
internal static class AbsenceChecks
{
    /// <summary>
    /// Whether the person who asks registers and removes absences in the project, and whether it is closed; null when the
    /// project does not exist for the person who asks or the person is not in it (404).
    /// </summary>
    public static async Task<(bool MayManage, bool Closed)?> AccessAsync(
        IProjectStore projects, Guid projectId, Guid userId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null || project.RoleOf(userId) is null)
        {
            return null;
        }

        var permissions = ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        return permissions.CanView ? (permissions.CanManageAbsences, project.IsClosed) : null;
    }

    /// <summary>The refusal of a change of the absences, or null when the person who asks makes it.</summary>
    public static AbsenceOutcome? Refusal((bool MayManage, bool Closed)? access) =>
        access switch
        {
            null => AbsenceOutcome.NotFound,
            { MayManage: false } => AbsenceOutcome.Forbidden,
            { Closed: true } => AbsenceOutcome.Closed,
            _ => null,
        };
}

/// <summary>
/// The absences of a person of a project, for whoever sees the project (ADR-0141): the people of the same projects see
/// when a colleague is away. The Administrator and the manager of the open project register and remove them.
/// </summary>
public sealed class ListMemberAbsences(IProjectStore projects, IAbsenceStore absences)
{
    /// <returns>The absences, or null when the project or the person in it does not exist for the person who asks.</returns>
    public async Task<MemberAbsences?> ExecuteAsync(Guid projectId, Guid userId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        if (await AbsenceChecks.AccessAsync(projects, projectId, userId, viewer, cancellationToken) is not { } access)
        {
            return null;
        }

        var kept = await absences.OfPersonAsync(userId, cancellationToken);
        return new MemberAbsences([.. kept.OrderByDescending(absence => absence.End)], access is { MayManage: true, Closed: false });
    }
}

/// <summary>
/// Includes an absence of a person of an open project, by the Administrator or the manager of the project (ADR-0141): the
/// first and the last day, from 1 to 366 calendar days, never over another absence of the person.
/// </summary>
public sealed class AddMemberAbsence(IProjectStore projects, IAbsenceStore absences, TimeProvider clock)
{
    public async Task<AbsenceResult> ExecuteAsync(
        Guid projectId, Guid userId, DateOnly start, DateOnly end, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (AbsenceChecks.Refusal(await AbsenceChecks.AccessAsync(projects, projectId, userId, viewer, cancellationToken)) is { } refusal)
        {
            return new AbsenceResult(refusal);
        }

        if (!PersonAbsence.IsValid(start, end))
        {
            return new AbsenceResult(AbsenceOutcome.Invalid);
        }

        if ((await absences.OfPersonAsync(userId, cancellationToken)).Any(absence => absence.Overlaps(start, end)))
        {
            return new AbsenceResult(AbsenceOutcome.Overlap);
        }

        var absence = new PersonAbsence(userId, start, end, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        absences.Add(absence);
        await absences.SaveChangesAsync(cancellationToken);
        return new AbsenceResult(AbsenceOutcome.Done, absence.Id);
    }
}

/// <summary>Removes an absence of a person of an open project, by the Administrator or the manager of the project (ADR-0141).</summary>
public sealed class RemoveMemberAbsence(IProjectStore projects, IAbsenceStore absences)
{
    public async Task<AbsenceResult> ExecuteAsync(
        Guid projectId, Guid userId, Guid absenceId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        if (AbsenceChecks.Refusal(await AbsenceChecks.AccessAsync(projects, projectId, userId, viewer, cancellationToken)) is { } refusal)
        {
            return new AbsenceResult(refusal);
        }

        if (await absences.FindAsync(userId, absenceId, cancellationToken) is not { } absence)
        {
            return new AbsenceResult(AbsenceOutcome.NotFound);
        }

        absences.Remove(absence);
        await absences.SaveChangesAsync(cancellationToken);
        return new AbsenceResult(AbsenceOutcome.Done, absence.Id);
    }
}

/// <summary>
/// Removes the absences kept past a year after their end, by the day of the installation (ADR-0141), in the background.
/// </summary>
public sealed class CleanUpAbsences(IAbsenceStore absences, InstallationClock clock)
{
    /// <returns>How many absences were removed.</returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        // Kept while today is on or before a year after the end: the ones that ended before today a year ago go.
        return await absences.RemoveEndedBeforeAsync(today.AddYears(-1), cancellationToken);
    }
}
