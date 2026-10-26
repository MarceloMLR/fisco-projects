namespace FiscoProjects.Users.Application;

/// <summary>The name and the e-mail of a user for the card of the person (ADR-0118); the anonymized has no e-mail.</summary>
public sealed record PersonDetails(Guid Id, string Name, string? Email);

/// <summary>
/// The name and the e-mail of a user, active or deactivated, for anyone with a session, as the people to choose
/// (ADR-0005, ADR-0007, ADR-0118). The e-mail of an anonymized person is gone (ADR-0110).
/// </summary>
public sealed class GetPerson(IUserStore store)
{
    public async Task<PersonDetails?> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        return user is null
            ? null
            : new PersonDetails(user.Id, user.Name, string.IsNullOrWhiteSpace(user.Email) ? null : user.Email);
    }
}
