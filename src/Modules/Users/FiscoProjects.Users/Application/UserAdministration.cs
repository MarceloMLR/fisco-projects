using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Application;

public enum UserChangeResult
{
    Done,
    NotFound,

    /// <summary>Refused: the installation would be left without an active administrator (ADR-0006).</summary>
    LastAdministrator,
}

/// <summary>Changes the role of a user, keeping at least one active administrator (ADR-0006, ADR-0040).</summary>
public sealed class ChangeSystemRole(IUserStore store)
{
    public Task<UserChangeResult> ExecuteAsync(Guid userId, SystemRole role, CancellationToken cancellationToken) =>
        store.InSerializableTransactionAsync(async token =>
        {
            var user = await store.FindAsync(userId, token);
            if (user is null)
            {
                return UserChangeResult.NotFound;
            }

            if (role != SystemRole.Administrator && user.IsActiveAdministrator
                && !await store.AnyOtherActiveAdministratorAsync(userId, token))
            {
                return UserChangeResult.LastAdministrator;
            }

            user.ChangeRole(role);
            await store.SaveChangesAsync(token);
            return UserChangeResult.Done;
        }, cancellationToken);
}

/// <summary>Blocks a user from the next request on, keeping at least one active administrator (ADR-0008).</summary>
public sealed class DeactivateUser(IUserStore store)
{
    public Task<UserChangeResult> ExecuteAsync(Guid userId, CancellationToken cancellationToken) =>
        store.InSerializableTransactionAsync(async token =>
        {
            var user = await store.FindAsync(userId, token);
            if (user is null)
            {
                return UserChangeResult.NotFound;
            }

            if (user.IsActiveAdministrator && !await store.AnyOtherActiveAdministratorAsync(userId, token))
            {
                return UserChangeResult.LastAdministrator;
            }

            user.Deactivate();
            await store.SaveChangesAsync(token);
            return UserChangeResult.Done;
        }, cancellationToken);
}

/// <summary>Gives the access back, with the role the user had (ADR-0008).</summary>
public sealed class ReactivateUser(IUserStore store)
{
    public async Task<UserChangeResult> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserChangeResult.NotFound;
        }

        user.Reactivate();
        await store.SaveChangesAsync(cancellationToken);
        return UserChangeResult.Done;
    }
}
