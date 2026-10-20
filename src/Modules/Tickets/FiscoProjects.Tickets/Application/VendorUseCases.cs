using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>After how many business days without an update a ticket at a vendor is stale (ADR-0028).</summary>
public sealed record VendorStaleSettings(int BusinessDays)
{
    public const int Default = 5;
}

/// <summary>A ticket at a vendor with the name of the vendor.</summary>
public sealed record VendorTicketRow(VendorTicket Record, string VendorName);

/// <summary>A ticket at a vendor as our ticket shows it: with the vendor and, when stale, the business days without an update.</summary>
public sealed record TicketVendorTicket(VendorTicket Record, string VendorName, int? StaleDays);

/// <summary>Another vendor already has the name; nothing was saved.</summary>
public sealed class VendorNameInUseException(Exception inner) : InvalidOperationException("The name of the vendor is in use.", inner);

/// <summary>Where the vendors and the tickets at them are kept.</summary>
public interface IVendorStore
{
    /// <summary>The vendors by name: all of them or only the active ones.</summary>
    Task<IReadOnlyList<Vendor>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    /// <summary>The vendor, tracked.</summary>
    Task<Vendor?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameInUseAsync(string name, Guid? except, CancellationToken cancellationToken);

    void Add(Vendor vendor);

    /// <summary>The tickets at vendors of our tickets, in the order they were registered.</summary>
    Task<IReadOnlyList<VendorTicketRow>> TicketsOfAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken);

    /// <summary>A ticket at a vendor of our ticket, tracked.</summary>
    Task<VendorTicket?> FindTicketAsync(Guid ticketId, Guid id, CancellationToken cancellationToken);

    void AddTicket(VendorTicket vendorTicket);

    /// <summary>Saves; throws <see cref="VendorNameInUseException"/> when another vendor took the name at the same time.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The rule of the stale tickets at vendors in one place (ADR-0028), with the calendar of the installation.</summary>
public static class VendorTickets
{
    /// <summary>
    /// The business days since the last update of a ticket still open at the vendor, from the day after it up to today,
    /// once they reach the setting; null while it is not stale or once the vendor finished it.
    /// </summary>
    public static int? StaleDaysOf(VendorTicket vendorTicket, TicketDay day, VendorStaleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(vendorTicket);
        ArgumentNullException.ThrowIfNull(day);
        ArgumentNullException.ThrowIfNull(settings);
        if (!vendorTicket.IsOpen)
        {
            return null;
        }

        var days = day.Days.CountBusinessDays(day.DayOf(vendorTicket.UpdatedAt).AddDays(1), day.Today);
        return days >= settings.BusinessDays ? days : null;
    }

    /// <summary>
    /// The ticket at a vendor a list shows for each ticket: the open one registered last, or the last one when none is
    /// open.
    /// </summary>
    public static IReadOnlyDictionary<Guid, VendorTicketRow> ShownIn(IEnumerable<VendorTicketRow> rows) =>
        rows.GroupBy(row => row.Record.TicketId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.Record.IsOpen).ThenByDescending(row => row.Record.RegisteredAt).First());

    public static TicketVendorTicket Of(VendorTicketRow row, TicketDay day, VendorStaleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new(row.Record, row.VendorName, StaleDaysOf(row.Record, day, settings));
    }
}

public enum VendorOutcome
{
    Done = 1,
    NotFound = 2,
    Invalid = 3,
    NameInUse = 4,
}

public enum VendorField
{
    Name = 1,
    PortalUrl = 2,
}

/// <summary>What happened to a vendor: its id, or why it was not saved.</summary>
public sealed record VendorResult(VendorOutcome Outcome, Guid? Id, IReadOnlyList<VendorField> Invalid)
{
    public static VendorResult Of(VendorOutcome outcome, Guid? id = null) => new(outcome, id, []);
}

/// <summary>The name and the address of the portal of a vendor, checked: null when one is not valid.</summary>
internal static class VendorForm
{
    public static (string? Name, string? PortalUrl, IReadOnlyList<VendorField> Invalid) Of(string? name, string? portalUrl)
    {
        var invalid = new List<VendorField>();
        var valid = Vendor.NameOf(name);
        if (valid is null)
        {
            invalid.Add(VendorField.Name);
        }

        if (!Vendor.TryUrl(portalUrl, out var url))
        {
            invalid.Add(VendorField.PortalUrl);
        }

        return (valid, url, invalid);
    }
}

/// <summary>The vendors of the installation, for the Administration (ADR-0028, ADR-0046) or, the active ones, for new registrations.</summary>
public sealed class ListVendors(IVendorStore vendors)
{
    public Task<IReadOnlyList<Vendor>> ExecuteAsync(bool activeOnly, CancellationToken cancellationToken) =>
        vendors.ListAsync(activeOnly, cancellationToken);
}

