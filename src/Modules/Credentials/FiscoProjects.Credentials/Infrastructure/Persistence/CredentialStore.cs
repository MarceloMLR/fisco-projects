using FiscoProjects.Credentials.Application;
using FiscoProjects.Credentials.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Credentials.Infrastructure.Persistence;

/// <summary>The credentials of the projects, in the table of the module.</summary>
internal sealed class CredentialStore(CredentialsDbContext db) : ICredentialStore
{
    public async Task<IReadOnlyList<PortalCredential>> ListAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.PortalCredentials.AsNoTracking().Where(credential => credential.ProjectId == projectId).ToListAsync(cancellationToken);

    public Task<PortalCredential?> FindAsync(Guid projectId, Guid id, CancellationToken cancellationToken) =>
        db.PortalCredentials.SingleOrDefaultAsync(credential => credential.Id == id && credential.ProjectId == projectId, cancellationToken);

    public void Add(PortalCredential credential) => db.PortalCredentials.Add(credential);

    public void AddRange(IEnumerable<PortalCredential> credentials) => db.PortalCredentials.AddRange(credentials);

    public void Remove(PortalCredential credential) => db.PortalCredentials.Remove(credential);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
