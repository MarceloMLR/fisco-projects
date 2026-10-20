using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Documents.Endpoints;

/// <summary>A category of links: the name and whether it is active.</summary>
public sealed record LinkCategoryResponse(Guid Id, string Name, bool IsActive);

public sealed record NewLinkCategoryRequest(string? Name);

public sealed record LinkCategoryRequest(string? Name, bool IsActive);

public sealed record LinkCategoryCreatedResponse(Guid Id);

/// <summary>A category the links of the project use, with how many links have it.</summary>
public sealed record LinkCategoryCountResponse(Guid Id, string Name, int Count);

/// <summary>A link of the project: the title, the address, what it is for, the category and who included it and when.</summary>
public sealed record LinkResponse(
    Guid Id,
    string Title,
    string Url,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    DocumentActionResponse Created);

/// <summary>
/// The links found, the categories the links of the project use with how many each has, the total for the tab and
/// whether the person may include, change and remove links (false in a completed or cancelled project).
/// </summary>
public sealed record LinkListResponse(
    IReadOnlyList<LinkResponse> Items, IReadOnlyList<LinkCategoryCountResponse> Categories, int Total, bool CanEdit);

public sealed record LinkCreatedResponse(Guid Id);

/// <summary>Problem types of the links, so the screen can tell the reason.</summary>
public static class LinkProblems
{
    /// <summary>Another category of links has the name (ADR-0032); status 409.</summary>
    public const string CategoryNameInUse = "urn:fiscoprojects:problem:link-category-name-in-use";
}

