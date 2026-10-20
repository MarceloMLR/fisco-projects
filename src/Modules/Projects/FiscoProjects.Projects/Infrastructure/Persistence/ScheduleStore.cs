using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ScheduleStore(ProjectsDbContext db) : IScheduleStore
{
    public Task<Schedule?> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.Schedules
            .Include(schedule => schedule.Phases)
            .Include(schedule => schedule.Items)
            .Include(schedule => schedule.Dependencies)
            .Include(schedule => schedule.Baseline!.Items)
            .AsSplitQuery()
            .SingleOrDefaultAsync(schedule => schedule.Id == projectId, cancellationToken);

    public async Task<Schedule> GetOrAddAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await FindAsync(projectId, cancellationToken) is { } schedule)
        {
            return schedule;
        }

        var created = new Schedule(projectId);
        db.Schedules.Add(created);
        return created;
    }

    public async Task<IReadOnlyDictionary<Guid, ScheduleParts>> OfProjectsAsync(
        IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
        {
            return new Dictionary<Guid, ScheduleParts>();
        }

        var phases = await db.SchedulePhases.AsNoTracking()
            .Where(phase => projectIds.Contains(phase.ProjectId))
            .ToListAsync(cancellationToken);
        var items = await db.ScheduleItems.AsNoTracking()
            .Where(item => projectIds.Contains(item.ProjectId))
            .ToListAsync(cancellationToken);
        var phasesByProject = phases.ToLookup(phase => phase.ProjectId);
        var itemsByProject = items.ToLookup(item => item.ProjectId);
        return projectIds.Distinct().ToDictionary(
            id => id,
            id => new ScheduleParts([.. phasesByProject[id]], [.. itemsByProject[id]]));
    }

    public async Task<SchedulePeriod?> PeriodAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var period = await db.ScheduleItems.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .GroupBy(item => item.ProjectId)
            .Select(items => new { Start = items.Min(item => item.Start), End = items.Max(item => item.End) })
            .SingleOrDefaultAsync(cancellationToken);
        return period is null ? null : new SchedulePeriod(period.Start, period.End);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> PhaseNamesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.SchedulePhases.AsNoTracking()
            .Where(phase => ids.Contains(phase.Id))
            .ToDictionaryAsync(phase => phase.Id, phase => phase.Name, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> ItemNamesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.ScheduleItems.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
}
