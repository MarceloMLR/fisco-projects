using FiscoProjects.Administration.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Application;

/// <summary>An e-mail ready to leave: the recipient, the subject and the text, in HTML and plain.</summary>
public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string HtmlBody, string TextBody);

/// <summary>
/// Sends an e-mail now (ADR-0004, ADR-0036): the local implementation, which writes it to a folder, in development and
/// tests; the Microsoft Graph from the shared mailbox in production (M365-4). Only the queue calls it.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends the e-mail, or throws when it could not leave.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// What an e-mail says, as the keys of the texts of the e-mails (ADR-0013) and their values: the subject, the
/// paragraphs, and the button with the address of the item in the screens (ADR-0037); a summary has sections of lines,
/// each line with the address of its item.
/// </summary>
public sealed record EmailContent(
    string SubjectKey,
    IReadOnlyList<string> ParagraphKeys,
    IReadOnlyDictionary<string, string> Values,
    string? ActionKey = null,
    string? Path = null)
{
    public IReadOnlyList<EmailSection> Sections { get; init; } = [];

    /// <summary>A last paragraph, after the sections and the button, as how to stop receiving the summary.</summary>
    public string? NoteKey { get; init; }
}

/// <summary>A titled list of lines of a summary.</summary>
public sealed record EmailSection(string TitleKey, IReadOnlyList<EmailLine> Lines);

/// <summary>A line of a summary: the text, by its key and values, and the address of its item.</summary>
public sealed record EmailLine(string Key, IReadOnlyDictionary<string, string> Values, string? Path);

/// <summary>The texts of an e-mail, put in the layout of the installation.</summary>
public sealed record RenderedEmail(string Subject, string HtmlBody, string TextBody);

/// <summary>Puts the texts of an e-mail in the layout, with the name of the installation and the colors of the design system.</summary>
public interface IEmailRenderer
{
    RenderedEmail Render(EmailContent content, string installationName);
}

/// <summary>The e-mail settings of the installation: the shared mailbox and how many days the queue keeps the content.</summary>
public sealed record EmailSettings(string? Mailbox, int KeepDays);

/// <summary>Where the e-mails of the queue are kept.</summary>
public interface IOutboxStore
{
    void Add(OutboxEmail email);

    /// <summary>The e-mails whose turn came, the oldest first, tracked.</summary>
    Task<IReadOnlyList<OutboxEmail>> DueAsync(DateTime now, int take, CancellationToken cancellationToken);

    /// <summary>The e-mails sent or failed before the moment whose content is still kept, tracked.</summary>
    Task<IReadOnlyList<OutboxEmail>> ToForgetAsync(DateTime before, int take, CancellationToken cancellationToken);

    /// <summary>The last e-mail of the kind, or null.</summary>
    Task<OutboxEmail?> LastOfKindAsync(string kind, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Saves the reservation of an e-mail; false when another process changed it first, and nothing was saved.</summary>
    Task<bool> TrySaveAsync(OutboxEmail email, CancellationToken cancellationToken);
}

/// <summary>The kinds of e-mails of the queue, kept after the content is forgotten.</summary>
public static class EmailKinds
{
    /// <summary>The test of the sending, by the Administrator.</summary>
    public const string Test = "Test";
}

/// <summary>
/// Puts an e-mail in the queue (ADR-0036), rendered with the name of the installation; the caller saves it with the
/// rest of the work.
/// </summary>
public sealed class EmailQueue(IOutboxStore store, IEmailRenderer renderer, IInstallationSettings installation, TimeProvider clock)
{
    private string? _installationName;

    public async Task EnqueueAsync(string kind, string toAddress, string toName, EmailContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        _installationName ??= (await installation.GetAsync(cancellationToken)).Name;
        var rendered = renderer.Render(content, _installationName);
        store.Add(new OutboxEmail(
            kind, toAddress, toName, rendered.Subject, rendered.HtmlBody, rendered.TextBody, clock.GetUtcNow().UtcDateTime));
    }
}

/// <summary>A failure of a try, for the log: the e-mail and the error, never the content.</summary>
public sealed record EmailFailure(Guid EmailId, int Attempts, bool Final, string Error);

/// <summary>What one round of the sending did.</summary>
public sealed record DispatchResult(int Sent, IReadOnlyList<EmailFailure> Failures);

/// <summary>
/// Sends the e-mails whose turn came (ADR-0036): each one is reserved before it is sent, so no other process sends it
/// at the same time, then marked sent, or failed with the wait for the next try.
/// </summary>
public sealed class DispatchEmails(IOutboxStore store, IEmailSender sender, TimeProvider clock)
{
    /// <summary>The most e-mails of one round; the next round takes the rest.</summary>
    public const int BatchSize = 20;

