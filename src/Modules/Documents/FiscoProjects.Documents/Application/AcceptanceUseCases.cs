using FiscoProjects.Documents.Contracts;
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
    /// <summary>The terms of the project, of one person or of all, newest first, without the history.</summary>
    Task<IReadOnlyList<AcceptanceTerm>> ListAsync(Guid projectId, Guid? recipientId, CancellationToken cancellationToken);

    /// <summary>The term of the project, tracked, with its history when asked.</summary>
    Task<AcceptanceTerm?> FindAsync(Guid projectId, Guid id, bool withEvents, CancellationToken cancellationToken);

    /// <summary>The documents of the signed terms of the person in the project, which the person sees in the folder of the terms.</summary>
    Task<IReadOnlySet<Guid>> SignedDocumentsOfAsync(Guid projectId, Guid recipientId, CancellationToken cancellationToken);

    /// <summary>How many requests of the project wait for the answer.</summary>
    Task<int> PendingCountAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Every request waiting for the answer, oldest first, for the daily summary.</summary>
    Task<IReadOnlyList<AcceptanceTerm>> PendingAsync(CancellationToken cancellationToken);

    /// <summary>The requests waiting for the answer for the person or made by the person, oldest first, for Meu trabalho.</summary>
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
}

/// <summary>A term as the screens show it: the milestone by its current name and day, and the names of the people.</summary>
public sealed record AcceptanceView(
    AcceptanceTerm Term,
    ScheduleItemInfo? Milestone,
    string Recipient,
    string RequestedBy,
    string? AnsweredBy);

/// <summary>How many terms of each status the person sees.</summary>
public sealed record AcceptanceCounts(int All, int Pending, int Signed, int Refused);

/// <summary>The terms the person sees, the counts, whether the person manages them and whether the project is closed.</summary>
public sealed record AcceptanceList(IReadOnlyList<AcceptanceView> Items, AcceptanceCounts Counts, bool CanManage, bool IsClosed);

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

        var people = terms.SelectMany(term => new[] { term.RecipientId, term.RequestedById, term.AnsweredById ?? Guid.Empty })
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
                names.GetValueOrDefault(term.RecipientId, string.Empty),
                names.GetValueOrDefault(term.RequestedById, string.Empty),
                term.AnsweredById is { } answered ? names.GetValueOrDefault(answered, string.Empty) : null)),
        ];
        return (views, names);
    }
}

/// <summary>
/// The terms of a project (ADR-0035, ADR-0045): every term for the Administrator and the manager, only the own ones for
/// the others, newest first, and of one status when asked; the counts are of all the terms the person sees.
/// </summary>
public sealed class ListAcceptances(AcceptanceAreas areas, IAcceptanceStore acceptances, AcceptanceViews views)
{
    public async Task<AcceptanceList?> ExecuteAsync(
        Guid projectId, AcceptanceStatus? status, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return null;
        }

        var all = await acceptances.ListAsync(projectId, area.Manages ? null : viewer.UserId, cancellationToken);
        var chosen = all.Where(term => status is null || term.Status == status).ToList();
        var counts = new AcceptanceCounts(
            all.Count,
            all.Count(term => term.Status == AcceptanceStatus.Pending),
            all.Count(term => term.Status == AcceptanceStatus.Signed),
            all.Count(term => term.Status == AcceptanceStatus.Refused));
        return new((await views.OfAsync(projectId, chosen, [], cancellationToken)).Views, counts, area.Manages, area.Project.IsClosed);
    }
}

/// <summary>An event of the history with the name of who did it.</summary>
public sealed record AcceptanceEventView(AcceptanceEvent Event, string Actor);

/// <summary>A term with its history, the signed document and what the person may do with it.</summary>
public sealed record AcceptanceDetails(
    AcceptanceView View, IReadOnlyList<AcceptanceEventView> Events, Document? Signed, AcceptanceActions Actions);

/// <summary>A term the person sees, with its history (ADR-0095).</summary>
public sealed class GetAcceptance(AcceptanceAreas areas, IAcceptanceStore acceptances, IDocumentStore documents, AcceptanceViews views)
{
    public async Task<AcceptanceDetails?> ExecuteAsync(
        Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await acceptances.FindAsync(projectId, termId, withEvents: true, cancellationToken) is not { } term
            || !AcceptanceAccess.Sees(viewer, area.Project, term))
        {
            return null;
        }

