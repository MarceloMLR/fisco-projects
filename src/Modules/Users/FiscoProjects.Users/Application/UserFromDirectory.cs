using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Application;

/// <summary>
/// The user of a person of the company directory, created when someone includes the person in a project before the
/// first sign-in (ADR-0005): with the name and the e-mail of the directory, the role User and the link to the provider
/// of the directory, so the first sign-in finds the same user. The creation is audited, with who included the person.
/// </summary>
public sealed class UserFromDirectory(IUserStore store, IPeopleDirectory directory, TimeProvider time) : IUserProvisioning
{
    public async Task<Guid?> UserFromDirectoryAsync(string directoryId, CancellationToken cancellationToken)
    {
        if (!DirectoryIds.TryParse(directoryId, out var provider, out var externalId))
        {
            return null;
        }

        if (await store.FindByIdentityAsync(provider, externalId, cancellationToken) is { } existing)
        {
            return existing.IsActive ? existing.Id : null;
        }

        if (await directory.FindAsync(provider, externalId, cancellationToken) is not { } person)
        {
            return null;
        }

        var user = User.FromDirectory(person.Name, person.Email, time.GetUtcNow().UtcDateTime);
        store.Add(user, IdentityLink.For(user, provider, externalId));
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
        catch (Exception exception) when (store.IsDuplicateIdentity(exception))
        {
            // Someone else included or signed in the same person meanwhile: that user is the one.
            store.Forget(user);
            return await store.FindByIdentityAsync(provider, externalId, cancellationToken) is { IsActive: true } other
                ? other.Id
                : null;
        }
    }
}
