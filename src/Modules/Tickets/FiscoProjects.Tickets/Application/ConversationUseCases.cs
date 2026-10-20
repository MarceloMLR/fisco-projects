using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>The size of the attachments the installation takes (<c>Tickets:Attachments:MaxMegabytes</c>, 15 by default).</summary>
public sealed record TicketAttachmentSettings(long MaxBytes);

/// <summary>A file sent with a message or with the description, still to be checked. Its content can be read from the start again.</summary>
public sealed record NewAttachment(string Name, long Size, Stream Content);

public enum ConversationOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    InvalidBody = 4,
    MentionRefused = 5,
    AttachmentRefused = 6,

    /// <summary>The ticket is closed and takes no more messages (ADR-0024).</summary>
    Closed = 7,

    /// <summary>Someone changed the ticket at the same time; nothing was saved.</summary>
    Changed = 8,
}

/// <summary>What happened: done, or why not; with a refused file, its name and the reason.</summary>
public sealed record ConversationResult(ConversationOutcome Outcome, string? FileName = null, AttachmentRefusal Refusal = AttachmentRefusal.None)
{
    public static readonly ConversationResult Done = new(ConversationOutcome.Done);
}

/// <summary>Keeps the files of a ticket in its folder, made with the first one (ADR-0030).</summary>
public sealed class TicketFiles(IFileStorage files, TicketAttachmentSettings settings)
{
    /// <summary>Why one of the files is refused; null when all are taken. Each content is read back from its start.</summary>
    public async Task<ConversationResult?> RefusalOfAsync(IReadOnlyList<NewAttachment> attachments, CancellationToken cancellationToken)
    {
        if (attachments.Count > TicketAttachments.MaxFilesPerMessage)
        {
            return new ConversationResult(ConversationOutcome.AttachmentRefused, null, AttachmentRefusal.TooLarge);
        }

        foreach (var attachment in attachments)
        {
            var start = new byte[8];
            var read = await attachment.Content.ReadAtLeastAsync(start, start.Length, throwOnEndOfStream: false, cancellationToken);
            attachment.Content.Position = 0;
            var refusal = TicketAttachments.Check(attachment.Name, attachment.Size, start.AsSpan(0, read), settings.MaxBytes);
            if (refusal != AttachmentRefusal.None)
            {
                return new ConversationResult(ConversationOutcome.AttachmentRefused, attachment.Name, refusal);
            }
        }

        return null;
    }

    /// <summary>
    /// Stores the files in the folder of the ticket, "{code} - {name}/Chamados/#{number}" under the root while the
    /// project has no folder of its own (ADR-0030), keeping both when a name repeats.
    /// </summary>
    public async Task<IReadOnlyList<TicketAttachment>> StoreAsync(
        Ticket ticket,
        ProjectAccess project,
        Guid? messageId,
        IReadOnlyList<NewAttachment> attachments,
        Guid uploadedById,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (attachments.Count == 0)
        {
            return [];
        }

        var folder = ticket.AttachmentFolderId
            ?? (await files.EnsureFolderAsync(
                null, $"{project.Code} - {project.Name}/Chamados/#{ticket.Number}", cancellationToken)).Id;
        ticket.KeepAttachmentFolder(folder);
        var stored = new List<TicketAttachment>();
        try
        {
            foreach (var attachment in attachments)
            {
                var file = await files.UploadAsync(folder, attachment.Name, attachment.Content, SameNameAction.KeepBoth, cancellationToken);
                stored.Add(new TicketAttachment(
                    ticket.Id, messageId, file.Id, file.Name, file.Size, TicketAttachments.ContentTypeOf(file.Name), uploadedById, now));
            }
        }
        catch
        {
            await RemoveAsync(stored, CancellationToken.None);
            throw;
        }

        return stored;
    }

    /// <summary>Removes files stored for a message that was not saved.</summary>
    public async Task RemoveAsync(IEnumerable<TicketAttachment> attachments, CancellationToken cancellationToken)
    {
        foreach (var attachment in attachments)
        {
            await files.DeleteFileAsync(attachment.FileId, cancellationToken);
        }
    }

