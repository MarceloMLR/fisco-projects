using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using FiscoProjects.Persistence.PersonalData;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Services of the Notifications module: the board of the notices of the other modules (ADR-0038), the queue of the
/// e-mails, sent in the background by the implementation the configuration chooses (ADR-0036), the daily summary
/// (ADR-0037), the bell (ADR-0038) and the Activity of Teams, with the channel each person chose (ADR-0149).
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<NotificationsDbContext>(configuration);
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidation>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            return new EmailSettings(string.IsNullOrWhiteSpace(options.Mailbox) ? null : options.Mailbox.Trim(), options.KeepDays);
        });
        services.AddSingleton<IEmailRenderer>(provider =>
            new EmailRenderer(provider.GetRequiredService<IOptions<EmailOptions>>().Value.SiteUrl));
        // The sending by the shared mailbox; Graph is asked again by the queue when it refuses or throttles (ADR-0123).
        services.AddHttpClient(GraphEmailSender.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddSingleton<IEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            if (options.IsGraph)
            {
                var graph = new GraphEmailSender(
                    provider.GetRequiredService<IHttpClientFactory>(),
                    provider.GetRequiredService<IGraphTokens>(),
                    options.Mailbox!.Trim());
                return options.Graph.LocalDomains.Count == 0
                    ? graph
                    : new EmailRouting(
                        graph,
                        new LocalEmailSender(Path.Combine(contentRoot, options.Local.Folder), options.Mailbox),
                        options.Graph.LocalDomains);
            }

            return options.IsLocal
                ? new LocalEmailSender(Path.Combine(contentRoot, options.Local.Folder), options.Mailbox)
                : new NoEmailSender();
        });
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<EmailQueue>();
        services.AddScoped<DispatchEmails>();
        services.AddScoped<ForgetOldEmails>();
        services.AddScoped<GetEmailStatus>();
        services.AddScoped<SendTestEmail>();
        services.AddScoped<IDigestStore, DigestStore>();
        services.AddScoped<INoticeChannel, EmailChannel>();
        services.AddScoped<INotificationStore, NotificationStore>();
        services.AddScoped<INoticeChannel, BellChannel>();
        AddTeams(services, configuration);
        services.AddScoped<ListNotifications>();
        services.AddScoped<CountUnreadNotifications>();
        services.AddScoped<ReadNotification>();
        services.AddScoped<ReadAllNotifications>();
        services.AddScoped<ForgetOldNotifications>();
        services.AddScoped<DeliverNotices>();
        services.AddScoped<SendDailySummaries>();
        services.AddSingleton<INoticeBoard, NoticeBoard>();
        // The metrics of the queue, for the alert of a queue that stopped (ADR-0112).
        services.AddMetrics();
        services.AddSingleton<EmailQueueMetrics>();
        services.AddHostedService<EmailDispatcher>();
        services.AddHostedService<TeamsDispatcher>();
        services.AddHostedService<DailySummaryScheduler>();
        services.AddHostedService<NotificationCleanup>();
        // LGPD (ADR-0110): the e-mails, the bell, the daily summary and the notices of Teams of an anonymized person.
        services.AddScoped<IPersonalDataEraser, NotificationsPersonalData>();
        services.AddScoped<IProjectDeletion, NotificationProjectDeletion>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, NotificationsWarmUp>();
        return services;
    }

    /// <summary>
    /// The Activity of Teams (ADR-0149): the channel, the queue sent in the background by the implementation the
    /// configuration chooses, the channel each person chose and the application of Teams of the installation.
    /// </summary>
    private static void AddTeams(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TeamsOptions>()
            .Bind(configuration.GetSection(TeamsOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TeamsOptions>, TeamsOptionsValidation>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TeamsOptions>>().Value;
            var site = provider.GetRequiredService<IOptions<EmailOptions>>().Value.SiteUrl;
            var client = configuration["EntraSignIn:ClientId"];
            return new TeamsSettings(
                options.Kind,
                string.IsNullOrWhiteSpace(site) ? null : site.Trim(),
                string.IsNullOrWhiteSpace(client) ? null : client.Trim());
        });
        // Graph is asked again by the queue when it throttles or is out of service.
        services.AddHttpClient(GraphTeamsSender.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddSingleton<ITeamsSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TeamsOptions>>().Value;
            return options.Kind switch
            {
                TeamsProvider.Graph => new GraphTeamsSender(
                    provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IGraphTokens>()),
                TeamsProvider.Local => new LocalTeamsSender(
                    Path.Combine(provider.GetRequiredService<IHostEnvironment>().ContentRootPath, options.Local.Folder)),
                _ => new NoTeamsSender(),
            };
        });
        services.AddSingleton<ITeamsTexts, TeamsTexts>();
        services.AddSingleton<ITeamsManifest, TeamsAppManifest>();
        services.AddScoped<ITeamsNoticeStore, TeamsNoticeStore>();
        services.AddScoped<ITeamsAppStore, TeamsAppStore>();
        services.AddScoped<INoticePreferenceStore, NoticePreferenceStore>();
        services.AddScoped<NoticeRouting>();
        services.AddScoped<INoticeChannel, TeamsChannel>();
        services.AddScoped<DispatchTeamsNotices>();
        services.AddScoped<ForgetOldTeamsNotices>();
        services.AddScoped<GetImmediateNotices>();
        services.AddScoped<ChooseImmediateNotices>();
        services.AddScoped<GetTeamsApp>();
        services.AddScoped<ChangeTeamsNotices>();
        services.AddScoped<PackageTeamsApp>();
    }
}

/// <summary>No sending configured yet: every try fails, and the e-mails wait in the queue for it.</summary>
internal sealed class NoEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No e-mail sending is configured. Set Email:Provider (Graph, or Local in development).");
}
