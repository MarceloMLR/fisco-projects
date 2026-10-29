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
