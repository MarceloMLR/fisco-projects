using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Fornecedores", "Chamado no fornecedor" and "Fornecedor parado" (ADR-0028, ADR-0044).
public sealed class VendorTicketTests
{
    private static readonly Guid Requester = Guid.CreateVersion7();

    // Monday, 19/10/2026, 12:00 in UTC: 09:00 in São Paulo.
    private static readonly DateTime Monday = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Monday to Friday, without holidays.</summary>
    private sealed class Weekdays : IBusinessDays
    {
        public bool IsBusinessDay(DateOnly date) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

        public DateOnly AddBusinessDays(DateOnly date, int days)
        {
            var step = Math.Sign(days);
            for (var left = Math.Abs(days); left > 0;)
            {
                date = date.AddDays(step);
                if (IsBusinessDay(date))
                {
                    left--;
                }
            }

            return date;
        }

        public int CountBusinessDays(DateOnly first, DateOnly last)
        {
            var count = 0;
            for (var date = first; date <= last; date = date.AddDays(1))
            {
                count += IsBusinessDay(date) ? 1 : 0;
            }

            return count;
        }
    }

    private static TicketDay DayOn(DateOnly today) =>
        new(new Weekdays(), TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"), today);

    private static VendorTicket At(VendorTicketStatus status, DateTime registeredAt) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "FX-20931", new DateOnly(2026, 10, 19), status, null, null, Requester, registeredAt);

    public static TheoryData<string, VendorTicketStatus, int, int?> Staleness => new()
    {
        // Today, the status at the vendor and the business days without an update shown.
        { "quatro dias úteis", VendorTicketStatus.InAnalysis, 23, null },
        { "o fim de semana não conta", VendorTicketStatus.InAnalysis, 25, null },
        { "cinco dias úteis", VendorTicketStatus.Open, 26, 5 },
        { "seis dias úteis", VendorTicketStatus.InAnalysis, 27, 6 },
        { "aguardando nosso retorno", VendorTicketStatus.WaitingForUs, 27, 6 },
        { "resolvido no fornecedor", VendorTicketStatus.Resolved, 27, null },
        { "fechado no fornecedor", VendorTicketStatus.Closed, 27, null },
    };

    [Theory(DisplayName = "Sem retorno do fornecedor")]
    [MemberData(nameof(Staleness))]
    public void StaleDays_CountTheBusinessDaysAfterTheLastUpdate(string when, VendorTicketStatus status, int today, int? staleDays)
    {
        var vendorTicket = At(status, Monday);

        VendorTickets.StaleDaysOf(vendorTicket, DayOn(new DateOnly(2026, 10, today)), new VendorStaleSettings(5))
            .Should().Be(staleDays, when);
    }

    [Fact(DisplayName = "Atualizar o status no fornecedor")]
    public void Update_EndsTheStaleness()
    {
        var vendorTicket = At(VendorTicketStatus.InAnalysis, Monday);

        vendorTicket.ChangeStatus(VendorTicketStatus.WaitingForUs, new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc));

        vendorTicket.Status.Should().Be(VendorTicketStatus.WaitingForUs);
        VendorTickets.StaleDaysOf(vendorTicket, DayOn(new DateOnly(2026, 10, 27)), new VendorStaleSettings(5)).Should().BeNull();
    }

    [Fact(DisplayName = "Com fornecedor")]
    public void List_ShowsTheOpenOneRegisteredLast_OrTheLastOne()
    {
        var ticket = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        VendorTicketRow Row(Guid ticketId, VendorTicketStatus status, int minutes, string vendor) =>
            new(new VendorTicket(ticketId, Guid.CreateVersion7(), vendor, new DateOnly(2026, 10, 19), status, null, null, Requester, Monday.AddMinutes(minutes)), vendor);

        var shown = VendorTickets.ShownIn(
        [
            Row(ticket, VendorTicketStatus.InAnalysis, 0, "Emissor Exemplo"),
            Row(ticket, VendorTicketStatus.Open, 5, "Fiscal Exemplo"),
            Row(ticket, VendorTicketStatus.Resolved, 10, "Portal Exemplo"),
            Row(other, VendorTicketStatus.Closed, 0, "Emissor Exemplo"),
            Row(other, VendorTicketStatus.Resolved, 5, "Fiscal Exemplo"),
        ]);

        shown[ticket].VendorName.Should().Be("Fiscal Exemplo");
        shown[other].VendorName.Should().Be("Fiscal Exemplo");
    }

    [Theory(DisplayName = "Nome e portal do fornecedor")]
    [InlineData("  Emissor Exemplo ", "https://suporte.emissor.example", true, true)]
    [InlineData("Emissor Exemplo", "", true, true)]
    [InlineData("Emissor Exemplo", "http://suporte.example", true, true)]
    [InlineData("   ", null, false, true)]
    [InlineData("Emissor Exemplo", "ftp://suporte.example", true, false)]
    [InlineData("Emissor Exemplo", "suporte.example", true, false)]
    public void Vendor_NeedsANameAndAWebPortal(string name, string? portal, bool validName, bool validPortal)
    {
        (Vendor.NameOf(name) is not null).Should().Be(validName);
        Vendor.TryUrl(portal, out _).Should().Be(validPortal);
        Vendor.NameOf(new string('a', Vendor.NameMaxLength + 1)).Should().BeNull();
    }

    public static TheoryData<string, ProjectMemberRole?, bool, bool, bool> Matrix => new()
    {
        // Who, role, Administrator, requester; registers and updates at vendors.
        { "administrador", null, true, false, true },
        { "gerente", ProjectMemberRole.Manager, false, false, true },
        { "líder técnico", ProjectMemberRole.TechLead, false, false, true },
        { "desenvolvedor", ProjectMemberRole.Developer, false, false, true },
        { "colaborador fora do time", ProjectMemberRole.Contributor, false, false, false },
        { "solicitante", ProjectMemberRole.Contributor, false, true, false },
        { "leitor", ProjectMemberRole.Viewer, false, false, false },
    };

    [Theory(DisplayName = "Solicitante não registra no fornecedor")]
    [MemberData(nameof(Matrix))]
    public void WhoServesTheTicket_RegistersAtVendors(
        string who, ProjectMemberRole? role, bool isAdministrator, bool isRequester, bool registers)
    {
        var ticket = Ticket.Open(
            104,
            Guid.CreateVersion7(),
            TicketForm.Of(TicketTeam.Development, TicketPriority.High, "Rejeição 539", "Texto"),
            Requester,
            Monday);
        var viewer = new TicketViewer(isRequester ? Requester : Guid.CreateVersion7(), isAdministrator);

        TicketAccess.MayRegisterAtVendor(viewer, role, ticket).Should().Be(registers, who);
    }
}