    public Task<FileDownload?> OpenAsync(TicketAttachment attachment, CancellationToken cancellationToken) =>
        files.OpenAsync(attachment.FileId, null, cancellationToken);
}

/// <summary>
/// Sends a message, with its files, in a ticket (ADR-0026, ADR-0044): by the Administrator, the managers, who serves
/// the team and the requester; mentioning only who may be mentioned, who then follows the ticket.
/// </summary>
/// <summary>
/// What a message of the conversation says about who serves the ticket (ADR-0025, ADR-0068), whether it comes alone or
/// with an action of the flow: the first one of who serves its team meets the first answer and, without an assignee,
/// makes the person the assignee, who follows the ticket. The requester who also serves the team still answers as the
/// requester; the manager who does not serve it and who only follows never take the ticket.
/// </summary>
public sealed class TicketAnswers(ITicketStore store)
{
    /// <summary>Whether the message made the person the assignee.</summary>
    public async Task<bool> ClaimAsync(
        Ticket ticket, ProjectAccess project, TicketViewer viewer, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(viewer);
        var serves = ticket.RequesterId != viewer.UserId
            && project.Role is { } role && TicketAccess.Serves(ticket.Team, role);
        if (!serves)
        {
            return false;
        }

        ticket.RespondFirst(now);
        if (ticket.AssigneeId is not null)
        {
            return false;
        }

        ticket.Assign(viewer.UserId, now);
        store.AddEvent(TicketEvent.AssigneeSet(ticket.Id, viewer.UserId, viewer.UserId, now));
        await store.FollowAsync(ticket.Id, viewer.UserId, now, cancellationToken);
        return true;
    }
}

