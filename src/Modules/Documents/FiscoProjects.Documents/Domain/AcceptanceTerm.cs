using FiscoProjects.SharedKernel;

namespace FiscoProjects.Documents.Domain;

/// <summary>Where a term stands (ADR-0035): waiting for the answer, signed, or refused with a reason.</summary>
public enum AcceptanceStatus
{
    Pending = 1,
    Signed = 2,
    Refused = 3,
}

/// <summary>What happened to a term, for its history (ADR-0095).</summary>
public enum AcceptanceEventKind
{
    /// <summary>The term was asked for, by e-mail and in the bell.</summary>
    Requested = 1,

    /// <summary>The person was reminded of the pending request.</summary>
    Reminded = 2,

    /// <summary>The person sent the signed term.</summary>
    Signed = 3,

    /// <summary>The Administrator or the manager attached the signed term received outside the system.</summary>
    Attached = 4,

    /// <summary>The person refused the term, with a reason.</summary>
    Refused = 5,
}

/// <summary>The fields of a term, to tell which ones are not valid.</summary>
public enum AcceptanceField
{
    Milestone = 1,
    Recipient = 2,
    Title = 3,
    Message = 4,
    Reason = 5,
    File = 6,
}

/// <summary>A file kept in the storage: its id, the name and the size.</summary>
public sealed record AcceptanceFile(string Id, string Name, long Size);

/// <summary>
/// An acceptance term of a milestone of the schedule (ADR-0035, ADR-0095): asked of a person of the project with the
/// term to sign in PDF, and answered once, with the signed PDF, which becomes a document of the folder of the terms, or
/// with a reason to refuse it. The Administrator and the manager attach a term signed outside the system, in a request
/// or without one, always in the name of a person of the project. A cancelled request is removed (ADR-0095).
/// </summary>
public sealed class AcceptanceTerm : Entity
{
    public const int TitleMaxLength = 120;
    public const int MessageMaxLength = 2_000;
    public const int ReasonMaxLength = 1_000;

    private readonly List<AcceptanceEvent> _events = [];

    private AcceptanceTerm()
    {
        Title = string.Empty;
    }

