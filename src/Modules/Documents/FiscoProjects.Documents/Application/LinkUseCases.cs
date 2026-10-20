using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>Another category of links already has the name; nothing was saved.</summary>
public sealed class LinkCategoryNameInUseException(Exception inner) : InvalidOperationException("The name of the category is in use.", inner);

/// <summary>Where the categories and the links of the projects are kept.</summary>
public interface ILinkStore
{
    /// <summary>The categories, in the order of the name: all of them, or only the active ones.</summary>
    Task<IReadOnlyList<LinkCategory>> CategoriesAsync(bool activeOnly, CancellationToken cancellationToken);

    /// <summary>The category, tracked.</summary>
    Task<LinkCategory?> FindCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CategoryNameInUseAsync(string name, Guid? except, CancellationToken cancellationToken);

    void AddCategory(LinkCategory category);

    /// <summary>The links of the project, not tracked.</summary>
    Task<IReadOnlyList<ProjectLink>> LinksAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The link of the project, tracked.</summary>
    Task<ProjectLink?> FindLinkAsync(Guid projectId, Guid id, CancellationToken cancellationToken);

    void AddLink(ProjectLink link);

    void RemoveLink(ProjectLink link);

    /// <summary>Saves; throws <see cref="LinkCategoryNameInUseException"/> when another category took the name.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public enum LinkCategoryOutcome
{
    Done = 1,
    NotFound = 2,
    Invalid = 3,
    NameInUse = 4,
}

/// <summary>What happened to a category: its id, or why it was not saved.</summary>
public sealed record LinkCategoryResult(LinkCategoryOutcome Outcome, Guid? Id = null);

/// <summary>The categories of links: all of them for the Administration (ADR-0046), or the active ones for new links.</summary>
public sealed class ListLinkCategories(ILinkStore links)
{
    public Task<IReadOnlyList<LinkCategory>> ExecuteAsync(bool activeOnly, CancellationToken cancellationToken) =>
        links.CategoriesAsync(activeOnly, cancellationToken);
}

/// <summary>The Administrator includes a category of links, active, with a name no other category has (ADR-0032).</summary>
public sealed class AddLinkCategory(ILinkStore links)
{
    public async Task<LinkCategoryResult> ExecuteAsync(string? name, CancellationToken cancellationToken)
    {
        if (LinkCategory.NameOf(name) is not { } valid)
        {
            return new(LinkCategoryOutcome.Invalid);
        }

        if (await links.CategoryNameInUseAsync(valid, null, cancellationToken))
        {
            return new(LinkCategoryOutcome.NameInUse);
        }

        var category = new LinkCategory(valid);
        links.AddCategory(category);
        return await SaveAsync(links, category.Id, cancellationToken);
    }

    internal static async Task<LinkCategoryResult> SaveAsync(ILinkStore links, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await links.SaveChangesAsync(cancellationToken);
        }
        catch (LinkCategoryNameInUseException)
        {
            return new(LinkCategoryOutcome.NameInUse);
        }

        return new(LinkCategoryOutcome.Done, id);
    }
}

/// <summary>
/// The Administrator renames, deactivates or reactivates a category of links (ADR-0032): an inactive one is not offered
/// for new links, and the links that have it keep it.
/// </summary>
public sealed class ChangeLinkCategory(ILinkStore links)
{
    public async Task<LinkCategoryResult> ExecuteAsync(Guid id, string? name, bool isActive, CancellationToken cancellationToken)
    {
        var category = await links.FindCategoryAsync(id, cancellationToken);
        if (category is null)
        {
            return new(LinkCategoryOutcome.NotFound);
        }

        if (LinkCategory.NameOf(name) is not { } valid)
        {
            return new(LinkCategoryOutcome.Invalid);
        }

        if (await links.CategoryNameInUseAsync(valid, id, cancellationToken))
        {
            return new(LinkCategoryOutcome.NameInUse);
        }

        category.Change(valid, isActive);
        return await AddLinkCategory.SaveAsync(links, id, cancellationToken);
    }
}

/// <summary>A link with the names a list shows: the category and who included it.</summary>
public sealed record LinkView(ProjectLink Link, string CategoryName, string CreatedBy);

/// <summary>A category the links of the project use, with how many links have it.</summary>
public sealed record LinkCategoryCount(Guid Id, string Name, int Count);

/// <summary>
/// The links of a project the person sees, after the search and the filter; the categories the links use, with how
/// many each has, and the total, without the search; and whether the person may change them.
/// </summary>
public sealed record LinkList(IReadOnlyList<LinkView> Items, IReadOnlyList<LinkCategoryCount> Categories, int Total, bool CanEdit);

/// <summary>A link as the person fills it in.</summary>
public sealed record LinkRequest(string? Title, string? Url, Guid? CategoryId, string? Description);

public enum LinkOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Invalid = 5,
}

/// <summary>What happened to a link: its id, or why nothing was saved, with the fields that are not valid.</summary>
public sealed record LinkResult(LinkOutcome Outcome, Guid? Id = null, IReadOnlyList<LinkField>? Invalid = null)
{
    public static LinkResult Of(LinkOutcome outcome, Guid? id = null) => new(outcome, id);
}

/// <summary>
/// The links of a project (ADR-0032): everyone in it and the Administrator sees them; a project the person does not see
/// is null (404). The search looks in the title and in the address, without telling case or accents apart; a project
/// has a few links, so they come all at once, in the order of the category and of the title.
/// </summary>
public sealed class ListLinks(IProjectAccess projects, ILinkStore links, IUserDirectory users)
{
    public async Task<LinkList?> ExecuteAsync(
        Guid projectId, string? search, Guid? categoryId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is not { } project)
        {
            return null;
        }

