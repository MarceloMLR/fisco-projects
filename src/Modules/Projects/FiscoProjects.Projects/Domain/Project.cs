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
    public ProjectRole? RoleOf(Guid userId) => _members.FirstOrDefault(member => member.UserId == userId)?.Role;

    /// <summary>Changes the code, the name, the description and the client.</summary>
    /// <exception cref="ProjectClosedException">The project is completed or cancelled.</exception>
    public void Change(string code, string name, string? description, Guid? clientId)
    {
        if (IsClosed)
        {
            throw new ProjectClosedException();
        }

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
}
