using System.Data;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Infrastructure.Persistence;

internal sealed class UserStore(UsersDbContext db) : IUserStore, IUserAccess
{
    // The session check and the authorization of one request ask for the same user: one query is enough.
    private readonly Dictionary<Guid, UserAccess?> _accessOfThisRequest = [];

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

    public Task<bool> AnyActiveAdministratorAsync(CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.IsActive && user.Role == SystemRole.Administrator, cancellationToken);

    public Task<bool> AnyOtherActiveAdministratorAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(
            user => user.Id != userId && user.IsActive && user.Role == SystemRole.Administrator, cancellationToken);

    public Task<TResult> InSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            // A repeated attempt starts from what is in the database, not from the changes of the failed one.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await work(token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }

    async Task<UserAccess?> IUserAccess.FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_accessOfThisRequest.TryGetValue(userId, out var access))
        {
            access = await db.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new UserAccess(user.Id, user.IsActive, user.Role))
                .SingleOrDefaultAsync(cancellationToken);
            _accessOfThisRequest[userId] = access;
        }

        return access;
    }
}
