using FiscoProjects.Documents.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Documents.Endpoints;

/// <summary>
/// The notices of the provider of signatures (webhooks, ADR-0130), at a public address outside /api: the provider sends
/// no session nor the X-CSRF header (ADR-0054), as Entra coming back from the sign-in. The notice is checked by its
/// signature with the secret of the installation and only puts the check of its document in the queue, answered at
/// once: what changed always comes from the provider.
/// </summary>
public static class SignatureNoticeEndpoints
{
    public const string Path = "/signature-notices";

    // A notice of a document with ten signers is a few kilobytes.
    private const int MaxBytes = 256 * 1024;

    public static IEndpointRouteBuilder MapSignatureNoticeEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost(Path, ReceiveAsync)
            .AllowAnonymous()
            .ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> ReceiveAsync(
        HttpRequest request, [FromServices] ISignatureNotices? notices, [FromServices] SignatureNoticeQueue? queue, CancellationToken cancellationToken)
    {
        if (notices is null || queue is null)
        {
            return Results.NotFound();
        }

        if (request.ContentLength > MaxBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        using var body = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (body.Length + read > MaxBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            body.Write(buffer, 0, read);
        }

        var notice = notices.Read(body.ToArray(), name => request.Headers.TryGetValue(name, out var value) ? value.ToString() : null);
        if (!notice.IsValid)
        {
            return Results.Unauthorized();
        }

        if (notice.DocumentId is { } documentId)
        {
            queue.Add(notices.Provider, documentId);
        }

        return Results.Ok();
    }
}
