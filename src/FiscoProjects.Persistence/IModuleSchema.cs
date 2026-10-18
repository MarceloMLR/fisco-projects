namespace FiscoProjects.Persistence;

/// <summary>
/// Declares the database schema owned by a module (ADR-0047): the module name in lower case, e.g. "tickets".
/// </summary>
public interface IModuleSchema
{
    static abstract string Schema { get; }
}
