using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>Where the models of project are kept, in the same unit of work as the projects (ADR-0017).</summary>
public interface ITemplateStore
{
    /// <summary>The model with its phases, items and dependencies, tracked; null when it does not exist.</summary>
    Task<ProjectTemplate?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The models by name, with their parts; only the active ones, or all of them.</summary>
    Task<IReadOnlyList<ProjectTemplate>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    /// <summary>Whether another model has the name, in any case.</summary>
    Task<bool> NameInUseAsync(string name, Guid? except, CancellationToken cancellationToken);

    void Add(ProjectTemplate template);

    void Remove(ProjectTemplate template);
}

public enum TemplateOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    Invalid = 4,
    NameInUse = 5,
}

public enum TemplateField
{
    Name = 1,
    Description = 2,

    /// <summary>A model needs tasks or milestones in the schedule.</summary>
    Schedule = 3,
}

/// <summary>What happened to a change of a model: the id of a new one, or the invalid fields.</summary>
public sealed record TemplateResult(TemplateOutcome Outcome, Guid? Id, IReadOnlyList<TemplateField> Invalid)
{
    public static TemplateResult Of(TemplateOutcome outcome, Guid? id = null) => new(outcome, id, []);

    public static TemplateResult InvalidFields(IReadOnlyList<TemplateField> fields) => new(TemplateOutcome.Invalid, null, fields);

    public static List<TemplateField> Checked(string? name, string? description)
    {
        var invalid = new List<TemplateField>();
        if (ProjectTemplate.ValidName(name) is null)
        {
            invalid.Add(TemplateField.Name);
        }

        if (!ProjectTemplate.IsValidDescription(description))
        {
            invalid.Add(TemplateField.Description);
        }

        return invalid;
    }
}

/// <summary>
/// Saves the schedule of a project as a model (ADR-0017): the Administrator and the manager (ADR-0043), also of a closed
/// project, the usual source of a model. Without tasks nor milestones, or with the name of another model, it refuses.
/// </summary>
public sealed class SaveScheduleAsTemplate(
    IProjectStore projects, IScheduleStore schedules, ITemplateStore templates, IWorkCalendar calendar)
{
    public async Task<TemplateResult> ExecuteAsync(
        Guid projectId, string? name, string? description, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return TemplateResult.Of(TemplateOutcome.NotFound);
        }

        // The role decides, not the status: a closed project is the usual source of a model.
        if (!permissions.CanEditSchedule)
        {
            return TemplateResult.Of(TemplateOutcome.Forbidden);
        }

        var invalid = TemplateResult.Checked(name, description);
        var schedule = await schedules.FindAsync(projectId, cancellationToken);
        if (schedule is null || schedule.Items.Count == 0)
        {
            invalid.Add(TemplateField.Schedule);
        }

        if (invalid.Count > 0)
        {
            return TemplateResult.InvalidFields(invalid);
        }

        var valid = ProjectTemplate.ValidName(name)!;
        if (await templates.NameInUseAsync(valid, null, cancellationToken))
        {
            return TemplateResult.Of(TemplateOutcome.NameInUse);
        }

        var template = ProjectTemplate.From(schedule!, valid, description, await calendar.GetAsync(cancellationToken))!;
        templates.Add(template);
        await projects.SaveChangesAsync(cancellationToken);
        return TemplateResult.Of(TemplateOutcome.Done, template.Id);
    }
}

/// <summary>The models by name: the active ones, for a new project, or all, for the Administrator (ADR-0046).</summary>
public sealed class ListTemplates(ITemplateStore templates)
{
    public Task<IReadOnlyList<ProjectTemplate>> ExecuteAsync(bool activeOnly, CancellationToken cancellationToken) =>
        templates.ListAsync(activeOnly, cancellationToken);
}

/// <summary>Renames, describes, deactivates or reactivates a model; only the Administrator (ADR-0017, ADR-0046).</summary>
public sealed class ChangeTemplate(IProjectStore projects, ITemplateStore templates)
{
    public async Task<TemplateResult> ExecuteAsync(
        Guid id, string? name, string? description, bool isActive, CancellationToken cancellationToken)
    {
        var template = await templates.FindAsync(id, cancellationToken);
        if (template is null)
        {
            return TemplateResult.Of(TemplateOutcome.NotFound);
        }

        var invalid = TemplateResult.Checked(name, description);
        if (invalid.Count > 0)
        {
            return TemplateResult.InvalidFields(invalid);
        }

        var valid = ProjectTemplate.ValidName(name)!;
        if (await templates.NameInUseAsync(valid, id, cancellationToken))
        {
            return TemplateResult.Of(TemplateOutcome.NameInUse);
        }

        template.Change(valid, description);
        template.SetActive(isActive);
        await projects.SaveChangesAsync(cancellationToken);
        return TemplateResult.Of(TemplateOutcome.Done, id);
    }
}

/// <summary>Removes a model; the projects made from it keep their schedules (ADR-0017, ADR-0046).</summary>
public sealed class RemoveTemplate(IProjectStore projects, ITemplateStore templates)
{
    public async Task<TemplateResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await templates.FindAsync(id, cancellationToken) is not { } template)
        {
            return TemplateResult.Of(TemplateOutcome.NotFound);
        }

        templates.Remove(template);
        await projects.SaveChangesAsync(cancellationToken);
        return TemplateResult.Of(TemplateOutcome.Done, id);
    }
}
