using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Persistence.PersonalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Erases an anonymized person from the notices (ADR-0110), in the transaction of the anonymization: the e-mails to the
/// person are forgotten (one still waiting is not sent), the e-mails still kept for other people name the identifier,
/// and the notifications of the bell, the items of the daily summary and the notices of Teams of the person are removed.
/// </summary>
internal sealed class NotificationsPersonalData(TimeProvider clock) : IPersonalDataEraser
{
    public async Task EraseAsync(DbContext context, PersonErasure erasure, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(erasure);
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The erasure runs in the transaction of the anonymization.");
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlServer(context.Database.GetDbConnection())
            .Options;
        await using var notices = new NotificationsDbContext(options);
        await notices.Database.UseTransactionAsync(transaction, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;

        if (!string.IsNullOrWhiteSpace(erasure.Email))
        {
            var email = erasure.Email.Trim();
            var own = await notices.OutboxEmails
                .Where(message => message.ForgottenAt == null && message.ToAddress == email)
                .ToListAsync(cancellationToken);
            foreach (var message in own)
            {
                message.Withdraw(now);
            }
        }

        if (!string.IsNullOrWhiteSpace(erasure.Name))
        {
            var name = erasure.Name.Trim();
            var htmlName = EmailRenderer.Encode(name);
            var (plain, html) = ($"%{Escaped(name)}%", $"%{Escaped(htmlName)}%");
            var others = await notices.OutboxEmails
                .Where(message => message.ForgottenAt == null && (
                    EF.Functions.Like(message.ToName!, plain, "\\")
                    || EF.Functions.Like(message.Subject!, plain, "\\")
                    || EF.Functions.Like(message.TextBody!, plain, "\\")
                    || EF.Functions.Like(message.HtmlBody!, plain, "\\")
                    || EF.Functions.Like(message.HtmlBody!, html, "\\")))
                .ToListAsync(cancellationToken);
            foreach (var message in others)
            {
                message.Rename(name, erasure.Replacement, htmlName, EmailRenderer.Encode(erasure.Replacement));
            }
        }

        await notices.SaveChangesAsync(cancellationToken);
        await notices.Notifications.Where(item => item.UserId == erasure.PersonId).ExecuteDeleteAsync(cancellationToken);
        await notices.DigestItems.Where(item => item.UserId == erasure.PersonId).ExecuteDeleteAsync(cancellationToken);
        // The notices of Teams keep who did it by the id, so the ones of other people say the identifier when they
        // leave; the ones to the person, even waiting, go away with the channel the person chose (ADR-0149).
        await notices.TeamsNotices.Where(item => item.UserId == erasure.PersonId).ExecuteDeleteAsync(cancellationToken);
        await notices.NoticePreferences.Where(item => item.UserId == erasure.PersonId).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>The text as a literal in a LIKE pattern.</summary>
    private static string Escaped(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
}
