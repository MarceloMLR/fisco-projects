namespace FiscoProjects.Users.Contracts;

/// <summary>
/// A person of the company found by the e-mail: a user (<see cref="UserId"/>) or a person of the directory who never
/// signed in (<see cref="DirectoryId"/>), as the people search returns them (ADR-0005, ADR-0119).
/// </summary>
public sealed record FoundPerson(Guid? UserId, string? DirectoryId, string Name, string Email);

/// <summary>Finds the people of the company by their e-mails, as the import of the MS Project suggests them (ADR-0135).</summary>
public interface IPeopleLookup
{
    /// <summary>The most e-mails looked up at once; the others find nobody.</summary>
    const int MaxEmails = 50;

    /// <summary>
    /// The person of each e-mail found, by the e-mail in any case: among the active users and the people of the company
    /// directory, as the people search finds them, with exactly that e-mail.
    /// </summary>
    Task<IReadOnlyDictionary<string, FoundPerson>> ByEmailsAsync(IReadOnlyCollection<string> emails, CancellationToken cancellationToken);
}
