using FiscoProjects.Documents.Domain;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>Someone answered or changed the same term at the same time; nothing was saved.</summary>
public sealed class AcceptanceChangedException(Exception inner) : InvalidOperationException("The term changed at the same time.", inner);

/// <summary>Where the acceptance terms are kept, with the documents of the module, saved together.</summary>
public interface IAcceptanceStore
{
    /// <summary>The terms of the project, newest first, with the signers and without the history.</summary>
    Task<IReadOnlyList<AcceptanceTerm>> ListAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The term of the project, tracked, with the signers and, when asked, the history.</summary>
    Task<AcceptanceTerm?> FindAsync(Guid projectId, Guid id, bool withEvents, CancellationToken cancellationToken);

    /// <summary>The term, of any project, tracked, with the signers.</summary>
    Task<AcceptanceTerm?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The term of a document at the provider.</summary>
    Task<Guid?> TermOfDocumentAsync(string provider, string documentId, CancellationToken cancellationToken);

    /// <summary>The requests at the provider waiting for the signatures, of every project, oldest first.</summary>
    Task<IReadOnlyList<(Guid Id, Guid ProjectId)>> OpenRequestsAsync(string provider, CancellationToken cancellationToken);

    /// <summary>How many requests of the project wait for the answer.</summary>
    Task<int> PendingCountAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The requests waiting for the answer made by the person, oldest first, with the signers, for Meu trabalho.</summary>
    Task<IReadOnlyList<AcceptanceTerm>> PendingOfAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The milestones of the projects with a request waiting for the answer.</summary>
    Task<IReadOnlySet<Guid>> PendingMilestonesAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);

    void Add(AcceptanceTerm term);

    void Remove(AcceptanceTerm term);

    /// <summary>Saves; throws <see cref="AcceptanceChangedException"/> when someone changed the term at the same time.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public enum AcceptanceOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Invalid = 5,
    Refused = 6,
    Answered = 7,

    /// <summary>The installation has no provider of signatures, or the request is not at it (ADR-0129); 409.</summary>
    NoProvider = 8,

    /// <summary>The provider of signatures failed or refused; nothing was saved; 502.</summary>
    ProviderFailed = 9,
}

/// <summary>Why the file of a term is refused: empty, larger than the documents take, or not a PDF (ADR-0035).</summary>
public enum AcceptanceFileRefusal
{
    None = 0,
    Empty = 1,
    TooLarge = 2,
    NotPdf = 3,
}

/// <summary>What happened to a term: its id, or why nothing was saved, with the fields not valid or the file refused.</summary>
public sealed record AcceptanceResult(
    AcceptanceOutcome Outcome,
    Guid? Id = null,
    IReadOnlyList<AcceptanceField>? Invalid = null,
    AcceptanceFileRefusal Refusal = AcceptanceFileRefusal.None)
{
    public static AcceptanceResult Of(AcceptanceOutcome outcome, Guid? id = null) => new(outcome, id);

    public static AcceptanceResult InvalidFields(IReadOnlyList<AcceptanceField> fields) => new(AcceptanceOutcome.Invalid, Invalid: fields);

    public static AcceptanceResult RefusedFile(AcceptanceFileRefusal refusal) => new(AcceptanceOutcome.Refused, Refusal: refusal);
}

/// <summary>A project as the person sees it in the terms, and whether the person manages them.</summary>
public sealed record AcceptanceArea(ProjectAccess Project, bool Manages);

/// <summary>Reads the project as the person sees it; null when the person does not see it (404).</summary>
public sealed class AcceptanceAreas(IProjectAccess projects)
{
    public async Task<AcceptanceArea?> OfAsync(Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is { } project
            ? new AcceptanceArea(project, AcceptanceAccess.Manages(viewer, project))
            : null;
    }

    /// <summary>The area of who manages the terms, or the refusal: 404 out of the project, 403 for the others (ADR-0129).</summary>
    public async Task<(AcceptanceOutcome Outcome, AcceptanceArea? Area)> OfManagerAsync(
        Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return (AcceptanceOutcome.NotFound, null);
        }

        return area.Manages ? (AcceptanceOutcome.Done, area) : (AcceptanceOutcome.Forbidden, null);
    }
}

/// <summary>A term as the screens show it: the milestone by its current name and day, and the names of the people.</summary>
public sealed record AcceptanceView(
    AcceptanceTerm Term,
    ScheduleItemInfo? Milestone,
    string? Recipient,
    string RequestedBy,
    string? AnsweredBy);

