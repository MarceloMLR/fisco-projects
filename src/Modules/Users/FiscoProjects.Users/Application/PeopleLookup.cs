using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Application;

/// <summary>
/// The people of the company by their e-mails (ADR-0135), by the people search, so the same rules choose them: the
/// active users and the people of the directory who never signed in, each once (ADR-0005, ADR-0119).
/// </summary>
public sealed class PeopleLookup(ListPeople people) : IPeopleLookup
{
    private const int CandidatesPerEmail = 10;

    public async Task<IReadOnlyDictionary<string, FoundPerson>> ByEmailsAsync(
        IReadOnlyCollection<string> emails, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(emails);
        var found = new Dictionary<string, FoundPerson>(StringComparer.OrdinalIgnoreCase);
        var wanted = emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(IPeopleLookup.MaxEmails);
        foreach (var email in wanted)
        {
            var candidates = await people.ExecuteAsync(new PeopleQuery(email, ManagersOnly: false, CandidatesPerEmail), cancellationToken);
            if (candidates.FirstOrDefault(person => string.Equals(person.Email, email, StringComparison.OrdinalIgnoreCase)) is { } person)
            {
                found[email] = new FoundPerson(person.Id, person.DirectoryId, person.Name, person.Email);
            }
        }

        return found;
    }
}
