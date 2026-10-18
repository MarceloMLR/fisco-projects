using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Persistence.Auditing;

/// <summary>
/// Writes the audit records of a save. Implemented by the Audit module, which owns the audit tables; the base
/// context calls it while saving, so the records go in the same transaction as the changes (ADR-0039).
/// </summary>
public interface IChangeAuditor
{
    /// <summary>Writes the records through the connection and the transaction of the module context that is saving.</summary>
    Task RecordAsync(DbContext context, IReadOnlyList<AuditedChange> changes, CancellationToken cancellationToken);
}
