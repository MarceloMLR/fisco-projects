using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Documents.Endpoints;

/// <summary>A default folder: the name, whether it is the folder of the acceptance terms, and whether it is active.</summary>
public sealed record DefaultFolderResponse(Guid Id, string Name, bool IsAcceptance, bool IsActive);

public sealed record NewDefaultFolderRequest(string? Name);

public sealed record DefaultFolderRequest(string? Name, bool IsActive);

public sealed record DefaultFolderCreatedResponse(Guid Id);

/// <summary>A folder of the project with how many documents not obsolete it has.</summary>
public sealed record DocumentFolderResponse(Guid Id, string Name, bool IsAcceptance, int Count);

/// <summary>What the person may do with the documents of the project; all false in a completed or cancelled project.</summary>
public sealed record DocumentPermissionsResponse(bool CanUpload, bool CanApprove, bool CanDelete);

/// <summary>The folders the person sees, the total for the tab, the permissions and the size a file may have.</summary>
public sealed record DocumentFoldersResponse(
    IReadOnlyList<DocumentFolderResponse> Folders, int Total, DocumentPermissionsResponse Permissions, long MaxBytes);

/// <summary>The item of the schedule a document is linked to, with its current name.</summary>
public sealed record DocumentLinkResponse(Guid Id, ScheduleItemType Type, string Name);

/// <summary>A person and when the person did something.</summary>
public sealed record DocumentActionResponse(Guid Id, string Name, DateTime At);

/// <summary>
/// A document of a list: the folder, the status with who changed it and when, the current version with its size and
/// who sent it, and the link with the schedule.
/// </summary>
public sealed record DocumentResponse(
    Guid Id,
    string Name,
    Guid FolderId,
    string FolderName,
    DocumentStatus Status,
    DocumentActionResponse StatusChanged,
    int Version,
    long Size,
    DocumentActionResponse Updated,
    DocumentLinkResponse? Link);

public sealed record DocumentPageResponse(IReadOnlyList<DocumentResponse> Items, int Total);

public sealed record DocumentVersionResponse(int Number, long Size, DocumentActionResponse Uploaded);

/// <summary>A document with its versions, newest first, and what the person may do with it.</summary>
public sealed record DocumentDetailsResponse(
    DocumentResponse Document, IReadOnlyList<DocumentVersionResponse> Versions, DocumentPermissionsResponse Permissions);

public sealed record DocumentCreatedResponse(Guid Id, int Version);

public sealed record DocumentStatusRequest(DocumentStatus Status);

/// <summary>The phase, or the task or milestone, the document is linked to; both null take the link away.</summary>
public sealed record DocumentLinkRequest(Guid? PhaseId, Guid? ItemId);

/// <summary>The lists of documents: without the obsolete ones, or all of them.</summary>
public enum DocumentStatusFilter
{
    Active = 1,
    All = 2,
}

/// <summary>Problem types of the documents, so the screen can tell the reason.</summary>
public static class DocumentProblems
{
    /// <summary>The project is completed or cancelled and is read-only (ADR-0016); status 409.</summary>
    public const string ProjectClosed = "urn:fiscoprojects:problem:project-closed";

    /// <summary>
    /// The file is empty, larger than the installation takes or a program or script (ADR-0031); status 400, with the
    /// extension reason (Empty, TooLarge or Dangerous).
    /// </summary>
    public const string DocumentRefused = "urn:fiscoprojects:problem:document-refused";

    /// <summary>The folder of the acceptance terms takes only the signed terms (ADR-0093); status 409.</summary>
    public const string AcceptanceFolderReadOnly = "urn:fiscoprojects:problem:acceptance-folder-read-only";

    /// <summary>Someone sent the same new file at the same time, and nothing was saved; status 409.</summary>
    public const string DocumentChanged = "urn:fiscoprojects:problem:document-changed";

    /// <summary>Another default folder has the name (ADR-0031); status 409.</summary>
    public const string FolderNameInUse = "urn:fiscoprojects:problem:document-folder-name-in-use";
}

/// <summary>
/// The documents of the projects (ADR-0031, ADR-0045): everyone in a project sees and downloads them, and the
/// Administrator sees all; the folders of the installation are only the Administrator's (ADR-0046).
/// </summary>
public static class DocumentEndpoints
{
    // The form around the file: its name, the folder and the boundaries.
    private const long FormOverhead = 1024 * 1024;