/// <summary>How many terms of each status the project has.</summary>
public sealed record AcceptanceCounts(int All, int Pending, int Signed, int Refused);

/// <summary>
/// The terms of the project, the counts, whether a signature can be asked for now (a provider and the project open) and
/// whether a signed term can be attached (the project open).
/// </summary>
public sealed record AcceptanceList(IReadOnlyList<AcceptanceView> Items, AcceptanceCounts Counts, bool CanRequest, bool CanAttach);

/// <summary>Reads the names the terms show, in one call to each source.</summary>
public sealed class AcceptanceViews(IUserDirectory users, IScheduleItems schedule)
{
    /// <summary>The views of the terms, and the names of their people and of <paramref name="morePeople"/>.</summary>
    public async Task<(IReadOnlyList<AcceptanceView> Views, IReadOnlyDictionary<Guid, string> Names)> OfAsync(
        Guid projectId, IReadOnlyList<AcceptanceTerm> terms, IEnumerable<Guid> morePeople, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (terms.Count == 0)
        {
            return ([], new Dictionary<Guid, string>());
        }

        var people = terms.SelectMany(term => new[] { term.RecipientId ?? Guid.Empty, term.RequestedById, term.AnsweredById ?? Guid.Empty })
            .Concat(morePeople)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        var names = await users.NamesAsync(people, cancellationToken);
        var milestones = (await schedule.ItemsAsync(projectId, cancellationToken))
            .Where(item => item.Type == ScheduleItemType.Milestone)
            .ToDictionary(item => item.Id);
        IReadOnlyList<AcceptanceView> views =
        [
            .. terms.Select(term => new AcceptanceView(
                term,
                milestones.GetValueOrDefault(term.MilestoneId),
                term.RecipientId is { } recipient ? names.GetValueOrDefault(recipient, string.Empty) : null,
                names.GetValueOrDefault(term.RequestedById, string.Empty),
                term.AnsweredById is { } answered ? names.GetValueOrDefault(answered, string.Empty) : null)),
        ];
        return (views, names);
    }
}

/// <summary>
/// The terms of a project (ADR-0129), only for the Administrator and the manager, newest first, and of one status when
/// asked; the counts are of all the terms.
/// </summary>
public sealed class ListAcceptances(AcceptanceAreas areas, IAcceptanceStore acceptances, AcceptanceViews views, SignatureProviders providers)
{
    public async Task<(AcceptanceOutcome Outcome, AcceptanceList? List)> ExecuteAsync(
        Guid projectId, AcceptanceStatus? status, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (outcome, area) = await areas.OfManagerAsync(projectId, viewer, cancellationToken);
        if (area is null)
        {
            return (outcome, null);
        }

        var all = await acceptances.ListAsync(projectId, cancellationToken);
        var chosen = all.Where(term => status is null || term.Status == status).ToList();
        var counts = new AcceptanceCounts(
            all.Count,
            all.Count(term => term.Status == AcceptanceStatus.Pending),
            all.Count(term => term.Status == AcceptanceStatus.Signed),
            all.Count(term => term.Status == AcceptanceStatus.Refused));
        var open = !area.Project.IsClosed;
        return (AcceptanceOutcome.Done, new(
            (await views.OfAsync(projectId, chosen, [], cancellationToken)).Views,
            counts,
            CanRequest: open && providers.Current is not null,
            CanAttach: open));
    }
}

/// <summary>An event of the history with the name of who did it: the person in the system, or the signer.</summary>
public sealed record AcceptanceEventView(AcceptanceEvent Event, string Actor);

/// <summary>A signer as the drawer shows it, with the link to sign while pending, from a provider that gives it.</summary>
public sealed record AcceptanceSignerView(AcceptanceSigner Signer, string? SigningLink);

/// <summary>A term with the signers, its history, the signed document and what the person may do with it.</summary>
public sealed record AcceptanceDetails(
    AcceptanceView View,
    IReadOnlyList<AcceptanceSignerView> Signers,
    IReadOnlyList<AcceptanceEventView> Events,
    Document? Signed,
    AcceptanceActions Actions);

/// <summary>A term of the project with its signers and history, for who manages the terms (ADR-0129).</summary>
public sealed class GetAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, IDocumentStore documents, AcceptanceViews views, SignatureProviders providers)
{
    public async Task<(AcceptanceOutcome Outcome, AcceptanceDetails? Details)> ExecuteAsync(
        Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, area) = await areas.OfManagerAsync(projectId, viewer, cancellationToken);
        if (area is null)
        {
            return (outcome, null);
        }

