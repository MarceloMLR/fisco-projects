using System.Diagnostics.Metrics;
using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// The metrics of the queue of e-mails (ADR-0036, ADR-0112), for the alert of a queue that stopped: how many e-mails wait,
/// for how long the oldest waits, read at the moment, so it grows even when the sending stops, and how many were sent
/// and failed for good. Metrics are not sampled.
/// </summary>
internal sealed class EmailQueueMetrics : IDisposable
{
    public const string MeterName = "FiscoProjects.Notifications";

    private readonly Meter _meter;
    private readonly Counter<long> _sent;
    private readonly Counter<long> _failed;
    private readonly TimeProvider _clock;
    private EmailQueueState _queue = EmailQueueState.Empty;

    public EmailQueueMetrics(IMeterFactory meters, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(meters);
        _clock = clock;
        _meter = meters.Create(MeterName);
        _sent = _meter.CreateCounter<long>("fiscoprojects.emails.sent", "{email}", "E-mails sent.");
        _failed = _meter.CreateCounter<long>("fiscoprojects.emails.failed", "{email}", "E-mails that failed for good.");
        _meter.CreateObservableGauge("fiscoprojects.emails.waiting", () => _queue.Waiting, "{email}", "E-mails waiting to be sent.");
        _meter.CreateObservableGauge(
            "fiscoprojects.emails.oldest_waiting", OldestWaiting, "s", "For how long the oldest e-mail waits to be sent.");
    }

    /// <summary>What a round of the sending did and the queue it left.</summary>
    public void Record(DispatchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _queue = result.Queue;
        if (result.Sent > 0)
        {
            _sent.Add(result.Sent);
        }

        var failed = result.Failures.Count(failure => failure.Final);
        if (failed > 0)
        {
            _failed.Add(failed);
        }
    }

    public void Dispose() => _meter.Dispose();

    private double OldestWaiting() =>
        _queue.OldestSince is { } since
            ? Math.Max(0, (_clock.GetUtcNow().UtcDateTime - since).TotalSeconds)
            : 0;
}
