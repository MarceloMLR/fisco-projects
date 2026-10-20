using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Writes each e-mail as a .eml file in a folder, without sending it (ADR-0004, ADR-0036): any e-mail program opens it,
/// for development and tests.
/// </summary>
internal sealed class LocalEmailSender(string folder, string? mailbox) : IEmailSender
{
    /// <summary>The sender when the installation has no mailbox yet, in development.</summary>
    private const string LocalSender = "fiscoprojects@localhost.example";

    public string Folder { get; } = folder;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        Directory.CreateDirectory(Folder);
        using var mail = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(mailbox) ? LocalSender : mailbox),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Body = message.TextBody,
            IsBodyHtml = false,
        };
        mail.To.Add(new MailAddress(message.ToAddress, message.ToName, Encoding.UTF8));
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));
        // Delivered to the folder, never to a server.
        using var client = new SmtpClient
        {
            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = Path.GetFullPath(Folder),
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