        if (await acceptances.FindAsync(projectId, termId, withEvents: true, cancellationToken) is not { } term)
        {
            return (AcceptanceOutcome.NotFound, null);
        }

        var (found, names) = await views.OfAsync(
            projectId, [term], term.Events.Where(item => item.ActorId is not null).Select(item => item.ActorId!.Value), cancellationToken);
        var signed = term.DocumentId is { } documentId
            ? await documents.FindAsync(projectId, documentId, withVersions: false, cancellationToken)
            : null;
        var provider = providers.Of(term);
        var signers = new List<AcceptanceSignerView>();
        foreach (var signer in term.Signers.OrderBy(item => item.Position))
        {
            var link = term.Status == AcceptanceStatus.Pending && signer.Status == AcceptanceSignerStatus.Pending && provider is ISigningLinks links
                ? await links.LinkOfAsync(term.ProviderDocumentId!, signer.ProviderSignerId, cancellationToken)
                : null;
            signers.Add(new AcceptanceSignerView(signer, link));
        }

        var signerNames = term.Signers.ToDictionary(signer => signer.Id, signer => signer.Name);
        // At the same moment, the request comes before the reminders and the signatures, and the answer last.
        var events = term.Events.OrderBy(item => item.At).ThenBy(item => item.Kind)
            .Select(item => new AcceptanceEventView(
                item,
                item.ActorId is { } actor
                    ? names.GetValueOrDefault(actor, string.Empty)
                    : item.SignerId is { } signerId ? signerNames.GetValueOrDefault(signerId, string.Empty) : string.Empty))
            .ToList();
        return (AcceptanceOutcome.Done, new(found[0], signers, events, signed, AcceptanceAccess.ActionsOf(viewer, area.Project, term, provider is not null)));
    }
}

/// <summary>The term to sign of a request, for who manages the terms.</summary>
public sealed class OpenAcceptanceFile(AcceptanceAreas areas, IAcceptanceStore acceptances, IFileStorage files)
{
    public async Task<(AcceptanceOutcome Outcome, OpenedDocument? File)> ExecuteAsync(
        Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, area) = await areas.OfManagerAsync(projectId, viewer, cancellationToken);
        if (area is null)
        {
            return (outcome, null);
        }

        if (await acceptances.FindAsync(projectId, termId, withEvents: false, cancellationToken) is not { FileId: { } fileId } term
            || await files.OpenAsync(fileId, null, cancellationToken) is not { } file)
        {
            return (AcceptanceOutcome.NotFound, null);
        }

        return (AcceptanceOutcome.Done, new(file, term.FileName ?? file.Name, FileTypes.ContentTypeOf(term.FileName ?? file.Name)));
    }
}

/// <summary>The milestones of the schedule of the project, for who asks for terms and attaches them.</summary>
public sealed class ListAcceptanceMilestones(AcceptanceAreas areas, IScheduleItems schedule)
{
    public async Task<(AcceptanceOutcome Outcome, IReadOnlyList<ScheduleItemInfo> Milestones)> ExecuteAsync(
        Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, area) = await areas.OfManagerAsync(projectId, viewer, cancellationToken);
        if (area is null)
        {
            return (outcome, []);
        }

        var items = await schedule.ItemsAsync(projectId, cancellationToken);
        return (AcceptanceOutcome.Done, [.. items.Where(item => item.Type == ScheduleItemType.Milestone)]);
    }
}

/// <summary>What a request or a term attached without one says: the milestone, the title, the message and the signers.</summary>
public sealed record AcceptanceRequest(
    Guid? MilestoneId, string? Title, string? Message, IReadOnlyList<(string? Name, string? Email)>? Signers = null);

