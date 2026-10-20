using FiscoProjects.Tickets.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// Closes the tickets resolved without an answer (ADR-0024), in the background of the application (ADR-0047): when it
/// starts and every hour, by the clock of the host.
/// </summary>
internal sealed partial class TicketAutoClose(IServiceScopeFactory scopes, TimeProvider clock, ILogger<TicketAutoClose> logger)
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
            var closed = await scope.ServiceProvider.GetRequiredService<CloseResolvedTickets>().ExecuteAsync(cancellationToken);
            if (closed > 0)
            {
                LogClosed(logger, closed);
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The next round tries again; a database still starting is not a reason to stop the application, and a
            // round cut by the application stopping is not a failure.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Closed {Count} resolved tickets without an answer.")]
    private static partial void LogClosed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The automatic closing of the resolved tickets failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
