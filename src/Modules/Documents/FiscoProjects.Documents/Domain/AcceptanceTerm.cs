using System.Net.Mail;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Documents.Domain;

/// <summary>Where a term stands (ADR-0129): waiting for the signatures, signed, or refused with a reason.</summary>
public enum AcceptanceStatus
{
    Pending = 1,
    Signed = 2,
    Refused = 3,
}

/// <summary>Where a signer of a request stands at the provider of signatures (ADR-0129).</summary>
public enum AcceptanceSignerStatus
{
    Pending = 1,
    Signed = 2,
    Refused = 3,
}

/// <summary>What happened to a term, for its history (ADR-0095, ADR-0129).</summary>
public enum AcceptanceEventKind
{
    /// <summary>The term was asked for: of the signers, at the provider; before, of a person of the project.</summary>
    Requested = 1,

    /// <summary>The signers who did not sign were reminded; before, the person of the request.</summary>
    Reminded = 2,

    /// <summary>A signer signed at the provider; before, the person of the request sent the signed term.</summary>
    Signed = 3,

    /// <summary>The Administrator or the manager attached the signed term received outside the system.</summary>
    Attached = 4,

    /// <summary>A signer refused at the provider, with a reason; before, the person of the request.</summary>
    Refused = 5,
}

/// <summary>The fields of a term, to tell which ones are not valid.</summary>
public enum AcceptanceField
{
    Milestone = 1,
    Title = 3,
    Message = 4,
    File = 6,
    Signers = 7,
}

/// <summary>A file kept in the storage: its id, the name and the size.</summary>
public sealed record AcceptanceFile(string Id, string Name, long Size);

/// <summary>Where a request went: the provider, the id of its document there and the id of each signer, in order.</summary>
public sealed record AcceptanceDelivery(string Provider, string DocumentId, IReadOnlyList<string> SignerIds);

/// <summary>
/// An acceptance term of a milestone of the schedule (ADR-0129): asked of 1 to 10 signers, by name and e-mail, at the
/// provider of signatures, with the term to sign in PDF. When every signer signs, the signed PDF of the provider becomes
/// a document of the folder of the terms; a refusal of one signer refuses the term. The Administrator and the manager
/// attach a term signed outside the system, in a request or without one. A cancelled request is removed (ADR-0095).
/// The terms asked of a person of the project before keep the person (<see cref="RecipientId"/>) and no provider.
/// </summary>
public sealed class AcceptanceTerm : Entity
{
    public const int TitleMaxLength = 120;
    public const int MessageMaxLength = 2_000;
    public const int ReasonMaxLength = 1_000;

    private readonly List<AcceptanceEvent> _events = [];
    private readonly List<AcceptanceSigner> _signers = [];

    private AcceptanceTerm()
    {
        Title = string.Empty;
    }