/// <summary>
/// The rules the requests and the signed terms share: the milestone of the project, the PDF, the folder of the terms to
/// sign, and the signed term kept as an approved document of the folder of the terms (ADR-0093).
/// </summary>
public sealed class AcceptanceFiles(
    ProjectFolders folders,
    IDocumentStore documents,
    IFileStorage files,
    IScheduleItems schedule,
    DocumentSettings settings,
    IProjectStorage storage)
{
    /// <summary>The folder of the terms to sign in the folder of a project, beside "Documentos".</summary>
    public const string RequestsFolder = "Pedidos de aceite";

    /// <summary>
    /// The fields not valid of a request: a milestone of the schedule of the project, the title, the message and a file
    /// with a name; the form when all are valid.
    /// </summary>
    public async Task<(AcceptanceForm? Form, ScheduleItemInfo? Milestone, string? FileName, IReadOnlyList<AcceptanceField> Invalid)> CheckAsync(
        ProjectAccess project, AcceptanceRequest request, DocumentFile? file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);
        var (form, invalid) = AcceptanceForm.Of(request.Title, request.Message);
        var fields = invalid.ToList();
        var milestone = request.MilestoneId is { } milestoneId
            ? (await schedule.ItemsAsync(project.Id, cancellationToken))
                .FirstOrDefault(item => item.Id == milestoneId && item.Type == ScheduleItemType.Milestone)
            : null;
        if (milestone is null)
        {
            fields.Add(AcceptanceField.Milestone);
        }

        var name = Document.NameOf(file?.Name);
        if (name is null)
        {
            fields.Add(AcceptanceField.File);
        }

        return fields.Count == 0 ? (form, milestone, name, []) : (null, null, null, [.. fields.Order()]);
    }

    /// <summary>Why the file is refused: empty, larger than the documents take, or not a PDF; None when it is a PDF.</summary>
    public async Task<AcceptanceFileRefusal> RefusalOfAsync(DocumentFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Size <= 0)
        {
            return AcceptanceFileRefusal.Empty;
        }

        if (file.Size > settings.MaxBytes)
        {
            return AcceptanceFileRefusal.TooLarge;
        }

        return await FileTypes.IsPdfAsync(file.Name, file.Content, cancellationToken) ? AcceptanceFileRefusal.None : AcceptanceFileRefusal.NotPdf;
    }

    /// <summary>Keeps the term to sign in "Pedidos de aceite" of the folder of the project, beside a file with the same name.</summary>
    public async Task<StoredFile> KeepToSignAsync(ProjectAccess project, string name, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var folder = await storage.EnsureFolderAsync(project.Id, RequestsFolder, cancellationToken);
        return await files.UploadAsync(folder, name, content, SameNameAction.KeepBoth, cancellationToken);
    }

    /// <summary>
    /// Keeps the signed term in the folder of the terms, beside a file with the same name, as an approved document linked
    /// to the milestone, sent by <paramref name="userId"/> (ADR-0093); the caller saves it with the term.
    /// </summary>
    public async Task<(Document Document, StoredFile Stored)> KeepSignedAsync(
        ProjectAccess project, Guid milestoneId, string name, Stream content, Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var folder = (await folders.OfAsync(project.Id, cancellationToken)).FirstOrDefault(item => item.IsAcceptance)
            ?? throw new InvalidOperationException("The project has no folder of the acceptance terms.");
        var stored = await folders.UploadAsync(folder, project, name, content, SameNameAction.KeepBoth, cancellationToken);
        var document = Document.Upload(project.Id, folder.Id, stored.Name, stored.Id, stored.VersionId, stored.Size, userId, now);
        document.ChangeStatus(DocumentStatus.Approved, userId, now);
        document.Link(null, milestoneId);
        documents.Add(document);
        return (document, stored);
    }

    /// <summary>Removes a file kept for a change that was not saved, so nobody's file is left in the storage.</summary>
    public Task ForgetAsync(StoredFile stored) => files.DeleteFileAsync(stored.Id, CancellationToken.None);

    /// <summary>The name of the milestone of a term in the schedule now; empty when it left the schedule.</summary>
    public async Task<string> MilestoneNameAsync(AcceptanceTerm term, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        return (await schedule.ItemsAsync(term.ProjectId, cancellationToken)).FirstOrDefault(item => item.Id == term.MilestoneId)?.Name
            ?? string.Empty;
    }
}

