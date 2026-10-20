using FiscoProjects.Persistence;
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
        services.AddScoped<GetTicketFlow>();
        services.AddScoped<ChangeTicketStatus>();
        // A resolved ticket without an answer closes after 5 business days, as each installation sets (ADR-0024).
        services.AddSingleton(new TicketAutoCloseSettings(
            configuration.GetValue("Tickets:AutoCloseBusinessDays", TicketAutoCloseSettings.Default)));
        services.AddScoped<CloseResolvedTickets>();
        services.AddHostedService<TicketAutoClose>();
        return services;
    }
}