        var (found, names) = await views.OfAsync(projectId, [term], term.Events.Select(item => item.ActorId), cancellationToken);
        var signed = term.DocumentId is { } documentId
            ? await documents.FindAsync(projectId, documentId, withVersions: false, cancellationToken)
            : null;
        // At the same moment, the request comes before the reminders, and the answer last.
        var events = term.Events.OrderBy(item => item.At).ThenBy(item => item.Kind)
            .Select(item => new AcceptanceEventView(item, names.GetValueOrDefault(item.ActorId, string.Empty)))
            .ToList();
        return new(found[0], events, signed, AcceptanceAccess.ActionsOf(viewer, area.Project, term));
    }
}

/// <summary>The term to sign of a request, for the person of the request and who manages the terms.</summary>
public sealed class OpenAcceptanceFile(AcceptanceAreas areas, IAcceptanceStore acceptances, IFileStorage files)
{
    public async Task<OpenedDocument?> ExecuteAsync(Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await acceptances.FindAsync(projectId, termId, withEvents: false, cancellationToken) is not { FileId: { } fileId } term
            || !AcceptanceAccess.Sees(viewer, area.Project, term)
            || await files.OpenAsync(fileId, null, cancellationToken) is not { } file)
        {
            return null;
        }

        return new(file, term.FileName ?? file.Name, FileTypes.ContentTypeOf(term.FileName ?? file.Name));
    }
}

/// <summary>The milestones of the schedule of the project, for who asks for terms and attaches them.</summary>
public sealed class ListAcceptanceMilestones(AcceptanceAreas areas, IScheduleItems schedule)
{
    public async Task<(AcceptanceOutcome Outcome, IReadOnlyList<ScheduleItemInfo> Milestones)> ExecuteAsync(
        Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return (AcceptanceOutcome.NotFound, []);
        }

        if (!area.Manages)
        {
            return (AcceptanceOutcome.Forbidden, []);
        }

        var items = await schedule.ItemsAsync(projectId, cancellationToken);
        return (AcceptanceOutcome.Done, [.. items.Where(item => item.Type == ScheduleItemType.Milestone)]);
    }
}

/// <summary>What a request or a term attached without one says: the milestone, the person, the title and the message.</summary>
public sealed record AcceptanceRequest(Guid? MilestoneId, Guid? RecipientId, string? Title, string? Message);