/// <summary>
/// The Administrator or the manager asks for the signature of the term of a milestone (ADR-0129): the title, the
/// message, the term to sign in PDF and 1 to 10 signers by name and e-mail, inside or outside the company, at the
/// provider of the installation, which sends them the link. Nothing is saved when the provider fails.
/// </summary>
public sealed class RequestAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, AcceptanceFiles rules, SignatureProviders providers, TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, AcceptanceRequest request, DocumentFile? file, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(request);
        var (refused, area) = await AcceptanceRefusals.OfManagerAsync(areas, projectId, viewer, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        if (providers.Current is null)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.NoProvider);
        }

        var (form, milestone, name, invalid) = await rules.CheckAsync(area!.Project, request, file, cancellationToken);
        var signers = AcceptanceSignerForm.Of(request.Signers ?? []);
        if (invalid.Count > 0 || signers is null)
        {
            return AcceptanceResult.InvalidFields([.. invalid.Concat(signers is null ? [AcceptanceField.Signers] : []).Order()]);
        }

        // Outside production, the signers of the demonstration sign at the local provider (ADR-0130).
        var provider = providers.For(signers.Select(signer => signer.Email))!;

        var refusal = await rules.RefusalOfAsync(file!, cancellationToken);
        if (refusal != AcceptanceFileRefusal.None)
        {
            return AcceptanceResult.RefusedFile(refusal);
        }

        var stored = await rules.KeepToSignAsync(area.Project, name!, file!.Content, cancellationToken);
        SentSignature sent;
        try
        {
            file.Content.Position = 0;
            sent = await provider.SendAsync(
                new SignatureRequest(form!.Title, form.Message, stored.Name, file.Content, [.. signers.Select(signer => new SignatureSigner(signer.Name, signer.Email))]),
                cancellationToken);
        }
        catch (SignatureProviderException)
        {
            await rules.ForgetAsync(stored);
            return AcceptanceResult.Of(AcceptanceOutcome.ProviderFailed);
        }

        var term = AcceptanceTerm.Request(
            projectId,
            milestone!.Id,
            form,
            signers,
            new AcceptanceFile(stored.Id, stored.Name, stored.Size),
            new AcceptanceDelivery(provider.Name, sent.DocumentId, sent.SignerIds),
            viewer.UserId,
            clock.GetUtcNow().UtcDateTime);
        acceptances.Add(term);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await rules.ForgetAsync(stored);
            await AcceptanceProviderCalls.TryAsync(() => provider.CancelAsync(sent.DocumentId, CancellationToken.None));
            throw;
        }

        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager attaches a term signed outside the system without a request (ADR-0129): the
/// milestone, the title and the PDF; who signed is in the PDF. The term is signed at once: the document in the folder
/// of the terms and the milestone reached.
/// </summary>
public sealed class AttachAcceptance(
    AcceptanceAreas areas,
    IAcceptanceStore acceptances,
    AcceptanceFiles rules,
    IScheduleMilestones milestones,
    AcceptanceNotices notices,
    TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, AcceptanceRequest request, DocumentFile? file, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(request);
        var (refused, area) = await AcceptanceRefusals.OfManagerAsync(areas, projectId, viewer, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        var (form, milestone, name, invalid) = await rules.CheckAsync(area!.Project, request with { Message = null }, file, cancellationToken);
        if (invalid.Count > 0)
        {
            return AcceptanceResult.InvalidFields(invalid);
        }

        var refusal = await rules.RefusalOfAsync(file!, cancellationToken);
        if (refusal != AcceptanceFileRefusal.None)
        {
            return AcceptanceResult.RefusedFile(refusal);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var (document, stored) = await rules.KeepSignedAsync(area.Project, milestone!.Id, name!, file!.Content, viewer.UserId, now, cancellationToken);
        var term = AcceptanceTerm.Attach(projectId, milestone.Id, form!.Title, document.Id, viewer.UserId, now);
        acceptances.Add(term);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await rules.ForgetAsync(stored);
            throw;
        }

        await milestones.ReachAsync(projectId, milestone.Id, cancellationToken);
        await notices.AnsweredAsync(term, area.Project, milestone.Name, viewer.UserId, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager attaches the signed term received outside the system to a pending request
/// (ADR-0129): at the provider, or asked of a person before. It becomes an approved document of the folder of the terms,
/// linked to the milestone, the milestone is reached and the document at the provider is cancelled; who asked and the
/// manager hear about it.
/// </summary>
public sealed class AttachSignedTerm(
    AcceptanceAreas areas,
    IAcceptanceStore acceptances,
    AcceptanceFiles rules,
    IScheduleMilestones milestones,
    SignatureProviders providers,
    AcceptanceNotices notices,
    TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, Guid termId, DocumentFile? file, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, area, term) = await AcceptanceRefusals.OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (file is null || Document.NameOf(file.Name) is not { } name)
        {
            return AcceptanceResult.InvalidFields([AcceptanceField.File]);
        }

        var fileRefusal = await rules.RefusalOfAsync(file, cancellationToken);
        if (fileRefusal != AcceptanceFileRefusal.None)
        {
            return AcceptanceResult.RefusedFile(fileRefusal);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var (document, stored) = await rules.KeepSignedAsync(area!.Project, term!.MilestoneId, name, file.Content, viewer.UserId, now, cancellationToken);
        term.Sign(document.Id, viewer.UserId, now);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            await rules.ForgetAsync(stored);
            return AcceptanceResult.Of(AcceptanceOutcome.Answered);
        }
        catch
        {
            await rules.ForgetAsync(stored);
            throw;
        }

        // The term is signed: the document left at the provider only closes, and a failure there changes nothing here.
        if (providers.Of(term) is { } provider)
        {
            await AcceptanceProviderCalls.TryAsync(() => provider.CancelAsync(term.ProviderDocumentId!, CancellationToken.None));
        }

        // A milestone removed from the schedule does not keep the term from being signed.
        await milestones.ReachAsync(projectId, term.MilestoneId, cancellationToken);
        await notices.AnsweredAsync(term, area.Project, await rules.MilestoneNameAsync(term, cancellationToken), viewer.UserId, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager reminds the signers of a pending request (ADR-0129): the provider sends the link
/// again to who did not sign, and the history keeps the reminder. A request asked of a person before has no reminder.
/// </summary>
public sealed class RemindAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, SignatureProviders providers, TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, _, term) = await AcceptanceRefusals.OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (providers.Of(term!) is not { } provider)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.NoProvider);
        }

        try
        {
            await provider.RemindAsync(term!.ProviderDocumentId!, cancellationToken);
        }
        catch (SignatureProviderException)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.ProviderFailed);
        }

        term.Remind(viewer.UserId, clock.GetUtcNow().UtcDateTime);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.Answered);
        }

        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager cancels a pending request (ADR-0095, ADR-0129): the document is cancelled at the
