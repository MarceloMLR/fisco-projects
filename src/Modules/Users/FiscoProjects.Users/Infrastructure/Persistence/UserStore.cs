using System.Data;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Infrastructure.Persistence;

internal sealed class UserStore(UsersDbContext db) : IUserStore, IUserAccess, IUserDirectory
{
    private const string AccentInsensitive = "Latin1_General_100_CI_AI";

    // The session check and the authorization of one request ask for the same user: one query is enough.
    private readonly Dictionary<Guid, UserAccess?> _accessOfThisRequest = [];

    public Task<User?> FindByIdentityAsync(string provider, string externalId, CancellationToken cancellationToken) =>
        db.Users
            .Where(user => db.IdentityLinks.Any(link =>
                link.UserId == user.Id && link.Provider == provider && link.ExternalId == externalId))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<string>> SignInProvidersAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.IdentityLinks.AsNoTracking()
            .Where(link => link.UserId == userId)
            .Select(link => link.Provider)
            .Distinct()
            .OrderBy(provider => provider)
            .ToListAsync(cancellationToken);

    public void Add(User user, IdentityLink link)
    {
        db.Users.Add(user);
        db.IdentityLinks.Add(link);
    }

    public bool IsDuplicateIdentity(Exception exception) =>
        exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } sql }
        && sql.Message.Contains("IX_identity_links_Provider_ExternalId", StringComparison.Ordinal);

    public void Forget(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        foreach (var link in db.ChangeTracker.Entries<IdentityLink>().Where(entry => entry.Entity.UserId == user.Id).ToList())
        {
            link.State = EntityState.Detached;
        }

        db.Entry(user).State = EntityState.Detached;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<bool> AnyActiveAdministratorAsync(CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.IsActive && user.Role == SystemRole.Administrator, cancellationToken);

    public async Task<UserListPage> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();
        if (query.Search is { } search)
        {
            // Without telling uppercase, lowercase and accents apart: "livia" finds "Lívia".
            users = users.Where(user =>
                EF.Functions.Collate(user.Name, AccentInsensitive).Contains(search)
                || EF.Functions.Collate(user.Email, AccentInsensitive).Contains(search));
        }

        if (query.Role is { } role)
        {
            users = users.Where(user => user.Role == role);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(user => user.IsActive == isActive);
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(user => user.Name).ThenBy(user => user.Email)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(user => new UserListItem(user.Id, user.Name, user.Email, user.Role, user.IsActive, user.LastSignInAt))
            .ToListAsync(cancellationToken);
        return new UserListPage(items, total);
    }

    public async Task<IReadOnlyList<PersonItem>> ListPeopleAsync(PeopleQuery query, CancellationToken cancellationToken)
    {
        var people = db.Users.AsNoTracking().Where(user => user.IsActive);
        if (query.Search is { } search)
        {
            people = people.Where(user =>
                EF.Functions.Collate(user.Name, AccentInsensitive).Contains(search)
                || EF.Functions.Collate(user.Email, AccentInsensitive).Contains(search));
        }

        if (query.ManagersOnly)
        {
            people = people.Where(user => user.Role == SystemRole.Administrator || user.Role == SystemRole.ProjectManager);
        }

        if (query.TechLeadsOnly)
        {
            people = people.Where(user => user.Role == SystemRole.Administrator || user.Role == SystemRole.TechLead);
        }

        return await people
            .OrderBy(user => user.Name).ThenBy(user => user.Email)
            .Take(query.Limit)
            .Select(user => new PersonItem(
                user.Id, null, user.Name, user.Email,
                user.Role == SystemRole.Administrator || user.Role == SystemRole.ProjectManager,
                user.Role == SystemRole.Administrator || user.Role == SystemRole.TechLead))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> LinkedExternalIdsAsync(
        string provider, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken) =>
        await db.IdentityLinks.AsNoTracking()
            .Where(link => link.Provider == provider && externalIds.Contains(link.ExternalId))
            .Select(link => link.ExternalId)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyOtherActiveAdministratorAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(
            user => user.Id != userId && user.IsActive && user.Role == SystemRole.Administrator, cancellationToken);

    public Task<TResult> InSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            // A repeated attempt starts from what is in the database, not from the changes of the failed one.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await work(token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }

    async Task<UserAccess?> IUserAccess.FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_accessOfThisRequest.TryGetValue(userId, out var access))
        {
            access = await db.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new UserAccess(user.Id, user.IsActive, user.Role))
                .SingleOrDefaultAsync(cancellationToken);
            _accessOfThisRequest[userId] = access;
        }

        return access;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken);

    public async Task<IReadOnlyList<PersonInfo>> PeopleAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new PersonInfo(
                user.Id, user.Name, user.Email, user.LastSignInAt, user.IsActive,
                user.IsActive && (user.Role == SystemRole.Administrator || user.Role == SystemRole.ProjectManager))
            {
                ReceivesDailySummary = user.ReceivesDailySummary,
                CanBeTechLead = user.IsActive && (user.Role == SystemRole.Administrator || user.Role == SystemRole.TechLead),
            })
            .ToListAsync(cancellationToken);
}
