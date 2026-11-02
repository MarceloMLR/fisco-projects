using FiscoProjects.Notifications.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Sends the notices of the queue of Teams in the background of the application (ADR-0047, ADR-0149): when it starts and
/// every 30 seconds, by the clock of the host, as the e-mails; once an hour, it also removes the old ones (ADR-0058).
/// </summary>
internal sealed partial class TeamsDispatcher(IServiceScopeFactory scopes, TimeProvider clock, ILogger<TeamsDispatcher> logger)
    : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ForgetEvery = TimeSpan.FromHours(1);

    private DateTime _lastForget = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every, clock);
        try
        {
            do
            {
                await RunAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is stopping, or its host is disposed without stopping, as in the tests.
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<DispatchTeamsNotices>().ExecuteAsync(cancellationToken);
            if (result.Sent > 0)
            {
                LogSent(logger, result.Sent);
            }

            foreach (var failure in result.Failures)
            {
                if (failure.Final)
                {
                    LogGaveUp(logger, failure.NoticeId, failure.Attempts, failure.Error);
                }
                else
                {
                    LogFailed(logger, failure.NoticeId, failure.Attempts, failure.Error);
                }
            }

            var now = clock.GetUtcNow().UtcDateTime;
            if (now - _lastForget >= ForgetEvery)
            {
                _lastForget = now;
                await scope.ServiceProvider.GetRequiredService<ForgetOldTeamsNotices>().ExecuteAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The next round tries again; a database still starting is not a reason to stop the application.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogRoundFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Count} notices to Teams.")]
    private static partial void LogSent(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The notice of Teams {NoticeId} failed on try {Attempts} and will be tried again: {Error}")]
    private static partial void LogFailed(ILogger logger, Guid noticeId, int attempts, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The notice of Teams {NoticeId} did not go, on try {Attempts}: {Error}")]
    private static partial void LogGaveUp(ILogger logger, Guid noticeId, int attempts, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A round of the sending to Teams failed.")]
    private static partial void LogRoundFailed(ILogger logger, Exception exception);
}
