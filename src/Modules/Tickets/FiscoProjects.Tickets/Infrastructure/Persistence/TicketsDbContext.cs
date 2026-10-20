using FiscoProjects.Persistence;
using FiscoProjects.Tickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure.Persistence;

internal sealed class TicketsDbContext(DbContextOptions<TicketsDbContext> options)
    : ModuleDbContext<TicketsDbContext>(options), IModuleSchema
{
    /// <summary>The numbers of the tickets, one sequence for the installation (ADR-0029).</summary>
    public const string NumberSequence = "ticket_numbers";

    public static string Schema => "tickets";

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    public DbSet<TicketFollower> TicketFollowers => Set<TicketFollower>();

    public DbSet<TicketTransition> TicketTransitions => Set<TicketTransition>();

    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();

    public DbSet<TicketDeadlineSetting> TicketDeadlines => Set<TicketDeadlineSetting>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(NumberSequence).StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketsDbContext).Assembly);
    }
}

// For dotnet ef to create the context without starting the API.
internal sealed class TicketsDbContextFactory : ModuleDesignTimeDbContextFactory<TicketsDbContext>
{
    protected override TicketsDbContext CreateContext(DbContextOptions<TicketsDbContext> options) => new(options);
}
