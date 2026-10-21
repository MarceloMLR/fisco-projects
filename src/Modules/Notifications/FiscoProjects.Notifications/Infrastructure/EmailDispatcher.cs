using FiscoProjects.Notifications.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Sends the e-mails of the queue in the background of the application (ADR-0036, ADR-0047): when it starts and every
/// 30 seconds, by the clock of the host, with the metrics of the queue (ADR-0112); once an hour, it also forgets the
/// content of the old ones (ADR-0058).
/// </summary>
internal sealed partial class EmailDispatcher(
    IServiceScopeFactory scopes, EmailQueueMetrics metrics, TimeProvider clock, ILogger<EmailDispatcher> logger)
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
            var result = await scope.ServiceProvider.GetRequiredService<DispatchEmails>().ExecuteAsync(cancellationToken);
            metrics.Record(result);
            if (result.Sent > 0)
            {
                LogSent(logger, result.Sent);
            }

            foreach (var failure in result.Failures)
            {
                if (failure.Final)
                {
                    LogGaveUp(logger, failure.EmailId, failure.Attempts, failure.Error);
                }
                else
                {
                    LogFailed(logger, failure.EmailId, failure.Attempts, failure.Error);
                }
            }

            var now = clock.GetUtcNow().UtcDateTime;
            if (now - _lastForget >= ForgetEvery)
            {
                _lastForget = now;
                await scope.ServiceProvider.GetRequiredService<ForgetOldEmails>().ExecuteAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The next round tries again; a database still starting is not a reason to stop the application, and a
            // round cut by the application stopping is not a failure.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogRoundFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Count} e-mails of the queue.")]
    private static partial void LogSent(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The e-mail {EmailId} failed on try {Attempts} and will be tried again: {Error}")]
    private static partial void LogFailed(ILogger logger, Guid emailId, int attempts, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "The e-mail {EmailId} failed on try {Attempts}, the last one: {Error}")]
    private static partial void LogGaveUp(ILogger logger, Guid emailId, int attempts, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A round of the sending of the e-mails failed.")]
    private static partial void LogRoundFailed(ILogger logger, Exception exception);
}