        var all = await links.LinksAsync(projectId, cancellationToken);
        var categories = (await links.CategoriesAsync(activeOnly: false, cancellationToken)).ToDictionary(category => category.Id, category => category.Name);
        var names = await users.NamesAsync([.. all.Select(link => link.CreatedById)], cancellationToken);
        var text = SearchText.Simplify(search);
        var found = all
            .Where(link => categoryId is null || link.CategoryId == categoryId)
            .Where(link => SearchText.Finds(link.Title, text) || SearchText.Finds(link.Url, text))
            .Select(link => new LinkView(
                link, categories.GetValueOrDefault(link.CategoryId, string.Empty), names.GetValueOrDefault(link.CreatedById, string.Empty)))
            .OrderBy(view => view.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(view => view.Link.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(view => view.Link.Id)
            .ToList();
        var counts = all
            .GroupBy(link => link.CategoryId)
            .Select(group => new LinkCategoryCount(group.Key, categories.GetValueOrDefault(group.Key, string.Empty), group.Count()))
            .OrderBy(count => count.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new(found, counts, all.Count, DocumentAccess.MayEditLinks(viewer, project) && !project.IsClosed);
    }
}

/// <summary>
/// Everyone in the project but the viewer includes a link, while the project is open (ADR-0016, ADR-0045), in an
/// active category.
/// </summary>
public sealed class AddLink(IProjectAccess projects, ILinkStore links, TimeProvider clock)
{
    public async Task<LinkResult> ExecuteAsync(Guid projectId, LinkRequest request, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await LinkChanges.RefusalAsync(projects, projectId, viewer, cancellationToken) is { } refusal)
        {
            return refusal;
        }

        var (form, invalid) = await LinkChanges.CheckAsync(links, request, current: null, cancellationToken);
        if (form is null)
        {
            return new LinkResult(LinkOutcome.Invalid, null, invalid);
        }

        var link = new ProjectLink(projectId, form, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        links.AddLink(link);
        await links.SaveChangesAsync(cancellationToken);
        return LinkResult.Of(LinkOutcome.Done, link.Id);
    }
}

/// <summary>
/// Everyone in the project but the viewer changes a link, while the project is open (ADR-0016, ADR-0045); a link keeps
/// a category that became inactive, and takes only active ones in its place.
/// </summary>
public sealed class ChangeLink(IProjectAccess projects, ILinkStore links, TimeProvider clock)
{
    public async Task<LinkResult> ExecuteAsync(
        Guid projectId, Guid linkId, LinkRequest request, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await LinkChanges.RefusalAsync(projects, projectId, viewer, cancellationToken) is { } refusal)
        {
            return refusal;
        }

        if (await links.FindLinkAsync(projectId, linkId, cancellationToken) is not { } link)
        {
            return LinkResult.Of(LinkOutcome.NotFound);
        }

        var (form, invalid) = await LinkChanges.CheckAsync(links, request, link.CategoryId, cancellationToken);
        if (form is null)
        {
            return new LinkResult(LinkOutcome.Invalid, null, invalid);
        }

        link.Change(form, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        await links.SaveChangesAsync(cancellationToken);
        return LinkResult.Of(LinkOutcome.Done, link.Id);
    }
}

/// <summary>Everyone in the project but the viewer removes a link, while the project is open (ADR-0016, ADR-0045).</summary>
public sealed class RemoveLink(IProjectAccess projects, ILinkStore links)
{
    public async Task<LinkResult> ExecuteAsync(Guid projectId, Guid linkId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await LinkChanges.RefusalAsync(projects, projectId, viewer, cancellationToken) is { } refusal)
        {
            return refusal;
        }

        if (await links.FindLinkAsync(projectId, linkId, cancellationToken) is not { } link)
        {
            return LinkResult.Of(LinkOutcome.NotFound);
        }

        links.RemoveLink(link);
        await links.SaveChangesAsync(cancellationToken);
        return LinkResult.Of(LinkOutcome.Done, link.Id);
    }
}

/// <summary>The checks every change of a link makes, in one place.</summary>
internal static class LinkChanges
{
    /// <summary>Why the person may not change the links of the project: not seen (404), the viewer (403) or closed (409).</summary>
    public static async Task<LinkResult?> RefusalAsync(
        IProjectAccess projects, Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is not { } project)
        {
            return LinkResult.Of(LinkOutcome.NotFound);
        }

        if (!DocumentAccess.MayEditLinks(viewer, project))
        {
            return LinkResult.Of(LinkOutcome.Forbidden);
        }

        return project.IsClosed ? LinkResult.Of(LinkOutcome.ProjectClosed) : null;
    }

    /// <summary>
    /// The link checked, or every field that is not valid: the category is valid when it is active or is the one the
    /// link already has.
    /// </summary>
    public static async Task<(LinkForm? Form, IReadOnlyList<LinkField> Invalid)> CheckAsync(
        ILinkStore links, LinkRequest request, Guid? current, CancellationToken cancellationToken)
    {
        var (form, invalid) = LinkForm.Of(request.Title, request.Url, request.CategoryId, request.Description);
        if (request.CategoryId is { } categoryId && !await CategoryTakenAsync(links, categoryId, current, cancellationToken))
        {
            return (null, [.. invalid, LinkField.Category]);
        }

        return (form, invalid);
    }

    private static async Task<bool> CategoryTakenAsync(ILinkStore links, Guid categoryId, Guid? current, CancellationToken cancellationToken) =>
        categoryId == current
        || await links.FindCategoryAsync(categoryId, cancellationToken) is { IsActive: true };
}
