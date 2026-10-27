using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Outside production, the e-mails to the domains of the demonstration stay in the local folder, and the others leave
/// through Microsoft Graph (ADR-0123): a demonstration that sends for real never writes to fictitious addresses.
/// </summary>
internal sealed class EmailRouting(IEmailSender graph, IEmailSender local, IReadOnlyCollection<string> localDomains) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return IsLocal(message.ToAddress, localDomains)
            ? local.SendAsync(message, cancellationToken)
            : graph.SendAsync(message, cancellationToken);
    }

    /// <summary>Whether the domain of the address is one of the local ones, without telling case apart.</summary>
    public static bool IsLocal(string address, IReadOnlyCollection<string> localDomains)
    {
        ArgumentNullException.ThrowIfNull(address);
        var at = address.LastIndexOf('@');
        var domain = at < 0 ? string.Empty : address[(at + 1)..].Trim();
        return domain.Length > 0
            && localDomains.Any(local => string.Equals(local.Trim(), domain, StringComparison.OrdinalIgnoreCase));
    }
}
