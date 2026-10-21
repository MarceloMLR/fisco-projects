using FiscoProjects.Audit.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Audit.Infrastructure;

/// <summary>
/// Runs the monthly cleanup of the audit in the background (ADR-0047, ADR-0110): every hour it checks whether the month
/// of the installation had its cleanup.
/// </summary>
internal sealed partial class AuditCleanup(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AuditCleanup> logger)
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
            if (await scope.ServiceProvider.GetRequiredService<CleanUpAudit>().ExecuteAsync(cancellationToken) is { } cleanup)
            {
                LogCleaned(logger, cleanup.Removed, cleanup.KeptFrom);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "The monthly cleanup removed {Count} audit records from before {KeptFrom}.")]
    private static partial void LogCleaned(ILogger logger, int count, DateOnly keptFrom);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The monthly cleanup of the audit failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
