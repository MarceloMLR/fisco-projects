using FiscoProjects.Projects.Domain;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>A task or a milestone of a schedule, as the comments need it: the name, the kind and the person in charge.</summary>
public sealed record CommentedItem(Guid Id, string Name, ScheduleItemKind Kind, Guid? OwnerId);

/// <summary>Where the comments of the schedules are kept (ADR-0157).</summary>
public interface IScheduleCommentStore
{
    /// <summary>The task or the milestone of the schedule of the project; null when it has none with the id.</summary>
    Task<CommentedItem?> ItemAsync(Guid projectId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>The comments of the item, the oldest first, not tracked.</summary>
    Task<IReadOnlyList<ScheduleComment>> OfItemAsync(Guid projectId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>How many comments each item of the project has, only the ones with some.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountsAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The comment of the item, tracked; null when the item has none with the id.</summary>
    Task<ScheduleComment?> FindAsync(Guid projectId, Guid itemId, Guid commentId, CancellationToken cancellationToken);

    void Add(ScheduleComment comment);

    void Remove(ScheduleComment comment);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>What happened to a request about the comments of an item.</summary>
public enum ScheduleCommentOutcome
{
    Done = 1,

    /// <summary>The project, the item or the comment does not exist for the person who asks.</summary>
    NotFound = 2,

    Forbidden = 3,

    /// <summary>The project is completed or cancelled: its comments stay as they are.</summary>
    Closed = 4,

    /// <summary>The text is empty, too long or has what the text with formatting does not allow (<see cref="RichText"/>).</summary>
    InvalidBody = 5,

    /// <summary>The text mentions a person who is not in the project.</summary>
    MentionRefused = 6,
}

/// <summary>What happened to a comment, and its id when it was included.</summary>
public sealed record ScheduleCommentResult(ScheduleCommentOutcome Outcome, Guid? Id = null);

/// <summary>A comment with the current name of its author and whether the person who asks may remove it.</summary>
public sealed record CommentLine(ScheduleComment Comment, string AuthorName, bool CanRemove);

/// <summary>The comments of an item, the oldest first, and whether the person who asks may comment now.</summary>
public sealed record ItemComments(IReadOnlyList<CommentLine> Comments, bool CanComment);

/// <summary>The project, the item and what the person who asks may do with its comments.</summary>
internal sealed record CommentAccess(Project Project, CommentedItem Item, ProjectPermissions Permissions);

internal static class CommentChecks
{
    /// <summary>The project and the item, for who sees the project; null when either does not exist for the person (404).</summary>
    public static async Task<CommentAccess?> AccessAsync(
        IProjectStore projects,
        IScheduleCommentStore comments,
        Guid projectId,
        Guid itemId,
        ProjectViewer viewer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await projects.FindAsync(projectId, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true }
            || await comments.ItemAsync(projectId, itemId, cancellationToken) is not { } item)
        {
            return null;
        }

        return new CommentAccess(project, item, permissions);
    }
}

/// <summary>The comments of a task or a milestone, for whoever sees the project (ADR-0157).</summary>
public sealed class ListScheduleComments(IProjectStore projects, IScheduleCommentStore comments, IUserDirectory users)
{
    /// <returns>The comments, or null when the project or the item does not exist for the person who asks.</returns>
    public async Task<ItemComments?> ExecuteAsync(Guid projectId, Guid itemId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        if (await CommentChecks.AccessAsync(projects, comments, projectId, itemId, viewer, cancellationToken) is not { } access)
        {
            return null;
        }

        var kept = await comments.OfItemAsync(projectId, itemId, cancellationToken);
        var names = await PeopleNames.OfAsync(users, kept.Select(comment => comment.AuthorId), cancellationToken);
        var open = !access.Project.IsClosed;
        return new ItemComments(
            [.. kept.Select(comment => new CommentLine(
                comment,
                PeopleNames.NameOf(names, comment.AuthorId),
                open && access.Permissions.MayRemoveComment(comment.AuthorId == viewer.UserId)))],
            open && access.Permissions.CanComment);
    }
}

/// <summary>
/// Comments on a task or a milestone of an open project (ADR-0157): everyone in it but the viewer, and the Administrator,
/// with the text with formatting of the tickets, mentioning only the people of the project; then the mentioned people and
/// the person in charge of the task hear about it now.
/// </summary>
public sealed class AddScheduleComment(
    IProjectStore projects, IScheduleCommentStore comments, TimeProvider clock, ProjectNotices notices)
{
    public async Task<ScheduleCommentResult> ExecuteAsync(
        Guid projectId, Guid itemId, string? body, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await CommentChecks.AccessAsync(projects, comments, projectId, itemId, viewer, cancellationToken) is not { } access)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.NotFound);
        }

        if (!access.Permissions.CanComment)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.Forbidden);
        }

        if (access.Project.IsClosed)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.Closed);
        }

        if (RichText.Parse(body) is not { } text)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.InvalidBody);
        }

        var people = access.Project.Members.Select(member => member.UserId).ToHashSet();
        if (text.Mentions.Any(person => !people.Contains(person)))
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.MentionRefused);
        }

        var comment = new ScheduleComment(projectId, itemId, viewer.UserId, text, clock.GetUtcNow().UtcDateTime);
        comments.Add(comment);
        await comments.SaveChangesAsync(cancellationToken);
        await notices.CommentedAsync(access.Project, access.Item, viewer.UserId, text.Mentions, cancellationToken);
        return new ScheduleCommentResult(ScheduleCommentOutcome.Done, comment.Id);
    }
}

/// <summary>
/// Removes a comment of an open project for good (ADR-0157): who wrote it, or the Administrator and the manager, any of
/// them.
/// </summary>
public sealed class RemoveScheduleComment(IProjectStore projects, IScheduleCommentStore comments)
{
    public async Task<ScheduleCommentResult> ExecuteAsync(
        Guid projectId, Guid itemId, Guid commentId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await CommentChecks.AccessAsync(projects, comments, projectId, itemId, viewer, cancellationToken) is not { } access
            || await comments.FindAsync(projectId, itemId, commentId, cancellationToken) is not { } comment)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.NotFound);
        }

        if (!access.Permissions.MayRemoveComment(comment.AuthorId == viewer.UserId))
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.Forbidden);
        }

        if (access.Project.IsClosed)
        {
            return new ScheduleCommentResult(ScheduleCommentOutcome.Closed);
        }

        comments.Remove(comment);
        await comments.SaveChangesAsync(cancellationToken);
        return new ScheduleCommentResult(ScheduleCommentOutcome.Done, comment.Id);
    }
}
