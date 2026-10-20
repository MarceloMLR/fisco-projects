using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// The board of the notices (ADR-0038): delivers them in a scope of its own, so a failure never touches the work of the
/// module that posted them, which is already saved; the failure is logged, and the action stays (ADR-0036).
/// </summary>
internal sealed partial class NoticeBoard(IServiceScopeFactory scopes, ILogger<NoticeBoard> logger) : INoticeBoard
{
    public async Task PostAsync(IReadOnlyCollection<Notice> notices, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notices);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DeliverNotices>().ExecuteAsync(notices, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogFailed(logger, string.Join(", ", notices.Select(notice => notice.Kind).Distinct()), exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The notices {Kinds} could not be delivered; the action that caused them stays.")]
    private static partial void LogFailed(ILogger logger, string kinds, Exception exception);
}

/// <summary>
/// Sends the daily summary in the background (ADR-0037, ADR-0047): every minute it checks whether the time of the
/// installation came and the day has no summary yet.
/// </summary>
internal sealed partial class DailySummaryScheduler(IServiceScopeFactory scopes, TimeProvider clock, ILogger<DailySummaryScheduler> logger)
    : Microsoft.Extensions.Hosting.BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

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
            if (await scope.ServiceProvider.GetRequiredService<SendDailySummaries>().ExecuteAsync(cancellationToken) is { } sent)
            {
                LogSent(logger, sent);
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The daily summary of the day put {Count} e-mails in the queue.")]
    private static partial void LogSent(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A round of the daily summary failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
