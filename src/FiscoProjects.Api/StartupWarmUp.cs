using System.Diagnostics;
using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Api;

/// <summary>
/// Once the API has started, runs the main reads of every module (<see cref="IWarmUp"/>), each in a scope of its own and
/// one after the other, in the background (ADR-0106): the first person to open each screen no longer pays for the first
/// run of its queries and of its code. The API answers meanwhile; a failure is only logged. <c>WarmUp:Enabled</c>
/// (true by default) turns it off, as the tests do.
/// </summary>
internal sealed partial class StartupWarmUp(
    IServiceScopeFactory scopes,
    IHostApplicationLifetime lifetime,
    IConfiguration configuration,
    ILogger<StartupWarmUp> logger) : BackgroundService
{
    public const string EnabledKey = "WarmUp:Enabled";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledKey, true)
            || string.IsNullOrWhiteSpace(configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName)))
        {
            return;
        }

        if (!await StartedAsync(stoppingToken))
        {
            return;
        }

        await RunAllAsync(scopes, logger, stoppingToken);
    }

    /// <summary>Runs every warm-up, each in a scope of its own; a failure is logged and the next one runs.</summary>
    public static async Task RunAllAsync(IServiceScopeFactory scopes, ILogger logger, CancellationToken cancellationToken)
    {
        int count;
        await using (var scope = scopes.CreateAsyncScope())
        {
            count = scope.ServiceProvider.GetServices<IWarmUp>().Count();
        }

        var total = Stopwatch.StartNew();
        for (var index = 0; index < count && !cancellationToken.IsCancellationRequested; index++)
        {
            await using var scope = scopes.CreateAsyncScope();
            var warmUp = scope.ServiceProvider.GetServices<IWarmUp>().ElementAt(index);
            var watch = Stopwatch.StartNew();
            try
            {
                await warmUp.RunAsync(cancellationToken);
                LogWarmedUp(logger, warmUp.Module, watch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogFailed(logger, exception, warmUp.Module);
            }
        }

        LogDone(logger, count, total.ElapsedMilliseconds);
    }

    /// <summary>Waits until the host has started; false when it stops first.</summary>
    private async Task<bool> StartedAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var onStarted = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        await using var onStopping = stoppingToken.Register(() => started.TrySetCanceled(stoppingToken));
        try
        {
            await started.Task;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Warm-up of {Module}: {Milliseconds} ms.")]
    private static partial void LogWarmedUp(ILogger logger, string module, long milliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warm-up of {Module} failed; the screens still work, only the first read is slower.")]
    private static partial void LogFailed(ILogger logger, Exception exception, string module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Warm-up of {Count} modules done in {Milliseconds} ms.")]
    private static partial void LogDone(ILogger logger, int count, long milliseconds);
}
