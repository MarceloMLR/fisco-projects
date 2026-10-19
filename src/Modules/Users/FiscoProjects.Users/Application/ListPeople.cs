namespace FiscoProjects.Users.Application;

/// <summary>
/// Which people to choose: part of the name or of the e-mail, only the Administrators and the Project managers (who may
/// manage a project, ADR-0072), and how many at most.
/// </summary>
public sealed record PeopleQuery(string? Search, bool ManagersOnly, int Limit)
{
    public const int MaxLimit = 100;
}

public sealed record PersonItem(Guid Id, string Name, string Email);

/// <summary>
/// The active people of the installation to choose as the manager or the tech lead of a project (ADR-0072). Until the
/// directory search (ADR-0005), only the people who already signed in can be chosen.
/// </summary>
public sealed class ListPeople(IUserStore store)
{
    public Task<IReadOnlyList<PersonItem>> ExecuteAsync(PeopleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListPeopleAsync(
            query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() }, cancellationToken);
    }
}
