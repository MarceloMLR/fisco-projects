using System.Globalization;
using System.Text;
using FiscoProjects.Users.Application;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The company directory while there is no Microsoft tenant: the fictitious people of the local sign-in (ADR-0078),
/// with the same provider and ids the local sign-in uses, so a person included in a project signs in with the same
/// user. With the local sign-in off, the directory has nobody.
/// </summary>
internal sealed class LocalPeopleDirectory(IOptions<LocalSignInOptions> options) : IPeopleDirectory
{
    public Task<IReadOnlyList<DirectoryPerson>> SearchAsync(string? search, int limit, CancellationToken cancellationToken)
    {
        var text = Simplify(search);
        IReadOnlyList<DirectoryPerson> found = [.. People()
            .Where(person => text.Length == 0 || Simplify(person.Name).Contains(text, StringComparison.Ordinal)
                || Simplify(person.Email).Contains(text, StringComparison.Ordinal))
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

    /// <summary>The text without accents, in lowercase, as the database compares names (accent and case insensitive).</summary>
    private static string Simplify(string? text)
    {
        var decomposed = (text ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var simple = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                simple.Append(char.ToLowerInvariant(character));
            }
        }

        return simple.ToString();
    }
}