/// <summary>The Administrator includes a vendor, active, with a name no other vendor has (ADR-0028).</summary>
public sealed class AddVendor(IVendorStore vendors)
{
    public async Task<VendorResult> ExecuteAsync(string? name, string? portalUrl, CancellationToken cancellationToken)
    {
        var form = VendorForm.Of(name, portalUrl);
        if (form.Invalid.Count > 0)
        {
            return new VendorResult(VendorOutcome.Invalid, null, form.Invalid);
        }

        if (await vendors.NameInUseAsync(form.Name!, null, cancellationToken))
        {
            return VendorResult.Of(VendorOutcome.NameInUse);
        }

        var vendor = new Vendor(form.Name!, form.PortalUrl);
        vendors.Add(vendor);
        return await SaveAsync(vendors, vendor.Id, cancellationToken);
    }

    internal static async Task<VendorResult> SaveAsync(IVendorStore vendors, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await vendors.SaveChangesAsync(cancellationToken);
        }
        catch (VendorNameInUseException)
        {
            return VendorResult.Of(VendorOutcome.NameInUse);
        }

        return VendorResult.Of(VendorOutcome.Done, id);
    }
}

/// <summary>
/// The Administrator changes the name, the portal and the situation of a vendor (ADR-0028): an inactive one is not
/// offered for new registrations, and the registrations it has keep it.
/// </summary>
public sealed class ChangeVendor(IVendorStore vendors)
{
    public async Task<VendorResult> ExecuteAsync(
        Guid id, string? name, string? portalUrl, bool isActive, CancellationToken cancellationToken)
    {
        var vendor = await vendors.FindAsync(id, cancellationToken);
        if (vendor is null)
        {
            return VendorResult.Of(VendorOutcome.NotFound);
        }

        var form = VendorForm.Of(name, portalUrl);
        if (form.Invalid.Count > 0)
        {
            return new VendorResult(VendorOutcome.Invalid, null, form.Invalid);
        }

        if (await vendors.NameInUseAsync(form.Name!, id, cancellationToken))
        {
            return VendorResult.Of(VendorOutcome.NameInUse);
        }

        vendor.Change(form.Name!, form.PortalUrl, isActive);
        return await AddVendor.SaveAsync(vendors, id, cancellationToken);
    }
}

/// <summary>A ticket opened at a vendor, as the person fills it in.</summary>
public sealed record NewVendorTicket(
    Guid? VendorId, string? Code, DateOnly? OpenedOn, VendorTicketStatus? Status, string? Link, string? Note);

public enum VendorTicketField
{
    Vendor = 1,
    Code = 2,
    OpenedOn = 3,
    Status = 4,
    Link = 5,
    Note = 6,
}

public enum VendorTicketOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    Invalid = 4,

    /// <summary>The ticket is closed.</summary>
    Closed = 5,

    /// <summary>Someone changed the ticket at the same time; nothing was saved.</summary>
    Changed = 6,
}

/// <summary>What happened to a ticket at a vendor: its id, or why it was not saved.</summary>
public sealed record VendorTicketResult(VendorTicketOutcome Outcome, Guid? Id, IReadOnlyList<VendorTicketField> Invalid)
{
    public static VendorTicketResult Of(VendorTicketOutcome outcome, Guid? id = null) => new(outcome, id, []);

    public static VendorTicketResult InvalidFields(IReadOnlyList<VendorTicketField> invalid) =>
        new(VendorTicketOutcome.Invalid, null, invalid);
}

/// <summary>Our ticket, for who serves it (ADR-0044): not found, forbidden, closed, or the ticket.</summary>
internal static class VendorTicketAccess
{
    public static async Task<(Ticket? Ticket, VendorTicketOutcome? Refusal)> TicketAsync(
        IProjectAccess projects, ITicketStore store, int number, TicketViewer viewer, CancellationToken cancellationToken)
    {
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return (null, VendorTicketOutcome.NotFound);
        }

        if (!TicketAccess.MayRegisterAtVendor(viewer, project.Role, ticket))
        {
            return (null, VendorTicketOutcome.Forbidden);
        }

        return ticket.Status == TicketStatus.Closed ? (null, VendorTicketOutcome.Closed) : (ticket, null);
    }

    public static async Task<VendorTicketOutcome> SaveAsync(ITicketStore store, CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return VendorTicketOutcome.Done;
        }
        catch (TicketChangedException)
        {
            return VendorTicketOutcome.Changed;
        }
    }
}

