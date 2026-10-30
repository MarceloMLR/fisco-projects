using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence;

/// <summary>The time entries, in the table of the module.</summary>
internal sealed class TimeEntryStore(TimeTrackingDbContext db) : ITimeEntryStore
{
    public async Task<IReadOnlyList<TimeEntry>> OfPersonAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Day >= from && entry.Day <= to)
            .ToListAsync(cancellationToken);

    public async Task<decimal> HoursOfDayAsync(Guid userId, DateOnly day, Guid? except, CancellationToken cancellationToken) =>
        await db.TimeEntries.AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Day == day && entry.Id != except)
            .SumAsync(entry => (decimal?)entry.Hours, cancellationToken) ?? 0m;

    public Task<TimeEntry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.TimeEntries.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    public void Add(TimeEntry entry) => db.TimeEntries.Add(entry);

    public void Remove(TimeEntry entry) => db.TimeEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
