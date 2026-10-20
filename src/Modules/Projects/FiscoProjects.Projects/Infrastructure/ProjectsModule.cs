using FiscoProjects.Persistence;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Infrastructure.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Services of the Projects module: the clients (ADR-0015), the projects and the people in them (ADR-0016, ADR-0072),
/// the schedules (ADR-0018 to ADR-0020), and the names of all of them for the audit (ADR-0084).
/// </summary>
public static class ProjectsModule
{
    public static IServiceCollection AddProjectsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<ProjectsDbContext>(configuration);
        services.AddScoped<IClientStore, ClientStore>();
        services.AddScoped<IProjectStore, ProjectStore>();
        services.AddScoped<IScheduleStore, ScheduleStore>();
        services.AddScoped<IAuditReferenceNames, ProjectReferenceNames>();
        services.AddScoped<IAuditReferenceNames, ClientReferenceNames>();
        services.AddScoped<IAuditReferenceNames, SchedulePhaseReferenceNames>();
        services.AddScoped<IAuditReferenceNames, ScheduleItemReferenceNames>();
        services.AddScoped<InstallationClock>();
        services.AddScoped<ListClientChoices>();
        services.AddScoped<ListProjects>();
        services.AddScoped<GetProject>();
        services.AddScoped<CreateProject>();
        services.AddScoped<ChangeProject>();
        services.AddScoped<ChangeProjectStatus>();
        services.AddScoped<ListProjectMembers>();
        services.AddScoped<AddProjectMember>();
        services.AddScoped<ChangeProjectMemberRole>();
        services.AddScoped<RemoveProjectMember>();
        services.AddScoped<GetSchedule>();
        services.AddScoped<AddSchedulePhase>();
        services.AddScoped<RenameSchedulePhase>();
        services.AddScoped<RemoveSchedulePhase>();
        services.AddScoped<AddScheduleItem>();
        services.AddScoped<ChangeScheduleItem>();
        services.AddScoped<RemoveScheduleItem>();
        services.AddScoped<UpdateScheduleItemProgress>();
        services.AddScoped<SetMilestoneReached>();
        services.AddScoped<SaveScheduleBaseline>();
        services.AddScoped<UndoScheduleChange>();
        services.AddScoped<RedoScheduleChange>();
        services.AddScoped<ExportScheduleToMsProject>();
        services.AddScoped<ITemplateStore, TemplateStore>();
        services.AddScoped<SaveScheduleAsTemplate>();
        services.AddScoped<ListTemplates>();
        services.AddScoped<ChangeTemplate>();
        services.AddScoped<RemoveTemplate>();
        services.AddScoped<RescheduleScheduleItem>();
        services.AddScoped<ListClients>();
        services.AddScoped<AddClient>();
        services.AddScoped<ChangeClient>();
        services.AddScoped<DeactivateClient>();
        services.AddScoped<ReactivateClient>();
        services.AddScoped<RemoveClient>();
        services.AddScoped<GetClientLogo>();
        services.AddScoped<ReplaceClientLogo>();
        services.AddScoped<RemoveClientLogo>();
        return services;
    }
}
