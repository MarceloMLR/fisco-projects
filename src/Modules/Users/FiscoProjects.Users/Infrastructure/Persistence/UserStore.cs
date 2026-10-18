using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Infrastructure.Persistence;

internal sealed class UserStore(UsersDbContext db) : IUserStore, IUserAccess
{
    public Task<User?> FindByIdentityAsync(string provider, string externalId, CancellationToken cancellationToken) =>
        db.Users
            .Where(user => db.IdentityLinks.Any(link =>
                link.UserId == user.Id && link.Provider == provider && link.ExternalId == externalId))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public void Add(User user, IdentityLink link)
    {
        db.Users.Add(user);
        db.IdentityLinks.Add(link);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);
}
