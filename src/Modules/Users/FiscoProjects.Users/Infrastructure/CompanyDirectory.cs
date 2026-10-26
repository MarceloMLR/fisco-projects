using System.Text.Json;
using Azure.Identity;
using FiscoProjects.Users.Application;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The company directory (ADR-0005, ADR-0119): the demo directory of the local sign-in (ADR-0078) and the one of
/// Microsoft Graph with the sign-in through Entra ID, each on with its sign-in, joined by name. A directory that fails
/// is left out of the search, with a warning in the log without the text searched nor names, so the people search
/// still answers.
/// </summary>
internal sealed partial class CompanyDirectory(IEnumerable<IProviderDirectory> directories, ILogger<CompanyDirectory> logger)
    : IPeopleDirectory
{
    public async Task<IReadOnlyList<DirectoryPerson>> SearchAsync(string? search, int limit, CancellationToken cancellationToken)
    {
        var found = new List<DirectoryPerson>();
        foreach (var directory in directories)
        {
            try
            {
                found.AddRange(await directory.SearchAsync(search, limit, cancellationToken));
            }
            catch (Exception exception) when (Failed(exception, cancellationToken))
            {
                LogSearchFailed(logger, directory.Provider, StatusOf(exception), exception.GetType().Name);
            }
        }

        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return [.. found.OrderBy(person => person.Name, comparer).ThenBy(person => person.Email, comparer).Take(limit)];
    }

    public async Task<DirectoryPerson?> FindAsync(string provider, string externalId, CancellationToken cancellationToken)
    {
        if (directories.FirstOrDefault(directory => directory.Provider == provider) is not { } directory)
        {
            return null;
        }

        try
        {
            return await directory.FindAsync(provider, externalId, cancellationToken);
        }
        catch (Exception exception) when (Failed(exception, cancellationToken))
        {
            LogFindFailed(logger, provider, StatusOf(exception), exception.GetType().Name);
            return null;
        }
    }

    /// <summary>A failure of the directory, not the request of the person cancelled.</summary>
    private static bool Failed(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or AuthenticationFailedException or JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private static int? StatusOf(Exception exception) => (int?)(exception as HttpRequestException)?.StatusCode;

    [LoggerMessage(Level = LogLevel.Warning, Message = "The people directory {Provider} failed in a search ({Status}, {Failure}).")]
    private static partial void LogSearchFailed(ILogger logger, string provider, int? status, string failure);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The people directory {Provider} failed to find a person ({Status}, {Failure}).")]
    private static partial void LogFindFailed(ILogger logger, string provider, int? status, string failure);
}
