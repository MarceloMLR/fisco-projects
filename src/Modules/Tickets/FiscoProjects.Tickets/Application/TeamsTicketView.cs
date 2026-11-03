using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>
/// The ticket as the page of FiscoProjects in Teams shows it, only to read (ADR-0152): the last interactions of the
/// conversation, the description counted as the first one, in the order of the conversation of the ticket page, and how
/// many came before, from the same reading as the page of the ticket, with the same rules of who sees it (ADR-0044).
/// </summary>
public sealed record TeamsTicketView(
    TicketDetails Details,
    bool ShowsDescription,
    IReadOnlyList<TicketMessage> Messages,
    IReadOnlyList<TicketEvent> Events,
    IReadOnlyList<TicketAttachment> Attachments,
    int Earlier)
{
    /// <summary>How many interactions the page in Teams shows; the others stay in FiscoProjects.</summary>
    public const int RecentInteractions = 5;

    public static TeamsTicketView Of(TicketDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        // The description first, then the messages and the events as they happened; at the same moment, the message
        // before the change it made, as in the conversation of the ticket page (OrderBy is stable).
        List<(DateTime At, object Item)> interactions =
        [
            (details.Ticket.OpenedAt, details.Ticket),
            .. details.Messages.Select(message => (message.SentAt, (object)message))
                .Concat(details.Events.Select(item => (item.At, (object)item)))
                .OrderBy(pair => pair.Item1),
        ];
        var earlier = Math.Max(0, interactions.Count - RecentInteractions);
        var shown = interactions.Skip(earlier).Select(pair => pair.Item).ToList();
        var showsDescription = shown.Contains(details.Ticket);
        var messages = shown.OfType<TicketMessage>().ToList();
        var shownMessages = messages.Select(message => message.Id).ToHashSet();
        var attachments = details.Attachments
            .Where(attachment => attachment.MessageId is { } messageId ? shownMessages.Contains(messageId) : showsDescription)
            .ToList();
        return new TeamsTicketView(details, showsDescription, messages, [.. shown.OfType<TicketEvent>()], attachments, earlier);
    }
}
