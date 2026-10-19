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

/// <summary>The names of the phases of the schedules that audit records point to (ADR-0084).</summary>
internal sealed class SchedulePhaseReferenceNames(IScheduleStore schedules) : IAuditReferenceNames
{
    public string Kind => ProjectsAudit.SchedulePhaseReference;

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        schedules.PhaseNamesAsync(ids, cancellationToken);
}

/// <summary>The names of the tasks and milestones of the schedules that audit records point to (ADR-0084).</summary>
internal sealed class ScheduleItemReferenceNames(IScheduleStore schedules) : IAuditReferenceNames
{
    public string Kind => ProjectsAudit.ScheduleItemReference;

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        schedules.ItemNamesAsync(ids, cancellationToken);
}
