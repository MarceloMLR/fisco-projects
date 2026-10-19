namespace FiscoProjects.Calendar.Domain;

/// <summary>The years the calendar answers for, in the dates of local holidays and in the year asked for.</summary>
public static class CalendarYears
{
    public const int First = 1900;
    public const int Last = 2199;

    public static bool IsSupported(int year) => year is >= First and <= Last;
}
