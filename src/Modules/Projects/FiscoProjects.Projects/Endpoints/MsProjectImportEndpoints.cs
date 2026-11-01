using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>
/// A task or milestone of the schedule converted from the MS Project, as the creation receives it back (ADR-0135): the
/// position of its phase, the position of the task it is in (an item before it), its kind, name, the business days
/// after the start it starts, its duration (zero for a milestone), its progress (100 for a milestone reached), the
/// positions of the items it depends on (all before it), the position of its resource and the business days after the
/// start of its deadline, negative before it (GanttA-2).
/// </summary>
public sealed record ImportedItemResponse(
    int Phase,
    int? Parent,
    ScheduleItemKind Kind,
    string Name,
    int StartOffset,
    int Duration,
    int Progress,
    IReadOnlyList<int> After,
    int? Resource,
    int? DeadlineOffset);

/// <summary>The schedule converted from the file: the names of the phases and the items in the order they are made.</summary>
public sealed record ImportedPlanResponse(IReadOnlyList<string> Phases, IReadOnlyList<ImportedItemResponse> Items);

/// <summary>A task or milestone of the preview, with the dates the schedule is going to have and its resource.</summary>
public sealed record MsProjectPreviewItemResponse(
    ScheduleItemKind Kind,
    string Name,
    bool IsSubtask,
    DateOnly Start,
    DateOnly End,
    int Duration,
    int Progress,
    [property: Description("A milestone at 100% in the file, reached when the project is created.")] bool Reached,
    int? Resource);

/// <summary>A phase of the preview with its items in the order of the Lista.</summary>
public sealed record MsProjectPreviewPhaseResponse(string Name, IReadOnlyList<MsProjectPreviewItemResponse> Items);

/// <summary>The person of the company a resource suggests: a user (<c>id</c>) or a person of the directory who never signed in.</summary>
public sealed record MsProjectPersonResponse(Guid? Id, string? DirectoryId, string Name, string Email);

/// <summary>
/// A work resource of the file that is the first of some task: its name, its e-mail (only for the preview, never kept)
/// and how many tasks it has, with the person found by the e-mail or the client of the form by the name.
/// </summary>
public sealed record MsProjectResourceResponse(string Name, string? Email, int Tasks, MsProjectPersonResponse? Person, bool Client);

/// <summary>
/// A change of the conversion, with the names of the lines or resources it touched ("A → B" for a dependency) and how
/// many: the names, but for the blank lines, which have none.
/// </summary>
public sealed record MsProjectWarningResponse(MsProjectWarningKind Kind, IReadOnlyList<string> Names, int Count);

/// <summary>
/// How much of the file came: its tasks (the lines with a name) and the ones brought, its resources (of any kind) and the
/// ones that own tasks.
/// </summary>
public sealed record MsProjectCountsResponse(int FileTasks, int Tasks, int FileResources, int Resources);

/// <summary>
/// The file read (ADR-0135): its title, its first day, the start used, the schedule converted to send back on the
/// creation, its phases and items with the dates it is going to have, the resources, the changes of the conversion and
/// how much of the file came.
/// </summary>
public sealed record MsProjectPreviewResponse(
    string Title,
    DateOnly FirstDay,
    DateOnly Start,
    ImportedPlanResponse Plan,
    IReadOnlyList<MsProjectPreviewPhaseResponse> Phases,
    IReadOnlyList<MsProjectResourceResponse> Resources,
    IReadOnlyList<MsProjectWarningResponse> Warnings,
    MsProjectCountsResponse Counts);

/// <summary>A task or milestone of the schedule from the MS Project, as the preview gave it (<see cref="ImportedItemResponse"/>).</summary>
public sealed record ImportedItemRequest(
    int Phase,
    int? Parent,
    ScheduleItemKind Kind,
    string? Name,
    int StartOffset,
    int Duration,
    int Progress,
    IReadOnlyList<int>? After,
    int? Resource,
    int? DeadlineOffset = null);

/// <summary>
/// Who a resource of the file becomes (ADR-0135): a user (<c>userId</c>) or a person of the directory who never signed in
/// (<c>directoryId</c>), with the role in the project (Contributor, the default, or Developer; the manager and the tech
/// lead keep theirs), the client of the project (<c>client</c>), or nobody.
/// </summary>
public sealed record ImportedResourceRequest(Guid? UserId, string? DirectoryId, bool Client, ProjectRole? Role);

/// <summary>The schedule converted by the preview, with who each resource is, in the order of the preview.</summary>
public sealed record ImportedScheduleRequest(
    IReadOnlyList<string>? Phases, IReadOnlyList<ImportedItemRequest>? Items, IReadOnlyList<ImportedResourceRequest>? Resources);

/// <summary>Problem types of the import of the MS Project, so the screen can tell the reason.</summary>
public static class MsProjectImportProblems
{
    /// <summary>
    /// The file is not read as a schedule of the MS Project (ADR-0135); status 400, with the extension reason
    /// (NotMsProjectXml, TooLarge, TooManyTasks, NoTasks or OutOfRange).
    /// </summary>
    public const string Refused = "urn:fiscoprojects:problem:ms-project-file-refused";
}

