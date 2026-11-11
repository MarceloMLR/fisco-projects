using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// A comment on a task or a milestone of the schedule (ADR-0157): the text with its formatting and mentions of the people
/// of the project, by a person who comments, never changed, but for the name of an anonymized person in its mentions
/// (ADR-0110). It points to the item by its id only, so a removed item that comes back with the undo brings its comments
/// back (ADR-0091).
/// </summary>
public sealed class ScheduleComment : Entity
{
    private ScheduleComment()
    {
        Body = string.Empty;
        Text = string.Empty;
    }

    public ScheduleComment(Guid projectId, Guid itemId, Guid authorId, RichText body, DateTime sentAt)
    {
        ArgumentNullException.ThrowIfNull(body);
        ProjectId = projectId;
        ItemId = itemId;
        AuthorId = authorId;
        Body = body.Json;
        Text = body.Text;
        SentAt = sentAt;
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The task or the milestone.</summary>
    public Guid ItemId { get; private init; }

    public Guid AuthorId { get; private init; }

    /// <summary>The text with its formatting, as the JSON of the editor (<see cref="RichText"/>).</summary>
    public string Body { get; private set; }

    /// <summary>The plain text.</summary>
    public string Text { get; private set; }

    public DateTime SentAt { get; private init; }

    /// <summary>The mentions of an anonymized person name the identifier, in the text with its formatting and the plain one.</summary>
    public void RenameMention(Guid person, string label)
    {
        if (RichText.Parse(Body)?.RenameMention(person, label) is { } renamed)
        {
            Body = renamed.Json;
            Text = renamed.Text;
        }
    }
}
