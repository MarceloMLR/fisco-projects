using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Tests;

/// <summary>The people of the installation, as the Users module would tell their access and names.</summary>
internal sealed class PeopleOfTheTest : IUserAccess, IUserDirectory
{
    private readonly Dictionary<Guid, (string Name, SystemRole Role, bool IsActive)> _people = [];

    public Guid Add(string name, SystemRole role, bool isActive = true)
    {
        var id = Guid.CreateVersion7();
        _people[id] = (name, role, isActive);
        return id;
    }

    public void Rename(Guid id, string name) => _people[id] = _people[id] with { Name = name };

    public Task<UserAccess?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_people.TryGetValue(userId, out var person)
            ? new UserAccess(userId, person.IsActive, person.Role)
            : null);

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            _people.Where(person => userIds.Contains(person.Key)).ToDictionary(person => person.Key, person => person.Value.Name));
}
