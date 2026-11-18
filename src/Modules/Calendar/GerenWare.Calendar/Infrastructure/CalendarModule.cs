using GerenWare.Calendar.Application;
using GerenWare.Calendar.Contracts;
using GerenWare.Calendar.Domain;
using GerenWare.Calendar.Infrastructure.Persistence;
using GerenWare.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GerenWare.Calendar.Infrastructure;

/// <summary>Services of the Calendar module: the calendar of the installation (ADR-0018, ADR-0083).</summary>
public static class CalendarModule
{
    public static IServiceCollection AddCalendarModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CalendarDbContext>(configuration);
        services.AddSingleton<INationalCalendar, BrazilianNationalCalendar>();
        services.AddScoped<ICalendarStore, CalendarStore>();
        services.AddScoped<IWorkCalendar, WorkCalendarReader>();
        services.AddScoped<GetCalendarYear>();
        services.AddScoped<ChangeWorkingDays>();
        services.AddScoped<ChooseCalculatedHoliday>();
        services.AddScoped<ConsiderHolidays>();
        services.AddScoped<AddLocalHoliday>();
        services.AddScoped<ChangeLocalHoliday>();
        services.AddScoped<RemoveLocalHoliday>();
        return services;
    }
}