/// <summary>
/// The links of the projects (ADR-0032, ADR-0045): everyone in a project sees them, and the Administrator sees all;
/// the categories are kept only by the Administrator (ADR-0046), and the active ones are offered to everyone.
/// </summary>
public static class LinkEndpoints
{
    public static RouteGroupBuilder MapLinkEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var links = api.MapGroup("/projects/{projectId:guid}/links")
            .WithTags("Links")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        links.MapGet("/", ListAsync).WithName("ListLinks");
        links.MapPost("/", AddAsync).WithName("AddLink")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        links.MapPut("/{linkId:guid}", ChangeAsync).WithName("ChangeLink")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        links.MapDelete("/{linkId:guid}", RemoveAsync).WithName("RemoveLink")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        api.MapGet("/link-categories", ActiveCategoriesAsync).WithName("ListActiveLinkCategories")
            .WithTags("Links")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // The categories, only for the Administrator (ADR-0032, ADR-0046).
        var categories = api.MapGroup("/admin/link-categories")
            .WithTags("Links")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        categories.MapGet("/", CategoriesAsync).WithName("ListLinkCategories");
        categories.MapPost("/", AddCategoryAsync).WithName("AddLinkCategory")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut("/{id:guid}", ChangeCategoryAsync).WithName("ChangeLinkCategory")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return api;
    }

    private static async Task<Results<Ok<LinkListResponse>, NotFound>> ListAsync(
        Guid projectId,
        string? search,
        Guid? categoryId,
        HttpContext http,
        IUserAccess access,
        ListLinks list,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        if (await list.ExecuteAsync(projectId, search, categoryId, viewer, cancellationToken) is not { } links)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new LinkListResponse(
            [.. links.Items.Select(view => new LinkResponse(
                view.Link.Id,
                view.Link.Title,
                view.Link.Url,
                view.Link.Description,
                view.Link.CategoryId,
                view.CategoryName,
                new DocumentActionResponse(view.Link.CreatedById, view.CreatedBy, view.Link.CreatedAt)))],
            [.. links.Categories.Select(count => new LinkCategoryCountResponse(count.Id, count.Name, count.Count))],
            links.Total,
            links.CanEdit));
    }

    private static async Task<Results<Created<LinkCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AddAsync(
        Guid projectId, LinkRequest request, HttpContext http, IUserAccess access, AddLink add, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await add.ExecuteAsync(projectId, request, viewer, cancellationToken);
        return result.Outcome == LinkOutcome.Done
            ? TypedResults.Created((string?)null, new LinkCreatedResponse(result.Id!.Value))
            : Refusal<Created<LinkCreatedResponse>>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeAsync(
        Guid projectId,
        Guid linkId,
        LinkRequest request,
        HttpContext http,
        IUserAccess access,
        ChangeLink change,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await change.ExecuteAsync(projectId, linkId, request, viewer, cancellationToken);
        return result.Outcome == LinkOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RemoveAsync(
        Guid projectId, Guid linkId, HttpContext http, IUserAccess access, RemoveLink remove, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await remove.ExecuteAsync(projectId, linkId, viewer, cancellationToken);
        return result.Outcome == LinkOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Ok<IReadOnlyList<LinkCategoryResponse>>> ActiveCategoriesAsync(
        ListLinkCategories list, CancellationToken cancellationToken) =>
        TypedResults.Ok(CategoriesOf(await list.ExecuteAsync(activeOnly: true, cancellationToken)));

    private static async Task<Ok<IReadOnlyList<LinkCategoryResponse>>> CategoriesAsync(
        ListLinkCategories list, CancellationToken cancellationToken) =>
        TypedResults.Ok(CategoriesOf(await list.ExecuteAsync(activeOnly: false, cancellationToken)));

    private static async Task<Results<Created<LinkCategoryCreatedResponse>, ValidationProblem, ProblemHttpResult>> AddCategoryAsync(
        NewLinkCategoryRequest request, AddLinkCategory add, CancellationToken cancellationToken)
    {
        var result = await add.ExecuteAsync(request.Name, cancellationToken);
        return result.Outcome switch
        {
            LinkCategoryOutcome.Done => TypedResults.Created((string?)null, new LinkCategoryCreatedResponse(result.Id!.Value)),
            LinkCategoryOutcome.Invalid => InvalidCategoryName(),
            _ => CategoryNameInUse(),
        };
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ChangeCategoryAsync(
        Guid id, LinkCategoryRequest request, ChangeLinkCategory change, CancellationToken cancellationToken)
    {
        var result = await change.ExecuteAsync(id, request.Name, request.IsActive, cancellationToken);
        return result.Outcome switch
        {
            LinkCategoryOutcome.Done => TypedResults.NoContent(),
            LinkCategoryOutcome.NotFound => TypedResults.NotFound(),
            LinkCategoryOutcome.Invalid => InvalidCategoryName(),
            _ => CategoryNameInUse(),
        };
    }

    private static IReadOnlyList<LinkCategoryResponse> CategoriesOf(IReadOnlyList<LinkCategory> categories) =>
        [.. categories.Select(category => new LinkCategoryResponse(category.Id, category.Name, category.IsActive))];

    private static Results<T, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Refusal<T>(LinkResult result)
        where T : IResult =>
        result.Outcome switch
        {
            LinkOutcome.NotFound => TypedResults.NotFound(),
            LinkOutcome.Forbidden => TypedResults.Forbid(),
            LinkOutcome.ProjectClosed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: DocumentProblems.ProjectClosed),
            _ => InvalidLink(result.Invalid ?? []),
        };

    private static ValidationProblem InvalidLink(IReadOnlyList<LinkField> fields) =>
        TypedResults.ValidationProblem(fields.Distinct().ToDictionary(
            field => field switch
            {
                LinkField.Title => "title",
                LinkField.Url => "url",
                LinkField.Category => "categoryId",
                _ => "description",
            },
            field => new[]
            {
                field switch
                {
                    LinkField.Title => $"The title is needed and has up to {ProjectLink.TitleMaxLength} characters.",
                    LinkField.Url => $"The address is http or https and has up to {ProjectLink.UrlMaxLength} characters.",
                    LinkField.Category => "The category is one of the active categories, or the one the link already has.",
                    _ => $"The description has up to {ProjectLink.DescriptionMaxLength} characters.",
                },
            }));

    private static ValidationProblem InvalidCategoryName() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = [$"The name is needed and has up to {LinkCategory.NameMaxLength} characters."],
        });

    private static ProblemHttpResult CategoryNameInUse() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Name in use",
            detail: "Another category of links has the name.",
            type: LinkProblems.CategoryNameInUse);
}