/// <summary>
/// Who serves the ticket registers in it a ticket opened at an active vendor (ADR-0028, ADR-0044), until the ticket is
/// closed: the vendor, its number there, the day it was opened (not after today), the status there, the link and a
/// note. The conversation records it; our deadlines keep running.
/// </summary>
public sealed class RegisterVendorTicket(
    IProjectAccess projects, ITicketStore store, IVendorStore vendors, TicketDeadlines deadlines, TimeProvider clock, TicketNotices notices)
{
    public async Task<VendorTicketResult> ExecuteAsync(
        int number, NewVendorTicket request, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);
        var (ticket, refusal) = await VendorTicketAccess.TicketAsync(projects, store, number, viewer, cancellationToken);
        if (ticket is null)
        {
            return VendorTicketResult.Of(refusal!.Value);
        }

        var invalid = new List<VendorTicketField>();
        var vendor = request.VendorId is { } vendorId ? await vendors.FindAsync(vendorId, cancellationToken) : null;
        if (vendor is not { IsActive: true })
        {
            // An inactive vendor takes no new registrations (ADR-0028).
            invalid.Add(VendorTicketField.Vendor);
        }

        var code = request.Code?.Trim();
        if (code is not { Length: > 0 and <= VendorTicket.CodeMaxLength })
        {
            invalid.Add(VendorTicketField.Code);
        }

        var today = (await deadlines.TodayAsync(cancellationToken)).Today;
        if (request.OpenedOn is not { } openedOn || openedOn > today)
        {
            invalid.Add(VendorTicketField.OpenedOn);
        }

        if (request.Status is not { } status || !Enum.IsDefined(status))
        {
            invalid.Add(VendorTicketField.Status);
        }

        if (!Vendor.TryUrl(request.Link, out var link))
        {
            invalid.Add(VendorTicketField.Link);
        }

        if (!VendorTicket.TryNote(request.Note, out var note))
        {
            invalid.Add(VendorTicketField.Note);
        }

        if (invalid.Count > 0)
        {
            return VendorTicketResult.InvalidFields(invalid);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var registered = new VendorTicket(
            ticket.Id, vendor!.Id, code!, request.OpenedOn!.Value, request.Status!.Value, link, note, viewer.UserId, now);
        vendors.AddTicket(registered);
        store.AddEvent(TicketEvent.VendorTicketRegistered(ticket.Id, viewer.UserId, registered, now));
        ticket.Touch(now);
        var outcome = await VendorTicketAccess.SaveAsync(store, cancellationToken);
        if (outcome == VendorTicketOutcome.Done)
        {
            // Who follows the ticket hears about it in the daily summary (ADR-0037).
            await notices.VendorUpdatedAsync(ticket, vendor.Name, registered.Status, viewer.UserId, cancellationToken);
        }

        return VendorTicketResult.Of(outcome, outcome == VendorTicketOutcome.Done ? registered.Id : null);
    }
}

/// <summary>
/// Who serves the ticket updates the status of a ticket at a vendor, with a note (ADR-0028), until our ticket is closed:
/// any status follows any other, and the conversation records the update, which also ends the staleness.
/// </summary>
public sealed class UpdateVendorTicket(
    IProjectAccess projects, ITicketStore store, IVendorStore vendors, TimeProvider clock, TicketNotices notices)
{
    public async Task<VendorTicketResult> ExecuteAsync(
        int number, Guid id, VendorTicketStatus? status, string? note, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (ticket, refusal) = await VendorTicketAccess.TicketAsync(projects, store, number, viewer, cancellationToken);
        if (ticket is null)
        {
            return VendorTicketResult.Of(refusal!.Value);
        }

        var vendorTicket = await vendors.FindTicketAsync(ticket.Id, id, cancellationToken);
        if (vendorTicket is null)
        {
            return VendorTicketResult.Of(VendorTicketOutcome.NotFound);
        }

        var invalid = new List<VendorTicketField>();
        if (status is not { } chosen || !Enum.IsDefined(chosen))
        {
            invalid.Add(VendorTicketField.Status);
        }

        if (!VendorTicket.TryNote(note, out var text))
        {
            invalid.Add(VendorTicketField.Note);
        }

        if (invalid.Count > 0)
        {
            return VendorTicketResult.InvalidFields(invalid);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var before = vendorTicket.Status;
        vendorTicket.ChangeStatus(status!.Value, now);
        store.AddEvent(TicketEvent.VendorStatusChanged(ticket.Id, viewer.UserId, vendorTicket.Id, before, status.Value, text, now));
        ticket.Touch(now);
        var outcome = await VendorTicketAccess.SaveAsync(store, cancellationToken);
        if (outcome == VendorTicketOutcome.Done && await vendors.FindAsync(vendorTicket.VendorId, cancellationToken) is { } vendor)
        {
            // Who follows the ticket hears about it in the daily summary (ADR-0037).
            await notices.VendorUpdatedAsync(ticket, vendor.Name, vendorTicket.Status, viewer.UserId, cancellationToken);
        }

        return VendorTicketResult.Of(outcome, outcome == VendorTicketOutcome.Done ? vendorTicket.Id : null);
    }
}
