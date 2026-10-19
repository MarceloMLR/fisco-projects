using System.ComponentModel;
using System.Globalization;
using FiscoProjects.Projects.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

public enum ClientStatus
{
    Active,
    Inactive,
}

/// <summary>
/// One client in the list: the CNPJ without punctuation (the screen formats it) and the address of the logo, which
/// changes whenever the logo changes, so the browser may keep it; null without a logo.
/// </summary>
public sealed record ClientResponse(
    Guid Id, string Name, string? Cnpj, string? LogoUrl, string? Observation, ClientStatus Status);

public sealed record ClientPageResponse(IReadOnlyList<ClientResponse> Items, int Total);

/// <summary>The client form: the name, the CNPJ with or without punctuation (empty for none) and the observation.</summary>
public sealed record ClientRequest(string? Name, string? Cnpj, string? Observation);

public sealed record ClientCreatedResponse(Guid Id);

/// <summary>Problem types of the clients, so the screen can tell the reason.</summary>
public static class ClientProblems
{
    /// <summary>Another client has the CNPJ; the extension clientName tells which.</summary>
    public const string CnpjInUse = "urn:fiscoprojects:problem:client-cnpj-in-use";
    public const string LogoFormat = "urn:fiscoprojects:problem:client-logo-format";
    public const string LogoTooLarge = "urn:fiscoprojects:problem:client-logo-too-large";
}

/// <summary>
/// The clients (ADR-0015): the Administrator and the Project manager list and include them, as a project needs them
/// (ADR-0043, ADR-0046); every other change is the Administrator's. The logo opens for anyone in a session.
/// </summary>
public static class ClientEndpoints
{
    /// <summary>A body larger than this is refused before it is read; the logo itself has up to 1 MB.</summary>
    public const long UploadLimit = 2 * 1024 * 1024;

    // A logo opened straight in the browser must not run anything nor load anything (ADR-0030).
    private const string LogoPolicy = "default-src 'none'; sandbox";

    public static RouteGroupBuilder MapProjectsEndpoints(this RouteGroupBuilder api)
    {
        var clients = api.MapGroup("/clients")
            .WithTags("Clients")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        clients.MapGet("/", ListAsync).WithName("ListClients")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        clients.MapPost("/", AddAsync).WithName("AddClient")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        clients.MapPut("/{id:guid}", ChangeAsync).WithName("ChangeClient")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        clients.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivateClient")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        clients.MapPost("/{id:guid}/reactivate", ReactivateAsync).WithName("ReactivateClient")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        clients.MapDelete("/{id:guid}", RemoveAsync).WithName("RemoveClient")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        clients.MapGet("/{id:guid}/logo", GetLogoAsync).WithName("GetClientLogo")
            .Produces(StatusCodes.Status200OK, contentType: "image/png", additionalContentTypes: "image/jpeg");
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        clients.MapPut("/{id:guid}/logo", ReplaceLogoAsync).WithName("ReplaceClientLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadLimit))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        clients.MapDelete("/{id:guid}/logo", RemoveLogoAsync).WithName("RemoveClientLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        return api;
    }

    private static async Task<Results<Ok<ClientPageResponse>, ValidationProblem>> ListAsync(
        HttpContext http,
        LinkGenerator links,
        ListClients listClients,
        CancellationToken cancellationToken,
        [Description("Part of the name, without telling case or accents apart, or of the CNPJ, with or without punctuation.")] string? search = null,
        ClientStatus? status = null,
        int page = 1,
        int pageSize = 25)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Must be 1 or more."];
        }

        if (pageSize is < 1 or > ClientListQuery.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"Must be between 1 and {ClientListQuery.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var isActive = status switch
        {
            ClientStatus.Active => true,
            ClientStatus.Inactive => false,
            _ => (bool?)null,
        };
        var result = await listClients.ExecuteAsync(new ClientListQuery(search, isActive, page, pageSize), cancellationToken);
        return TypedResults.Ok(new ClientPageResponse(
            [.. result.Items.Select(client => new ClientResponse(
                client.Id,
                client.Name,
                client.Cnpj,
                client.LogoChangedAt is { } changedAt
                    ? links.GetPathByName(http, "GetClientLogo", new { id = client.Id, v = VersionOf(changedAt) })
                    : null,
                client.Observation,
                client.IsActive ? ClientStatus.Active : ClientStatus.Inactive))],
            result.Total));
    }

