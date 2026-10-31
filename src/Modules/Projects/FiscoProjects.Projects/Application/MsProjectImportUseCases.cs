using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Application;

/// <summary>
/// What a resource of the file suggests on the screen (ADR-0135): the person found by its e-mail, or the client of the
/// form when the resource has its name, or nobody.
/// </summary>
public sealed record MsProjectSuggestion(FoundPerson? Person, bool Client);

/// <summary>
/// A file of the MS Project read for a new project, nothing kept: the conversion, the start used, the schedule it makes
/// (in memory, with the dates it is going to have; its items in the order of the outline) and the suggestion of each
/// resource.
/// </summary>
public sealed record MsProjectPreview(
    MsProjectSchedule File, DateOnly Start, Schedule Schedule, ScheduleFigures Figures, IReadOnlyList<MsProjectSuggestion> Suggestions);

/// <summary>
/// Reads a file of the MS Project for the "Novo projeto" (ADR-0135), without keeping anything: converts it, makes the
/// schedule in memory from the start (the first day of the file, when none) and suggests a person for each resource by
/// its e-mail, or the client of the form by its name. The e-mails of the file are not kept nor written anywhere.
/// </summary>
public sealed class PreviewMsProjectImport(
    IPeopleLookup people, IClientStore clients, IWorkCalendar calendar, InstallationClock installationClock)
{
    /// <exception cref="MsProjectFileRefusedException">The file is not read, with the reason.</exception>
    public async Task<MsProjectPreview> ExecuteAsync(
        Stream content, string fileName, DateOnly? start, Guid? clientId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var file = MsProjectImport.Read(content);
        var days = await calendar.GetAsync(cancellationToken);
        var today = await installationClock.TodayAsync(cancellationToken);
        var converted = MsProjectImport.Convert(file, Path.GetFileNameWithoutExtension(fileName), days);
        var first = start is { } chosen && Schedule.IsValidDay(chosen) ? chosen
            : Schedule.IsValidDay(converted.FirstDay) ? converted.FirstDay : today;
        var schedule = new Schedule(Guid.CreateVersion7());
        try
        {
            schedule.ApplyTemplate(converted.Outline, first, days, today);
        }
        catch (ArgumentException)
        {
            // Only dates beyond the days a schedule may use: the conversion gives the rest valid.
            throw new MsProjectFileRefusedException(MsProjectRefusal.OutOfRange);
        }

        var client = clientId is { } id ? await clients.FindAsync(id, cancellationToken) : null;
        var found = await people.ByEmailsAsync(
            [.. converted.Resources.Where(resource => resource.Email is not null).Select(resource => resource.Email!)], cancellationToken);
        var suggestions = converted.Resources.Select(resource =>
            resource.Email is { } email && found.TryGetValue(email, out var person)
                ? new MsProjectSuggestion(person, Client: false)
                : new MsProjectSuggestion(null, client is not null
                    && string.Equals(resource.Name.Trim(), client.Name.Trim(), StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        return new MsProjectPreview(converted, first, schedule, schedule.Figures(today), suggestions);
    }
}

/// <summary>
/// Who a resource of the file becomes in the new project (ADR-0135): a user, a person of the directory who never signed
/// in (who becomes a user, ADR-0005), with the role of who is not yet in the project, the client of the project
/// (ADR-0102), or nobody.
/// </summary>
public sealed record ImportedResourceChoice(Guid? UserId, string? DirectoryId, bool Client, ProjectRole? Role);

/// <summary>The schedule converted by the preview, as the screen confirmed it: the outline, the resource of each item and who each resource is.</summary>
public sealed record ImportedSchedule(
    SchedulePlanOutline Outline, IReadOnlyList<int?> ItemResources, IReadOnlyList<ImportedResourceChoice> Resources);

/// <summary>
/// The people and the owners of a schedule imported, checked before anything is created (ADR-0135): each resource is a
/// person who may be in the project with the role (Contributor or Developer for who is not the manager nor the tech
/// lead), the client of a project with one, or nobody; each item points to a resource of the list.
/// </summary>
internal static class ImportedScheduleChecks
{
    /// <summary>The outline with the owner of each item: the user of its resource, or the client.</summary>
    public static SchedulePlanOutline OutlineOf(ImportedSchedule imported, IReadOnlyList<Guid?> users) =>
        imported.Outline with
        {
            Items = [.. imported.Outline.Items.Select((item, index) => imported.ItemResources[index] is { } resource
                ? item with { OwnerId = users[resource], OwnedByClient = imported.Resources[resource].Client }
                : item with { OwnerId = null, OwnedByClient = false })],
        };

    public static readonly ProjectRole[] RolesOfNewPeople = [ProjectRole.Contributor, ProjectRole.Developer];

    public static async Task<bool> IsValidAsync(
        ImportedSchedule imported, Guid? clientId, IUserAccess access, CancellationToken cancellationToken)
    {
        if (imported.Outline.Items.Count is 0 or > MsProjectImport.MaxTasks
            || imported.Outline.Phases.Count > MsProjectImport.MaxTasks
            || imported.ItemResources.Count != imported.Outline.Items.Count
            || imported.Resources.Count > MsProjectImport.MaxTasks
            || imported.ItemResources.Any(resource => resource is { } index && (index < 0 || index >= imported.Resources.Count)))
        {
            return false;
        }

        foreach (var resource in imported.Resources)
        {
            var kinds = (resource.UserId is null ? 0 : 1) + (string.IsNullOrWhiteSpace(resource.DirectoryId) ? 0 : 1) + (resource.Client ? 1 : 0);
            if (kinds > 1
                || (resource.Client && clientId is null)
                || (resource.Role is { } role && !RolesOfNewPeople.Contains(role))
                || (resource.UserId is { } userId && await access.FindAsync(userId, cancellationToken) is not { IsActive: true }))
            {
                return false;
            }
        }

        return true;
    }
}