/// <summary>
/// The rules the requests and the signed terms share: the milestone and the person of the project, the PDF, the folder
/// of the terms to sign, and the signed term kept as an approved document of the folder of the terms (ADR-0093).
/// </summary>
public sealed class AcceptanceFiles(
    ProjectFolders folders, IDocumentStore documents, IFileStorage files, IScheduleItems schedule, IProjectAccess projects, DocumentSettings settings)
{
    /// <summary>The folder of the terms to sign in the folder of a project, beside "Documentos".</summary>
    public const string RequestsFolder = "Pedidos de aceite";

    /// <summary>
    /// The fields not valid of a request: a milestone of the schedule of the project, a person of the project, the
    /// title and the message, and a file with a name; the form when all are valid.
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

        var members = await projects.MembersAsync([project.Id], cancellationToken);
        if (request.RecipientId is not { } recipientId || !members.Any(member => member.UserId == recipientId))
        {
            fields.Add(AcceptanceField.Recipient);
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

    /// <summary>Keeps the term to sign at "{code} - {name}/Pedidos de aceite", beside a file with the same name.</summary>
    public async Task<StoredFile> KeepToSignAsync(ProjectAccess project, string name, Stream content, CancellationToken cancellationToken)
    {
        var folder = await files.EnsureFolderAsync(null, $"{ProjectFolders.PathOf(project)}/{RequestsFolder}", cancellationToken);
        return await files.UploadAsync(folder.Id, name, content, SameNameAction.KeepBoth, cancellationToken);
    }

    /// <summary>
    /// Keeps the signed term in the folder of the terms, beside a file with the same name, as an approved document linked
    /// to the milestone, sent by the person (ADR-0093); the caller saves it with the term.
    /// </summary>
    public async Task<(Document Document, StoredFile Stored)> KeepSignedAsync(
        ProjectAccess project, Guid milestoneId, string name, Stream content, Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var folder = (await folders.OfAsync(project.Id, cancellationToken)).FirstOrDefault(item => item.IsAcceptance)
            ?? throw new InvalidOperationException("The project has no folder of the acceptance terms.");
        var storageFolder = await folders.StorageOfAsync(folder, project, cancellationToken);
        var stored = await files.UploadAsync(storageFolder, name, content, SameNameAction.KeepBoth, cancellationToken);
        var document = Document.Upload(project.Id, folder.Id, stored.Name, stored.Id, stored.VersionId, stored.Size, userId, now);
        document.ChangeStatus(DocumentStatus.Approved, userId, now);
        document.Link(null, milestoneId);
        documents.Add(document);
        return (document, stored);
    }

    /// <summary>Removes a file kept for a change that was not saved, so nobody's file is left in the storage.</summary>
    public Task ForgetAsync(StoredFile stored) => files.DeleteFileAsync(stored.Id, CancellationToken.None);
}

/// <summary>
/// The Administrator or the manager asks for the acceptance of a milestone of the schedule (ADR-0035, ADR-0045): of a
/// person of the project in any role, with the title, the message and the term to sign in PDF; the person hears about
/// it by e-mail now (ADR-0037).
/// </summary>
public sealed class RequestAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, AcceptanceFiles rules, AcceptanceNotices notices, TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, AcceptanceRequest request, DocumentFile? file, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refused, area) = await AcceptanceRefusals.OfManagerAsync(areas, projectId, viewer, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        var (form, milestone, name, invalid) = await rules.CheckAsync(area!.Project, request, file, cancellationToken);
        if (invalid.Count > 0)
        {
            return AcceptanceResult.InvalidFields(invalid);
        }

        var refusal = await rules.RefusalOfAsync(file!, cancellationToken);
        if (refusal != AcceptanceFileRefusal.None)
        {
            return AcceptanceResult.RefusedFile(refusal);
        }

        var stored = await rules.KeepToSignAsync(area.Project, name!, file!.Content, cancellationToken);
        var term = AcceptanceTerm.Request(
            projectId,
            milestone!.Id,
            form!,
            request.RecipientId!.Value,
            new AcceptanceFile(stored.Id, stored.Name, stored.Size),
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
            throw;
        }

        await notices.RequestedAsync(term, area.Project, milestone.Name, viewer.UserId, reminder: false, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager attaches a term signed outside the system without a request (ADR-0035, ADR-0095):
/// the milestone, the title and the person of the project in whose name it is. The term is signed at once: the document
/// in the folder of the terms and the milestone reached.
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
        var term = AcceptanceTerm.Attach(projectId, milestone.Id, form!.Title, request.RecipientId!.Value, document.Id, viewer.UserId, now);
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
/// The signed term of a pending request (ADR-0035, ADR-0095): sent by the person of the request, in any role, or attached
/// by the Administrator or the manager in the name of the person. It becomes an approved document of the folder of the
/// terms, linked to the milestone, and the milestone is reached; who asked and the manager hear about it.
/// </summary>
public sealed class SignAcceptance(
    AcceptanceAreas areas,
    IAcceptanceStore acceptances,
    AcceptanceFiles rules,
    IScheduleMilestones milestones,
    IScheduleItems schedule,
    AcceptanceNotices notices,
    TimeProvider clock)
{
    /// <param name="projectId">The project.</param>
    /// <param name="termId">The request.</param>
    /// <param name="file">The signed PDF.</param>
    /// <param name="onBehalf">Whether who manages the terms attaches it in the name of the person; otherwise the person sends it.</param>
    /// <param name="viewer">Who does it.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, Guid termId, DocumentFile? file, bool onBehalf, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, area, term) = await AcceptanceRefusals.OfAnswerAsync(areas, acceptances, projectId, termId, onBehalf, viewer, cancellationToken);
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

        // A milestone removed from the schedule does not keep the term from being signed.
        await milestones.ReachAsync(projectId, term.MilestoneId, cancellationToken);
        await notices.AnsweredAsync(term, area.Project, await MilestoneNameAsync(schedule, term, cancellationToken), viewer.UserId, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }

    internal static async Task<string> MilestoneNameAsync(IScheduleItems schedule, AcceptanceTerm term, CancellationToken cancellationToken) =>
        (await schedule.ItemsAsync(term.ProjectId, cancellationToken)).FirstOrDefault(item => item.Id == term.MilestoneId)?.Name ?? string.Empty;
}

/// <summary>
/// The person of a pending request refuses it, with the reason (ADR-0035): the milestone stays open, and who asked and
/// the manager hear about it.
/// </summary>
public sealed class RefuseAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, IScheduleItems schedule, AcceptanceNotices notices, TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(
        Guid projectId, Guid termId, string? reason, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, area, term) = await AcceptanceRefusals.OfAnswerAsync(areas, acceptances, projectId, termId, onBehalf: false, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (AcceptanceTerm.ReasonOf(reason) is not { } valid)
        {
            return AcceptanceResult.InvalidFields([AcceptanceField.Reason]);
        }

        term!.Refuse(valid, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.Answered);
        }

        await notices.AnsweredAsync(term, area!.Project, await SignAcceptance.MilestoneNameAsync(schedule, term, cancellationToken), viewer.UserId, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager reminds the person of a pending request (ADR-0095): the e-mail of the request goes
/// again, and the history keeps the reminder.
/// </summary>
public sealed class RemindAcceptance(
    AcceptanceAreas areas, IAcceptanceStore acceptances, IScheduleItems schedule, AcceptanceNotices notices, TimeProvider clock)
{
    public async Task<AcceptanceResult> ExecuteAsync(Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, area, term) = await AcceptanceRefusals.OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        term!.Remind(viewer.UserId, clock.GetUtcNow().UtcDateTime);
        try
        {
            await acceptances.SaveChangesAsync(cancellationToken);
        }
        catch (AcceptanceChangedException)
        {
            return AcceptanceResult.Of(AcceptanceOutcome.Answered);
        }

        var milestone = await SignAcceptance.MilestoneNameAsync(schedule, term, cancellationToken);
        await notices.RequestedAsync(term, area!.Project, milestone, viewer.UserId, reminder: true, cancellationToken);
        return AcceptanceResult.Of(AcceptanceOutcome.Done, term.Id);
    }
}

/// <summary>
/// The Administrator or the manager cancels a pending request (ADR-0095): it leaves the list with its history and the
/// term to sign, and stays only in the audit.
/// </summary>
public sealed class CancelAcceptance(AcceptanceAreas areas, IAcceptanceStore acceptances, IFileStorage files)
{
    public async Task<AcceptanceResult> ExecuteAsync(Guid projectId, Guid termId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (refusal, _, term) = await AcceptanceRefusals.OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
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

/// <summary>The checks the actions on the terms share, in the order of the answers: 404, 403, 409 of the project, 409 of the term.</summary>
internal static class AcceptanceRefusals
{
    /// <summary>The area of who manages the terms of an open project, or the refusal.</summary>
    public static async Task<(AcceptanceResult? Refusal, AcceptanceArea? Area)> OfManagerAsync(
        AcceptanceAreas areas, Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.NotFound), null);
        }

        if (!area.Manages)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.Forbidden), null);
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

    /// <summary>
    /// A pending term the person answers, or the refusal: the person of the request answers it; with
    /// <paramref name="onBehalf"/>, who manages the terms attaches the signed term in the name of the person.
    /// </summary>
    public static async Task<(AcceptanceResult? Refusal, AcceptanceArea? Area, AcceptanceTerm? Term)> OfAnswerAsync(
        AcceptanceAreas areas,
        IAcceptanceStore acceptances,
        Guid projectId,
        Guid termId,
        bool onBehalf,
        DocumentViewer viewer,
        CancellationToken cancellationToken)
    {
        if (onBehalf)
        {
            return await OfManagedTermAsync(areas, acceptances, projectId, termId, viewer, cancellationToken);
        }

        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await acceptances.FindAsync(projectId, termId, withEvents: false, cancellationToken) is not { } term)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.NotFound), null, null);
        }

        if (term.RecipientId != viewer.UserId)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.Forbidden), null, null);
        }

        if (area.Project.IsClosed)
        {
            return (AcceptanceResult.Of(AcceptanceOutcome.ProjectClosed), null, null);
        }

        return term.Status == AcceptanceStatus.Pending ? (null, area, term) : (AcceptanceResult.Of(AcceptanceOutcome.Answered), null, null);
    }
}

