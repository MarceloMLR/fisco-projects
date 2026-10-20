using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Persistence;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>Services of the Tickets module: the tickets of the projects (ADR-0022, ADR-0029, ADR-0044, ADR-0068).</summary>
public static class TicketsModule
{
    public static IServiceCollection AddTicketsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<TicketsDbContext>(configuration);
        services.AddScoped<ITicketStore, TicketStore>();
        services.AddScoped<ListTickets>();
        services.AddScoped<CountTickets>();
        services.AddScoped<ListTicketChoices>();
        services.AddScoped<OpenTicket>();
        services.AddScoped<GetTicket>();
        // Up to 15 MB per file by default, as each installation sets (ADR-0026).
        var megabytes = configuration.GetValue("Tickets:Attachments:MaxMegabytes", TicketAttachments.DefaultMaxMegabytes);
        services.AddSingleton(new TicketAttachmentSettings(megabytes * 1024L * 1024L));
        services.AddScoped<TicketFiles>();
        services.AddScoped<SendTicketMessage>();
        services.AddScoped<AttachToDescription>();
        services.AddScoped<FollowTicket>();
        services.AddScoped<OpenTicketAttachment>();
        services.AddScoped<TicketDeadlines>();
        services.AddScoped<TicketAnswers>();
        services.AddScoped<TicketBatch>();
        services.AddScoped<ChangeTicketAssignees>();
        services.AddScoped<ChangeTicketRequesters>();
        services.AddScoped<ListTicketPeopleChoices>();
        services.AddScoped<TransferTicket>();
        // The tickets of a person who leaves a project or gets another role (ADR-0042).
        services.AddScoped<IProjectMemberChanges, TicketMemberChanges>();
        // The tickets of a project being completed or cancelled (ADR-0043).
        services.AddScoped<IProjectClosing, TicketProjectClosing>();
        // The tickets of the person in Meu trabalho (ADR-0096).
        services.AddScoped<IMyWorkSource, TicketWorkSource>();
        services.AddScoped<ChangeTicketPriority>();
        services.AddScoped<GetTicketDeadlines>();
        services.AddScoped<ChangeTicketDeadlines>();
        services.AddScoped<GetTicketFlow>();
        services.AddScoped<ChangeTicketStatus>();
        // A resolved ticket without an answer closes after 5 business days, as each installation sets (ADR-0024).
        services.AddSingleton(new TicketAutoCloseSettings(
            Math.Max(1, configuration.GetValue("Tickets:AutoCloseBusinessDays", TicketAutoCloseSettings.Default))));
        services.AddScoped<CloseResolvedTickets>();
        services.AddScoped<TicketNotices>();
        services.AddScoped<AlertTicketDeadlines>();
        // The vendors and the tickets at them (ADR-0028): stale after 5 business days without an update, as each
        // installation sets.
        services.AddScoped<IVendorStore, VendorStore>();
        services.AddSingleton(new VendorStaleSettings(
            Math.Max(1, configuration.GetValue("Tickets:VendorStaleBusinessDays", VendorStaleSettings.Default))));
        services.AddScoped<ListVendors>();
        services.AddScoped<AddVendor>();
        services.AddScoped<ChangeVendor>();
        services.AddScoped<RegisterVendorTicket>();
        services.AddScoped<UpdateVendorTicket>();
        services.AddScoped<IAuditReferenceNames, TicketReferenceNames>();
        services.AddScoped<IAuditReferenceNames, VendorReferenceNames>();
        services.AddHostedService<TicketAutoClose>();
        services.AddHostedService<TicketDeadlineAlerts>();
        return services;
    }
}