/// provider first, and the request leaves the list with its signers, its history and the term to sign, staying only in
/// the audit.
/// </summary>
public sealed class CancelAcceptance(AcceptanceAreas areas, IAcceptanceStore acceptances, SignatureProviders providers, IFileStorage files)
{
    public async Task<AcceptanceResult> ExecuteAsync(Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, _, term) = await AcceptanceRefusals.OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (providers.Of(term!) is { } provider)
        {
            try
            {
                await provider.CancelAsync(term!.ProviderDocumentId!, cancellationToken);
            }
            catch (SignatureProviderException)
            {
                return AcceptanceResult.Of(AcceptanceOutcome.ProviderFailed);
            }
        }

        acceptances.Remove(term!);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.Answered);
        }

        if (term!.FileId is { } fileId)
        {
            await files.DeleteFileAsync(fileId, CancellationToken.None);
        }

        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// Applies what the provider tells of a request at it (ADR-0129), the same for the check in the background, the local
/// page and the notice of the provider: each signer who signed or refused, once; a refusal refuses the term and closes
/// the document at the provider; with every signer signed, the signed PDF of the provider becomes an approved document
/// of the folder of the terms, the milestone is reached, and who asked and the manager hear about it. A failure of the
/// provider leaves the request as it was, for the next check; a change saved at the same time by another way wins.
/// </summary>
public sealed class ApplySignatureStatus(
    IAcceptanceStore acceptances,
    SignatureProviders providers,
    AcceptanceFiles rules,
    IProjectAccess projects,
    IScheduleMilestones milestones,
    AcceptanceNotices notices,
    TimeProvider clock)
{
    /// <summary>Whether anything changed in the request.</summary>
    public async Task<bool> ExecuteAsync(Guid termId, CancellationToken cancellationToken)
    {
        if (await acceptances.FindAsync(termId, cancellationToken) is not { Status: AcceptanceStatus.Pending } term
            || providers.Of(term) is not { } provider
            || await projects.FindAsync(term.ProjectId, Guid.Empty, isAdministrator: true, cancellationToken) is not { IsClosed: false } project)
        {
            return false;
        }

        IReadOnlyList<SignerState> states;
        try
        {
            states = await provider.StatusAsync(term.ProviderDocumentId!, cancellationToken);
        }
        catch (SignatureProviderException)
        {
            return false;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var changed = Apply(term, states, now);
        StoredFile? stored = null;
        if (term.Status == AcceptanceStatus.Pending && term.AllSigned)
        {
            stored = await KeepSignedAsync(term, project, provider, now, cancellationToken);
        }

        if (!changed && stored is null)
        {
            return false;
        }

        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            if (stored is not null)
            {
                await rules.ForgetAsync(stored);
            }

            return false;
        }
        catch
        {
            if (stored is not null)
            {
                await rules.ForgetAsync(stored);
            }

            throw;
        }

        if (term.Status == AcceptanceStatus.Refused)
        {
            await AcceptanceProviderCalls.TryAsync(() => provider.CancelAsync(term.ProviderDocumentId!, CancellationToken.None));
        }

        if (term.Status == AcceptanceStatus.Signed)
        {
            await milestones.ReachAsync(term.ProjectId, term.MilestoneId, cancellationToken);
        }

        if (term.Status != AcceptanceStatus.Pending)
        {
            await notices.AnsweredAsync(term, project, await rules.MilestoneNameAsync(term, cancellationToken), null, cancellationToken);
        }

        return true;
    }

    /// <summary>The term of a document at the provider, for the local page and the notice of the provider.</summary>
    public async Task<bool> ForDocumentAsync(string provider, string documentId, CancellationToken cancellationToken) =>
        await acceptances.TermOfDocumentAsync(provider, documentId, cancellationToken) is { } termId
        && await ExecuteAsync(termId, cancellationToken);

    // The signatures first, then the first refusal: a term refused keeps who had signed.
    private static bool Apply(AcceptanceTerm term, IReadOnlyList<SignerState> states, DateTime now)
    {
        var changed = false;
        var signers = term.Signers.ToDictionary(signer => signer.ProviderSignerId, StringComparer.Ordinal);
        foreach (var state in states.Where(item => item.Status == AcceptanceSignerStatus.Signed))
        {
            if (signers.TryGetValue(state.SignerId, out var signer))
            {
                changed |= term.SignerSigned(signer.Id, state.At ?? now);
            }
        }

        foreach (var state in states.Where(item => item.Status == AcceptanceSignerStatus.Refused))
        {
            if (term.Status == AcceptanceStatus.Pending && signers.TryGetValue(state.SignerId, out var signer))
            {
                changed |= term.SignerRefused(signer.Id, state.Reason, state.At ?? now);
            }
        }

        return changed;
    }

    // The signed PDF of the provider as the document of the term, sent by who asked; null while the provider fails.
    private async Task<StoredFile?> KeepSignedAsync(
        AcceptanceTerm term, ProjectAccess project, ISignatureProvider provider, DateTime now, CancellationToken cancellationToken)
    {
        SignedFile signed;
        try
        {
            signed = await provider.SignedFileAsync(term.ProviderDocumentId!, cancellationToken);
        }
        catch (SignatureProviderException)
        {
            return null;
        }

        await using var content = new MemoryStream();
        await using (signed.Content)
        {
            await signed.Content.CopyToAsync(content, cancellationToken);
        }

        content.Position = 0;
        var name = Document.NameOf(signed.Name) ?? Document.NameOf(term.FileName) ?? "Termo assinado.pdf";
        var (document, stored) = await rules.KeepSignedAsync(project, term.MilestoneId, name, content, term.RequestedById, now, cancellationToken);
        term.CompleteSigning(document.Id, now);
        return stored;
    }
}

