using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>Where a project is in its life (ADR-0016). Completed and cancelled projects are closed.</summary>
public enum ProjectStatus
{
    Planning = 1,
    InProgress = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5,
}

/// <summary>The role of a person in one project (ADR-0040): one per person in each project.</summary>
public enum ProjectRole
{
    Manager = 1,
    TechLead = 2,
    Contributor = 3,
    Developer = 4,
    Viewer = 5,
}

/// <summary>A closed project only changes its status, until it is reopened (ADR-0016).</summary>
public sealed class ProjectClosedException() : InvalidOperationException("The project is closed: reopen it to change it.");

/// <summary>The person is already in the project: one role per person, changed by changing the role (ADR-0042).</summary>
public sealed class ProjectMemberExistsException() : InvalidOperationException("The person is already in the project.");

/// <summary>The change would leave the project without a manager (ADR-0042).</summary>
public sealed class LastManagerException() : InvalidOperationException("The project always has a manager.");

/// <summary>
/// A project (ADR-0016): the code, the name, the description, the client (none for an internal project), the status
/// and the people in it, each with a role. It is created with its manager and, optionally, its tech lead (ADR-0072).
/// The dates and the progress come from the schedule.
/// </summary>
public sealed class Project : Entity
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 4000;

    private readonly List<ProjectMember> _members = [];

    private Project()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    /// <summary>The code in capitals (<see cref="ProjectCode"/>).</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    /// <summary>The client the project is made for; null for an internal project.</summary>
    public Guid? ClientId { get; private set; }

    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;

    public DateTime CreatedAt { get; private init; }

    /// <summary>
    /// The folder of the project in the file storage, where every module keeps its files (ADR-0121); null until the
    /// first file. Kept by the store, outside the audit, as the ids of the folders of the other modules.
    /// </summary>
    public string? StorageFolderId { get; private set; }

    /// <summary>
    /// The name the folder was created or renamed with last, so a rename that failed is tried again, or the name of the
    /// folder someone chose.
    /// </summary>
    public string? StorageFolderName { get; private set; }

    /// <summary>
    /// Whether someone chose the folder among the ones that already existed (ADR-0122): it is never renamed with the
    /// project nor removed with it.
    /// </summary>
    public bool StorageFolderChosen { get; private set; }

    /// <summary>The name of the folder of the project: "{code} - {name}".</summary>
    public string FolderName => FolderNameOf(Code, Name);

    public IReadOnlyList<ProjectMember> Members => _members;

    /// <summary>Completed or cancelled: read-only until reopened.</summary>
    public bool IsClosed => IsClosedStatus(Status);

    public static bool IsClosedStatus(ProjectStatus status) => status is ProjectStatus.Completed or ProjectStatus.Cancelled;

    /// <summary>The name without the spaces at the ends, when it is not empty and has up to 150 characters.</summary>
    public static string? ValidName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength ? null : trimmed;
    }

    /// <summary>The description without the spaces at the ends, null when empty; false when it is too long.</summary>
    public static bool TryNormalizeDescription(string? description, out string? normalized)
    {
        var trimmed = description?.Trim();
        normalized = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        return normalized is null || normalized.Length <= DescriptionMaxLength;
    }

    /// <summary>
    /// A new project in Planning, with its manager and, when given, its tech lead, who must be another person
    /// (ADR-0072). The code comes normalized.
    /// </summary>
    public static Project Create(
        string code, string name, string? description, Guid? clientId, Guid managerId, Guid? techLeadId, DateTime createdAt)
    {
        if (techLeadId == managerId)
        {
            throw new ArgumentException("The tech lead is another person than the manager.", nameof(techLeadId));
        }

        var project = new Project { CreatedAt = createdAt };
        project.SetData(code, name, description, clientId);
        project._members.Add(new ProjectMember(project.Id, managerId, ProjectRole.Manager, createdAt));
        if (techLeadId is { } techLead)
        {
            project._members.Add(new ProjectMember(project.Id, techLead, ProjectRole.TechLead, createdAt));
        }

        return project;
    }

    /// <summary>The role of the person in the project, or null when the person is not in it.</summary>
    public ProjectRole? RoleOf(Guid userId) => MemberOf(userId)?.Role;

    /// <summary>Includes a person with a role (ADR-0042).</summary>
    /// <exception cref="ProjectClosedException">The project is completed or cancelled.</exception>
    /// <exception cref="ProjectMemberExistsException">The person is already in the project.</exception>
    public ProjectMember AddMember(Guid userId, ProjectRole role, DateTime now)
    {
        EnsureOpen();
        EnsureKnown(role);
        if (MemberOf(userId) is not null)
        {
            throw new ProjectMemberExistsException();
        }

        var member = new ProjectMember(Id, userId, role, now);
        _members.Add(member);
        return member;
    }

    /// <summary>Changes the role of a person in the project; false when the person is not in it.</summary>
    /// <exception cref="ProjectClosedException">The project is completed or cancelled.</exception>
    /// <exception cref="LastManagerException">The person is the last manager and the new role is another.</exception>
    public bool ChangeMemberRole(Guid userId, ProjectRole role)
    {
        EnsureOpen();
        EnsureKnown(role);
        if (MemberOf(userId) is not { } member)
        {
            return false;
        }

        if (role != ProjectRole.Manager)
        {
            EnsureAnotherManager(member);
        }

        member.ChangeRole(role);
        return true;
    }

    /// <summary>
    /// Removes a person from the project, who loses the access from the next request on (ADR-0042); false when the
    /// person is not in it.
    /// </summary>
    /// <exception cref="ProjectClosedException">The project is completed or cancelled.</exception>
    /// <exception cref="LastManagerException">The person is the last manager.</exception>
    public bool RemoveMember(Guid userId)
    {
        EnsureOpen();
        if (MemberOf(userId) is not { } member)
        {
            return false;
        }

        EnsureAnotherManager(member);
        _members.Remove(member);
        return true;
    }

    /// <summary>Uses a folder that already existed, already checked by the rules of the choice (ADR-0122).</summary>
    public void ChooseFolder(string folderId, string folderName)
    {
        StorageFolderId = folderId;
        StorageFolderName = folderName;
        StorageFolderChosen = true;
    }

    /// <summary>Goes back to the folder the system creates, made with the next folder a module asks for (ADR-0121).</summary>
    public void UseSystemFolder()
    {
        StorageFolderId = null;
        StorageFolderName = null;
        StorageFolderChosen = false;
    }

    /// <summary>
    /// The name of the folder of a project in the file storage (ADR-0030, ADR-0121), one folder: a "/" or "\\" of the
    /// name becomes "-", as the folder was always named.
    /// </summary>
    public static string FolderNameOf(string code, string name) =>
        $"{code} - {name}".Replace('/', '-').Replace('\\', '-');

    /// <summary>Changes the code, the name, the description and the client.</summary>
    /// <exception cref="ProjectClosedException">The project is completed or cancelled.</exception>
    public void Change(string code, string name, string? description, Guid? clientId)
    {
        EnsureOpen();
        SetData(code, name, description, clientId);
    }

    /// <summary>Moves the project to any status; from a closed one to an open one, it is reopened.</summary>
    public void ChangeStatus(ProjectStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown project status.");
        }

        Status = status;
    }

    private ProjectMember? MemberOf(Guid userId) => _members.FirstOrDefault(member => member.UserId == userId);

    private void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new ProjectClosedException();
        }
    }

    private static void EnsureKnown(ProjectRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown project role.");
        }
    }

    /// <summary>A manager leaves the role only while another manager stays (ADR-0042).</summary>
    private void EnsureAnotherManager(ProjectMember member)
    {
        if (member.Role == ProjectRole.Manager
            && !_members.Any(other => other != member && other.Role == ProjectRole.Manager))
        {
            throw new LastManagerException();
        }
    }

    private void SetData(string code, string name, string? description, Guid? clientId)
    {
        Code = ProjectCode.Normalize(code) == code
            ? code
            : throw new ArgumentException($"'{code}' is no project code.", nameof(code));
        Name = ValidName(name) ?? throw new ArgumentException(
            $"The name of the project is needed and has up to {NameMaxLength} characters.", nameof(name));
        Description = TryNormalizeDescription(description, out var normalized)
            ? normalized
            : throw new ArgumentException(
                $"The description has up to {DescriptionMaxLength} characters.", nameof(description));
        ClientId = clientId;
    }
}

/// <summary>A person in a project, with the role in it (ADR-0040, ADR-0042).</summary>
public sealed class ProjectMember : Entity
{
    private ProjectMember()
    {
    }

    internal ProjectMember(Guid projectId, Guid userId, ProjectRole role, DateTime addedAt)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        AddedAt = addedAt;
    }

    public Guid ProjectId { get; private init; }

    public Guid UserId { get; private init; }

    public ProjectRole Role { get; private set; }

    public DateTime AddedAt { get; private init; }

    internal void ChangeRole(ProjectRole role) => Role = role;
}
