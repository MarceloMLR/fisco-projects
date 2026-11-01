using FiscoProjects.Persistence;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.TimeTracking.Application;
using FiscoProjects.TimeTracking.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.TimeTracking.Infrastructure;

/// <summary>
/// Services of the TimeTracking module: the hours each person works on the projects and the contracted and additional
/// hours of each project, with the balance (ADR-0128), and the planned hours of each task (ADR-0138).
/// </summary>
public static class TimeTrackingModule
{
    public static IServiceCollection AddTimeTrackingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<TimeTrackingDbContext>(configuration);
        services.AddScoped<ITimeEntryStore, TimeEntryStore>();
        services.AddScoped<IProjectDeletion, TimeEntryProjectDeletion>();
        services.AddScoped<TimeEntryClock>();
        services.AddScoped<TimeEntryProjects>();
        services.AddScoped<TimeEntryTargets>();
        services.AddScoped<ListTimeEntries>();
        services.AddScoped<ListTimeEntryProjects>();
        services.AddScoped<ListTimeEntryWork>();
        services.AddScoped<AddTimeEntry>();
        services.AddScoped<ChangeTimeEntry>();
        services.AddScoped<DeleteTimeEntry>();
        services.AddScoped<IProjectHoursStore, ProjectHoursStore>();
        services.AddScoped<ListProjectHours>();
        services.AddScoped<GetProjectHours>();
        services.AddScoped<ChangeContractedHours>();
        services.AddScoped<AddAdditionalHours>();
        services.AddScoped<ChangeAdditionalHours>();
        services.AddScoped<DeleteAdditionalHours>();
        services.AddScoped<GetTaskHours>();
        services.AddScoped<ChangePlannedHours>();
        services.AddScoped<GetTeamLoad>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, TimeTrackingWarmUp>();
        return services;
    }
}
