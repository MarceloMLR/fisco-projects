using GerenWare.Credentials.Application;
using GerenWare.Credentials.Domain;
using GerenWare.Credentials.Infrastructure.Persistence;
using GerenWare.Projects.Contracts;
using GerenWare.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.Credentials.Infrastructure;

/// <summary>The read of the tab Acessos (ADR-0106), as the manager of the first project with accesses sees it, without the passwords.</summary>
internal sealed class CredentialsWarmUp(CredentialsDbContext db, IProjectAccess projects, ListCredentials list) : IWarmUp
{
    public string Module => "Credentials";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var projectId = await db.PortalCredentials.AsNoTracking()
            .OrderBy(credential => credential.ProjectId)
            .Select(credential => (Guid?)credential.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
        if (projectId is not { } project)
        {
            return;
        }

        var people = await projects.MembersAsync([project], cancellationToken);
        if (people.FirstOrDefault(person => person.Role == ProjectMemberRole.Manager) is { } manager)
        {
            await list.ExecuteAsync(project, null, null, new CredentialViewer(manager.UserId, IsAdministrator: false), cancellationToken);
        }
    }
}
