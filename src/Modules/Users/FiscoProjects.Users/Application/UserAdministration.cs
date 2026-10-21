using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Application;

public enum UserChangeResult
{
    Done,
    NotFound,

    /// <summary>Refused: the installation would be left without an active administrator (ADR-0006).</summary>
    LastAdministrator,

    /// <summary>Refused: the person was anonymized, for good (ADR-0110).</summary>
    Anonymized,
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

            if (user.IsAnonymized)
            {
                return UserChangeResult.Anonymized;
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

        if (user.IsAnonymized)
        {
            return UserChangeResult.Anonymized;
        }

        user.Reactivate();
        await store.SaveChangesAsync(cancellationToken);
        return UserChangeResult.Done;
    }
}

public enum AnonymizeResult
{
    Done,
    NotFound,

    /// <summary>Refused: the person is active; the Administrator deactivates first (ADR-0110).</summary>
    Active,

    /// <summary>Refused: the person was already anonymized.</summary>
    Anonymized,
}

/// <summary>
/// Anonymizes a deactivated person, for good (ADR-0058, ADR-0110): the name becomes the identifier of the
/// installation, with the next number, the e-mail is emptied and the identity links are removed; then, in the same
/// transaction, every module erases its copies of the name and the e-mail. The save of the person is the audit
/// record, whose name and e-mail the audit erases with the rest.
/// </summary>
public sealed class AnonymizeUser(IUserStore store, IOptions<AnonymizationOptions> options, TimeProvider time)
{
    public Task<AnonymizeResult> ExecuteAsync(Guid userId, CancellationToken cancellationToken) =>
        store.InSerializableTransactionAsync(async token =>
        {
            var user = await store.FindAsync(userId, token);
            if (user is null)
            {
                return AnonymizeResult.NotFound;
            }

            if (user.IsAnonymized)
            {
                return AnonymizeResult.Anonymized;
            }

            if (user.IsActive)
            {
                return AnonymizeResult.Active;
            }

            var (name, email) = (user.Name, user.Email);
            var number = await store.NextAnonymousNumberAsync(token);
            var identifier = options.Value.IdentifierFor(number);
            user.Anonymize(identifier, number, time.GetUtcNow().UtcDateTime);
            await store.RemoveIdentityLinksAsync(user.Id, token);
            await store.SaveChangesAsync(token);
            await store.ErasePersonalDataAsync(user.Id, name, email, identifier, token);
            return AnonymizeResult.Done;
        }, cancellationToken);
}
