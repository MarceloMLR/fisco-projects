using GerenWare.Administration.Contracts;
using GerenWare.Calendar.Contracts;
using GerenWare.Files.Contracts;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;
using GerenWare.Users.Contracts;

namespace GerenWare.Projects.Application;

/// <summary>Where the notes of the last status report of each project are kept (ADR-0165).</summary>
public interface IStatusReportStore
{
    /// <summary>The notes of the project, tracked, with the risks; null when it never had a report.</summary>
    Task<StatusReportNotes?> FindAsync(Guid projectId, CancellationToken cancellationToken);

    void Add(StatusReportNotes notes);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>An image of the report, as stored: its content and its type (PNG, JPG or SVG).</summary>
public sealed record StatusReportImage(byte[] Content, string ContentType);

/// <summary>
/// The installation in the report (ADR-0146, ADR-0165): its name, its logo for light backgrounds and its main and
/// secondary colors, null when the Administrator chose none.
/// </summary>
public sealed record StatusReportBrand(string Name, StatusReportImage? Logo, string? MainColor, string? SecondaryColor);

/// <summary>A phase with dates of the report: its period, its progress and how much should be done by today, if anything.</summary>
public sealed record StatusReportPhase(string Name, DateOnly Start, DateOnly End, int Progress, int? Planned);

/// <summary>A milestone ahead: its day and the business days left until it, or the business days it is late.</summary>
public sealed record StatusReportMilestone(string Name, DateOnly Day, int BusinessDaysLeft, int? DaysLate);

/// <summary>
/// A late task of the report (ADR-0020, ADR-0164): its dates, its business days of delay, whether it is late only for
/// not having started, and what its projection pushes (none in a closed project).
/// </summary>
public sealed record StatusReportLateTask(
    string Name, DateOnly Start, DateOnly End, int DaysLate, bool NotStarted, ScheduleProjection? Projection);

/// <summary>
/// A risk of the report (ADR-0165): the ticket (number, title, status, priority, whether it is late and the ticket at a
/// vendor), the level and the point of attention the manager wrote, and the milestones not reached that depend on the
/// items linked to it (ADR-0164), up to the limit, with how many more.
/// </summary>
public sealed record StatusReportRiskItem(
    int Number,
    string Title,
    ReportTicketStatus Status,
    ReportTicketPriority Priority,
    bool Overdue,
    ReportVendorTicket? Vendor,
    RiskLevel Level,
    string Attention,
    IReadOnlyList<StatusReportMilestone> Milestones,
    int MoreMilestones);

/// <summary>A list of the report cut at its limit, with how many items were left out.</summary>
public sealed record StatusReportList<T>(IReadOnlyList<T> Items, int More)
{
    public static StatusReportList<T> Of(IEnumerable<T> items, int limit)
    {
        var all = items.ToList();
        return new([.. all.Take(limit)], Math.Max(0, all.Count - limit));
    }
}

/// <summary>
/// Everything that goes in a status report (ADR-0165, ADR-0166), on the day of the installation: the installation, the
/// client, the project and its managers; the progress, the deadline, the phases, the milestones ahead and the late
/// tasks, by the rules of the overview (ADR-0073); the open and overdue tickets; the texts and the risks; and the hours,
/// only with "Mostrar horas".
/// </summary>
public sealed record StatusReportData(
    DateOnly Today,
    StatusReportBrand Brand,
    string ProjectCode,
    string ProjectName,
    ProjectStatus Status,
    string? ClientName,
    StatusReportImage? ClientLogo,
    IReadOnlyList<string> Managers,
    OverviewProgress? Progress,
    OverviewDeadline? Deadline,
    StatusReportList<StatusReportPhase> Phases,
    IReadOnlyList<StatusReportMilestone> Milestones,
    StatusReportList<StatusReportLateTask> LateTasks,
    int OpenTickets,
    int OverdueTickets,
    StatusReportTexts Texts,
    IReadOnlyList<StatusReportRiskItem> Risks,
    ReportHours? Hours);

/// <summary>Makes the PowerPoint of a status report, in the default template (ADR-0165); the infrastructure implements it.</summary>
public interface IStatusReportRenderer
{
    Task<byte[]> RenderAsync(StatusReportData data, CancellationToken cancellationToken);

