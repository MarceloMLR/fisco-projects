namespace FiscoProjects.SharedKernel;

/// <summary>
/// The main reads of a module, run once in the background after the API starts (ADR-0106): the first person to open
/// each screen no longer waits for the first run of its queries and of its code. Only reads, with a sample taken from
/// the module's own data; with no data, nothing runs.
/// </summary>
public interface IWarmUp
{
    /// <summary>The name of the module, for the log.</summary>
    string Module { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
