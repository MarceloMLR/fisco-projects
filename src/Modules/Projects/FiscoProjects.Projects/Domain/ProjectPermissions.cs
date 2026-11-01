namespace FiscoProjects.Projects.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record ProjectViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do in a project, from the system role and the role in the project (ADR-0043). This is the only
/// place with the rule; the API returns it with the project, so the screens show only what the person may do.
/// </summary>
/// <param name="CanView">Sees the project, its people and everything in it.</param>
/// <param name="CanEditData">Changes the code, the name, the description and the client.</param>
/// <param name="CanChangeStatus">Pauses, completes, cancels and reopens the project.</param>
/// <param name="CanManagePeople">Includes people, changes their roles and removes them (ADR-0042).</param>
/// <param name="CanManageManagers">Does the same with the managers; the tech lead manages the people, but not them.</param>
/// <param name="CanEditSchedule">
/// Changes the schedule: phases, tasks, milestones, dependencies, owners, the progress of any task and the milestones
/// reached; everyone else only sees it.
/// </param>
/// <param name="CanUpdateOwnProgress">Updates the progress of the tasks the person is in charge of (ADR-0043).</param>
/// <param name="CanDelete">Deletes the project for good, with everything in it: only the Administrator (ADR-0041).</param>
/// <param name="CanChooseFolder">
/// Chooses the folder of the project in the file storage, while it has no files: the Administrator and the manager
/// (ADR-0122).
/// </param>
/// <param name="CanExportSchedule">
/// Downloads the schedule in the XML of the MS Project, with the e-mails of the people in charge, also of a closed
/// project: the Administrator and the manager (ADR-0136).
/// </param>
/// <param name="CanManageAbsences">
/// Registers and removes the absences of the people of the project: the Administrator and the manager (ADR-0141).
/// </param>
public sealed record ProjectPermissions(
    bool CanView,
    bool CanEditData,
    bool CanChangeStatus,
    bool CanManagePeople,
    bool CanManageManagers,
    bool CanEditSchedule,
    bool CanUpdateOwnProgress,
    bool CanDelete = false,
    bool CanChooseFolder = false,
    bool CanExportSchedule = false,
    bool CanManageAbsences = false)
{
    private static readonly ProjectPermissions All = new(true, true, true, true, true, true, true, true, true, true, true);
    private static readonly ProjectPermissions None = new(false, false, false, false, false, false, false);

    public static ProjectPermissions Of(ProjectViewer viewer, ProjectRole? role)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (viewer.IsAdministrator)
        {
            return All;
        }

        return role switch
        {
            ProjectRole.Manager => All with { CanDelete = false },
            ProjectRole.TechLead => None with
            {
                CanView = true,
                CanEditData = true,
                CanManagePeople = true,
                CanUpdateOwnProgress = true,
            },
            ProjectRole.Contributor or ProjectRole.Developer => None with { CanView = true, CanUpdateOwnProgress = true },
            ProjectRole.Viewer => None with { CanView = true },
            _ => None,
        };
    }

    /// <summary>
    /// Whether the person may include (no current role), change or remove (no new role) a person of the project: the
    /// tech lead does it with everyone but the managers, and never makes someone a manager (ADR-0042).
    /// </summary>
    public bool MayManage(ProjectRole? current, ProjectRole? next) =>
        CanManagePeople && (CanManageManagers || (current != ProjectRole.Manager && next != ProjectRole.Manager));

    /// <summary>
    /// Whether the person may update the progress of a task: the Administrator and the manager of any task, the tech
    /// lead, the contributor and the developer only of the ones they are in charge of (ADR-0043).
    /// </summary>
    public bool MayUpdateProgress(bool isOwner) => CanEditSchedule || (CanUpdateOwnProgress && isOwner);
}
