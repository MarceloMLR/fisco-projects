using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Persistence.PersonalData;

/// <summary>
/// A person being anonymized (ADR-0058, ADR-0110): the id, the name and the e-mail the person had, the identifier that
/// takes the place of the name, and how the audit names the person: the item type and its fields with personal data.
/// </summary>
public sealed record PersonErasure(
    Guid PersonId, string Name, string Email, string Replacement, string ItemType, IReadOnlyList<string> PersonalFields);

/// <summary>
/// Erases the copies of the name and the e-mail of a person that a module keeps. Implemented by each module that keeps
/// one; the anonymization calls every one of them in its transaction, through the connection of its own context, so
/// the whole erasure is saved or nothing is (ADR-0110). It may run more than once for the same person.
/// </summary>
public interface IPersonalDataEraser
{
    Task EraseAsync(DbContext context, PersonErasure erasure, CancellationToken cancellationToken);
}
