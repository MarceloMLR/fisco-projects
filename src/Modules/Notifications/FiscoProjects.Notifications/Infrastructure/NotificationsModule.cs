using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Services of the Notifications module: the queue of the e-mails, sent in the background by the implementation the
/// configuration chooses (ADR-0036).
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
        services.TryAddSingleton<IEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            if (!options.IsLocal)
            {
                return new NoEmailSender();
            }

            var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            return new LocalEmailSender(Path.Combine(contentRoot, options.Local.Folder), options.Mailbox);
        });
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<EmailQueue>();
        services.AddScoped<DispatchEmails>();
        services.AddScoped<ForgetOldEmails>();
        services.AddScoped<GetEmailStatus>();
        services.AddScoped<SendTestEmail>();
        services.AddHostedService<EmailDispatcher>();
        return services;
    }
}

/// <summary>No sending configured yet: every try fails, and the e-mails wait in the queue for it.</summary>
internal sealed class NoEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No e-mail sending is configured. Set Email:Provider (Local in development).");
}
