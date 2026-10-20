namespace FiscoProjects.Tickets.Contracts;

/// <summary>The types of the items of the Tickets module that audit records point to (ADR-0084).</summary>
public static class TicketsAudit
{
    /// <summary>The type of a field that holds the id of a ticket, shown on the Audit screen by its number and title.</summary>
    public const string TicketReference = "Ticket";

    /// <summary>The type of a field that holds the id of a vendor, shown on the Audit screen by the vendor name.</summary>
    public const string VendorReference = "Vendor";
}
