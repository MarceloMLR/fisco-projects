using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.TimeTracking.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record TimeEntryViewer(Guid UserId, bool IsAdministrator);

/// <summary>Why a person may not log or change hours in a project.</summary>
public enum TimeEntryRefusal
{
    /// <summary>The person does not see the project (404).</summary>
    NotSeen = 1,

    /// <summary>The person sees the project but does not log hours in it, as the viewer (403).</summary>
    Forbidden = 2,

    /// <summary>The project is completed or cancelled, and read-only (409, ADR-0016).</summary>
    Closed = 3,
}

/// <summary>Who logs, sees and changes the hours (ADR-0128), in one place.</summary>
public static class TimeEntryAccess
{
    /// <summary>
    /// The project as the person sees it: everyone in it and the Administrator, who sees every project; the role is
    /// null for whoever is not in it.
    /// </summary>
    public static bool Sees(TimeEntryViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        return viewer.IsAdministrator || project.Role is not null;
    }

    /// <summary>
    /// Why the person may not log hours in the project, or null when the person may: the Administrator in any project,
    /// the people of the project but the viewer, only while it is open.
    /// </summary>
    public static TimeEntryRefusal? RefusalToLog(TimeEntryViewer viewer, ProjectAccess? project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (project is null || !Sees(viewer, project))
        {
            return TimeEntryRefusal.NotSeen;
        }

        if (!viewer.IsAdministrator && project.Role == ProjectMemberRole.Viewer)
        {
            return TimeEntryRefusal.Forbidden;
        }

        return project.IsClosed ? TimeEntryRefusal.Closed : null;
    }

    /// <summary>The entries of another person are seen only by the Administrator.</summary>
    public static bool SeesEntry(TimeEntryViewer viewer, TimeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(entry);
        return viewer.IsAdministrator || entry.UserId == viewer.UserId;
    }

    /// <summary>
    /// The hours of the tasks of a project and the entries of each task (ADR-0138, ADR-0144): the Administrator, in any
    /// project, and the manager of the project, in the projects the person manages, of any status.
    /// </summary>
    public static bool SeesTaskHours(TimeEntryViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        return viewer.IsAdministrator || project.Role == ProjectMemberRole.Manager;
    }

    /// <summary>The planned hours of the tasks are put, changed and taken out only by the Administrator (ADR-0138).</summary>
    public static bool ChangesPlannedHours(TimeEntryViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.IsAdministrator;
    }

    /// <summary>
    /// Whether the person may change and remove an entry of a list: an entry the person sees, of another person only for
    /// the Administrator, by the rules of logging in its project. A list of a task shows the entries of everyone to its
    /// manager, who still changes only the own ones (ADR-0144).
    /// </summary>
    public static bool MayChange(TimeEntryViewer viewer, TimeEntry entry, ProjectAccess? project) =>
        SeesEntry(viewer, entry) && RefusalToChange(viewer, entry, project) is null;

    /// <summary>
    /// Why the person may not change or remove an entry the person sees, or null when the person may: the rules of
    /// logging in the project of the entry. Whoever left the project, or became a viewer, no longer changes the
    /// entries in it; they stay (403).
    /// </summary>
    public static TimeEntryRefusal? RefusalToChange(TimeEntryViewer viewer, TimeEntry entry, ProjectAccess? project)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return RefusalToLog(viewer, project) switch
        {
            TimeEntryRefusal.NotSeen when SeesEntry(viewer, entry) => TimeEntryRefusal.Forbidden,
            var refusal => refusal,
        };
    }
}
