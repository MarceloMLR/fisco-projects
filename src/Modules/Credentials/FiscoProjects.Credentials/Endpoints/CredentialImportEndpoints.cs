using FiscoProjects.Credentials.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Credentials.Endpoints;

/// <summary>A line of the spreadsheet: its number in the spreadsheet and the text of each column.</summary>
public sealed record SpreadsheetRowResponse(int Number, IReadOnlyList<string> Cells);

/// <summary>The spreadsheet read: the names of the sheets, the sheet read, the header and the lines under it.</summary>
public sealed record SpreadsheetResponse(
    IReadOnlyList<string> Sheets, int Sheet, IReadOnlyList<string> Columns, IReadOnlyList<SpreadsheetRowResponse> Rows);

/// <summary>A credential mapped from a line of the spreadsheet, with the number of the line.</summary>
public sealed record ImportedCredentialRequest(
    int Row, string? Portal, string? Environment, string? Url, string? Username, string? Password, string? Notes);

/// <summary>The credentials to include from the spreadsheet: the lines the person marked, already mapped.</summary>
public sealed record CredentialImportRequest(IReadOnlyList<ImportedCredentialRequest>? Items);

public sealed record CredentialImportResponse(int Imported);

/// <summary>Problem types of the import, so the screen can tell the reason.</summary>
public static class CredentialImportProblems
{
    /// <summary>
    /// The file is not a spreadsheet the import reads (ADR-0094); status 400, with the extension reason (Type,
    /// Unreadable, Empty, TooLarge or TooManyRows).
    /// </summary>
    public const string Refused = "urn:fiscoprojects:problem:credential-import-refused";

    /// <summary>Some lines are not valid credentials and nothing was included; status 400, with the extension rows.</summary>
    public const string Invalid = "urn:fiscoprojects:problem:credential-import-invalid";
}

/// <summary>
/// The import of the credentials from a spreadsheet (ADR-0034, ADR-0094): read the file, then include the lines the
/// person marked, mapped on the screen. Only who removes credentials imports (ADR-0045).
/// </summary>
public static class CredentialImportEndpoints
{
    // The form around the file: its name, the sheet and the boundaries.
    private const long FormOverhead = 1024 * 1024;

    public static RouteGroupBuilder MapCredentialImportEndpoints(this RouteGroupBuilder credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        // The protection against other sites is the X-CSRF header of the application (ADR-0054).
        credentials.MapPost("/import/preview", PreviewAsync).WithName("PreviewCredentialImport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(CredentialImportLimits.MaxBytes + FormOverhead))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        credentials.MapPost("/import", ImportAsync).WithName("ImportCredentials")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return credentials;
    }

    private static async Task<Results<Ok<SpreadsheetResponse>, NotFound, ForbidHttpResult, ProblemHttpResult>> PreviewAsync(
        Guid projectId,
        IFormFile file,
        [FromForm] int? sheet,
        HttpContext http,
        IUserAccess access,
        PreviewCredentialImport preview,
        CancellationToken cancellationToken)
    {
        var viewer = await CredentialEndpoints.ViewerOfAsync(http, access, cancellationToken);
        await using var content = file.OpenReadStream();
        var result = await preview.ExecuteAsync(projectId, file.FileName, file.Length, content, sheet, viewer, cancellationToken);
        if (result is { Outcome: ImportOutcome.Done, Spreadsheet: { } spreadsheet })
        {
            // The spreadsheet has the passwords as they are: neither the browser nor anything on the way keeps it.
            http.Response.Headers.CacheControl = "no-store";
            return TypedResults.Ok(new SpreadsheetResponse(
                spreadsheet.Sheets,
                spreadsheet.Sheet,
                spreadsheet.Columns,
                [.. spreadsheet.Rows.Select(row => new SpreadsheetRowResponse(row.Number, row.Cells))]));
        }

        return result.Outcome switch
        {
            ImportOutcome.NotFound => TypedResults.NotFound(),
            ImportOutcome.Forbidden => TypedResults.Forbid(),
            ImportOutcome.ProjectClosed => ProjectClosed(),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Spreadsheet refused",
                detail: "The file is not a .xlsx or .csv the import reads, has no lines or is too large.",
                type: CredentialImportProblems.Refused,
                extensions: new Dictionary<string, object?> { ["reason"] = result.Refusal.ToString() }),
        };
    }

    private static async Task<Results<Ok<CredentialImportResponse>, NotFound, ForbidHttpResult, ProblemHttpResult>> ImportAsync(
        Guid projectId,
        CredentialImportRequest request,
        HttpContext http,
        IUserAccess access,
        ImportCredentials import,
        CancellationToken cancellationToken)
    {
        var viewer = await CredentialEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var items = (request.Items ?? [])
            .Select(item => new ImportedCredential(item.Row, item.Portal, item.Environment, item.Url, item.Username, item.Password, item.Notes))
            .ToList();
        var result = await import.ExecuteAsync(projectId, items, viewer, cancellationToken);
        return result.Outcome switch
        {
            ImportOutcome.Done => TypedResults.Ok(new CredentialImportResponse(result.Imported)),
            ImportOutcome.NotFound => TypedResults.NotFound(),
            ImportOutcome.Forbidden => TypedResults.Forbid(),
            ImportOutcome.ProjectClosed => ProjectClosed(),
            // The lines and their fields, never what was typed: the passwords do not go back (ADR-0033).
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Lines not valid",
                detail: $"Between 1 and {CredentialImportLimits.MaxRows} credentials, each with the rules of a credential; nothing was included.",
                type: CredentialImportProblems.Invalid,
                extensions: new Dictionary<string, object?>
                {
                    ["rows"] = (result.InvalidRows ?? []).Select(row => new
                    {
                        row = row.Row,
                        fields = row.Fields.Select(CredentialEndpoints.FieldName).ToList(),
                    }).ToList(),
                }),
        };
    }

    private static ProblemHttpResult ProjectClosed() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Project closed",
            detail: "A completed or cancelled project is read-only until it is reopened.",
            type: CredentialProblems.ProjectClosed);
}
