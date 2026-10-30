using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.Documents.Infrastructure.Signatures;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Persistence;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>
/// Services of the Documents module: the folders, the documents, the links and the acceptance terms of the projects,
/// signed by the provider of signatures the configuration chooses (ADR-0031, ADR-0032, ADR-0092, ADR-0129).
/// </summary>
public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<DocumentsDbContext>(configuration);
        services.AddScoped<DocumentStores>();
        services.AddScoped<IFolderStore>(provider => provider.GetRequiredService<DocumentStores>());
        services.AddScoped<IDocumentStore>(provider => provider.GetRequiredService<DocumentStores>());
        services.AddScoped<IAuditReferenceNames, FolderReferenceNames>();
        services.AddScoped<IAuditReferenceNames, LinkCategoryReferenceNames>();
        services.AddScoped<IAuditReferenceNames, AcceptanceTermReferenceNames>();
        services.AddScoped<ILinkStore, LinkStore>();
        // Up to 25 MB per document by default, as each installation sets (ADR-0031).
        var megabytes = Math.Max(1, configuration.GetValue("Documents:MaxMegabytes", DocumentSettings.DefaultMaxMegabytes));
        services.AddSingleton(new DocumentSettings(megabytes * 1024L * 1024L));
        services.AddScoped<ListDefaultFolders>();
        services.AddScoped<AddDefaultFolder>();
        services.AddScoped<ChangeDefaultFolder>();
        services.AddScoped<ProjectFolders>();
        // The folders of a new project (ADR-0092).
        services.AddScoped<IProjectSetup, DocumentProjectSetup>();
        services.AddScoped<IProjectDeletion, DocumentProjectDeletion>();
        services.AddScoped<IProjectFolderUse, DocumentProjectFolderUse>();
        services.AddScoped<DocumentAreas>();
        services.AddScoped<DocumentViews>();
        services.AddScoped<ListDocumentFolders>();
        services.AddScoped<ListDocuments>();
        services.AddScoped<GetDocument>();
        services.AddScoped<UploadDocument>();
        services.AddScoped<OpenDocument>();
        services.AddScoped<ChangeDocumentStatus>();
        services.AddScoped<LinkDocument>();
        services.AddScoped<RemoveDocument>();
        // The links of the projects and their categories (ADR-0032).
        services.AddScoped<ListLinkCategories>();
        services.AddScoped<AddLinkCategory>();
        services.AddScoped<ChangeLinkCategory>();
        services.AddScoped<ListLinks>();
        services.AddScoped<AddLink>();
        services.AddScoped<ChangeLink>();
        services.AddScoped<RemoveLink>();
        // The acceptance terms (ADR-0129), with the summary of the closing and the overview.
        services.AddScoped<IAcceptanceStore, AcceptanceStore>();
        services.AddScoped<IProjectAcceptances, ProjectAcceptances>();
        services.AddSignatureProvider(configuration);
        // The requests of the person in Meu trabalho (ADR-0096).
        services.AddScoped<IMyWorkSource, AcceptanceWorkSource>();
        services.AddScoped<AcceptanceAreas>();
        services.AddScoped<AcceptanceViews>();
        services.AddScoped<AcceptanceFiles>();
        services.AddScoped<AcceptanceNotices>();
        services.AddScoped<ListAcceptances>();
        services.AddScoped<GetAcceptance>();
        services.AddScoped<OpenAcceptanceFile>();
        services.AddScoped<ListAcceptanceMilestones>();
        services.AddScoped<RequestAcceptance>();
        services.AddScoped<AttachAcceptance>();
        services.AddScoped<AttachSignedTerm>();
        services.AddScoped<RemindAcceptance>();
        services.AddScoped<CancelAcceptance>();
        services.AddScoped<ApplySignatureStatus>();
        services.AddScoped<ListOpenSignatureRequests>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, DocumentsWarmUp>();
        return services;
    }

    // The provider of signatures of Signature:Provider, none when it is empty (ADR-0129); the application does not
    // start with one it does not know, nor with the local one in production. The requests at it are checked in the
    // background.
    private static void AddSignatureProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SignatureOptions>()
            .Bind(configuration.GetSection(SignatureOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SignatureOptions>, SignatureOptionsValidation>();
        services.AddScoped<SignatureProviders>();
        var options = configuration.GetSection(SignatureOptions.Section).Get<SignatureOptions>() ?? new SignatureOptions();
        if (!options.IsLocal)
        {
            return;
        }

        services.AddSingleton(provider => new LocalSignatureProvider(
            Path.Combine(provider.GetRequiredService<IHostEnvironment>().ContentRootPath, options.Local.Folder),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<LocalSignatureProvider>>()));
        services.AddSingleton<ISignatureProvider>(provider => provider.GetRequiredService<LocalSignatureProvider>());
        services.AddSingleton<ILocalSigningPage>(provider => provider.GetRequiredService<LocalSignatureProvider>());
        services.AddScoped<AnswerLocalSigning>();
        services.AddHostedService(provider => new SignatureCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<TimeProvider>(),
            TimeSpan.FromMinutes(Math.Max(1, options.CheckMinutes)),
            provider.GetRequiredService<ILogger<SignatureCheck>>()));
    }
}
