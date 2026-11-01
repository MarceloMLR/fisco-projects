using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>
/// The phases, tasks and milestones of a schedule, read from the tables of the module for the other modules (ADR-0047);
/// a milestone with its day and the day it was reached, and a task whether it is done.
/// </summary>
internal sealed class ScheduleItemDirectory(ProjectsDbContext db) : IScheduleItems
{
    public async Task<IReadOnlyList<ScheduleItemInfo>> ItemsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var phases = await db.SchedulePhases.AsNoTracking()
            .Where(phase => phase.ProjectId == projectId)
            .OrderBy(phase => phase.Position)
            .Select(phase => new { phase.Id, phase.Name, phase.Position })
            .ToListAsync(cancellationToken);
        var positions = phases.ToDictionary(phase => phase.Id, phase => phase.Position);
        var items = await db.ScheduleItems.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .Select(item => new { item.Id, item.PhaseId, item.Kind, item.Name, item.Start, item.ReachedOn, item.Progress })
            .ToListAsync(cancellationToken);
        return
        [
            .. phases.Select(phase => new ScheduleItemInfo(phase.Id, ScheduleItemType.Phase, phase.Name)),
            .. items
                .OrderBy(item => positions.GetValueOrDefault(item.PhaseId))
                .ThenBy(item => item.Start)
                .ThenBy(item => item.Name, StringComparer.CurrentCulture)
                .Select(item => item.Kind == ScheduleItemKind.Milestone
                    ? new ScheduleItemInfo(item.Id, ScheduleItemType.Milestone, item.Name, item.Start, item.ReachedOn)
                    : new ScheduleItemInfo(item.Id, ScheduleItemType.Task, item.Name, IsDone: item.Progress == 100)),
        ];
    }
}
