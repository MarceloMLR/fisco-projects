using FiscoProjects.Notifications.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Notifications.Domain;

/// <summary>
/// A notice waiting for the daily summary of a person (ADR-0037): the kind, who did it, the address of the item and its
/// data, in JSON. A later notice with the same key for the same person replaces it, so the summary shows the newest
/// state of the item, as the last dates of a task.
/// </summary>
public sealed class DigestItem : Entity
{
    public const int KeyMaxLength = 120;
    public const int PathMaxLength = 300;

    private DigestItem()
    {
        Path = string.Empty;
        Values = string.Empty;
    }

    public DigestItem(Guid userId, NoticeKind kind, Guid? actorId, string path, string values, string? key, DateTime now)
    {
        UserId = userId;
        Kind = kind;
        ActorId = actorId;
        Path = path;
        Values = values;
        Key = key;
        CreatedAt = now;
    }

    public Guid UserId { get; private init; }

    public NoticeKind Kind { get; private set; }

    public Guid? ActorId { get; private set; }

    public string Path { get; private set; }

    /// <summary>The data of the item, in JSON, as the notice brought it.</summary>
    public string Values { get; private set; }

    public string? Key { get; private init; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>The newer notice of the same item takes the place of this one.</summary>
    public void Replace(NoticeKind kind, Guid? actorId, string path, string values, DateTime now)
    {
        Kind = kind;
        ActorId = actorId;
        Path = path;
        Values = values;
        CreatedAt = now;
    }
}

/// <summary>
/// The daily summary of one day of the installation (ADR-0037): the day is the key, so only one process sends it, even
/// with the application restarted or in more than one instance.
/// </summary>
public sealed class DailySummaryRun
{
    private DailySummaryRun()
    {
    }

    public DailySummaryRun(DateOnly day, DateTime startedAt)
    {
        Day = day;
        StartedAt = startedAt;
    }

    public DateOnly Day { get; private init; }

    public DateTime StartedAt { get; private init; }

    /// <summary>How many summaries were put in the queue.</summary>
    public int Emails { get; private set; }

    /// <summary>When the summaries of the day were in the queue; null while they are put there.</summary>
    public DateTime? FinishedAt { get; private set; }

    public void Finish(int emails, DateTime now)
    {
        Emails = emails;
        FinishedAt = now;
    }
}
