using FiscoProjects.Projects.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Removes the absences kept past a year after their end (ADR-0141), in the background of the application (ADR-0047):
/// when it starts and every six hours, by the clock of the host.
/// </summary>
internal sealed partial class AbsenceCleanup(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AbsenceCleanup> logger)
    : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromHours(6);

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
            var removed = await scope.ServiceProvider.GetRequiredService<CleanUpAbsences>().ExecuteAsync(cancellationToken);
            if (removed > 0)
            {
                LogRemoved(logger, removed);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} absences kept past a year after their end.")]
    private static partial void LogRemoved(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The cleanup of the absences failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
