using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Application;

/// <summary>What the person sees of themselves in My profile.</summary>
public sealed record Profile(
    Guid Id,
    string Name,
    string Email,
    SystemRole Role,
    ThemePreference Theme,
    DateTime LastSignInAt,
    IReadOnlyList<string> SignInProviders);

/// <summary>The profile of the person in the session, while the user exists and is active.</summary>
public sealed class GetProfile(IUserStore store)
{
    public async Task<Profile?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        if (user is not { IsActive: true })
        {
            return null;
        }

        var providers = await store.SignInProvidersAsync(userId, cancellationToken);
        // The person in the session has signed in; only a user from the directory has no sign-in yet.
        return new Profile(user.Id, user.Name, user.Email, user.Role, user.Theme, user.LastSignInAt ?? user.CreatedAt, providers);
    }
}

/// <summary>Keeps the theme the person chose in their profile (ADR-0014).</summary>
public sealed class ChooseTheme(IUserStore store)
{
    /// <returns>False when the user no longer exists or may no longer use the system.</returns>
    public async Task<bool> ExecuteAsync(Guid userId, ThemePreference theme, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        if (user is not { IsActive: true })
        {
            return false;
        }

        user.ChooseTheme(theme);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}
