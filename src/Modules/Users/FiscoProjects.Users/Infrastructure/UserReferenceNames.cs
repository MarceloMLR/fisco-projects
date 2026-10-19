using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>The names of the people that audit records point to (ADR-0084).</summary>
internal sealed class UserReferenceNames(IUserDirectory directory) : IAuditReferenceNames
{
    public string Kind => UserAudit.Reference;

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        directory.NamesAsync(ids, cancellationToken);
}
