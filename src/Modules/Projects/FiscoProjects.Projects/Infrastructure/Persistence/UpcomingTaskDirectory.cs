using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>The tasks that end soon, read from the tables of the module for the daily summary (ADR-0037, ADR-0047).</summary>
internal sealed class UpcomingTaskDirectory(ProjectsDbContext db) : IUpcomingTasks
{
    public async Task<IReadOnlyList<UpcomingTask>> EndingAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in db.ScheduleItems.AsNoTracking()
            join project in db.Projects.AsNoTracking() on item.ProjectId equals project.Id
            where item.Kind == ScheduleItemKind.Task
                && item.OwnerId != null
                && item.Progress < 100
                && item.End >= first
                && item.End <= last
                && project.Status != ProjectStatus.Completed
                && project.Status != ProjectStatus.Cancelled
                && !db.ScheduleItems.Any(child => child.ParentId == item.Id)
            orderby item.End, project.Code, item.Name
            select new { OwnerId = item.OwnerId!.Value, project.Id, project.Code, project.Name, ItemId = item.Id, ItemName = item.Name, item.End })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => new UpcomingTask(row.OwnerId, row.Id, row.Code, row.Name, row.ItemId, row.ItemName, row.End))];
    }
}