    private static async Task<Results<Created<ClientCreatedResponse>, ValidationProblem, ProblemHttpResult>> AddAsync(
        ClientRequest request, AddClient addClient, CancellationToken cancellationToken)
    {
        var result = await addClient.ExecuteAsync(new ClientChanges(request.Name, request.Cnpj, request.Observation), cancellationToken);
        return result.Outcome switch
        {
            ClientSaveOutcome.Saved => TypedResults.Created((string?)null, new ClientCreatedResponse(result.Id!.Value)),
            ClientSaveOutcome.CnpjInUse => CnpjInUse(result.CnpjInUseBy),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        Guid id, ClientRequest request, ChangeClient changeClient, CancellationToken cancellationToken)
    {
        var result = await changeClient.ExecuteAsync(
            id, new ClientChanges(request.Name, request.Cnpj, request.Observation), cancellationToken);
        return result.Outcome switch
        {
            ClientSaveOutcome.Saved => TypedResults.NoContent(),
            ClientSaveOutcome.NotFound => TypedResults.NotFound(),
            ClientSaveOutcome.CnpjInUse => CnpjInUse(result.CnpjInUseBy),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync(
        Guid id, DeactivateClient deactivateClient, CancellationToken cancellationToken) =>
        await deactivateClient.ExecuteAsync(id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> ReactivateAsync(
        Guid id, ReactivateClient reactivateClient, CancellationToken cancellationToken) =>
        await reactivateClient.ExecuteAsync(id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> RemoveAsync(
        Guid id, RemoveClient removeClient, CancellationToken cancellationToken) =>
        await removeClient.ExecuteAsync(id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<Results<FileStreamHttpResult, NotFound>> GetLogoAsync(
        Guid id, HttpContext http, GetClientLogo getLogo, CancellationToken cancellationToken, string? v = null)
    {
        var logo = await getLogo.ExecuteAsync(id, cancellationToken);
        if (logo is null)
        {
            return TypedResults.NotFound();
        }

        var headers = http.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = LogoPolicy;
        // The address of the current logo never shows another file; any other address is checked again.
        headers.CacheControl = v == VersionOf(logo.ChangedAt) ? "private, max-age=31536000, immutable" : "no-cache";
        return TypedResults.Stream(logo.File.Content, logo.ContentType);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ReplaceLogoAsync(
        Guid id, IFormFile? file, ReplaceClientLogo replaceLogo, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Send the logo in the field file."],
            });
        }

        await using var content = file.OpenReadStream();
        return await replaceLogo.ExecuteAsync(id, file.FileName, content, cancellationToken) switch
        {
            ClientLogoOutcome.Saved => TypedResults.NoContent(),
            ClientLogoOutcome.NotFound => TypedResults.NotFound(),
            ClientLogoOutcome.TooLarge => Refused(ClientProblems.LogoTooLarge, "Logo too large", "The logo has up to 1 MB."),
            _ => Refused(ClientProblems.LogoFormat, "Logo not accepted", "The logo is a PNG or a JPG."),
        };
    }

    private static async Task<Results<NoContent, NotFound>> RemoveLogoAsync(
        Guid id, RemoveClientLogo removeLogo, CancellationToken cancellationToken) =>
        await removeLogo.ExecuteAsync(id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static ValidationProblem Invalid(IReadOnlyList<ClientField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field switch
            {
                ClientField.Name => "name",
                ClientField.Cnpj => "cnpj",
                _ => "observation",
            },
            field => new[]
            {
                field switch
                {
                    ClientField.Name => "The name is needed and has up to 150 characters.",
                    ClientField.Cnpj => "The CNPJ has 14 characters and its check digits, or is empty.",
                    _ => "The observation has up to 500 characters.",
                },
            }));

    private static ProblemHttpResult CnpjInUse(string? clientName) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "CNPJ in use",
            detail: "Another client has this CNPJ.",
            type: ClientProblems.CnpjInUse,
            extensions: new Dictionary<string, object?> { ["clientName"] = clientName });

    private static ProblemHttpResult Refused(string type, string title, string detail) =>
        TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: title, detail: detail, type: type);

    private static string VersionOf(DateTime changedAt) => changedAt.Ticks.ToString(CultureInfo.InvariantCulture);
}