/// <summary>
/// The notices of the terms (ADR-0037, ADR-0038), after saving: the request and the reminder to the person of the
/// request, by e-mail now; the signed term and the refusal to who asked and to the manager of the project, in the bell
/// and in the daily summary. The notice leads to the term, never with the file or the reason.
/// </summary>
public sealed class AcceptanceNotices(IEnumerable<INoticeBoard> boards, IProjectAccess projects)
{
    /// <summary>The address of a term in the screens: the tab of the terms with its drawer open.</summary>
    public static string PathOf(Guid projectId, Guid termId) => $"/projects/{projectId}/acceptances?term={termId}";

    public Task RequestedAsync(
        AcceptanceTerm term, ProjectAccess project, string milestone, Guid actorId, bool reminder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(project);
        var kind = reminder ? NoticeKind.AcceptanceReminded : NoticeKind.AcceptanceRequested;
        return boards.PostAsync(
            [new Notice(kind, [term.RecipientId], actorId, project.Id, PathOf(project.Id, term.Id), ValuesOf(term, project, milestone))],
            cancellationToken);
    }

    public async Task AnsweredAsync(AcceptanceTerm term, ProjectAccess project, string milestone, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(project);
        var managers = (await projects.MembersAsync([project.Id], cancellationToken))
            .Where(member => member.Role == ProjectMemberRole.Manager)
            .Select(member => member.UserId);
        var values = ValuesOf(term, project, milestone);
        if (term.Status == AcceptanceStatus.Signed && term.AnsweredById != term.RecipientId)
        {
            values[NoticeValues.Attached] = "true";
        }

        var kind = term.Status == AcceptanceStatus.Signed ? NoticeKind.AcceptanceSigned : NoticeKind.AcceptanceRefused;
        await boards.PostAsync(
            [new Notice(kind, [.. managers.Prepend(term.RequestedById).Distinct()], actorId, project.Id, PathOf(project.Id, term.Id), values)],
            cancellationToken);
    }

