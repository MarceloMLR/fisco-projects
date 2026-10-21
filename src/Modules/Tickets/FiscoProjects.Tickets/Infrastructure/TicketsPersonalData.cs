using FiscoProjects.Persistence.PersonalData;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// Erases an anonymized person from the tickets (ADR-0110), in the transaction of the anonymization: the mentions of
/// the person in the messages name the identifier (the descriptions take no mentions, ADR-0026). The rest of a ticket
/// points to the person by the id. What people typed stays as typed (ADR-0058).
/// </summary>
internal sealed class TicketsPersonalData : IPersonalDataEraser
{
    public async Task EraseAsync(DbContext context, PersonErasure erasure, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(erasure);
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The erasure runs in the transaction of the anonymization.");
        // A context of its own on the connection of the anonymization, without the audit, as the messages (ADR-0069).
        var options = new DbContextOptionsBuilder<TicketsDbContext>()
            .UseSqlServer(context.Database.GetDbConnection())
            .Options;
        await using var tickets = new TicketsDbContext(options);
        await tickets.Database.UseTransactionAsync(transaction, cancellationToken);

        // The documents keep the id of a mention as text.
        var mentioned = $"%{erasure.PersonId}%";
        var messages = await tickets.TicketMessages
            .Where(message => EF.Functions.Like(message.Body, mentioned))
            .ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.RenameMention(erasure.PersonId, erasure.Replacement);
        }

        await tickets.SaveChangesAsync(cancellationToken);
    }
}
