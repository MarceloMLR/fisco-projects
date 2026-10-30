using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Documents.Endpoints;

/// <summary>A request as the local page of a signer shows it: the title, the message, the name of the signer and the PDF.</summary>
public sealed record LocalSigningResponse(string Title, string? Message, string Signer, string FileName);

/// <summary>The reason why the signer refuses the term.</summary>
public sealed record LocalRefusalRequest(string? Reason);

/// <summary>
/// The page of the local provider of signatures (ADR-0129), without a session: the signer opens the link, reads the
/// term and signs or refuses it. Without the local provider, which does not start in production, every link answers
/// 404; each link is of one signer of one open request, and is no longer valid after the answer.
/// </summary>
public static class LocalSigningEndpoints
{
    public static RouteGroupBuilder MapLocalSigningEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var signing = api.MapGroup("/local-signatures/{token}")
            .AllowAnonymous()
            .WithTags("LocalSignatures");
        signing.MapGet("/", GetAsync).WithName("GetLocalSigning");
        signing.MapGet("/document", DocumentAsync).WithName("OpenLocalSigningDocument")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
        signing.MapPost("/sign", SignAsync).WithName("SignLocally");
        signing.MapPost("/refuse", RefuseAsync).WithName("RefuseLocally")
            .ProducesValidationProblem();
        return api;
    }

    private static async Task<Results<Ok<LocalSigningResponse>, NotFound>> GetAsync(
        string token, HttpContext http, [FromServices] ILocalSigningPage? page, CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";
        return page is not null && await page.FindAsync(token, cancellationToken) is { } signing
            ? TypedResults.Ok(new LocalSigningResponse(signing.Title, signing.Message, signing.SignerName, signing.FileName))
            : TypedResults.NotFound();
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> DocumentAsync(
        string token, HttpContext http, [FromServices] ILocalSigningPage? page, CancellationToken cancellationToken) =>
        page is not null && await page.OpenAsync(token, cancellationToken) is { } content
            ? AcceptanceEndpoints.PdfOf(http, content, "application/pdf")
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> SignAsync(
        string token, [FromServices] AnswerLocalSigning? answer, CancellationToken cancellationToken) =>
        answer is not null && await answer.SignAsync(token, cancellationToken) == LocalSigningOutcome.Done
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> RefuseAsync(
        string token, LocalRefusalRequest request, [FromServices] AnswerLocalSigning? answer, CancellationToken cancellationToken) =>
        answer is null ? TypedResults.NotFound() : await answer.RefuseAsync(token, request.Reason, cancellationToken) switch
        {
            LocalSigningOutcome.Done => TypedResults.NoContent(),
            LocalSigningOutcome.Invalid => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = [$"The reason is needed and has up to {AcceptanceTerm.ReasonMaxLength} characters."],
            }),
            _ => TypedResults.NotFound(),
        };
}
