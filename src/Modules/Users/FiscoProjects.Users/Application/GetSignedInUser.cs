using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Application;

public sealed record SignedInUser(Guid Id, string Name, string Email, SystemRole Role, ThemePreference Theme);

/// <summary>The person of a session, while the user exists and is active.</summary>
public sealed class GetSignedInUser(IUserStore store)
{
    public async Task<SignedInUser?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        return user is { IsActive: true } ? new SignedInUser(user.Id, user.Name, user.Email, user.Role, user.Theme) : null;
    }
}
