using FiscoProjects.Tickets.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// Tells the deadlines of the tickets due soon or overdue (ADR-0025, ADR-0037), in the background of the application
/// (ADR-0047): when it starts and every hour, by the clock of the host.
/// </summary>
internal sealed partial class TicketDeadlineAlerts(IServiceScopeFactory scopes, TimeProvider clock, ILogger<TicketDeadlineAlerts> logger)
    : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromHours(1);

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
            var told = await scope.ServiceProvider.GetRequiredService<AlertTicketDeadlines>().ExecuteAsync(cancellationToken);
            if (told > 0)
            {
                LogTold(logger, told);
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The next round tries again; a database still starting is not a reason to stop the application.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Told {Count} deadlines of tickets due soon or overdue.")]
    private static partial void LogTold(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The alerts of the deadlines of the tickets failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
