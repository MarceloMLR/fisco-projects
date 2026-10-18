using FiscoProjects.Audit.Domain;
using FiscoProjects.Audit.Infrastructure.Persistence;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Audit.Infrastructure;

/// <summary>
/// Writes the audit records of a module save (ADR-0039, ADR-0075), in the transaction of that save: a context of
/// the audit tables on the same connection, enlisted in the same transaction. The author is the person of the
/// current request's session; without one, the system.
/// </summary>
internal sealed class ChangeAuditor(IHttpContextAccessor http, TimeProvider time) : IChangeAuditor
{
    public async Task RecordAsync(DbContext context, IReadOnlyList<AuditedChange> changes, CancellationToken cancellationToken)
    {
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Audit records are written in the transaction of the save.");

        // No retry strategy here: the saving context already runs in one, and a retrying context refuses a
        // transaction it did not open.
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(context.Database.GetDbConnection())
            .Options;
        await using var audit = new AuditDbContext(options);
        await audit.Database.UseTransactionAsync(transaction, cancellationToken);

        var occurredAt = time.GetUtcNow().UtcDateTime;
        var actorId = UserSession.UserIdOf(http.HttpContext?.User);
        audit.Entries.AddRange(changes.Select(change => AuditEntry.ForChange(
            occurredAt,
            actorId,
            change.Module,
            change.ItemType,
            change.ItemId,
            ActionOf(change.Action),
            change.ItemLabel,
            change.Fields.Select(field => new AuditField(field.Name, field.Before, field.After, field.Secret)))));
        await audit.SaveChangesAsync(cancellationToken);
    }

    private static AuditAction ActionOf(AuditedAction action) => action switch
    {
        AuditedAction.Created => AuditAction.Created,
        AuditedAction.Updated => AuditAction.Updated,
        AuditedAction.Deleted => AuditAction.Deleted,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };
}
