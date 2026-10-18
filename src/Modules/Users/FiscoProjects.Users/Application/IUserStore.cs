using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Application;

/// <summary>Where users and their identity links are kept; implemented in Infrastructure.</summary>
public interface IUserStore
{
    Task<User?> FindByIdentityAsync(string provider, string externalId, CancellationToken cancellationToken);

    Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> AnyActiveAdministratorAsync(CancellationToken cancellationToken);

    void Add(User user, IdentityLink link);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
