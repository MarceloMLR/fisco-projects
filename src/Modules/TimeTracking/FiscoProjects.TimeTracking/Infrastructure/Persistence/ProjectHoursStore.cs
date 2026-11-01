using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence;

/// <summary>The contracted, additional and planned hours, and the sums of the logged hours, in the tables of the module.</summary>
internal sealed class ProjectHoursStore(TimeTrackingDbContext db) : IProjectHoursStore
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> LoggedByProjectAsync(CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .GroupBy(entry => entry.ProjectId)
            .Select(group => new { group.Key, Hours = group.Sum(entry => entry.Hours) })
            .ToDictionaryAsync(row => row.Key, row => row.Hours, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> ContractedByProjectAsync(CancellationToken cancellationToken) =>
        await db.ContractedHours.AsNoTracking()
            .ToDictionaryAsync(contracted => contracted.ProjectId, contracted => contracted.Hours, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> AdditionalByProjectAsync(CancellationToken cancellationToken) =>
        await db.AdditionalHours.AsNoTracking()
            .GroupBy(additional => additional.ProjectId)
            .Select(group => new { group.Key, Hours = group.Sum(additional => additional.Hours) })
            .ToDictionaryAsync(row => row.Key, row => row.Hours, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> LoggedByPersonAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .Where(entry => entry.ProjectId == projectId)
            .GroupBy(entry => entry.UserId)
            .Select(group => new { group.Key, Hours = group.Sum(entry => entry.Hours) })
            .ToDictionaryAsync(row => row.Key, row => row.Hours, cancellationToken);

    public async Task<DateOnly?> FirstDayAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .Where(entry => entry.ProjectId == projectId)
            .MinAsync(entry => (DateOnly?)entry.Day, cancellationToken);

    public Task<ContractedHours?> ContractedOfAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.ContractedHours.SingleOrDefaultAsync(contracted => contracted.ProjectId == projectId, cancellationToken);

    public async Task<IReadOnlyList<AdditionalHours>> AdditionalOfAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.AdditionalHours.AsNoTracking()
            .Where(additional => additional.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public Task<AdditionalHours?> FindAdditionalAsync(Guid id, CancellationToken cancellationToken) =>
        db.AdditionalHours.SingleOrDefaultAsync(additional => additional.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> PlannedOfAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.PlannedHours.AsNoTracking()
            .Where(planned => planned.ProjectId == projectId)
            .ToDictionaryAsync(planned => planned.ScheduleItemId, planned => planned.Hours, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> PlannedOfItemsAsync(
        IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        itemIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await db.PlannedHours.AsNoTracking()
                .Where(planned => itemIds.Contains(planned.ScheduleItemId))
                .ToDictionaryAsync(planned => planned.ScheduleItemId, planned => planned.Hours, cancellationToken);

    public Task<PlannedHours?> FindPlannedAsync(Guid projectId, Guid scheduleItemId, CancellationToken cancellationToken) =>
        db.PlannedHours.SingleOrDefaultAsync(
            planned => planned.ProjectId == projectId && planned.ScheduleItemId == scheduleItemId, cancellationToken);

    public async Task<IReadOnlyList<LoggedInTask>> LoggedByTaskAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .Where(entry => entry.ProjectId == projectId)
            .GroupBy(entry => entry.ScheduleItemId)
            .Select(group => new LoggedInTask(group.Key, group.Sum(entry => entry.Hours)))
            .ToListAsync(cancellationToken);

    public void Add(ContractedHours contracted) => db.ContractedHours.Add(contracted);

    public void Remove(ContractedHours contracted) => db.ContractedHours.Remove(contracted);

    public void Add(AdditionalHours additional) => db.AdditionalHours.Add(additional);

    public void Remove(AdditionalHours additional) => db.AdditionalHours.Remove(additional);

    public void Add(PlannedHours planned) => db.PlannedHours.Add(planned);

    public void Remove(PlannedHours planned) => db.PlannedHours.Remove(planned);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
