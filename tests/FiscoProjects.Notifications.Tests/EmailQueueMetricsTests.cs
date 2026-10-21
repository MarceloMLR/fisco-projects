using System.Diagnostics.Metrics;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Notifications.Tests;

// Spec: monitoramento, requirement "Métricas da fila de e-mails" (ADR-0112).
public sealed class EmailQueueMetricsTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact(DisplayName = "Fila parada nas métricas")]
    public void TheWaitOfTheOldest_GrowsEvenWithoutNewRounds()
    {
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var clock = new FakeTimeProvider(Now);
        using var metrics = new EmailQueueMetrics(services.GetRequiredService<IMeterFactory>(), clock);
        var measured = new Dictionary<string, double>();
        using var listener = Listen(measured);

        metrics.Record(new DispatchResult(
            3,
            [new EmailFailure(Guid.CreateVersion7(), 6, true, "out"), new EmailFailure(Guid.CreateVersion7(), 1, false, "out")],
            new EmailQueueState(2, Now.AddMinutes(-10))));
        listener.RecordObservableInstruments();
        var first = new Dictionary<string, double>(measured);
        clock.Advance(TimeSpan.FromMinutes(20));
        listener.RecordObservableInstruments();

        first["fiscoprojects.emails.waiting"].Should().Be(2);
        first["fiscoprojects.emails.oldest_waiting"].Should().Be(600);
        first["fiscoprojects.emails.sent"].Should().Be(3);
        first["fiscoprojects.emails.failed"].Should().Be(1, "only the final failure counts");
        measured["fiscoprojects.emails.oldest_waiting"].Should().Be(1800);
    }

    [Fact(DisplayName = "Fila vazia nas métricas")]
    public void AnEmptyQueue_WaitsNothing()
    {
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var metrics = new EmailQueueMetrics(services.GetRequiredService<IMeterFactory>(), new FakeTimeProvider(Now));
        var measured = new Dictionary<string, double>();
        using var listener = Listen(measured);

        metrics.Record(new DispatchResult(0, [], EmailQueueState.Empty));
        listener.RecordObservableInstruments();

        measured["fiscoprojects.emails.waiting"].Should().Be(0);
        measured["fiscoprojects.emails.oldest_waiting"].Should().Be(0);
        measured.Should().NotContainKey("fiscoprojects.emails.sent");
    }

    /// <summary>Keeps the last value of each instrument of the meter of the queue; the counters, added up.</summary>
    private static MeterListener Listen(Dictionary<string, double> measured)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == EmailQueueMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, _, _) => measured[instrument.Name] = value);
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => measured[instrument.Name] = value);
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            measured[instrument.Name] = measured.GetValueOrDefault(instrument.Name) + value);
        listener.Start();
        return listener;
    }
}
