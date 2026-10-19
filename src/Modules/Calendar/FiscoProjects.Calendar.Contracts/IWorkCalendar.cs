namespace FiscoProjects.Calendar.Contracts;

/// <summary>
/// The calendar of the installation, for the dates of schedules and the deadlines of tickets (ADR-0018, ADR-0025).
/// </summary>
public interface IWorkCalendar
{
    /// <summary>The business days as the calendar is now, to compute many dates without going back to the database.</summary>
    Task<IBusinessDays> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Which days are business days: a working day of the week that is no national holiday, optional holiday or local
/// holiday the installation considers. Days have no time and no time zone.
/// </summary>
public interface IBusinessDays
{
    bool IsBusinessDay(DateOnly date);

    /// <summary>
    /// The date that many business days after the given one (before it, when negative), without counting the date
    /// itself; the date itself when zero. Friday plus one business day is Monday, when Monday is one.
    /// </summary>
    DateOnly AddBusinessDays(DateOnly date, int days);

    /// <summary>The business days from the first date to the last, both included; zero when the last comes first.</summary>
    int CountBusinessDays(DateOnly first, DateOnly last);
}