/// <summary>
/// The requests at the provider waiting for the signatures, in the projects not completed nor cancelled, checked one by
/// one (ADR-0129); <see cref="ApplySignatureStatus"/> applies each in a scope of its own.
/// </summary>
public sealed class ListOpenSignatureRequests(IAcceptanceStore acceptances, SignatureProviders providers, IProjectAccess projects)
{
    public async Task<IReadOnlyList<Guid>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var requests = new List<(Guid Id, Guid ProjectId)>();
        foreach (var provider in providers.All)
        {
            requests.AddRange(await acceptances.OpenRequestsAsync(provider.Name, cancellationToken));
        }

        var open = new Dictionary<Guid, bool>();
        var ids = new List<Guid>();
        foreach (var (id, projectId) in requests)
        {
            if (!open.TryGetValue(projectId, out var isOpen))
            {
                isOpen = await projects.FindAsync(projectId, Guid.Empty, isAdministrator: true, cancellationToken) is { IsClosed: false };
                open[projectId] = isOpen;
            }

            if (isOpen)
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}

/// <summary>A call to the provider that may fail without changing what was saved, as closing a document no longer needed.</summary>
internal static class AcceptanceProviderCalls
{
    public static async Task TryAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (SignatureProviderException)
        {
            // The document stays at the provider; nothing of the term depends on it.
        }
    }
}

/// <summary>The checks the actions on the terms share, in the order of the answers: 404, 403, 409 of the project, 409 of the term.</summary>
internal static class AcceptanceRefusals
{
    /// <summary>The area of who manages the terms of an open project, or the refusal.</summary>
    public static async Task<(AcceptanceResult? Refusal, AcceptanceArea? Area)> OfManagerAsync(
        AcceptanceAreas areas, Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, area) = await areas.OfManagerAsync(projectId, viewer, cancellationToken);
        if (area is null)
        {
            return (AcceptanceResult.Of(outcome), null);
        }