    /// <summary>The name of the file: the code of the project, the name of the report and the day.</summary>
    string FileNameOf(string projectCode, DateOnly day);
}

/// <summary>An open ticket that may go in as a risk: its number, title and status, and whether it threatens milestones (ADR-0164).</summary>
public sealed record StatusReportTicket(int Number, string Title, ReportTicketStatus Status, bool Threatens);

/// <summary>When and by whom the last report of the project was generated.</summary>
public sealed record StatusReportLast(DateTime GeneratedAt, Guid GeneratedById, string GeneratedBy);

/// <summary>
/// The draft of a new status report (ADR-0165): the last report, the texts it had, the open tickets that may go in as
/// risks and the risks suggested.
/// </summary>
public sealed record StatusReportDraft(
    StatusReportLast? Last, StatusReportTexts Texts, IReadOnlyList<StatusReportTicket> Tickets, IReadOnlyList<StatusReportRisk> Risks);

public enum StatusReportOutcome
{
    Done = 1,

    /// <summary>The project does not exist or the person is not in it (404).</summary>
    NotFound = 2,

    /// <summary>The person is in the project, but does not generate its reports (403).</summary>
    Forbidden = 3,

    /// <summary>A field was refused (400).</summary>
    Invalid = 4,
}

/// <summary>What happened to a report: the document kept, or why nothing was made.</summary>
public sealed record StatusReportResult(
    StatusReportOutcome Outcome, SavedStatusReport? Saved = null, IReadOnlyList<StatusReportField>? Invalid = null);

/// <summary>Who generates the reports of a project (ADR-0165): the Administrator and the manager, in any status of it.</summary>
internal static class StatusReportAccess
{
    public static async Task<(Project? Project, StatusReportOutcome? Refusal)> CheckAsync(
        IProjectStore projects, Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null)
        {
            return (null, StatusReportOutcome.NotFound);
        }

        var permissions = ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        return !permissions.CanView
            ? (null, StatusReportOutcome.NotFound)
            : permissions.CanGenerateStatusReport ? (project, null) : (null, StatusReportOutcome.Forbidden);
    }

    /// <summary>The open tickets of the project, by the number; none without the Tickets module.</summary>
    public static async Task<IReadOnlyList<ReportTicket>> OpenTicketsAsync(
        IEnumerable<IProjectReportTickets> sources, Guid projectId, CancellationToken cancellationToken)
    {
        var tickets = new List<ReportTicket>();
        foreach (var source in sources)
        {
            tickets.AddRange(await source.OpenAsync(projectId, cancellationToken));
        }

        return tickets;
    }
}

/// <summary>
/// The draft of a new status report (ADR-0165), for who generates it: the texts of the last report, the open tickets of
/// the project and the risks suggested (<see cref="StatusReportSuggestions"/>), with the threats in the order of the
/// overview (ADR-0164).
/// </summary>
public sealed class GetStatusReportDraft(
    IProjectStore projects,
    IStatusReportStore notes,
    GetProjectOverview overview,
    IEnumerable<IProjectReportTickets> tickets,
    IUserDirectory users)
{
    public async Task<(StatusReportDraft? Draft, StatusReportOutcome Outcome)> ExecuteAsync(
        Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (_, refusal) = await StatusReportAccess.CheckAsync(projects, projectId, viewer, cancellationToken);
        if (refusal is { } outcome)
        {
            return (null, outcome);
        }

        var last = await notes.FindAsync(projectId, cancellationToken);
        var open = await StatusReportAccess.OpenTicketsAsync(tickets, projectId, cancellationToken);
        var threats = (await overview.ExecuteAsync(projectId, viewer, cancellationToken))?.Threats ?? [];
        var threatening = threats.Select(threat => threat.Number).ToList();
        var numbers = open.Select(ticket => ticket.Number).ToHashSet();
        StatusReportLast? lastReport = null;
        if (last is not null)
        {
            var names = await PeopleNames.OfAsync(users, [last.GeneratedById], cancellationToken);
            lastReport = new StatusReportLast(last.GeneratedAt, last.GeneratedById, PeopleNames.NameOf(names, last.GeneratedById));
        }

        return (new StatusReportDraft(
            lastReport,
            last?.Texts ?? StatusReportTexts.Empty,
            [.. open.Select(ticket => new StatusReportTicket(
                ticket.Number, ticket.Title, ticket.Status, threatening.Contains(ticket.Number)))],
            StatusReportSuggestions.Of(last, numbers, threatening)), StatusReportOutcome.Done);
    }
}

