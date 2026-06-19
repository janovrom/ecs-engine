using System.Diagnostics.Metrics;
using EcsEngine.Runtime;
using NUnit.Framework;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class MetricsSystemExecutionObserverTests
{
    [Test]
    public void Observer_WithMinimumSeverityWarning_SuppressesInformationAndDebugMetrics()
    {
        using MeterListener listener = new();
        List<string> instruments = [];

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "EcsEngine.Runtime")
                meterListener.EnableMeasurementEvents(instrument);
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            instruments.Add(instrument.Name));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            instruments.Add(instrument.Name));
        listener.Start();

        MetricsSystemExecutionObserver observer = new(new RuntimeObservabilityConfig
        {
            Enabled = true,
            PublishEveryTicks = 1,
            MinimumSeverity = RuntimeObservabilitySeverity.Warning,
        });

        observer.OnTickStarted(tick: 0, systemCount: 2, batchCount: 1, maxBatchSize: 2, batchingEfficiency: 1d);
        observer.OnSystemExecuted(tick: 0, typeof(MetricsSystemExecutionObserverTests), elapsedTicks: 10, allocatedBytes: 64);
        observer.OnTickCompleted(tick: 0, elapsedTicks: 20, allocatedBytes: 128);

        Assert.That(instruments, Is.Empty);
    }

    [Test]
    public void Observer_WithMinimumSeverityDebug_EmitsBatchingAndExecutionMetrics()
    {
        using MeterListener listener = new();
        HashSet<string> instruments = [];

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "EcsEngine.Runtime")
                meterListener.EnableMeasurementEvents(instrument);
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            instruments.Add(instrument.Name));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            instruments.Add(instrument.Name));
        listener.Start();

        MetricsSystemExecutionObserver observer = new(new RuntimeObservabilityConfig
        {
            Enabled = true,
            PublishEveryTicks = 1,
            MinimumSeverity = RuntimeObservabilitySeverity.Debug,
        });

        observer.OnTickStarted(tick: 0, systemCount: 2, batchCount: 1, maxBatchSize: 2, batchingEfficiency: 1d);
        observer.OnSystemExecuted(tick: 0, typeof(MetricsSystemExecutionObserverTests), elapsedTicks: 10, allocatedBytes: 64);
        observer.OnTickCompleted(tick: 0, elapsedTicks: 20, allocatedBytes: 128);

        Assert.Multiple(() =>
        {
            Assert.That(instruments, Contains.Item("ecs.tick.count"));
            Assert.That(instruments, Contains.Item("ecs.tick.batch.count"));
            Assert.That(instruments, Contains.Item("ecs.tick.batch.max_size"));
            Assert.That(instruments, Contains.Item("ecs.tick.batch.efficiency"));
            Assert.That(instruments, Contains.Item("ecs.system.run.count"));
            Assert.That(instruments, Contains.Item("ecs.system.alloc.bytes"));
            Assert.That(instruments, Contains.Item("ecs.system.duration.ms"));
            Assert.That(instruments, Contains.Item("ecs.tick.alloc.bytes"));
            Assert.That(instruments, Contains.Item("ecs.tick.duration.ms"));
        });
    }
}