    private static Dictionary<string, string> ValuesOf(AcceptanceTerm term, ProjectAccess project, string milestone) =>
        new()
        {
            [NoticeValues.Project] = project.Code,
            [NoticeValues.Title] = term.Title,
            [NoticeValues.Item] = milestone,
        };
}

/// <summary>
/// The requests pending in the projects, for the summary of the closing (ADR-0043) and the next milestones of Meu
/// trabalho (ADR-0096).
/// </summary>
public sealed class ProjectAcceptances(IAcceptanceStore acceptances) : IProjectAcceptances
{
    public Task<int> PendingAsync(Guid projectId, CancellationToken cancellationToken) =>
        acceptances.PendingCountAsync(projectId, cancellationToken);

    public Task<IReadOnlySet<Guid>> RequestedMilestonesAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken) =>
        projectIds.Count == 0
            ? Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>())
            : acceptances.PendingMilestonesAsync(projectIds, cancellationToken);
}

/// <summary>
/// The requests waiting for the answer, for the daily summary (ADR-0037): only in projects not completed nor cancelled
/// (ADR-0095), and of the people still in the project.
/// </summary>
public sealed class PendingAcceptanceDirectory(IAcceptanceStore acceptances, IProjectAccess projects) : IPendingAcceptances
{
    public async Task<IReadOnlyList<PendingAcceptance>> PendingAsync(CancellationToken cancellationToken)
    {
        var pending = await acceptances.PendingAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return [];
        }

        var open = new Dictionary<Guid, ProjectAccess>();
        foreach (var projectId in pending.Select(term => term.ProjectId).Distinct())
        {
            if (await projects.FindAsync(projectId, Guid.Empty, isAdministrator: true, cancellationToken) is { IsClosed: false } project)
            {
                open[projectId] = project;
            }
        }

        var members = (await projects.MembersAsync(open.Keys, cancellationToken))
            .Select(member => (member.ProjectId, member.UserId))
            .ToHashSet();
        return
        [
            .. pending
                .Where(term => open.ContainsKey(term.ProjectId) && members.Contains((term.ProjectId, term.RecipientId)))
                .Select(term => new PendingAcceptance(
                    term.RecipientId, term.ProjectId, open[term.ProjectId].Code, term.Id, term.Title, term.RequestedAt)),
        ];
    }
}