/// <summary>
/// Generates a status report (ADR-0165, ADR-0166): checks who generates and what was written
/// (<see cref="StatusReportForm"/>), gathers the data (<see cref="StatusReportData"/>) by the rules of the overview,
/// makes the PowerPoint (<see cref="IStatusReportRenderer"/>), keeps it in the folder of the status reports of the
/// project (<see cref="IStatusReportDocuments"/>) and then the notes for the next draft.
/// </summary>
public sealed class GenerateStatusReport(
    IProjectStore projects,
    IScheduleStore schedules,
    IClientStore clients,
    IStatusReportStore notes,
    GetProjectOverview overview,
    IEnumerable<IProjectReportTickets> tickets,
    IEnumerable<IProjectReportHours> hours,
    IEnumerable<IStatusReportDocuments> documents,
    IInstallationBrand brand,
    IFileStorage files,
    IWorkCalendar calendar,
    IUserDirectory users,
    IStatusReportRenderer renderer,
    TimeProvider clock)
{
    public async Task<StatusReportResult> ExecuteAsync(
        Guid projectId,
        ProjectViewer viewer,
        StatusReportTexts? texts,
        IReadOnlyList<StatusReportRiskInput>? risks,
        bool showHours,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (project, refusal) = await StatusReportAccess.CheckAsync(projects, projectId, viewer, cancellationToken);
        if (refusal is { } outcome)
        {
            return new(outcome);
        }

        var open = await StatusReportAccess.OpenTicketsAsync(tickets, projectId, cancellationToken);
        var (form, invalid) = StatusReportForm.Of(
            texts?.Summary,
            texts?.Focus,
            texts?.NextSteps,
            texts?.ClientNeeds,
            risks,
            showHours,
            open.Select(ticket => ticket.Number).ToHashSet());
        if (form is null)
        {
            return new(StatusReportOutcome.Invalid, Invalid: invalid);
        }

        var store = documents.FirstOrDefault()
            ?? throw new InvalidOperationException("No module keeps the documents of the status reports.");
        var data = await DataOfAsync(project!, viewer, form, open, cancellationToken);
        var content = await renderer.RenderAsync(data, cancellationToken);
        SavedStatusReport saved;
        await using (var stream = new MemoryStream(content, writable: false))
        {
            saved = await store.SaveAsync(
                projectId, renderer.FileNameOf(project!.Code, data.Today), stream, viewer.UserId, cancellationToken);
        }

        var kept = await notes.FindAsync(projectId, cancellationToken);
        if (kept is null)
        {
            kept = new StatusReportNotes(projectId);
            notes.Add(kept);
        }

        kept.Keep(form, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        await notes.SaveChangesAsync(cancellationToken);
        return new(StatusReportOutcome.Done, saved);
    }

    private async Task<StatusReportData> DataOfAsync(
        Project project, ProjectViewer viewer, StatusReportForm form, IReadOnlyList<ReportTicket> open, CancellationToken cancellationToken)
    {
        var view = await overview.ExecuteAsync(project.Id, viewer, cancellationToken)
            ?? throw new InvalidOperationException("The person no longer sees the project.");
        var days = await calendar.GetAsync(cancellationToken);
        var schedule = await schedules.FindAsync(project.Id, cancellationToken) ?? new Schedule(project.Id);
        var figures = schedule.Figures(view.Today);
        var installation = await brand.GetAsync(cancellationToken);
        var client = project.ClientId is { } clientId ? await clients.FindAsync(clientId, cancellationToken) : null;
        var managers = project.Members.Where(member => member.Role == ProjectRole.Manager).OrderBy(member => member.AddedAt).ToList();
        var names = await PeopleNames.OfAsync(users, managers.Select(member => member.UserId), cancellationToken);
        ReportHours? projectHours = null;
        if (form.ShowHours)
        {
            foreach (var source in hours)
            {
                projectHours = await source.ForReportAsync(project.Id, viewer.UserId, viewer.IsAdministrator, cancellationToken);
            }
        }

        var byNumber = open.ToDictionary(ticket => ticket.Number);
        return new StatusReportData(
            view.Today,
            new StatusReportBrand(
                installation.Name,
                await ImageOfAsync(installation.LogoFileId, installation.LogoContentType, cancellationToken),
                installation.MainColor,
                installation.SecondaryColor),
            project.Code,
            project.Name,
            project.Status,
            client?.Name,
            client is null ? null : await ImageOfAsync(client.LogoFileId, client.LogoContentType, cancellationToken),
            [.. managers.Select(member => PeopleNames.NameOf(names, member.UserId))],
            view.Progress,
            view.Deadline,
            StatusReportList<StatusReportPhase>.Of(
                figures.Phases
                    .Where(phase => phase.Start is not null)
                    .Select(phase => new StatusReportPhase(
                        phase.Phase.Name,
                        phase.Start!.Value,
                        phase.End!.Value,
                        phase.Progress,
                        SchedulePlan.PlannedProgress(phase.Items, schedule.Baseline, view.Today, days))),
                StatusReportLimits.Phases),
            [.. OpenMilestones.Of(schedule, view.Today, days).Take(StatusReportLimits.Milestones).Select(MilestoneOf)],
            StatusReportList<StatusReportLateTask>.Of(
                view.LateTasks.Select(task => new StatusReportLateTask(
                    task.Name, task.Start, task.End, task.DaysLate, task.DaysLate == 0, task.Projection)),
                StatusReportLimits.LateTasks),
            view.Tickets.Open,
            view.Tickets.Overdue,
            form.Texts,
            [.. form.Risks.Select(risk =>
            {
                var ticket = byNumber[risk.TicketNumber];
                var reach = ScheduleImpacts.Of(schedule, ticket.ItemIds, new HashSet<Guid>());
                var milestones = StatusReportList<StatusReportMilestone>.Of(
                    reach.Milestones.Select(item => MilestoneOf(OpenMilestones.MilestoneOf(item, view.Today, days))),
                    StatusReportLimits.RiskMilestones);
                return new StatusReportRiskItem(
                    ticket.Number,
                    ticket.Title,
                    ticket.Status,
                    ticket.Priority,
                    ticket.Overdue,
                    ticket.Vendor,
                    risk.Level,
                    risk.Attention,
                    milestones.Items,
                    milestones.More);
            })],
            projectHours);
    }

    private static StatusReportMilestone MilestoneOf(OverviewMilestone milestone) =>
        new(milestone.Name, milestone.Day, milestone.BusinessDaysLeft, milestone.DaysLate);

    /// <summary>The image of a logo, or none when there is no logo or its file is gone from the storage.</summary>
    private async Task<StatusReportImage?> ImageOfAsync(string? fileId, string? contentType, CancellationToken cancellationToken)
    {
        if (fileId is null || contentType is null)
        {
            return null;
        }

        await using var file = await files.OpenAsync(fileId, null, cancellationToken);
        if (file is null)
        {
            return null;
        }

        using var content = new MemoryStream();
        await file.Content.CopyToAsync(content, cancellationToken);
        return new StatusReportImage(content.ToArray(), contentType);
    }
}
