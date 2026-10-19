namespace FiscoProjects.SharedKernel;

/// <summary>
/// Names of the items of one type that audit records point to, such as the people (<c>User</c>) or the clients
/// (<c>Client</c>). The module that owns the type offers them, so the Audit screen shows the current name of an item
/// instead of its id (ADR-0084).
/// </summary>
public interface IAuditReferenceNames
{
    /// <summary>The type of item, as the modules mark it in their audit configuration.</summary>
    string Kind { get; }

    /// <summary>The name of each item found; ids of items that no longer exist are left out.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