    private AcceptanceTerm(Guid projectId, Guid milestoneId, string title, string? message, Guid recipientId, Guid userId, DateTime now)
    {
        ProjectId = projectId;
        MilestoneId = milestoneId;
        Title = title;
        Message = message;
        RecipientId = recipientId;
        RequestedById = userId;
        RequestedAt = now;
        Status = AcceptanceStatus.Pending;
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The milestone of the schedule the term accepts.</summary>
    public Guid MilestoneId { get; private init; }

    public string Title { get; private init; }

    public string? Message { get; private init; }

    /// <summary>The person of the project who answers the request, or in whose name the signed term was attached.</summary>
    public Guid RecipientId { get; private init; }

    public AcceptanceStatus Status { get; private set; }

    /// <summary>Who asked for the term, and when; for a term attached without a request, who attached it.</summary>
    public Guid RequestedById { get; private init; }

    public DateTime RequestedAt { get; private init; }

    /// <summary>The id of the term to sign in the storage; null for a term attached without a request.</summary>
    public string? FileId { get; private init; }

    public string? FileName { get; private init; }

    public long? FileSize { get; private init; }

    /// <summary>The document of the signed term, in the folder of the terms (ADR-0093).</summary>
    public Guid? DocumentId { get; private set; }

    /// <summary>Who answered (sent, attached or refused) and when.</summary>
    public Guid? AnsweredById { get; private set; }

    public DateTime? AnsweredAt { get; private set; }

    public string? RefusalReason { get; private set; }

    /// <summary>The history, when read with the term, oldest first.</summary>
    public IReadOnlyList<AcceptanceEvent> Events => _events;

    /// <summary>Whether the term was asked for in the system, with a term to sign.</summary>
    public bool IsRequest => FileId is not null;

    /// <summary>A request of a person of the project, pending, with the term to sign kept in the storage.</summary>
    public static AcceptanceTerm Request(
        Guid projectId, Guid milestoneId, AcceptanceForm form, Guid recipientId, AcceptanceFile file, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(file);
        var term = new AcceptanceTerm(projectId, milestoneId, form.Title, form.Message, recipientId, userId, now)
        {
            FileId = file.Id,
            FileName = file.Name,
            FileSize = file.Size,
        };
        term._events.Add(new AcceptanceEvent(term.Id, AcceptanceEventKind.Requested, userId, now));
        return term;
    }

    /// <summary>A term signed outside the system, attached by the Administrator or the manager without a request (ADR-0095).</summary>
    public static AcceptanceTerm Attach(
        Guid projectId, Guid milestoneId, string title, Guid recipientId, Guid documentId, Guid userId, DateTime now)
    {
        var term = new AcceptanceTerm(projectId, milestoneId, title, null, recipientId, userId, now);
        term.Answer(AcceptanceStatus.Signed, userId, now);
        term.DocumentId = documentId;
        term._events.Add(new AcceptanceEvent(term.Id, AcceptanceEventKind.Attached, userId, now));
        return term;
    }

    /// <summary>
    /// The signed term of a pending request, sent by the person or attached in the name of the person by someone else.
    /// </summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public void Sign(Guid documentId, Guid userId, DateTime now)
    {
        Answer(AcceptanceStatus.Signed, userId, now);
        DocumentId = documentId;
        var kind = userId == RecipientId ? AcceptanceEventKind.Signed : AcceptanceEventKind.Attached;
        _events.Add(new AcceptanceEvent(Id, kind, userId, now));
    }

    /// <summary>The person refuses the pending request, with the reason; the milestone stays open.</summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public void Refuse(string reason, Guid userId, DateTime now)
    {
        Answer(AcceptanceStatus.Refused, userId, now);
        RefusalReason = reason;
        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Refused, userId, now));
    }

    /// <summary>The person is reminded of the pending request.</summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public void Remind(Guid userId, DateTime now)
    {
        EnsurePending();
        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Reminded, userId, now));
    }

    /// <summary>A reason with something in it, up to 1,000 characters, trimmed; null when it is not valid.</summary>
    public static string? ReasonOf(string? reason) =>
        reason?.Trim() is { Length: > 0 and <= ReasonMaxLength } trimmed ? trimmed : null;

    private void Answer(AcceptanceStatus status, Guid userId, DateTime now)
    {
        EnsurePending();
        Status = status;
        AnsweredById = userId;
        AnsweredAt = now;
    }

    private void EnsurePending()
    {
        if (Status != AcceptanceStatus.Pending)
        {
            throw new InvalidOperationException("The request was answered.");
        }
    }
}

/// <summary>An event of the history of a term: what happened, who did it and when.</summary>
public sealed class AcceptanceEvent : Entity
{
    private AcceptanceEvent()
    {
    }

    internal AcceptanceEvent(Guid termId, AcceptanceEventKind kind, Guid actorId, DateTime at)
    {
        TermId = termId;
        Kind = kind;
        ActorId = actorId;
        At = at;
    }

    public Guid TermId { get; private init; }

    public AcceptanceEventKind Kind { get; private init; }

    public Guid ActorId { get; private init; }

    public DateTime At { get; private init; }
}

/// <summary>The title and the message of a request, trimmed and checked (ADR-0035).</summary>
public sealed record AcceptanceForm(string Title, string? Message)
{
    /// <summary>
    /// The form when the title has something in it, up to 120 characters, and the message up to 2,000 (empty is none);
    /// otherwise the fields that are not valid.
    /// </summary>
    public static (AcceptanceForm? Form, IReadOnlyList<AcceptanceField> Invalid) Of(string? title, string? message)
    {
        var invalid = new List<AcceptanceField>();
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is 0 or > AcceptanceTerm.TitleMaxLength || trimmedTitle.Any(char.IsControl))
        {
            invalid.Add(AcceptanceField.Title);
        }

        var trimmedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        if (trimmedMessage is { Length: > AcceptanceTerm.MessageMaxLength })
        {
            invalid.Add(AcceptanceField.Message);
        }

        return invalid.Count == 0 ? (new AcceptanceForm(trimmedTitle, trimmedMessage), []) : (null, invalid);
    }
}