/// <summary>The preview of a file of the MS Project for the "Novo projeto" (ADR-0135), for who creates projects.</summary>
internal static class MsProjectImportEndpoints
{
    // The form around the file: its name, the start, the client and the boundaries.
    private const long FormOverhead = 1024 * 1024;

    public static void MapMsProjectImportEndpoints(this RouteGroupBuilder projects)
    {
        // The protection against other sites is the X-CSRF header of the application (ADR-0054).
        projects.MapPost("/ms-project-preview", PreviewAsync).WithName("PreviewMsProjectImport")
            .WithDescription(
                "Reads an XML of the MS Project and converts it into the schedule of a new project (ADR-0135), keeping nothing.")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MsProjectImport.MaxBytes + FormOverhead))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    /// <summary>The schedule of the request, for the creation; null when it is too broken to read.</summary>
    internal static ImportedSchedule? Of(ImportedScheduleRequest request) =>
        request.Phases is not { } phases || request.Items is not { } items
            ? null
            : new ImportedSchedule(
                new SchedulePlanOutline(
                    [.. phases.Select(name => new OutlinePhase(name))],
                    [.. items.Select(item => new OutlineItem(
                        item.Phase,
                        item.Parent,
                        item.Kind,
                        item.Name ?? string.Empty,
                        item.StartOffset,
                        item.Duration,
                        item.Progress,
                        DeadlineOffset: item.DeadlineOffset))],
                    [.. items.SelectMany((item, index) => (item.After ?? []).Select(before => new OutlineDependency(before, index)))]),
                [.. items.Select(item => item.Resource)],
                [.. (request.Resources ?? []).Select(resource => new ImportedResourceChoice(
                    resource.UserId, resource.DirectoryId, resource.Client, resource.Role))]);

    private static async Task<Results<Ok<MsProjectPreviewResponse>, ProblemHttpResult>> PreviewAsync(
        IFormFile file,
        [FromForm] DateOnly? start,
        [FromForm] Guid? clientId,
        HttpContext http,
        PreviewMsProjectImport preview,
        CancellationToken cancellationToken)
    {
        if (file.Length > MsProjectImport.MaxBytes)
        {
            return Refused(MsProjectRefusal.TooLarge);
        }

        MsProjectPreview result;
        try
        {
            await using var content = file.OpenReadStream();
            result = await preview.ExecuteAsync(content, file.FileName, start, clientId, cancellationToken);
        }
        catch (MsProjectFileRefusedException refused)
        {
            return Refused(refused.Reason);
        }

        // The e-mails of the resources are personal data of the file: neither the browser nor anything on the way keeps them.
        http.Response.Headers.CacheControl = "no-store";
        var converted = result.File;
        var position = result.Schedule.Items.Select((item, index) => (item.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);
        return TypedResults.Ok(new MsProjectPreviewResponse(
            converted.Title,
            converted.FirstDay,
            result.Start,
            new ImportedPlanResponse(
                [.. converted.Outline.Phases.Select(phase => phase.Name)],
                [.. converted.Outline.Items.Select((item, index) => new ImportedItemResponse(
                    item.Phase,
                    item.Parent,
                    item.Kind,
                    item.Name,
                    item.StartOffset,
                    item.Duration,
                    item.Progress,
                    [.. converted.Outline.Dependencies.Where(dependency => dependency.Successor == index).Select(dependency => dependency.Predecessor)],
                    converted.ItemResources[index],
                    item.DeadlineOffset))]),
            [.. result.Figures.Phases.Select(phase => new MsProjectPreviewPhaseResponse(
                phase.Phase.Name,
                [.. phase.Items.Select(item => new MsProjectPreviewItemResponse(
                    item.Kind,
                    item.Name,
                    item.ParentId is not null,
                    item.Start,
                    item.End,
                    item.Duration,
                    item.Progress,
                    item.ReachedOn is not null,
                    converted.ItemResources[position[item.Id]]))]))],
            [.. converted.Resources.Zip(result.Suggestions).Select(pair => new MsProjectResourceResponse(
                pair.First.Name,
                pair.First.Email,
                pair.First.Tasks,
                pair.Second.Person is { } person ? new MsProjectPersonResponse(person.UserId, person.DirectoryId, person.Name, person.Email) : null,
                pair.Second.Client))],
            [.. converted.Warnings.Select(warning => new MsProjectWarningResponse(warning.Kind, warning.Names, warning.Count))],
            new MsProjectCountsResponse(converted.Counts.FileTasks, converted.Counts.Tasks, converted.Counts.FileResources, converted.Counts.Resources)));
    }

    private static ProblemHttpResult Refused(MsProjectRefusal reason) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "File refused",
            detail: "The file is not an XML of the MS Project the import reads: save it in the MS Project with \"Save as\", \"XML\".",
            type: MsProjectImportProblems.Refused,
            extensions: new Dictionary<string, object?> { ["reason"] = reason.ToString() });
}
