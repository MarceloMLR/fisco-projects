using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Tests;

/// <summary>
/// The people of the installation, as the Users module would tell their access, names and data, and the company
/// directory, whose people become users when included in a project.
/// </summary>
internal sealed class PeopleOfTheTest : IUserAccess, IUserDirectory, IUserProvisioning
{
    private readonly Dictionary<Guid, (string Name, SystemRole Role, bool IsActive, DateTime? LastSignInAt)> _people = [];
    private readonly Dictionary<string, string> _directory = [];
    private readonly Dictionary<string, Guid> _fromDirectory = [];

    public Guid Add(string name, SystemRole role, bool isActive = true)
    {
        var id = Guid.CreateVersion7();
        _people[id] = (name, role, isActive, new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc));
        return id;
    }

    /// <summary>A person of the directory who never signed in; returns the directory id the people search gives.</summary>
    public string AddToDirectory(string name)
    {
        var directoryId = $"local:{EmailOf(name)}";
        _directory[directoryId] = name;
        return directoryId;
    }

    /// <summary>The user a person of the directory became, if included.</summary>
    public Guid? UserOf(string directoryId) => _fromDirectory.TryGetValue(directoryId, out var id) ? id : null;

    public void Rename(Guid id, string name) => _people[id] = _people[id] with { Name = name };

    public void ChangeRole(Guid id, SystemRole role) => _people[id] = _people[id] with { Role = role };

    public void Deactivate(Guid id) => _people[id] = _people[id] with { IsActive = false };

    public Task<UserAccess?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_people.TryGetValue(userId, out var person)
            ? new UserAccess(userId, person.IsActive, person.Role)
            : null);

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            _people.Where(person => userIds.Contains(person.Key)).ToDictionary(person => person.Key, person => person.Value.Name));

    public Task<IReadOnlyList<PersonInfo>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PersonInfo>>([.. _people
            .Where(person => userIds.Contains(person.Key))
            .Select(person => new PersonInfo(
                person.Key,
                person.Value.Name,
                EmailOf(person.Value.Name),
                person.Value.LastSignInAt,
                person.Value.IsActive,
                person.Value.IsActive && SystemRoles.ManagesProjects(person.Value.Role)))]);

    public Task<Guid?> UserFromDirectoryAsync(string directoryId, CancellationToken cancellationToken)
    {
        if (_fromDirectory.TryGetValue(directoryId, out var existing))
        {
            return Task.FromResult<Guid?>(existing);
        }

        if (!_directory.TryGetValue(directoryId, out var name))
        {
            return Task.FromResult<Guid?>(null);
        }

        var id = Guid.CreateVersion7();
        _people[id] = (name, SystemRole.User, true, null);
        _fromDirectory[directoryId] = id;
        return Task.FromResult<Guid?>(id);
    }

    private static string EmailOf(string name) => $"{name.ToLowerInvariant().Replace(' ', '.')}@empresa.example";
}
