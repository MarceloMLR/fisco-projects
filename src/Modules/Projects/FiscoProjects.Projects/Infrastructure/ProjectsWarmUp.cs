using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Projects.Infrastructure.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// The reads of the screens of projects (ADR-0106), as the manager of the first open project sees them: the lists, the
/// project with its header, the overview, the schedule, the members, the next milestones and Meu trabalho.
/// </summary>
internal sealed class ProjectsWarmUp(
    ProjectsDbContext db,
    ListProjects listProjects,
    ListClientChoices clientChoices,
    GetProject getProject,
    GetProjectOverview overview,
    GetSchedule schedule,
    ListProjectMembers members,
    ListUpcomingMilestones milestones,
    InstallationClock clock,
    IEnumerable<IMyWorkSource> work) : IWarmUp
{
    public string Module => "Projects";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var sample = await (
                from member in db.ProjectMembers.AsNoTracking()
                join project in db.Projects.AsNoTracking() on member.ProjectId equals project.Id
                where member.Role == ProjectRole.Manager
                    && project.Status != ProjectStatus.Completed
                    && project.Status != ProjectStatus.Cancelled
                orderby project.Id
                select new { member.ProjectId, member.UserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (sample is null)
        {
            return;
        }

        var viewer = new ProjectViewer(sample.UserId, IsAdministrator: false);
        await listProjects.ExecuteAsync(new ProjectListQuery(null, null, ProjectSituation.Active, Mine: false, 1, 25), viewer, cancellationToken);
        await listProjects.ExecuteAsync(new ProjectListQuery(null, null, ProjectSituation.Active, Mine: true, 1, 30), viewer, cancellationToken);
        await clientChoices.ExecuteAsync(cancellationToken);
        await getProject.ExecuteAsync(sample.ProjectId, viewer, cancellationToken);
        await overview.ExecuteAsync(sample.ProjectId, viewer, cancellationToken);
        await schedule.ExecuteAsync(sample.ProjectId, viewer, cancellationToken);
        await members.ExecuteAsync(sample.ProjectId, viewer, cancellationToken);
        await milestones.ExecuteAsync(sample.UserId, cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        foreach (var source in work.OfType<ScheduleWorkSource>())
        {
            await source.ItemsAsync(new MyWorkQuery(sample.UserId, false, today, today.AddDays(7)), cancellationToken);
        }
    }
}
