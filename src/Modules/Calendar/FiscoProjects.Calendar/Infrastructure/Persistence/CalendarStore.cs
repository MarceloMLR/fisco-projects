using FiscoProjects.Calendar.Application;
using FiscoProjects.Calendar.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Calendar.Infrastructure.Persistence;

internal sealed class CalendarStore(CalendarDbContext db) : ICalendarStore
{
    public Task<WorkCalendar> GetWorkCalendarAsync(CancellationToken cancellationToken) =>
        db.WorkCalendars.SingleAsync(calendar => calendar.Id == WorkCalendar.SingleId, cancellationToken);

    public async Task<IReadOnlyList<CalculatedHoliday>> GetCalculatedHolidaysAsync(CancellationToken cancellationToken) =>
        await db.CalculatedHolidays.ToListAsync(cancellationToken);

    public Task<CalculatedHoliday?> FindCalculatedHolidayAsync(string key, CancellationToken cancellationToken) =>
        db.CalculatedHolidays.SingleOrDefaultAsync(holiday => holiday.Key == key, cancellationToken);

    public async Task<IReadOnlyList<LocalHoliday>> GetLocalHolidaysAsync(CancellationToken cancellationToken) =>
        await db.LocalHolidays.ToListAsync(cancellationToken);

    public Task<LocalHoliday?> FindLocalHolidayAsync(Guid id, CancellationToken cancellationToken) =>
        db.LocalHolidays.SingleOrDefaultAsync(holiday => holiday.Id == id, cancellationToken);

    public void Add(LocalHoliday holiday) => db.LocalHolidays.Add(holiday);

    public void Remove(LocalHoliday holiday) => db.LocalHolidays.Remove(holiday);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
