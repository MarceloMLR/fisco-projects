namespace FiscoProjects.Users.Application;

/// <summary>
/// Which people to choose: part of the name or of the e-mail, only the Administrators and the Project managers (who may
/// manage a project, ADR-0072), only the Administrators and the Tech leads (who may lead a project technically,
/// ADR-0097), and how many at most.
/// </summary>
public sealed record PeopleQuery(string? Search, bool ManagersOnly, int Limit, bool TechLeadsOnly = false)
{
    public const int MaxLimit = 100;
}

/// <summary>
/// A person to choose: a user (<see cref="Id"/>) or a person of the directory who never signed in
/// (<see cref="DirectoryId"/>), who becomes a user when included in a project. Only an active Administrator or Project
/// manager can be a manager (ADR-0085), and only an active Administrator or Tech lead a tech lead (ADR-0097).
/// </summary>
public sealed record PersonItem(
    Guid? Id, string? DirectoryId, string Name, string Email, bool CanBeManager, bool CanBeTechLead = false);

/// <summary>
/// The people of the company to choose, such as the manager or the tech lead of a project or a person to include in it
/// (ADR-0005, ADR-0072): the active users and the people of the directory who never signed in, each once, by name.
/// A person of the directory whose user is inactive is left out. Only users may be managers and tech leads, so asking
/// for either leaves the directory out.
/// </summary>
public sealed class ListPeople(IUserStore store, IPeopleDirectory directory)
{
    public async Task<IReadOnlyList<PersonItem>> ExecuteAsync(PeopleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query = query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() };
        var users = await store.ListPeopleAsync(query, cancellationToken);
        if (query.ManagersOnly || query.TechLeadsOnly)
        {
            return users;
        }

        var found = await directory.SearchAsync(query.Search, query.Limit, cancellationToken);
        var linked = new HashSet<(string, string)>();
        foreach (var provider in found.Select(person => person.Provider).Distinct())
        {
            var externalIds = found.Where(person => person.Provider == provider).Select(person => person.ExternalId).ToList();
            foreach (var externalId in await store.LinkedExternalIdsAsync(provider, externalIds, cancellationToken))
            {
                linked.Add((provider, externalId));
            }
        }

        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return [.. users
            .Concat(found
                .Where(person => !linked.Contains((person.Provider, person.ExternalId)))
                .Select(person => new PersonItem(null, person.DirectoryId, person.Name, person.Email, CanBeManager: false)))
            .OrderBy(person => person.Name, comparer)
            .ThenBy(person => person.Email, comparer)
            .Take(query.Limit)];
    }
}