public sealed class SendTicketMessage(
    IProjectAccess projects,
    ITicketStore store,
    TicketFiles files,
    TicketDeadlines deadlines,
    TicketAnswers answers,
    TimeProvider clock,
    TicketNotices notices)
{
    public async Task<ConversationResult> ExecuteAsync(
        int number, string? body, IReadOnlyList<NewAttachment> attachments, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return new ConversationResult(ConversationOutcome.NotFound);
        }

        if (!TicketAccess.MaySend(viewer, project.Role, ticket))
        {
            return new ConversationResult(ConversationOutcome.Forbidden);
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return new ConversationResult(ConversationOutcome.Closed);
        }

        if (RichText.Parse(body) is not { } text)
        {
            return new ConversationResult(ConversationOutcome.InvalidBody);
        }

        var mentionable = TicketAccess.Mentionable(ticket, await projects.MembersAsync([project.Id], cancellationToken));
        if (text.Mentions.Any(person => !mentionable.Contains(person)))
        {
            return new ConversationResult(ConversationOutcome.MentionRefused);
        }

        if (await files.RefusalOfAsync(attachments, cancellationToken) is { } refused)
        {
            return refused;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var message = new TicketMessage(ticket.Id, viewer.UserId, text, now);
        var stored = await files.StoreAsync(ticket, project, message.Id, attachments, viewer.UserId, now, cancellationToken);
        try
        {
            store.AddMessage(message);
            foreach (var attachment in stored)
            {
                store.AddAttachment(attachment);
            }

            // Who is mentioned starts to follow the ticket (ADR-0026).
            foreach (var person in text.Mentions)
            {
                await store.FollowAsync(ticket.Id, person, now, cancellationToken);
            }

            ticket.Touch(now);
            await AnswerAsync(ticket, project, viewer, now, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (TicketChangedException)
        {
            await files.RemoveAsync(stored, CancellationToken.None);
            return new ConversationResult(ConversationOutcome.Changed);
        }
        catch
        {
            await files.RemoveAsync(stored, CancellationToken.None);
            throw;
        }

        // The mentioned people and who follows the ticket hear about it now (ADR-0037).
        await notices.MessageAsync(ticket, project, viewer.UserId, text.Mentions, transition: null, cancellationToken);
        return ConversationResult.Done;
    }

    /// <summary>
    /// What an answer does to the ticket (ADR-0024, ADR-0068): the first one of who serves its team, without an
    /// assignee, makes the person the assignee, who follows the ticket, and takes an Open ticket to In progress; the
    /// answer of the requester, or of who serves it, to a ticket Waiting for the requester takes it back to In
    /// progress. The answers of the requester, of the manager and of who only follows never set the assignee.
    /// </summary>
    private async Task AnswerAsync(
        Ticket ticket, ProjectAccess project, TicketViewer viewer, DateTime now, CancellationToken cancellationToken)
    {
        var flow = await store.FlowAsync(cancellationToken);
        if (await answers.ClaimAsync(ticket, project, viewer, now, cancellationToken)
            && flow.Of(ticket.Status, TicketAction.FirstResponse) is { } first)
        {
            await deadlines.TakeAsync(ticket, first, viewer.UserId, now, cancellationToken);
        }

        if (flow.Of(ticket.Status, TicketAction.Reply) is { } reply
            && (reply.Actors & TicketAccess.ActorsOf(viewer, project.Role, ticket)) != 0)
        {
            await deadlines.TakeAsync(ticket, reply, viewer.UserId, now, cancellationToken);
        }
    }
}

/// <summary>
/// Attaches files to the description of a ticket, right after opening it (ADR-0069): only the requester, and only while
/// the ticket has no messages.
/// </summary>
public sealed class AttachToDescription(IProjectAccess projects, ITicketStore store, TicketFiles files, TimeProvider clock)
{
    public async Task<ConversationResult> ExecuteAsync(
        int number, IReadOnlyList<NewAttachment> attachments, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return new ConversationResult(ConversationOutcome.NotFound);
        }

        if (ticket.RequesterId != viewer.UserId || await store.HasMessagesAsync(ticket.Id, cancellationToken))
        {
            return new ConversationResult(ConversationOutcome.Forbidden);
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return new ConversationResult(ConversationOutcome.Closed);
        }

        if (attachments.Count == 0)
        {
            return new ConversationResult(ConversationOutcome.AttachmentRefused, null, AttachmentRefusal.Empty);
        }

        if (await files.RefusalOfAsync(attachments, cancellationToken) is { } refused)
        {
            return refused;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var stored = await files.StoreAsync(ticket, project, null, attachments, viewer.UserId, now, cancellationToken);
        try
        {
            foreach (var attachment in stored)
            {
                store.AddAttachment(attachment);
            }

            ticket.Touch(now);
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (TicketChangedException)
        {
            await files.RemoveAsync(stored, CancellationToken.None);
            return new ConversationResult(ConversationOutcome.Changed);
        }
        catch
        {
            await files.RemoveAsync(stored, CancellationToken.None);
            throw;
        }

        return ConversationResult.Done;
    }
}

/// <summary>Follows a ticket, or stops following it (ADR-0026): anyone who sees it.</summary>
public sealed class FollowTicket(IProjectAccess projects, ITicketStore store, TimeProvider clock)
{
    /// <summary>False when the person does not see the ticket.</summary>
    public async Task<bool> ExecuteAsync(int number, bool follow, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        if (ticket is null
            || await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is null)
        {
            return false;
        }

        if (follow)
        {
            await store.FollowAsync(ticket.Id, viewer.UserId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        }
        else
        {
            await store.UnfollowAsync(ticket.Id, viewer.UserId, cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>An attachment of a ticket with its content, for who sees the ticket.</summary>
public sealed record OpenedAttachment(TicketAttachment Attachment, FileDownload File);

/// <summary>Opens an attachment of a ticket (ADR-0030): only who sees the ticket; null for anyone else.</summary>
public sealed class OpenTicketAttachment(IProjectAccess projects, ITicketStore store, TicketFiles files)
{
    public async Task<OpenedAttachment?> ExecuteAsync(
        int number, Guid attachmentId, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        if (ticket is null
            || await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is null)
        {
            return null;
        }

        var attachment = (await store.AttachmentsAsync(ticket.Id, cancellationToken))
            .SingleOrDefault(candidate => candidate.Id == attachmentId);
        if (attachment is null || await files.OpenAsync(attachment, cancellationToken) is not { } file)
        {
            return null;
        }

        return new OpenedAttachment(attachment, file);
    }
}
