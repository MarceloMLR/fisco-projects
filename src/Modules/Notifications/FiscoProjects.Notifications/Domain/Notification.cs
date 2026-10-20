using FiscoProjects.Notifications.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Notifications.Domain;

/// <summary>
/// A notification of the bell of a person (ADR-0038): what happened, who did it, the project and the address of the
/// item, with the data of the item in JSON; read or not. The name of who did it is read when the notification is shown,
/// so it is always the current one (ADR-0084), and no name is kept (ADR-0058).
/// </summary>
public sealed class Notification : Entity
{
    public const int PathMaxLength = 300;

    private Notification()
    {
        Path = string.Empty;
        Values = string.Empty;
    }

    public Notification(Guid userId, NoticeKind kind, Guid? actorId, Guid? projectId, string path, string values, DateTime now)
    {
        UserId = userId;
        Kind = kind;
        ActorId = actorId;
        ProjectId = projectId;
        Path = path;
        Values = values;
        CreatedAt = now;
    }

    public Guid UserId { get; private init; }

    public NoticeKind Kind { get; private init; }

    public Guid? ActorId { get; private init; }

    public Guid? ProjectId { get; private init; }

    public string Path { get; private init; }

    /// <summary>The data of the item, in JSON, as the notice brought it.</summary>
    public string Values { get; private init; }

    public DateTime CreatedAt { get; private init; }

    public DateTime? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public void Read(DateTime now) => ReadAt ??= now;
}
