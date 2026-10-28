using System.Text.Json;
using Azure.Identity;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// When a call to Microsoft Graph failed on the side of Graph or of the token (ADR-0119, ADR-0124), and not because the
/// request of the person was cancelled: the people search and the photos go on without it.
/// </summary>
internal static class GraphFailures
{
    public static bool Failed(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or AuthenticationFailedException or JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>The HTTP status Graph answered, when it answered.</summary>
    public static int? StatusOf(Exception exception) => (int?)(exception as HttpRequestException)?.StatusCode;
}
