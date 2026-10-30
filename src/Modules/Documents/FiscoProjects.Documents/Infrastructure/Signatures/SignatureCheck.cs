using FiscoProjects.Documents.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>
/// Checks the requests at the provider of signatures waiting for the signatures (ADR-0129), in the background of the
/// application (ADR-0047): when it starts and every <c>Signature:CheckMinutes</c>, by the clock of the host. Each request
/// is applied in a scope of its own, so a change saved at the same time by another way leaves the others going.
/// </summary>
internal sealed partial class SignatureCheck(
    IServiceScopeFactory scopes, TimeProvider clock, TimeSpan every, ILogger<SignatureCheck> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(every, clock);
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

    /// <summary>One round: how many requests changed.</summary>
    internal async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Guid> requests;
            await using (var scope = scopes.CreateAsyncScope())
            {
                requests = await scope.ServiceProvider.GetRequiredService<ListOpenSignatureRequests>().ExecuteAsync(cancellationToken);
            }

            var changed = 0;
            foreach (var request in requests)
            {
                await using var scope = scopes.CreateAsyncScope();
                if (await scope.ServiceProvider.GetRequiredService<ApplySignatureStatus>().ExecuteAsync(request, cancellationToken))
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                LogChanged(logger, changed);
            }

            return changed;
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The next round tries again; a database still starting is not a reason to stop the application, and a
            // round cut by the application stopping is not a failure.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogFailed(logger, exception);
            }

            return 0;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} requests of signature changed at the provider.")]
    private static partial void LogChanged(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The check of the requests of signature failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
