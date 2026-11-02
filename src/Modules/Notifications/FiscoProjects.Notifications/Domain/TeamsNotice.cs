using FiscoProjects.Notifications.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Notifications.Domain;

/// <summary>Where a notice of the queue of Teams stands (ADR-0149).</summary>
public enum TeamsNoticeStatus
{
    /// <summary>In the queue, waiting for its turn or for another try.</summary>
    Pending = 1,

    Sent = 2,

    /// <summary>Teams did not take it, for good or after the last try; it is not tried again.</summary>
    Failed = 3,
}

/// <summary>
/// A notice of the queue of Teams (ADR-0149): written with the action that causes it, as the e-mails (ADR-0036), and
/// sent in the background to the Activity of Teams of the person. It keeps who did it by the id, never by the name, so
/// the text says the current name when it leaves. When Teams does not take it, it stays failed: the e-mail goes only to
/// who has the e-mail on, and the bell has it anyway (ADR-0151).
/// </summary>
public sealed class TeamsNotice : Entity
{
    public const int PathMaxLength = DigestItem.PathMaxLength;
    public const int ErrorMaxLength = OutboxEmail.ErrorMaxLength;

    private TeamsNotice()
    {
        Path = string.Empty;
        Values = string.Empty;
    }

    public TeamsNotice(
        Guid userId, NoticeKind kind, Guid? actorId, Guid? projectId, string path, string values, DateTime now)
    {
        UserId = userId;
        Kind = kind;
        ActorId = actorId;
        ProjectId = projectId;
        Path = path;
        Values = values;
        Status = TeamsNoticeStatus.Pending;
        CreatedAt = now;
        NextAttemptAt = now;
    }

    public Guid UserId { get; private init; }

    public NoticeKind Kind { get; private init; }

    public Guid? ActorId { get; private init; }

    /// <summary>The project of the item, so the notice leaves with the project (ADR-0111).</summary>
    public Guid? ProjectId { get; private init; }

    public string Path { get; private init; }

    /// <summary>The data of the item, in JSON, as the notice brought it.</summary>
    public string Values { get; private init; }

    public TeamsNoticeStatus Status { get; private set; }

    /// <summary>How many times it was taken to be sent.</summary>
    public int Attempts { get; private set; }

    public DateTime CreatedAt { get; private init; }

    /// <summary>When it may be taken again: its turn, the end of a reservation or the wait after a failure.</summary>
    public DateTime NextAttemptAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    /// <summary>The error of the last failure, without the person nor the texts.</summary>
    public string? LastError { get; private set; }

    public bool IsDue(DateTime now) => Status == TeamsNoticeStatus.Pending && NextAttemptAt <= now;

    /// <summary>Takes the notice to send it: one more try, and no other process takes it while the reservation lasts.</summary>
    public void Reserve(DateTime now)
    {
        if (!IsDue(now))
        {
            throw new InvalidOperationException("Only a notice of the queue whose turn came is taken to be sent.");
        }

        Attempts++;
        NextAttemptAt = now + OutboxEmail.Reservation;
    }

    public void MarkSent(DateTime now)
    {
        Status = TeamsNoticeStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    /// <summary>The try failed for a while, as Teams throttling or out of service: another try later, or failed after the last.</summary>
    public void MarkFailed(DateTime now, string error)
    {
        LastError = Cut(error);
        if (Attempts >= OutboxEmail.MaxAttempts)
        {
            Status = TeamsNoticeStatus.Failed;
            FailedAt = now;
            return;
        }

        NextAttemptAt = now + OutboxEmail.Retries[Math.Clamp(Attempts - 1, 0, OutboxEmail.Retries.Count - 1)];
    }

    /// <summary>Teams did not take it for good, as the application not installed for the person: no other try.</summary>
    public void Refuse(DateTime now, string error)
    {
        LastError = Cut(error);
        Status = TeamsNoticeStatus.Failed;
        FailedAt = now;
    }

    private static string Cut(string error) => error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength];
}