    public static RouteGroupBuilder MapDocumentsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var settings = ((IEndpointRouteBuilder)api).ServiceProvider.GetRequiredService<DocumentSettings>();
        var documents = api.MapGroup("/projects/{projectId:guid}/documents")
            .WithTags("Documents")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        documents.MapGet("/folders", FoldersAsync).WithName("ListDocumentFolders");
        documents.MapGet("/", ListAsync).WithName("ListDocuments");
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        documents.MapPost("/", UploadAsync).WithName("UploadDocument")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(settings.MaxBytes + FormOverhead))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        documents.MapGet("/{documentId:guid}", GetAsync).WithName("GetDocument");
        documents.MapGet("/{documentId:guid}/content", OpenAsync).WithName("OpenDocument")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
        documents.MapPut("/{documentId:guid}/status", ChangeStatusAsync).WithName("ChangeDocumentStatus")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        documents.MapPut("/{documentId:guid}/link", LinkAsync).WithName("LinkDocument")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        documents.MapDelete("/{documentId:guid}", RemoveAsync).WithName("RemoveDocument")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // The default folders, only for the Administrator (ADR-0031, ADR-0046).
        var folders = api.MapGroup("/admin/document-folders")
            .WithTags("Documents")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        folders.MapGet("/", DefaultFoldersAsync).WithName("ListDefaultFolders");
        folders.MapPost("/", AddDefaultFolderAsync).WithName("AddDefaultFolder")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        folders.MapPut("/{id:guid}", ChangeDefaultFolderAsync).WithName("ChangeDefaultFolder")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        api.MapLinkEndpoints();
        api.MapAcceptanceEndpoints();
        return api;
    }

    private static async Task<Results<Ok<DocumentFoldersResponse>, NotFound>> FoldersAsync(
        Guid projectId, HttpContext http, IUserAccess access, ListDocumentFolders list, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await list.ExecuteAsync(projectId, viewer, cancellationToken) is not { } folders)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new DocumentFoldersResponse(
            [.. folders.Folders.Select(item => new DocumentFolderResponse(item.Folder.Id, item.Folder.Name, item.Folder.IsAcceptance, item.Count))],
            folders.Total,
            PermissionsOf(folders.Permissions, folders.IsClosed),
            folders.MaxBytes));
    }

    private static async Task<Results<Ok<DocumentPageResponse>, NotFound>> ListAsync(
        Guid projectId,
        Guid? folderId,
        string? search,
        DocumentStatusFilter? status,
        int? page,
        int? pageSize,
        HttpContext http,
        IUserAccess access,
        ListDocuments list,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var filter = new DocumentFilter(folderId, search, status == DocumentStatusFilter.All, page ?? 1, pageSize ?? 50);
        if (await list.ExecuteAsync(projectId, filter, viewer, cancellationToken) is not { } documents)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new DocumentPageResponse([.. documents.Items.Select(DocumentOf)], documents.Total));
    }

    private static async Task<Results<Ok<DocumentDetailsResponse>, NotFound>> GetAsync(
        Guid projectId, Guid documentId, HttpContext http, IUserAccess access, GetDocument get, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await get.ExecuteAsync(projectId, documentId, viewer, cancellationToken) is not { } details)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new DocumentDetailsResponse(
            DocumentOf(details.View),
            [.. details.View.Versions.Select(item => new DocumentVersionResponse(
                item.Version.Number,
                item.Version.Size,
                new DocumentActionResponse(item.Version.UploadedById, item.UploadedBy, item.Version.UploadedAt)))],
            PermissionsOf(details.Permissions, details.IsClosed)));
    }

    private static async Task<Results<Created<DocumentCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> UploadAsync(
        Guid projectId,
        [FromForm] Guid folderId,
        IFormFile file,
        HttpContext http,
        IUserAccess access,
        UploadDocument upload,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        await using var content = file.OpenReadStream();
        var result = await upload.ExecuteAsync(
            projectId, folderId, new DocumentFile(file.FileName, file.Length, content), viewer, cancellationToken);
        if (result.Outcome == DocumentOutcome.Done)
        {
            return TypedResults.Created((string?)null, new DocumentCreatedResponse(result.Id!.Value, result.Version!.Value));
        }

        return Refusal<Created<DocumentCreatedResponse>>(result, "file", "The file needs a name of up to 255 characters.");
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> OpenAsync(
        Guid projectId,
        Guid documentId,
        int? version,
        bool? download,
        HttpContext http,
        IUserAccess access,
        OpenDocument open,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await open.ExecuteAsync(projectId, documentId, version, viewer, cancellationToken) is not { } opened)
        {
            return TypedResults.NotFound();
        }

        // PDF and images open in a tab as they are; text and CSV as plain text; the rest, or when asked, is downloaded.
        var text = FileTypes.IsText(opened.ContentType);
        var inline = download != true && (FileTypes.OpensInBrowser(opened.ContentType) || text);
        var headers = http.Response.Headers;
        // The browser neither guesses another type nor runs anything of the file (ADR-0031).
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = "sandbox";
        headers.CacheControl = "private, no-store";
        return TypedResults.Stream(
            opened.File.Content,
            inline && text ? "text/plain; charset=utf-8" : opened.ContentType,
            fileDownloadName: inline ? null : opened.Name,
            enableRangeProcessing: false);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeStatusAsync(
        Guid projectId,
        Guid documentId,
        DocumentStatusRequest request,
        HttpContext http,
        IUserAccess access,
        ChangeDocumentStatus change,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await change.ExecuteAsync(projectId, documentId, request.Status, viewer, cancellationToken);
        return result.Outcome == DocumentOutcome.Done
            ? TypedResults.NoContent()
            : Refusal<NoContent>(result, "status", "The status is Draft, InReview, Approved or Obsolete.");
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> LinkAsync(
        Guid projectId,
        Guid documentId,
        DocumentLinkRequest request,
        HttpContext http,
        IUserAccess access,
        LinkDocument link,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await link.ExecuteAsync(projectId, documentId, request.PhaseId, request.ItemId, viewer, cancellationToken);
        return result.Outcome == DocumentOutcome.Done
            ? TypedResults.NoContent()
            : Refusal<NoContent>(result, "itemId", "The link is a phase, or a task or milestone, of the schedule of the project.");
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RemoveAsync(
        Guid projectId, Guid documentId, HttpContext http, IUserAccess access, RemoveDocument remove, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await remove.ExecuteAsync(projectId, documentId, viewer, cancellationToken);
        return result.Outcome == DocumentOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result, "id", string.Empty);
    }

    private static async Task<Ok<IReadOnlyList<DefaultFolderResponse>>> DefaultFoldersAsync(
        ListDefaultFolders list, CancellationToken cancellationToken) =>
        TypedResults.Ok<IReadOnlyList<DefaultFolderResponse>>(
            [.. (await list.ExecuteAsync(cancellationToken)).Select(folder =>
                new DefaultFolderResponse(folder.Id, folder.Name, folder.IsAcceptance, folder.IsActive))]);

    private static async Task<Results<Created<DefaultFolderCreatedResponse>, ValidationProblem, ProblemHttpResult>> AddDefaultFolderAsync(
        NewDefaultFolderRequest request, AddDefaultFolder add, CancellationToken cancellationToken)
    {
        var result = await add.ExecuteAsync(request.Name, cancellationToken);
        return result.Outcome switch
        {
            FolderOutcome.Done => TypedResults.Created((string?)null, new DefaultFolderCreatedResponse(result.Id!.Value)),
            FolderOutcome.Invalid => InvalidFolderName(),
            _ => FolderNameInUse(),
        };
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ChangeDefaultFolderAsync(
        Guid id, DefaultFolderRequest request, ChangeDefaultFolder change, CancellationToken cancellationToken)
    {
        var result = await change.ExecuteAsync(id, request.Name, request.IsActive, cancellationToken);
        return result.Outcome switch
        {
            FolderOutcome.Done => TypedResults.NoContent(),
            FolderOutcome.NotFound => TypedResults.NotFound(),
            FolderOutcome.Invalid => InvalidFolderName(),
            FolderOutcome.AcceptanceAlwaysActive => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["isActive"] = ["The folder of the acceptance terms is always active."],
            }),
            _ => FolderNameInUse(),
        };
    }

    private static Results<T, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Refusal<T>(
        DocumentResult result, string field, string invalid)
        where T : IResult =>
        result.Outcome switch
        {
            DocumentOutcome.NotFound => TypedResults.NotFound(),
            DocumentOutcome.Forbidden => TypedResults.Forbid(),
            DocumentOutcome.Invalid => TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [invalid] }),
            DocumentOutcome.ProjectClosed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: DocumentProblems.ProjectClosed),
            DocumentOutcome.ReadOnlyFolder => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Folder of the acceptance terms",
                detail: "The folder of the acceptance terms takes only the signed terms, which do not change.",
                type: DocumentProblems.AcceptanceFolderReadOnly),
            DocumentOutcome.Changed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Documents changed",
                detail: "Someone sent the same file at the same time; nothing was saved.",
                type: DocumentProblems.DocumentChanged),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Document refused",
                detail: "The file is empty, larger than the installation takes, or a program or script.",
                type: DocumentProblems.DocumentRefused,
                extensions: new Dictionary<string, object?> { ["reason"] = result.Refusal.ToString() }),
        };

    private static ValidationProblem InvalidFolderName() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = [$"The name is needed, has up to {FolderNames.MaxLength} characters and none of \" * : < > ? / \\ |."],
        });

    private static ProblemHttpResult FolderNameInUse() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Name in use",
            detail: "Another default folder has the name.",
            type: DocumentProblems.FolderNameInUse);

    private static DocumentPermissionsResponse PermissionsOf(DocumentPermissions permissions, bool isClosed) =>
        new(
            permissions.CanUpload && !isClosed,
            permissions.CanApprove && !isClosed,
            permissions.CanDelete && !isClosed);

    private static DocumentResponse DocumentOf(DocumentView view)
    {
        var document = view.Document;
        return new(
            document.Id,
            document.Name,
            document.FolderId,
            view.FolderName,
            document.Status,
            new DocumentActionResponse(document.StatusChangedById, view.StatusChangedBy, document.StatusChangedAt),
            document.Version,
            document.Size,
            new DocumentActionResponse(document.UpdatedById, view.UpdatedBy, document.UpdatedAt),
            view.Link is { } link ? new DocumentLinkResponse(link.Id, link.Type, link.Name) : null);
    }

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    internal static async Task<DocumentViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The documents are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new DocumentViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }
}