    private AcceptanceTerm(Guid projectId, Guid milestoneId, string title, string? message, Guid userId, DateTime now)
    {
        ProjectId = projectId;
        MilestoneId = milestoneId;
        Title = title;
        Message = message;
        RequestedById = userId;
        RequestedAt = now;
        Status = AcceptanceStatus.Pending;
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The milestone of the schedule the term accepts.</summary>
    public Guid MilestoneId { get; private init; }

    public string Title { get; private init; }

    public string? Message { get; private init; }

    /// <summary>The person of the project a term was asked of before the provider of signatures (ADR-0095); null since.</summary>
    public Guid? RecipientId { get; private init; }

    public AcceptanceStatus Status { get; private set; }

    /// <summary>Who asked for the term, and when; for a term attached without a request, who attached it.</summary>
    public Guid RequestedById { get; private init; }

    public DateTime RequestedAt { get; private init; }

    /// <summary>The id of the term to sign in the storage; null for a term attached without a request.</summary>
    public string? FileId { get; private init; }

    public string? FileName { get; private init; }

    public long? FileSize { get; private init; }

    /// <summary>The provider of signatures that has the request, as the configuration names it; null without one.</summary>
    public string? SignatureProvider { get; private init; }

    /// <summary>The id of the document of the request at the provider.</summary>
    public string? ProviderDocumentId { get; private init; }

    /// <summary>The document of the signed term, in the folder of the terms (ADR-0093).</summary>
    public Guid? DocumentId { get; private set; }

    /// <summary>Who answered in the system (attached, or, before, sent or refused); null when the signers answered.</summary>
    public Guid? AnsweredById { get; private set; }

    public DateTime? AnsweredAt { get; private set; }

    public string? RefusalReason { get; private set; }

    /// <summary>The signers of a request at the provider, in the order they were typed.</summary>
    public IReadOnlyList<AcceptanceSigner> Signers => _signers;

    /// <summary>The history, when read with the term, oldest first.</summary>
    public IReadOnlyList<AcceptanceEvent> Events => _events;

    /// <summary>Whether the term was asked for in the system, with a term to sign.</summary>
    public bool IsRequest => FileId is not null;

    /// <summary>Whether the request is at a provider of signatures (ADR-0129), not one asked of a person before.</summary>
    public bool IsSignatureRequest => SignatureProvider is not null;

    /// <summary>
    /// A request of the signers at the provider, pending, with the term to sign kept in the storage; the signers are
    /// asked in the order typed, each with the id the provider gave.
    /// </summary>
    public static AcceptanceTerm Request(
        Guid projectId,
        Guid milestoneId,
        AcceptanceForm form,
        IReadOnlyList<AcceptanceSignerForm> signers,
        AcceptanceFile file,
        AcceptanceDelivery delivery,
        Guid userId,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(signers);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(delivery);
        if (signers.Count != delivery.SignerIds.Count)
        {
            throw new ArgumentException("The provider gives one id to each signer.", nameof(delivery));
        }

        var term = new AcceptanceTerm(projectId, milestoneId, form.Title, form.Message, userId, now)
        {
            FileId = file.Id,
            FileName = file.Name,
            FileSize = file.Size,
            SignatureProvider = delivery.Provider,
            ProviderDocumentId = delivery.DocumentId,
        };
        for (var index = 0; index < signers.Count; index++)
        {
            term._signers.Add(new AcceptanceSigner(term.Id, index + 1, signers[index], delivery.SignerIds[index]));
        }

        term._events.Add(new AcceptanceEvent(term.Id, AcceptanceEventKind.Requested, userId, null, now));
        return term;
    }

    /// <summary>A term signed outside the system, attached by the Administrator or the manager without a request (ADR-0129).</summary>
    public static AcceptanceTerm Attach(Guid projectId, Guid milestoneId, string title, Guid documentId, Guid userId, DateTime now)
    {
        var term = new AcceptanceTerm(projectId, milestoneId, title, null, userId, now);
        term.Close(AcceptanceStatus.Signed, now);
        term.AnsweredById = userId;
        term.DocumentId = documentId;
        term._events.Add(new AcceptanceEvent(term.Id, AcceptanceEventKind.Attached, userId, null, now));
        return term;
    }

    /// <summary>
    /// The signed term of a pending request, received outside the system and attached by the Administrator or the
    /// manager; the signers who did not sign stay as they were, without the e-mail.
    /// </summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public void Sign(Guid documentId, Guid userId, DateTime now)
    {
        EnsurePending();
        Close(AcceptanceStatus.Signed, now);
        AnsweredById = userId;
        DocumentId = documentId;
        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Attached, userId, null, now));
    }

    /// <summary>
    /// A signer of the pending request signed at the provider, at the moment it tells; false when the signer had
    /// answered, so the same signature is kept once (ADR-0129).
    /// </summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public bool SignerSigned(Guid signerId, DateTime at)
    {
        EnsurePending();
        var signer = SignerOf(signerId);
        if (signer.Status != AcceptanceSignerStatus.Pending)
        {
            return false;
        }

        signer.Answer(AcceptanceSignerStatus.Signed, at, null);
        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Signed, null, signer.Id, at));
        return true;
    }

    /// <summary>Whether every signer of the request signed, and the signed PDF of the provider is what is missing.</summary>
    public bool AllSigned => _signers.Count > 0 && _signers.TrueForAll(signer => signer.Status == AcceptanceSignerStatus.Signed);

    /// <summary>
    /// A signer of the pending request refused at the provider, with the reason: the term is refused, and the e-mails of
    /// the signers are forgotten (ADR-0129); false when the signer had answered.
    /// </summary>
    /// <exception cref="InvalidOperationException">The request was answered.</exception>
    public bool SignerRefused(Guid signerId, string? reason, DateTime at)
    {
        EnsurePending();
        var signer = SignerOf(signerId);
        if (signer.Status != AcceptanceSignerStatus.Pending)
        {
            return false;
        }

        // The reason as the provider tells it, up to the length of a reason; none when it tells none.
        var valid = reason?.Trim() is { Length: > 0 } trimmed ? trimmed[..Math.Min(trimmed.Length, ReasonMaxLength)] : null;
        signer.Answer(AcceptanceSignerStatus.Refused, at, valid);
        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Refused, null, signer.Id, at));
        Close(AcceptanceStatus.Refused, at);
        RefusalReason = valid;
        return true;
    }

    /// <summary>Every signer signed: the signed PDF of the provider is the document of the term, and the e-mails are forgotten.</summary>
    /// <exception cref="InvalidOperationException">The request was answered, or a signer did not sign.</exception>
    public void CompleteSigning(Guid documentId, DateTime now)
    {
        EnsurePending();
        if (!AllSigned)
        {
            throw new InvalidOperationException("A signer did not sign.");
        }

        Close(AcceptanceStatus.Signed, now);
        DocumentId = documentId;
    }

    /// <summary>The signers who did not sign are reminded, by the provider.</summary>
    /// <exception cref="InvalidOperationException">The request was answered, or it is not at a provider.</exception>
    public void Remind(Guid userId, DateTime now)
    {
        EnsurePending();
        if (!IsSignatureRequest)
        {
            throw new InvalidOperationException("Only a request at the provider is reminded.");
        }

        _events.Add(new AcceptanceEvent(Id, AcceptanceEventKind.Reminded, userId, null, now));
    }

    /// <summary>A reason with something in it, up to 1,000 characters, trimmed; null when it is not valid.</summary>
    public static string? ReasonOf(string? reason) =>
        reason?.Trim() is { Length: > 0 and <= ReasonMaxLength } trimmed ? trimmed : null;

    private AcceptanceSigner SignerOf(Guid signerId) =>
        _signers.Find(signer => signer.Id == signerId) ?? throw new ArgumentException("The signer is not of the term.", nameof(signerId));

    // The request closes once; the e-mails of the signers are kept only while it is open (ADR-0129, D2).
    private void Close(AcceptanceStatus status, DateTime now)
    {
        Status = status;
        AnsweredAt = now;
        foreach (var signer in _signers)
        {
            signer.ForgetEmail();
        }
    }

    private void EnsurePending()
    {
        if (Status != AcceptanceStatus.Pending)
        {
            throw new InvalidOperationException("The request was answered.");
        }
    }
}

