using System.Net;
using System.Net.Http.Headers;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The photos of the people in Microsoft 365, read through Microsoft Graph with the token of the application, whose
/// User.Read.All also gives them (ADR-0119, ADR-0124): the 96x96 one, twice the largest avatar for sharp screens, or the
/// largest one when that size does not exist, as with a photo kept only in Entra ID. Only JPEG and PNG, up to 1 MB. The
/// answers stay in the memory (<see cref="PersonPhotoCache"/>), and a failure of Graph is a warning in the log, without
/// the person, and no photo. Off while the sign-in through Entra ID is off.
/// </summary>
internal sealed partial class GraphPersonPhotos(
    IHttpClientFactory clients,
    IGraphTokens tokens,
    IOptions<EntraSignInOptions> options,
    PersonPhotoCache cache,
    ILogger<GraphPersonPhotos> logger) : IPersonPhotos
{
    /// <summary>The largest photo kept; a 96x96 one has a few KB.</summary>
    public const int MaxBytes = 1024 * 1024;

    public async Task<PersonPhoto?> FindAsync(IReadOnlyList<UserIdentity> identities, CancellationToken cancellationToken)
    {
        var entra = identities.FirstOrDefault(identity => identity.Provider == SignInWithEntra.Provider);
        if (!options.Value.Enabled || entra is null || !Guid.TryParse(entra.ExternalId, out var objectId))
        {
            return null;
        }

        var key = objectId.ToString("D");
        if (cache.TryGet(key, out var kept))
        {
            return kept;
        }

        try
        {
            var photo = await ReadAsync(objectId, cancellationToken);
            cache.Keep(key, photo, PersonPhotoCache.Kept);
            return photo;
        }
        catch (Exception exception) when (GraphFailures.Failed(exception, cancellationToken))
        {
            LogReadFailed(logger, GraphFailures.StatusOf(exception), exception.GetType().Name);
            cache.Keep(key, null, PersonPhotoCache.KeptAfterFailure);
            return null;
        }
    }

    private async Task<PersonPhoto?> ReadAsync(Guid objectId, CancellationToken cancellationToken)
    {
        string[] paths = [$"users/{objectId:D}/photos/96x96/$value", $"users/{objectId:D}/photo/$value"];
        foreach (var path in paths)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(MicrosoftGraph.Address, path));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(cancellationToken));
            using var response = await clients.CreateClient(MicrosoftGraph.ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            response.EnsureSuccessStatusCode();
            var type = ImageTypeOf(response.Content.Headers.ContentType?.MediaType);
            if (type is null || response.Content.Headers.ContentLength > MaxBytes)
            {
                return null;
            }

            var content = await ContentOfAsync(response.Content, cancellationToken);
            return content is { Length: > 0 } ? new PersonPhoto(content, type) : null;
        }

        return null;
    }

    /// <summary>The type of a photo the avatar shows: JPEG, which Graph may call "image/jpg", or PNG.</summary>
    private static string? ImageTypeOf(string? mediaType) => mediaType?.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" => "image/jpeg",
        "image/png" => "image/png",
        _ => null,
    };

    /// <summary>The bytes of the photo, or none past <see cref="MaxBytes"/>.</summary>
    private static async Task<byte[]?> ContentOfAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + read > MaxBytes)
            {
                return null;
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return memory.ToArray();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The photo of a person could not be read from Microsoft Graph ({Status}, {Failure}).")]
    private static partial void LogReadFailed(ILogger logger, int? status, string failure);
}