    public async Task<DispatchResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sent = 0;
        var failures = new List<EmailFailure>();
        var due = await store.DueAsync(clock.GetUtcNow().UtcDateTime, BatchSize, cancellationToken);
        foreach (var email in due)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            if (!email.IsDue(now))
            {
                continue;
            }

            email.Reserve(now);
            if (!await store.TrySaveAsync(email, cancellationToken))
            {
                // Another process took it first.
                continue;
            }

            try
            {
                await sender.SendAsync(
                    new EmailMessage(email.ToAddress!, email.ToName ?? string.Empty, email.Subject!, email.HtmlBody!, email.TextBody!),
                    cancellationToken);
                email.MarkSent(clock.GetUtcNow().UtcDateTime);
                sent++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                email.MarkFailed(clock.GetUtcNow().UtcDateTime, $"{exception.GetType().Name}: {exception.Message}");
                failures.Add(new EmailFailure(email.Id, email.Attempts, email.Status == OutboxEmailStatus.Failed, email.LastError!));
            }

            await store.TrySaveAsync(email, cancellationToken);
        }

        return new DispatchResult(sent, failures);
    }
}

/// <summary>
/// Forgets the content of the e-mails sent or failed before the days the installation keeps them (ADR-0058): the
/// day, the kind and the status stay.
/// </summary>
public sealed class ForgetOldEmails(IOutboxStore store, EmailSettings settings, TimeProvider clock)
{
    private const int BatchSize = 200;

    /// <summary>How many e-mails were forgotten.</summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var before = now.AddDays(-settings.KeepDays);
        var forgotten = 0;
        while (true)
        {
            var emails = await store.ToForgetAsync(before, BatchSize, cancellationToken);
            foreach (var email in emails)
            {
                email.Forget(now);
            }

            await store.SaveChangesAsync(cancellationToken);
            forgotten += emails.Count;
            if (emails.Count < BatchSize)
            {
                return forgotten;
            }
        }
    }
}

/// <summary>The last test of the sending: where it stands, when and to whom.</summary>
public sealed record EmailTest(OutboxEmailStatus Status, DateTime At, string? To);

/// <summary>The shared mailbox and the last test of the sending, for the Administration (ADR-0036, ADR-0046).</summary>
public sealed record EmailStatus(string? Mailbox, EmailTest? LastTest);

public sealed class GetEmailStatus(IOutboxStore store, EmailSettings settings)
{
    public async Task<EmailStatus> ExecuteAsync(CancellationToken cancellationToken)
    {
        var last = await store.LastOfKindAsync(EmailKinds.Test, cancellationToken);
        return new EmailStatus(
            settings.Mailbox,
            last is null ? null : new EmailTest(last.Status, last.SentAt ?? last.FailedAt ?? last.CreatedAt, last.ToAddress));
    }
}

/// <summary>
/// The Administrator puts in the queue an e-mail to the own address, to see the sending working (ADR-0036); returns
/// the address, or null when the person has none.
/// </summary>
public sealed class SendTestEmail(IUserDirectory users, EmailQueue queue, IOutboxStore store)
{
    public async Task<string?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var person = (await users.PeopleAsync([userId], cancellationToken)).SingleOrDefault();
        if (person is not { Email.Length: > 0 })
        {
            return null;
        }

        await queue.EnqueueAsync(
            EmailKinds.Test,
            person.Email,
            person.Name,
            new EmailContent(
                "test.subject",
                ["test.greeting", "test.body"],
                new Dictionary<string, string> { ["name"] = person.Name },
                "test.action",
                "/admin/settings"),
            cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return person.Email;
    }
}
