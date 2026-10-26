using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Application;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The demo directory of the local sign-in: its fictitious people (ADR-0078), with the same provider and ids the local
/// sign-in uses, so a person included in a project signs in with the same user. With the local sign-in off, the
/// directory has nobody.
/// </summary>
internal sealed class LocalPeopleDirectory(IOptions<LocalSignInOptions> options) : IProviderDirectory
{
    public string Provider => SignInLocally.Provider;

    public Task<IReadOnlyList<DirectoryPerson>> SearchAsync(string? search, int limit, CancellationToken cancellationToken)
    {
        var text = SearchText.Simplify(search);
        IReadOnlyList<DirectoryPerson> found = [.. People()
            .Where(person => SearchText.Finds(person.Name, text) || SearchText.Finds(person.Email, text))
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(limit)];
        return Task.FromResult(found);
    }

    public Task<DirectoryPerson?> FindAsync(string provider, string externalId, CancellationToken cancellationToken) =>
        Task.FromResult(People().FirstOrDefault(person => person.Provider == provider && person.ExternalId == externalId));

    private IEnumerable<DirectoryPerson> People() =>
        options.Value.Enabled
            ? options.Value.People.Select(person => new DirectoryPerson(
                SignInLocally.Provider, SignInLocally.Normalize(person.Email), person.Name.Trim(), person.Email.Trim()))
            : [];
}
