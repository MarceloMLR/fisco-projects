using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>The author of a comment, by the current name.</summary>
public sealed record CommentAuthorResponse(Guid Id, string Name);

/// <summary>
/// A comment on a task or a milestone (ADR-0157): the author, the text with its formatting as the JSON of the editor, the
/// moment it was sent and whether the person who asks may remove it now.
/// </summary>
public sealed record ScheduleCommentResponse(
    Guid Id,
    CommentAuthorResponse Author,
    [property: Description("The text with its formatting, as the JSON of the editor (ADR-0026).")] string Body,
    DateTime SentAt,
    bool CanRemove);

/// <summary>The comments of an item, the oldest first, and whether the person who asks may comment now.</summary>
public sealed record ScheduleCommentsResponse(IReadOnlyList<ScheduleCommentResponse> Items, bool CanComment);

/// <summary>A new comment: the text with its formatting, as the JSON of the editor.</summary>
public sealed record ScheduleCommentRequest(string? Body);

/// <summary>
/// The comments of the tasks and the milestones (ADR-0157): whoever sees the project reads them; everyone in it but the
/// viewer, and the Administrator, comments and removes the own ones, and the Administrator and the manager remove any,
/// while the project is open.
/// </summary>
internal static class ScheduleCommentEndpoints
{
    public static void MapScheduleCommentEndpoints(this RouteGroupBuilder projects)
    {
        projects.MapGet("/{id:guid}/schedule/items/{itemId:guid}/comments", ListAsync).WithName("ListScheduleComments");
        projects.MapPost("/{id:guid}/schedule/items/{itemId:guid}/comments", AddAsync).WithName("AddScheduleComment")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapDelete("/{id:guid}/schedule/items/{itemId:guid}/comments/{commentId:guid}", RemoveAsync)
            .WithName("RemoveScheduleComment")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<ScheduleCommentsResponse>, NotFound>> ListAsync(
        Guid id, Guid itemId, HttpContext http, IUserAccess access, ListScheduleComments list, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return await list.ExecuteAsync(id, itemId, viewer, cancellationToken) is { } comments
            ? TypedResults.Ok(new ScheduleCommentsResponse(
                [.. comments.Comments.Select(line => new ScheduleCommentResponse(
                    line.Comment.Id,
                    new CommentAuthorResponse(line.Comment.AuthorId, line.AuthorName),
                    line.Comment.Body,
                    line.Comment.SentAt,
                    line.CanRemove))],
                comments.CanComment))
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ScheduleCreatedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> AddAsync(
        Guid id,
        Guid itemId,
        HttpContext http,
        ScheduleCommentRequest request,
        IUserAccess access,
        AddScheduleComment add,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await add.ExecuteAsync(id, itemId, request.Body, viewer, cancellationToken);
        return result.Outcome switch
        {
            ScheduleCommentOutcome.Done => TypedResults.Created((string?)null, new ScheduleCreatedResponse(result.Id!.Value)),
            ScheduleCommentOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleCommentOutcome.Closed => ProjectEndpoints.Closed(),
            ScheduleCommentOutcome.InvalidBody => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = [$"Write the comment, with up to {RichText.MaxTextLength} characters, in the formatting of the editor."],
            }),
            ScheduleCommentOutcome.MentionRefused => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mention refused",
                detail: "Only the people of the project may be mentioned.",
                type: ProjectProblems.CommentMentionRefused),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> RemoveAsync(
        Guid id, Guid itemId, Guid commentId, HttpContext http, IUserAccess access, RemoveScheduleComment remove, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await remove.ExecuteAsync(id, itemId, commentId, viewer, cancellationToken);
        return result.Outcome switch
        {
            ScheduleCommentOutcome.Done => TypedResults.NoContent(),
            ScheduleCommentOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleCommentOutcome.Closed => ProjectEndpoints.Closed(),
            _ => TypedResults.NotFound(),
        };
    }
}