/// <summary>
/// A signer of a request (ADR-0129): the name and the e-mail typed, inside or outside the company, without a link to a
/// user. The e-mail is kept only while the request is open; the name stays in the history.
/// </summary>
public sealed class AcceptanceSigner : Entity
{
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 254;
    public const int ProviderIdMaxLength = 200;

    private AcceptanceSigner()
    {
        Name = string.Empty;
        ProviderSignerId = string.Empty;
    }

    internal AcceptanceSigner(Guid termId, int position, AcceptanceSignerForm form, string providerSignerId)
    {
        TermId = termId;
        Position = position;
        Name = form.Name;
        Email = form.Email;
        ProviderSignerId = providerSignerId;
        Status = AcceptanceSignerStatus.Pending;
    }

    public Guid TermId { get; private init; }

    /// <summary>The order in which the signer was typed, from 1.</summary>
    public int Position { get; private init; }

    public string Name { get; private init; }

    /// <summary>The e-mail, while the request is open; null after it is signed, refused or attached.</summary>
    public string? Email { get; private set; }

    public AcceptanceSignerStatus Status { get; private set; }

    /// <summary>When the signer signed or refused, as the provider tells.</summary>
    public DateTime? RespondedAt { get; private set; }

    public string? RefusalReason { get; private set; }

    /// <summary>The id of the signer at the provider, to read the status.</summary>
    public string ProviderSignerId { get; private init; }

    internal void Answer(AcceptanceSignerStatus status, DateTime at, string? reason)
    {
        Status = status;
        RespondedAt = at;
        RefusalReason = reason;
    }

    internal void ForgetEmail() => Email = null;
}

/// <summary>An event of the history of a term: what happened, who did it in the system or which signer, and when.</summary>
public sealed class AcceptanceEvent : Entity
{
    private AcceptanceEvent()
    {
    }

    internal AcceptanceEvent(Guid termId, AcceptanceEventKind kind, Guid? actorId, Guid? signerId, DateTime at)
    {
        TermId = termId;
        Kind = kind;
        ActorId = actorId;
        SignerId = signerId;
        At = at;
    }

    public Guid TermId { get; private init; }

    public AcceptanceEventKind Kind { get; private init; }

    /// <summary>The person in the system who did it; null when a signer did it at the provider.</summary>
    public Guid? ActorId { get; private init; }

    /// <summary>The signer who signed or refused at the provider.</summary>
    public Guid? SignerId { get; private init; }

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

/// <summary>A signer as typed in a request, trimmed and checked (ADR-0129).</summary>
public sealed record AcceptanceSignerForm(string Name, string Email)
{
    public const int MaxSigners = 10;

    /// <summary>
    /// The signers when there are 1 to 10, each with a name of up to 120 characters and a valid e-mail of up to 254,
    /// without the same e-mail twice; null when any is not valid.
    /// </summary>
    public static IReadOnlyList<AcceptanceSignerForm>? Of(IReadOnlyList<(string? Name, string? Email)> typed)
    {
        ArgumentNullException.ThrowIfNull(typed);
        if (typed.Count is 0 or > MaxSigners)
        {
            return null;
        }

        var signers = new List<AcceptanceSignerForm>();
        foreach (var (name, email) in typed)
        {
            var trimmedName = name?.Trim() ?? string.Empty;
            var trimmedEmail = email?.Trim() ?? string.Empty;
            if (trimmedName.Length is 0 or > AcceptanceSigner.NameMaxLength || trimmedName.Any(char.IsControl) || !IsEmail(trimmedEmail))
            {
                return null;
            }

            signers.Add(new AcceptanceSignerForm(trimmedName, trimmedEmail));
        }

        return signers.Select(signer => signer.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() == signers.Count ? signers : null;
    }

    private static bool IsEmail(string email) =>
        email.Length <= AcceptanceSigner.EmailMaxLength
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && address.DisplayName.Length == 0
        && address.Host.Contains('.', StringComparison.Ordinal)
        && !address.Host.StartsWith('.')
        && !address.Host.EndsWith('.');
}
