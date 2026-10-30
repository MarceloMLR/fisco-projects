using FiscoProjects.Documents.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>
/// Checks the requests at the provider of signatures waiting for the signatures (ADR-0129, ADR-0130), in the background
/// of the application (ADR-0047): when it starts and every <c>Signature:CheckMinutes</c>, by the clock of the host, and
/// each document whose notice the provider sent, as soon as it arrives in the queue. Each request is applied in a scope
/// of its own and one at a time, so the two ways do not keep the same signed PDF twice; a change saved at the same time
/// by another way leaves the others going.
/// </summary>
internal sealed partial class SignatureCheck(
    IServiceScopeFactory scopes, TimeProvider clock, TimeSpan every, SignatureNoticeQueue notices, ILogger<SignatureCheck> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _one = new(1, 1);

    public override void Dispose()
    {
        _one.Dispose();
        base.Dispose();
    }

    // An array, not the span of params: the inline array the compiler makes for it has no namespace for the tests of
    // the architecture.
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(new Task[] { PeriodicAsync(stoppingToken), NoticesAsync(stoppingToken) });

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
                if (await OneAsync(apply => apply.ExecuteAsync(request, cancellationToken), cancellationToken))
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

    /// <summary>The document of a notice of the provider: whether its request changed.</summary>
    internal async Task<bool> ApplyNoticeAsync(string provider, string documentId, CancellationToken cancellationToken)
    {
        try
        {
            return await OneAsync(apply => apply.ForDocumentAsync(provider, documentId, cancellationToken), cancellationToken);
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested || exception is not OperationCanceledException)
        {
            // The periodic check tries again.
            if (!cancellationToken.IsCancellationRequested)
            {
                LogNoticeFailed(logger, exception);
            }

            return false;
        }
    }

    private async Task PeriodicAsync(CancellationToken stoppingToken)
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

    private async Task NoticesAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var (provider, documentId) in notices.ReadAllAsync(stoppingToken))
            {
                await ApplyNoticeAsync(provider, documentId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is stopping.
        }
    }

    private async Task<bool> OneAsync(Func<ApplySignatureStatus, Task<bool>> apply, CancellationToken cancellationToken)
    {
        await _one.WaitAsync(cancellationToken);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            return await apply(scope.ServiceProvider.GetRequiredService<ApplySignatureStatus>());
        }
        finally
        {
            _one.Release();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} requests of signature changed at the provider.")]
    private static partial void LogChanged(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The check of the requests of signature failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The check of a notice of the provider of signatures failed.")]
    private static partial void LogNoticeFailed(ILogger logger, Exception exception);
}
