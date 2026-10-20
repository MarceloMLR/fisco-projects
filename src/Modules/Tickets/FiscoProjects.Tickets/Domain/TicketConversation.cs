using FiscoProjects.Files.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Tickets.Domain;

/// <summary>A message of the conversation of a ticket (ADR-0026): seen by everyone who sees the ticket, never changed.</summary>
public sealed class TicketMessage : Entity
{
    private TicketMessage()
    {
        Body = string.Empty;
        Text = string.Empty;
    }

    public TicketMessage(Guid ticketId, Guid authorId, RichText body, DateTime sentAt)
    {
        ArgumentNullException.ThrowIfNull(body);
        TicketId = ticketId;
        AuthorId = authorId;
        Body = body.Json;
        Text = body.Text;
        SentAt = sentAt;
    }

    public Guid TicketId { get; private init; }

    public Guid AuthorId { get; private init; }

    /// <summary>The text with its formatting, as the JSON of the editor (<see cref="RichText"/>).</summary>
    public string Body { get; private init; }

    /// <summary>The plain text, for the search and the e-mails.</summary>
    public string Text { get; private init; }

    public DateTime SentAt { get; private init; }
}

/// <summary>
/// A file of a ticket: of a message, or of the description when the message is null (ADR-0026). The file itself is in
/// the file storage (ADR-0030); the ticket keeps its id, name, size and type.
/// </summary>
public sealed class TicketAttachment : Entity
{
    private TicketAttachment()
    {
        FileId = string.Empty;
        Name = string.Empty;
        ContentType = string.Empty;
    }

    public TicketAttachment(
        Guid ticketId, Guid? messageId, string fileId, string name, long size, string contentType, Guid uploadedById, DateTime uploadedAt)
    {
        TicketId = ticketId;
        MessageId = messageId;
        FileId = fileId;
        Name = name;
        Size = size;
        ContentType = contentType;
        UploadedById = uploadedById;
        UploadedAt = uploadedAt;
    }

    public Guid TicketId { get; private init; }

    public Guid? MessageId { get; private init; }

    public string FileId { get; private init; }

    public string Name { get; private init; }

    public long Size { get; private init; }

    public string ContentType { get; private init; }

    public Guid UploadedById { get; private init; }

    public DateTime UploadedAt { get; private init; }
}

/// <summary>A person who follows a ticket (ADR-0026): the requester, who is mentioned, and who chooses to.</summary>
public sealed class TicketFollower : Entity
{
    private TicketFollower()
    {
    }

    public TicketFollower(Guid ticketId, Guid userId, DateTime since)
    {
        TicketId = ticketId;
        UserId = userId;
        Since = since;
    }

    public Guid TicketId { get; private init; }

    public Guid UserId { get; private init; }

    public DateTime Since { get; private init; }
}

/// <summary>
/// The files a ticket takes (ADR-0026): up to the size of the installation, never a program or a script, by the rules
/// every use of files shares (<see cref="FileTypes"/>). Images and PDF open in the browser; the rest is downloaded.
/// </summary>
public static class TicketAttachments
{
    /// <summary>The size an installation takes when it does not say: 15 MB per file.</summary>
    public const int DefaultMaxMegabytes = 15;

    /// <summary>The most files in one message.</summary>
    public const int MaxFilesPerMessage = 10;
}
