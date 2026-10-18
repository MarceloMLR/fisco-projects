using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Application;

/// <summary>Where users and their identity links are kept; implemented in Infrastructure.</summary>
public interface IUserStore
{
    Task<User?> FindByIdentityAsync(string provider, string externalId, CancellationToken cancellationToken);

    Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> AnyActiveAdministratorAsync(CancellationToken cancellationToken);

    Task<UserListPage> ListAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<bool> AnyOtherActiveAdministratorAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the work in a serializable transaction, repeated from scratch when the database picks it as a deadlock
    /// victim, so checks such as "another administrator stays active" hold against concurrent changes.
    /// </summary>
    Task<TResult> InSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken);

    void Add(User user, IdentityLink link);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
