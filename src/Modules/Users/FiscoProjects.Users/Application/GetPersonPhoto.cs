namespace FiscoProjects.Users.Application;

/// <summary>A link of a user with an identity provider: the provider and the id of the person there.</summary>
public sealed record UserIdentity(string Provider, string ExternalId);

/// <summary>The photo of a person, as the identity provider keeps it (ADR-0124).</summary>
public sealed record PersonPhoto(byte[] Content, string ContentType);

/// <summary>Where the photos of the people come from: Microsoft 365, with the sign-in through Entra ID (ADR-0124).</summary>
public interface IPersonPhotos
{
    /// <summary>
    /// The photo of the person of the identities, or none: without a photo, without a provider that keeps photos, or
    /// with the provider out of service.
    /// </summary>
    Task<PersonPhoto?> FindAsync(IReadOnlyList<UserIdentity> identities, CancellationToken cancellationToken);
}

/// <summary>
/// The photo of a user in Microsoft 365, for the avatar, to anyone with a session, as the name and the e-mail of the
/// card (ADR-0118, ADR-0124). It goes by the links of the user with the identity providers, so a person without them,
/// as the local sign-in and the anonymized (ADR-0110), has none.
/// </summary>
public sealed class GetPersonPhoto(IUserStore store, IPersonPhotos photos)
{
    public async Task<PersonPhoto?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var identities = await store.IdentitiesAsync(userId, cancellationToken);
        return identities.Count == 0 ? null : await photos.FindAsync(identities, cancellationToken);
    }
}
