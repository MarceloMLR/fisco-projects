using FiscoProjects.SharedKernel;

namespace FiscoProjects.Users.Domain;

/// <summary>Links a user to an identity of a provider: "local" today, "entra" later (ADR-0009, ADR-0078).</summary>
public sealed class IdentityLink : Entity
{
    private IdentityLink(Guid userId, string provider, string externalId)
    {
        UserId = userId;
        Provider = provider;
        ExternalId = externalId;
    }

    public Guid UserId { get; private init; }

    public string Provider { get; private init; }

    public string ExternalId { get; private init; }

    public static IdentityLink For(User user, string provider, string externalId) => new(user.Id, provider, externalId);
}
