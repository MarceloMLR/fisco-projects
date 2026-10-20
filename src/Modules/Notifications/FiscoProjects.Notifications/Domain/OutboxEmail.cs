using FiscoProjects.SharedKernel;

namespace FiscoProjects.Notifications.Domain;

/// <summary>Where an e-mail of the queue stands (ADR-0036).</summary>
public enum OutboxEmailStatus
{
    /// <summary>In the queue, waiting for its turn or for another try.</summary>
    Pending = 1,

    Sent = 2,

    /// <summary>Failed every try; it is not tried again.</summary>
    Failed = 3,
}

/// <summary>
/// An e-mail of the queue (ADR-0036): written with the action that causes it and sent in the background, so the action
/// never depends on the sending. A failure is tried again later; after the last try the e-mail stays failed. After the
/// days the installation keeps them, the content is forgotten and only the day and the kind stay (ADR-0058).
/// </summary>
public sealed class OutboxEmail : Entity
{
    public const int KindMaxLength = 40;
    public const int AddressMaxLength = 320;
    public const int NameMaxLength = 200;
    public const int SubjectMaxLength = 300;
    public const int ErrorMaxLength = 500;

    /// <summary>How many times an e-mail is tried before it stays failed.</summary>
    public const int MaxAttempts = 6;

    /// <summary>How long a reservation lasts: a process that stops while sending lets another try after it.</summary>
    public static readonly TimeSpan Reservation = TimeSpan.FromMinutes(10);

    /// <summary>The wait after each failure, before the next try.</summary>
    public static readonly IReadOnlyList<TimeSpan> Retries =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4),
    ];

    private OutboxEmail()
    {
        Kind = string.Empty;
    }

    public OutboxEmail(string kind, string toAddress, string toName, string subject, string htmlBody, string textBody, DateTime now)
    {
        Kind = kind;
        ToAddress = toAddress;
        ToName = toName;
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        Status = OutboxEmailStatus.Pending;
        CreatedAt = now;
        NextAttemptAt = now;
    }

    /// <summary>What the e-mail is about, as Test, kept after the content is forgotten.</summary>
    public string Kind { get; private init; }

    public string? ToAddress { get; private set; }

    public string? ToName { get; private set; }

    public string? Subject { get; private set; }

    public string? HtmlBody { get; private set; }

    public string? TextBody { get; private set; }

    public OutboxEmailStatus Status { get; private set; }

    /// <summary>How many times it was taken to be sent.</summary>
    public int Attempts { get; private set; }

    public DateTime CreatedAt { get; private init; }

    /// <summary>When it may be taken again: its turn, the end of a reservation or the wait after a failure.</summary>
    public DateTime NextAttemptAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    /// <summary>The error of the last failure, without the content of the e-mail.</summary>
    public string? LastError { get; private set; }

    /// <summary>When the content was forgotten (ADR-0058).</summary>
    public DateTime? ForgottenAt { get; private set; }

    public bool IsDue(DateTime now) => Status == OutboxEmailStatus.Pending && NextAttemptAt <= now;

    /// <summary>Takes the e-mail to send it: one more try, and no other process takes it while the reservation lasts.</summary>
    public void Reserve(DateTime now)
    {
        if (!IsDue(now))
        {
            throw new InvalidOperationException("Only an e-mail of the queue whose turn came is taken to be sent.");
        }

        Attempts++;
        NextAttemptAt = now + Reservation;
    }

    public void MarkSent(DateTime now)
    {
        Status = OutboxEmailStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    /// <summary>The try failed: the e-mail waits for the next one, or stays failed after the last.</summary>
    public void MarkFailed(DateTime now, string error)
    {
        LastError = error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength];
        if (Attempts >= MaxAttempts)
        {
            Status = OutboxEmailStatus.Failed;
            FailedAt = now;
            return;
        }

        NextAttemptAt = now + Retries[Math.Clamp(Attempts - 1, 0, Retries.Count - 1)];
    }

    /// <summary>Forgets the recipient and the content, keeping the day, the kind and the status (ADR-0058).</summary>
    public void Forget(DateTime now)
    {
        ToAddress = null;
        ToName = null;
        Subject = null;
        HtmlBody = null;
        TextBody = null;
        LastError = null;
        ForgottenAt = now;
    }
}
