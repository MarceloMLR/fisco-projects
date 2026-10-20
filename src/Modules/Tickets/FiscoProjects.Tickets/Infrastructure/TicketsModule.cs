using FiscoProjects.Persistence;
using FiscoProjects.Tickets.Application;
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
        return services;
    }
}
