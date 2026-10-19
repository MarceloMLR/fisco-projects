using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>The names of the projects that audit records point to (ADR-0084).</summary>
internal sealed class ProjectReferenceNames(IProjectStore projects) : IAuditReferenceNames
{
    public string Kind => ProjectsAudit.ProjectReference;

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        projects.NamesAsync(ids, cancellationToken);
}

/// <summary>The names of the clients that audit records point to (ADR-0084).</summary>
internal sealed class ClientReferenceNames(IClientStore clients) : IAuditReferenceNames
{
    public string Kind => ProjectsAudit.ClientReference;

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        clients.NamesAsync(ids, cancellationToken);
}
