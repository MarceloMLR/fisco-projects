using FiscoProjects.Audit.Domain;
using FiscoProjects.Audit.Infrastructure.Persistence;
using FiscoProjects.Persistence.PersonalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Audit.Infrastructure;

/// <summary>
/// Erases an anonymized person from the audit (ADR-0058, ADR-0110), in the transaction of the anonymization: the records
/// of the person's own item name the identifier and keep the personal fields only as changed, and a refused sign-in
/// with the person's e-mail names the identifier. Records of other items point to the person by the id, and show the
/// current name (ADR-0084).
/// </summary>
internal sealed class AuditPersonalData : IPersonalDataEraser
{
    public async Task EraseAsync(DbContext context, PersonErasure erasure, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(erasure);
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The erasure runs in the transaction of the anonymization.");
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(context.Database.GetDbConnection())
            .Options;
        await using var audit = new AuditDbContext(options);
        await audit.Database.UseTransactionAsync(transaction, cancellationToken);

        var own = await audit.Entries
            .Where(entry => entry.ItemType == erasure.ItemType && entry.ItemId == erasure.PersonId)
            .ToListAsync(cancellationToken);
        foreach (var entry in own)
        {
            entry.EraseOwner(erasure.Replacement, erasure.PersonalFields);
        }

        if (!string.IsNullOrWhiteSpace(erasure.Email))
        {
            var email = erasure.Email.Trim();
            var refused = await audit.Entries
                .Where(entry => entry.Action == AuditAction.SignInRefused && entry.ItemLabel == email)
                .ToListAsync(cancellationToken);
            foreach (var entry in refused)
            {
                entry.EraseIdentity(erasure.Replacement);
            }
        }

        await audit.SaveErasureAsync(cancellationToken);
    }
}
