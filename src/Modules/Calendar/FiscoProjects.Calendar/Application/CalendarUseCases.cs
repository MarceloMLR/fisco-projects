using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Calendar.Domain;

namespace FiscoProjects.Calendar.Application;

/// <summary>Where the calendar of the installation is kept.</summary>
public interface ICalendarStore
{
    /// <summary>The one row of working days, tracked, so a change is saved by <see cref="SaveChangesAsync"/>.</summary>
    Task<WorkCalendar> GetWorkCalendarAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CalculatedHoliday>> GetCalculatedHolidaysAsync(CancellationToken cancellationToken);

    Task<CalculatedHoliday?> FindCalculatedHolidayAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<LocalHoliday>> GetLocalHolidaysAsync(CancellationToken cancellationToken);

    Task<LocalHoliday?> FindLocalHolidayAsync(Guid id, CancellationToken cancellationToken);

    void Add(LocalHoliday holiday);

    void Remove(LocalHoliday holiday);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// One date of the calendar of a year: a calculated holiday (with its key) or a local holiday (with its id and name).
/// </summary>
public sealed record CalendarDay(
    DateOnly Date,
    CalendarDayKind Kind,
    string? Key,
    Guid? LocalHolidayId,
    string? Name,
    bool RepeatsEveryYear,
    bool IsConsidered);

/// <summary>The calendar of one year, as the screen shows it: the working days and the dates in order.</summary>
public sealed record CalendarYear(int Year, WorkingDays WorkingDays, IReadOnlyList<CalendarDay> Days);

/// <summary>The calendar of a year (ADR-0018, ADR-0083), the calculated holidays found again for the year.</summary>
public sealed class GetCalendarYear(ICalendarStore store, INationalCalendar nationalCalendar)
{
    /// <returns>Null for a year outside the calendar (<see cref="CalendarYears"/>).</returns>
    public async Task<CalendarYear?> ExecuteAsync(int year, CancellationToken cancellationToken)
    {
        if (!CalendarYears.IsSupported(year))
        {
            return null;
        }

        var calendar = await store.GetWorkCalendarAsync(cancellationToken);
        var choices = (await store.GetCalculatedHolidaysAsync(cancellationToken))
            .ToDictionary(holiday => holiday.Key, holiday => holiday.IsConsidered, StringComparer.Ordinal);
        var defaults = nationalCalendar.Holidays
            .ToDictionary(holiday => holiday.Key, holiday => holiday.ConsideredByDefault, StringComparer.Ordinal);

        var calculated = nationalCalendar.In(year).Select(holiday => new CalendarDay(
            holiday.Date,
            holiday.Kind,
            holiday.Key,
            null,
            null,
            RepeatsEveryYear: true,
            choices.TryGetValue(holiday.Key, out var considered) ? considered : defaults[holiday.Key]));
        var local = (await store.GetLocalHolidaysAsync(cancellationToken))
            .Select(holiday => (Holiday: holiday, Date: holiday.OccursIn(year)))
            .Where(occurrence => occurrence.Date is not null)
            .Select(occurrence => new CalendarDay(
                occurrence.Date!.Value,
                CalendarDayKind.LocalHoliday,
                null,
                occurrence.Holiday.Id,
                occurrence.Holiday.Name,
                occurrence.Holiday.RepeatsEveryYear,
                occurrence.Holiday.IsConsidered));

        return new CalendarYear(
            year,
            calendar.WorkingDays,
            [.. calculated.Concat(local)
                .OrderBy(day => day.Date)
                .ThenBy(day => day.Kind)
                .ThenBy(day => day.Name, StringComparer.CurrentCulture)]);
    }
}

/// <summary>Changes the working days of the week; never to a week without one (ADR-0018).</summary>
public sealed class ChangeWorkingDays(ICalendarStore store)
{
    /// <returns>False, with nothing changed, for a week without a working day.</returns>
    public async Task<bool> ExecuteAsync(IEnumerable<DayOfWeek> days, CancellationToken cancellationToken)
    {
        var workingDays = Weekdays.Of(days);
        if (!WorkCalendar.IsValid(workingDays))
        {
            return false;
        }

        var calendar = await store.GetWorkCalendarAsync(cancellationToken);
        calendar.ChangeWorkingDays(workingDays);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Considers or disregards a calculated holiday, in every year (ADR-0083).</summary>
public sealed class ChooseCalculatedHoliday(ICalendarStore store)
{
    /// <returns>False for a holiday the national calendar does not calculate.</returns>
    public async Task<bool> ExecuteAsync(string key, bool isConsidered, CancellationToken cancellationToken)
    {
        var holiday = await store.FindCalculatedHolidayAsync(key, cancellationToken);
        if (holiday is null)
        {
            return false;
        }

        holiday.Consider(isConsidered);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>A field of the local holiday form.</summary>
public enum LocalHolidayField
{
    Name = 1,
    Date = 2,
}

/// <summary>The local holiday form: the name, the date and whether it repeats every year.</summary>
public sealed record LocalHolidayChanges(string? Name, DateOnly? Date, bool RepeatsEveryYear)
{
    /// <summary>The invalid fields: an empty or long name, and a missing date or one outside the calendar.</summary>
    public IReadOnlyList<LocalHolidayField> Invalid()
    {
        var invalid = new List<LocalHolidayField>();
        if (LocalHoliday.ValidName(Name) is null)
        {
            invalid.Add(LocalHolidayField.Name);
        }

        if (Date is not { } date || !LocalHoliday.IsValidDate(date))
        {
            invalid.Add(LocalHolidayField.Date);
        }

        return invalid;
    }
}

/// <summary>The local holiday included, or the invalid fields with nothing included.</summary>
public sealed record AddedLocalHoliday(Guid? Id, IReadOnlyList<LocalHolidayField> Invalid);

/// <summary>Includes a local holiday, considered (ADR-0018, ADR-0083).</summary>
public sealed class AddLocalHoliday(ICalendarStore store)
{
    public async Task<AddedLocalHoliday> ExecuteAsync(LocalHolidayChanges changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var invalid = changes.Invalid();
        if (invalid.Count > 0)
        {
            return new AddedLocalHoliday(null, invalid);
        }

        var holiday = LocalHoliday.Create(changes.Name!, changes.Date!.Value, changes.RepeatsEveryYear);
        store.Add(holiday);
        await store.SaveChangesAsync(cancellationToken);
        return new AddedLocalHoliday(holiday.Id, []);
    }
}

/// <summary>Whether the local holiday was found and, when it was, the invalid fields; none when it was saved.</summary>
public sealed record ChangedLocalHoliday(bool Found, IReadOnlyList<LocalHolidayField> Invalid);

/// <summary>Changes a local holiday, and considers or disregards it, in one save.</summary>
public sealed class ChangeLocalHoliday(ICalendarStore store)
{
    public async Task<ChangedLocalHoliday> ExecuteAsync(
        Guid id, LocalHolidayChanges changes, bool isConsidered, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var holiday = await store.FindLocalHolidayAsync(id, cancellationToken);
        if (holiday is null)
        {
            return new ChangedLocalHoliday(false, []);
        }

        var invalid = changes.Invalid();
        if (invalid.Count > 0)
        {
            return new ChangedLocalHoliday(true, invalid);
        }

        holiday.Change(changes.Name!, changes.Date!.Value, changes.RepeatsEveryYear);
        holiday.Consider(isConsidered);
        await store.SaveChangesAsync(cancellationToken);
        return new ChangedLocalHoliday(true, []);
    }
}

/// <summary>Removes a local holiday from the calendar.</summary>
public sealed class RemoveLocalHoliday(ICalendarStore store)
{
    /// <returns>False for a holiday that does not exist.</returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var holiday = await store.FindLocalHolidayAsync(id, cancellationToken);
        if (holiday is null)
        {
            return false;
        }

        store.Remove(holiday);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>The calendar for the other modules (contract <see cref="IWorkCalendar"/>), read once per call.</summary>
public sealed class WorkCalendarReader(ICalendarStore store, INationalCalendar nationalCalendar) : IWorkCalendar
{
    public async Task<IBusinessDays> GetAsync(CancellationToken cancellationToken)
    {
        var calendar = await store.GetWorkCalendarAsync(cancellationToken);
        var calculated = await store.GetCalculatedHolidaysAsync(cancellationToken);
        var local = await store.GetLocalHolidaysAsync(cancellationToken);
        return new BusinessDays(
            calendar.WorkingDays,
            nationalCalendar,
            calculated.Where(holiday => holiday.IsConsidered).Select(holiday => holiday.Key),
            local);
    }
}
