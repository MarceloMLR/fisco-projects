using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Persistence;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Infrastructure.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Services of the Projects module: the clients (ADR-0015), the projects and the people in them (ADR-0016, ADR-0072),
/// the schedules (ADR-0018 to ADR-0020), the names of all of them for the audit (ADR-0084) and who is in each project,
/// for the other modules (<see cref="IProjectAccess"/>).
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
        services.AddScoped<IProjectAccess, ProjectAccessDirectory>();
        services.AddScoped<IProjectCatalog, ProjectCatalog>();
        services.AddScoped<IAuditReferenceNames, ProjectReferenceNames>();
        services.AddScoped<IAuditReferenceNames, ClientReferenceNames>();
        services.AddScoped<IAuditReferenceNames, SchedulePhaseReferenceNames>();
        services.AddScoped<IAuditReferenceNames, ScheduleItemReferenceNames>();
        services.AddScoped<InstallationClock>();
        services.AddScoped<ListClientChoices>();
        services.AddScoped<ListProjects>();
        services.AddScoped<GetProject>();
        services.AddScoped<CreateProject>();
        services.AddScoped<PreviewMsProjectImport>();
        services.AddScoped<ChangeProject>();
        services.AddScoped<ChangeProjectStatus>();
        services.AddScoped<GetProjectClosing>();
        services.AddScoped<IProjectDeletionStore, ProjectDeletionStore>();
        services.AddScoped<GetProjectDeletion>();
        services.AddScoped<DeleteProject>();
        services.AddScoped<GetProjectOverview>();
        services.AddScoped<ListProjectMembers>();
        services.AddScoped<AddProjectMember>();
        services.AddScoped<ChangeProjectMemberRole>();
        services.AddScoped<RemoveProjectMember>();
        services.AddLogging();
        services.AddScoped<IMemberChangeNotices, MemberChangeNotices>();
        services.AddScoped<ProjectNotices>();
        services.AddScoped<IUpcomingTasks, UpcomingTaskDirectory>();
        // The tasks of the person in Meu trabalho and the next milestones of the projects of the person (ADR-0096).
        services.AddScoped<IMyWorkSource, ScheduleWorkSource>();
        services.AddScoped<ListUpcomingMilestones>();
        services.AddScoped<IScheduleItems, ScheduleItemDirectory>();
        // The milestones reached by the signed acceptance terms (ADR-0035).
        services.AddScoped<IScheduleMilestones, ScheduleMilestones>();
        services.AddScoped<IProjectSetupNotices, ProjectSetupNotices>();
        services.AddScoped<IProjectFolderStore, ProjectFolderStore>();
        services.AddScoped<IProjectStorage, ProjectStorage>();
        services.AddScoped<IProjectFolderRenaming, ProjectFolderRenaming>();
        services.AddScoped<ProjectFolderChoices>();
        services.AddScoped<GetProjectFolder>();
        services.AddScoped<ChangeProjectFolder>();
        services.AddScoped<IProjectFolderCleanup, ProjectFolderCleanup>();
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
        services.AddScoped<PreviewScheduleRecalculation>();
        services.AddScoped<RecalculateSchedules>();
        services.AddScoped<ListClients>();
        services.AddScoped<AddClient>();
        services.AddScoped<ChangeClient>();
        services.AddScoped<DeactivateClient>();
        services.AddScoped<ReactivateClient>();
        services.AddScoped<RemoveClient>();
        services.AddScoped<GetClientLogo>();
        services.AddScoped<ReplaceClientLogo>();
        services.AddScoped<RemoveClientLogo>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, ProjectsWarmUp>();
        return services;
    }
}
