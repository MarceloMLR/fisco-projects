using FiscoProjects.SharedKernel;

namespace FiscoProjects.Tickets.Domain;

/// <summary>
/// A vendor the company opens tickets with (ADR-0028), registered by the Administrator: none is fixed in the code
/// (ADR-0002). It is never removed, only deactivated, since the tickets keep it.
/// </summary>
public sealed class Vendor : Entity
{
    public const int NameMaxLength = 150;
    public const int UrlMaxLength = 500;

    private Vendor()
    {
        Name = string.Empty;
    }

    public Vendor(string name, string? portalUrl)
    {
        Name = name;
        PortalUrl = portalUrl;
        IsActive = true;
    }

    public string Name { get; private set; }

    /// <summary>The support portal of the vendor, where its tickets are opened.</summary>
    public string? PortalUrl { get; private set; }

    public bool IsActive { get; private set; }

    public void Change(string name, string? portalUrl, bool isActive)
    {
        Name = name;
        PortalUrl = portalUrl;
        IsActive = isActive;
    }

    /// <summary>A name with something in it, up to 150 characters, trimmed; null when it is not valid.</summary>
    public static string? NameOf(string? name) =>
        name?.Trim() is { Length: > 0 and <= NameMaxLength } trimmed ? trimmed : null;

    /// <summary>
    /// An address of the web, http or https, up to 500 characters; empty is none. False when it is not valid.
    /// </summary>
    public static bool TryUrl(string? text, out string? url)
    {
        url = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return url is null
            || (url.Length <= UrlMaxLength
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
    }
}

/// <summary>Where a ticket stands at the vendor (ADR-0028); the vendor decides its own cycle, so any status follows any other.</summary>
public enum VendorTicketStatus
{
    Open = 1,
    InAnalysis = 2,
    WaitingForUs = 3,
    Resolved = 4,
    Closed = 5,
}

/// <summary>
/// A ticket opened at a vendor, registered in our ticket (ADR-0028): the vendor, its number there, the day it was
/// opened, the status there, the link and a note. Our ticket keeps its own flow and deadlines.
/// </summary>
public sealed class VendorTicket : Entity
{
    public const int CodeMaxLength = 60;
    public const int NoteMaxLength = 1000;

    private VendorTicket()
    {
        Code = string.Empty;
    }

    public VendorTicket(
        Guid ticketId, Guid vendorId, string code, DateOnly openedOn, VendorTicketStatus status, string? link, string? note, Guid registeredById, DateTime now)
    {
        TicketId = ticketId;
        VendorId = vendorId;
        Code = code;
        OpenedOn = openedOn;
        Status = status;
        Link = link;
        Note = note;
        RegisteredById = registeredById;
        RegisteredAt = now;
        UpdatedAt = now;
    }

    public Guid TicketId { get; private init; }

    public Guid VendorId { get; private init; }

    /// <summary>The number of the ticket at the vendor, as FX-20931.</summary>
    public string Code { get; private init; }

    public DateOnly OpenedOn { get; private init; }

    public VendorTicketStatus Status { get; private set; }

    public string? Link { get; private init; }

    public string? Note { get; private init; }

    public Guid RegisteredById { get; private init; }

    public DateTime RegisteredAt { get; private init; }

    /// <summary>The last time someone updated it, for the vendor without an answer (ADR-0028).</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>The vendor still works on it: open, in analysis or waiting for us.</summary>
    public bool IsOpen => Status is VendorTicketStatus.Open or VendorTicketStatus.InAnalysis or VendorTicketStatus.WaitingForUs;

    public void ChangeStatus(VendorTicketStatus status, DateTime now)
    {
        Status = status;
        UpdatedAt = now;
    }

    /// <summary>A note with something in it, up to 1,000 characters, trimmed; empty is none. False when too long.</summary>
    public static bool TryNote(string? text, out string? note)
    {
        note = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return note is null || note.Length <= NoteMaxLength;
    }
}
