using System.Diagnostics.CodeAnalysis;

namespace FiscoProjects.Users.Application;

/// <summary>A person of the company directory (ADR-0005): the identity provider, the id there, the name and the e-mail.</summary>
public sealed record DirectoryPerson(string Provider, string ExternalId, string Name, string Email)
{
    /// <summary>How the API names the person while the person has no user (<see cref="DirectoryIds"/>).</summary>
    public string DirectoryId => DirectoryIds.Of(Provider, ExternalId);
}

/// <summary>The id of a person of the directory for the API: "{provider}:{externalId}", opaque to the screens.</summary>
public static class DirectoryIds
{
    public static string Of(string provider, string externalId) => $"{provider}:{externalId}";

    public static bool TryParse(
        string? directoryId, [NotNullWhen(true)] out string? provider, [NotNullWhen(true)] out string? externalId)
    {
        var separator = directoryId?.IndexOf(':', StringComparison.Ordinal) ?? -1;
        if (directoryId is null || separator <= 0 || separator == directoryId.Length - 1)
        {
            provider = null;
            externalId = null;
            return false;
        }

        provider = directoryId[..separator];
        externalId = directoryId[(separator + 1)..];
        return true;
    }
}

/// <summary>
/// The company directory, where people are found before they sign in (ADR-0005): the demo directory of the local
/// sign-in today (ADR-0078), Microsoft Graph with the Entra sign-in (ADR-0004).
/// </summary>
public interface IPeopleDirectory
{
    /// <summary>The people whose name or e-mail has the text, without telling case or accents apart, by name.</summary>
    Task<IReadOnlyList<DirectoryPerson>> SearchAsync(string? search, int limit, CancellationToken cancellationToken);

    Task<DirectoryPerson?> FindAsync(string provider, string externalId, CancellationToken cancellationToken);
}
