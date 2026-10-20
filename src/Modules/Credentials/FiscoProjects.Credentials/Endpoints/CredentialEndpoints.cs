using FiscoProjects.Credentials.Application;
using FiscoProjects.Credentials.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Credentials.Endpoints;

/// <summary>A credential of the project, without the password: the portal, the environment, the address, the user and the notes.</summary>
public sealed record CredentialResponse(Guid Id, string Portal, string Environment, string? Url, string Username, string? Notes);

/// <summary>An environment the credentials of the project use, with how many credentials it has.</summary>
public sealed record EnvironmentCountResponse(string Name, int Count);

/// <summary>What the person may do with the credentials; all false in a completed or cancelled project.</summary>
public sealed record CredentialPermissionsResponse(bool CanEdit, bool CanDelete);

/// <summary>
/// The credentials found, without the passwords; the environments of the project with how many credentials each has;
/// the total for the tab; and what the person may do.
/// </summary>
public sealed record CredentialListResponse(
    IReadOnlyList<CredentialResponse> Items,
    IReadOnlyList<EnvironmentCountResponse> Environments,
    int Total,
    CredentialPermissionsResponse Permissions);

/// <summary>The password of a credential, asked for showing or copying it.</summary>
public sealed record CredentialPasswordResponse(string Password);

public sealed record CredentialCreatedResponse(Guid Id);

/// <summary>Problem types of the credentials, so the screen can tell the reason.</summary>
public static class CredentialProblems
{
    /// <summary>The project is completed or cancelled and is read-only (ADR-0016); status 409.</summary>
    public const string ProjectClosed = "urn:fiscoprojects:problem:project-closed";
}

/// <summary>
/// The credentials of the projects (ADR-0034, ADR-0045): everyone in a project sees them and their passwords, and the
/// Administrator sees all. The password comes only by its own address, which the browser does not keep, and never in
/// a list, an e-mail or the audit (ADR-0033).
/// </summary>
public static class CredentialEndpoints
{
    public static RouteGroupBuilder MapCredentialsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var credentials = api.MapGroup("/projects/{projectId:guid}/credentials")
            .WithTags("Credentials")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        credentials.MapGet("/", ListAsync).WithName("ListCredentials");
        credentials.MapGet("/{credentialId:guid}/password", PasswordAsync).WithName("RevealCredentialPassword");
        credentials.MapPost("/", AddAsync).WithName("AddCredential")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        credentials.MapPut("/{credentialId:guid}", ChangeAsync).WithName("ChangeCredential")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        credentials.MapDelete("/{credentialId:guid}", RemoveAsync).WithName("RemoveCredential")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return api;
    }

    private static async Task<Results<Ok<CredentialListResponse>, NotFound>> ListAsync(
        Guid projectId,
        string? search,
        string? environment,
        HttpContext http,
        IUserAccess access,
        ListCredentials list,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await list.ExecuteAsync(projectId, search, environment, viewer, cancellationToken) is not { } credentials)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CredentialListResponse(
            [.. credentials.Items.Select(credential => new CredentialResponse(
                credential.Id, credential.Portal, credential.Environment, credential.Url, credential.Username, credential.Notes))],
            [.. credentials.Environments.Select(count => new EnvironmentCountResponse(count.Name, count.Count))],
            credentials.Total,
            new CredentialPermissionsResponse(credentials.Permissions.CanEdit, credentials.Permissions.CanDelete)));
    }

    private static async Task<Results<Ok<CredentialPasswordResponse>, NotFound>> PasswordAsync(
        Guid projectId,
        Guid credentialId,
        HttpContext http,
        IUserAccess access,
        RevealPassword reveal,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await reveal.ExecuteAsync(projectId, credentialId, viewer, cancellationToken) is not { } password)
        {
            return TypedResults.NotFound();
        }

        // Neither the browser nor anything on the way keeps the password (ADR-0034).
        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(new CredentialPasswordResponse(password));
    }

    private static async Task<Results<Created<CredentialCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AddAsync(
        Guid projectId, CredentialRequest request, HttpContext http, IUserAccess access, AddCredential add, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await add.ExecuteAsync(projectId, request, viewer, cancellationToken);
        return result.Outcome == CredentialOutcome.Done
            ? TypedResults.Created((string?)null, new CredentialCreatedResponse(result.Id!.Value))
            : Refusal<Created<CredentialCreatedResponse>>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeAsync(
        Guid projectId,
        Guid credentialId,
        CredentialRequest request,
        HttpContext http,
        IUserAccess access,
        ChangeCredential change,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await change.ExecuteAsync(projectId, credentialId, request, viewer, cancellationToken);
        return result.Outcome == CredentialOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RemoveAsync(
        Guid projectId, Guid credentialId, HttpContext http, IUserAccess access, RemoveCredential remove, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await remove.ExecuteAsync(projectId, credentialId, viewer, cancellationToken);
        return result.Outcome == CredentialOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    internal static Results<T, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Refusal<T>(CredentialResult result)
        where T : IResult =>
        result.Outcome switch
        {
            CredentialOutcome.NotFound => TypedResults.NotFound(),
            CredentialOutcome.Forbidden => TypedResults.Forbid(),
            CredentialOutcome.ProjectClosed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: CredentialProblems.ProjectClosed),
            _ => InvalidCredential(result.Invalid ?? []),
        };

    /// <summary>The fields that are not valid, never with what was typed: the password does not go back (ADR-0033).</summary>
    internal static ValidationProblem InvalidCredential(IReadOnlyList<CredentialField> fields) =>
        TypedResults.ValidationProblem(fields.Distinct().ToDictionary(FieldName, field => new[] { MessageOf(field) }));

    internal static string FieldName(CredentialField field) => field switch
    {
        CredentialField.Portal => "portal",
        CredentialField.Environment => "environment",
        CredentialField.Url => "url",
        CredentialField.Username => "username",
        CredentialField.Password => "password",
        _ => "notes",
    };

    private static string MessageOf(CredentialField field) => field switch
    {
        CredentialField.Portal => $"The portal is needed and has up to {PortalCredential.PortalMaxLength} characters.",
        CredentialField.Environment => $"The environment is needed and has up to {PortalCredential.EnvironmentMaxLength} characters.",
        CredentialField.Url => $"The address is empty, or http or https with up to {PortalCredential.UrlMaxLength} characters.",
        CredentialField.Username => $"The user is needed and has up to {PortalCredential.UsernameMaxLength} characters.",
        CredentialField.Password => $"The password is needed in a new credential and has up to {PortalCredential.PasswordMaxLength} characters.",
        _ => $"The notes have up to {PortalCredential.NotesMaxLength} characters.",
    };

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    internal static async Task<CredentialViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The credentials are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new CredentialViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }
}
