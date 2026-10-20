using FiscoProjects.SharedKernel;

namespace FiscoProjects.Documents.Domain;

/// <summary>
/// A category of the links of the projects (ADR-0032), from a list the Administrator keeps: none is fixed in the code
/// (ADR-0003). It is never removed, only deactivated, since the links keep it.
/// </summary>
public sealed class LinkCategory : Entity
{
    public const int NameMaxLength = 80;

    private LinkCategory()
    {
        Name = string.Empty;
    }

    public LinkCategory(string name)
    {
        Name = name;
        IsActive = true;
    }

    public string Name { get; private set; }

    /// <summary>An inactive category is not offered for new links; the links that have it keep it.</summary>
    public bool IsActive { get; private set; }

    public void Change(string name, bool isActive)
    {
        Name = name;
        IsActive = isActive;
    }

    /// <summary>A name with something in it, up to 80 characters, trimmed; null when it is not valid.</summary>
    public static string? NameOf(string? name) =>
        name?.Trim() is { Length: > 0 and <= NameMaxLength } trimmed && !trimmed.Any(char.IsControl) ? trimmed : null;
}

/// <summary>The fields of a link, to tell which ones are not valid.</summary>
public enum LinkField
{
    Title = 1,
    Url = 2,
    Category = 3,
    Description = 4,
}

/// <summary>
/// A link of a project (ADR-0032): an address the team uses, as a system or an environment, with a title, a category
/// and what it is for, kept in the database of the FiscoProjects, not as a file.
/// </summary>
public sealed class ProjectLink : Entity
{
    public const int TitleMaxLength = 120;
    public const int UrlMaxLength = 2_000;
    public const int DescriptionMaxLength = 500;

    private ProjectLink()
    {
        Title = string.Empty;
        Url = string.Empty;
    }

    public ProjectLink(Guid projectId, LinkForm form, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        ProjectId = projectId;
        CategoryId = form.CategoryId;
        Title = form.Title;
        Url = form.Url;
        Description = form.Description;
        CreatedById = userId;
        CreatedAt = now;
        UpdatedById = userId;
        UpdatedAt = now;
    }

    public Guid ProjectId { get; private init; }

    public Guid CategoryId { get; private set; }

    public string Title { get; private set; }

    public string Url { get; private set; }

    public string? Description { get; private set; }

    public Guid CreatedById { get; private init; }

    public DateTime CreatedAt { get; private init; }

    public Guid UpdatedById { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Change(LinkForm form, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        CategoryId = form.CategoryId;
        Title = form.Title;
        Url = form.Url;
        Description = form.Description;
        UpdatedById = userId;
        UpdatedAt = now;
    }
}

/// <summary>A link as the person filled it in, already checked: the title, the address, the category and the description.</summary>
public sealed record LinkForm(string Title, string Url, Guid CategoryId, string? Description)
{
    /// <summary>
    /// The link, trimmed, or the fields that are not valid: the title up to 120 characters, the address http or https
    /// up to 2,000, a category and the description, empty or up to 500.
    /// </summary>
    public static (LinkForm? Form, IReadOnlyList<LinkField> Invalid) Of(
        string? title, string? url, Guid? categoryId, string? description)
    {
        var invalid = new List<LinkField>();
        var validTitle = title?.Trim() is { Length: > 0 and <= ProjectLink.TitleMaxLength } trimmed ? trimmed : null;
        if (validTitle is null)
        {
            invalid.Add(LinkField.Title);
        }

        if (!WebAddress.TryOf(url, ProjectLink.UrlMaxLength, out var validUrl) || validUrl is null)
        {
            invalid.Add(LinkField.Url);
        }

        if (categoryId is null)
        {
            invalid.Add(LinkField.Category);
        }

        var validDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (validDescription?.Length > ProjectLink.DescriptionMaxLength)
        {
            invalid.Add(LinkField.Description);
        }

        return invalid.Count > 0
            ? (null, invalid)
            : (new LinkForm(validTitle!, validUrl!, categoryId!.Value, validDescription), invalid);
    }
}
