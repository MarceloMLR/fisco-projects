using FiscoProjects.SharedKernel;
using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.TimeTracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.TimeTracking.Infrastructure;

/// <summary>
/// The reads of Minhas horas (ADR-0106), as the person of the last entry sees the week of it and the projects to log in,
/// and the hours of the open projects, as the Administrator sees them.
/// </summary>
internal sealed class TimeTrackingWarmUp(
    TimeTrackingDbContext db, ListTimeEntries list, ListTimeEntryProjects projects, ListProjectHours projectHours) : IWarmUp
{
    public string Module => "TimeTracking";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await projectHours.ExecuteAsync(ProjectHoursView.Open, null, cancellationToken);
        var last = await db.TimeEntries.AsNoTracking()
            .OrderByDescending(entry => entry.Day)
            .Select(entry => new { entry.UserId, entry.Day })
            .FirstOrDefaultAsync(cancellationToken);
        if (last is null)
        {
            return;
        }

        var viewer = new TimeEntryViewer(last.UserId, IsAdministrator: false);
        await list.ExecuteAsync(last.Day.AddDays(-6), last.Day, null, null, viewer, cancellationToken);
        await projects.ExecuteAsync(viewer, cancellationToken);
    }
}