        return area.Project.IsClosed ? (AcceptanceResult.Of(AcceptanceOutcome.ProjectClosed), null) : (null, area);
    }

    /// <summary>A pending term of the project for who manages the terms, or the refusal.</summary>
    public static async Task<(AcceptanceResult? Refusal, AcceptanceArea? Area, AcceptanceTerm? Term)> OfManagedTermAsync(
        AcceptanceAreas areas, IAcceptanceStore acceptances, Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (refused, area) = await OfManagerAsync(areas, projectId, viewer, cancellationToken);
        if (refused is not null)
        {
            return (refused, null, null);
        }

        if (await acceptances.FindAsync(projectId, termId, withEvents: false, cancellationToken) is not { } term)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.NotFound), null, null);
        }

        return term.Status == AcceptanceStatus.Pending ? (null, area, term) : (AcceptanceResult.Of(AcceptanceOutcome.Answered), null, null);
    }
}

/// <summary>
/// The notices of the terms (ADR-0037, ADR-0038, ADR-0129), after saving: the signed term, also the attached one, and the
/// refusal, to who asked and to the manager of the project, in the bell and in the daily summary. The request and the
/// reminder have none: the provider sends the link. The notice leads to the term, never with the file, the e-mails of
/// the signers or the reason.
/// </summary>
public sealed class AcceptanceNotices(IEnumerable<INoticeBoard> boards, IProjectAccess projects)
{
    /// <summary>The address of a term in the screens: the tab of the terms with its drawer open.</summary>
    public static string PathOf(Guid projectId, Guid termId) => $"/projects/{projectId}/acceptances?term={termId}";

    /// <param name="term">The term signed or refused.</param>
    /// <param name="project">Its project.</param>
    /// <param name="milestone">The name of the milestone.</param>
    /// <param name="actorId">Who attached the term; null when the signers signed or refused at the provider.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    public async Task AnsweredAsync(AcceptanceTerm term, ProjectAccess project, string milestone, Guid? actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(project);
        var managers = (await projects.MembersAsync([project.Id], cancellationToken))
            .Where(member => member.Role == ProjectMemberRole.Manager)
            .Select(member => member.UserId);
        var values = new Dictionary<string, string>
        {
            [NoticeValues.Project] = project.Code,
            [NoticeValues.Title] = term.Title,
            [NoticeValues.Item] = milestone,
        };
        if (term.Status == AcceptanceStatus.Signed && actorId is not null)
        {
            values[NoticeValues.Attached] = "true";
        }

        var kind = term.Status == AcceptanceStatus.Signed ? NoticeKind.AcceptanceSigned : NoticeKind.AcceptanceRefused;
        await boards.PostAsync(
            [new Notice(kind, [.. managers.Prepend(term.RequestedById).Distinct()], actorId, project.Id, PathOf(project.Id, term.Id), values)],
            cancellationToken);
    }
}

/// <summary>
/// The requests pending in the projects, for the summary of the closing (ADR-0043), the next milestones of Meu trabalho
/// (ADR-0096) and the overview of the project, where only who manages the terms sees them (ADR-0073, ADR-0129).
/// </summary>
public sealed class ProjectAcceptances(IAcceptanceStore acceptances, IProjectAccess projects) : IProjectAcceptances
{
    public async Task<IReadOnlyList<PendingProjectAcceptance>> PendingForAsync(
        Guid projectId, Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, userId, isAdministrator, cancellationToken);
        if (project is null || !AcceptanceAccess.Manages(new DocumentViewer(userId, isAdministrator), project))
        {
            return [];
        }

        var terms = await acceptances.ListAsync(projectId, cancellationToken);
        return
        [
            .. terms
                .Where(term => term.Status == AcceptanceStatus.Pending)
                .OrderBy(term => term.RequestedAt)
                .Select(term => new PendingProjectAcceptance(
                    term.Id,
                    term.Title,
                    term.MilestoneId,
                    term.RecipientId,
                    [.. term.Signers.OrderBy(signer => signer.Position).Select(signer => signer.Name)],
                    term.RequestedAt)),
        ];
    }

    public Task<int> PendingAsync(Guid projectId, CancellationToken cancellationToken) =>
        acceptances.PendingCountAsync(projectId, cancellationToken);

    public Task<IReadOnlySet<Guid>> RequestedMilestonesAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken) =>
        projectIds.Count == 0
            ? Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>())
            : acceptances.PendingMilestonesAsync(projectIds, cancellationToken);
}
