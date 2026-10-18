namespace FiscoProjects.Users.Application;

/// <summary>
/// The installation's first administrator, by the identity in the provider (ADR-0006): in the Entra, provider
/// "entra" and the immutable object id (oid); in the local sign-in, provider "local" and the e-mail in lowercase.
/// </summary>
public sealed class FirstAdministratorOptions
{
    public const string Section = "Users:FirstAdministrator";

    public string? Provider { get; set; }

    public string? ExternalId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Provider) && !string.IsNullOrWhiteSpace(ExternalId);

    public bool Matches(ProviderIdentity identity) =>
        IsConfigured
        && string.Equals(Provider, identity.Provider, StringComparison.OrdinalIgnoreCase)
        && string.Equals(ExternalId, identity.ExternalId, StringComparison.OrdinalIgnoreCase);
}
