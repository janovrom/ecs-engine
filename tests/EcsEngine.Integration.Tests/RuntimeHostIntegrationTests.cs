using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using EcsEngine.Runtime;
using EcsEngine.Transport;
using NUnit.Framework;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class RuntimeHostIntegrationTests
{
    [Test]
    public void ConfigLoader_ParsesStartupModes_CaseInsensitive()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ \"startupMode\": \"server\" }");
            RuntimeConfig config = RuntimeConfigLoader.LoadFromFile(path);
            Assert.That(config.StartupMode, Is.EqualTo(StartupMode.Server));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ConfigLoader_ParsesObservabilityConfig_CaseInsensitive()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                """
                {
                  "startupMode": "local",
                  "observability": {
                    "enabled": true,
                    "publishEveryTicks": 4,
                    "minimumSeverity": "warning"
                  }
                }
                """);

            RuntimeConfig config = RuntimeConfigLoader.LoadFromFile(path);

            Assert.That(config.Observability.Enabled, Is.True);
            Assert.That(config.Observability.PublishEveryTicks, Is.EqualTo(4));
            Assert.That(config.Observability.MinimumSeverity, Is.EqualTo(RuntimeObservabilitySeverity.Warning));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Build_ServerModeWithoutTransport_ThrowsInvalidOperationException()
    {
        RuntimeHostBuilder builder = new RuntimeHostBuilder()
            .UseStartupMode(StartupMode.Server);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void RegistrationWindow_IsClosedAfterBuild()
    {
        RuntimeHostBuilder builder = new RuntimeHostBuilder()
            .UseStartupMode(StartupMode.Local)
            .RegisterSystem(_ => new ZSystem());

        _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() =>
            builder.RegisterSystem(_ => new ASystem()));
    }

    [Test]
    public void RuntimeRegistration_ProducesDeterministicOrder()
    {
        RuntimeHost host = new RuntimeHostBuilder()
            .RegisterSystem(_ => new ZSystem())
            .RegisterSystem(_ => new ASystem())
            .Build();

        string[] names = host.Executor.Systems.Select(s => s.GetType().Name).ToArray();
        Assert.That(names, Is.EqualTo(new[] { nameof(ASystem), nameof(ZSystem) }));
    }

    [Test]
    public void ConstructorInjection_UsesRegisteredSingleton()
    {
        CounterSink sink = new();

        RuntimeHost host = new RuntimeHostBuilder()
            .AddSingleton(sink)
            .RegisterSystem(sp => new IncrementSystem(sp.GetRequired<CounterSink>()))
            .Build();

        EcsWorld world = new();
        host.RunTick(world);

        Assert.That(sink.Count, Is.EqualTo(1));
    }

    [Test]
    public void RunTick_WithCustomObserver_EmitsExecutionCallbacks()
    {
        RuntimeObserver observer = new();

        RuntimeHost host = new RuntimeHostBuilder()
            .UseSystemExecutionObserver(observer)
            .RegisterSystem(_ => new ASystem())
            .Build();

        EcsWorld world = new();
        host.RunTick(world);

        Assert.That(observer.TickStarts, Is.EqualTo(1));
        Assert.That(observer.TickCompletes, Is.EqualTo(1));
        Assert.That(observer.SystemRuns, Is.EqualTo(1));
    }

    [Test]
    public void InMemoryTransport_PreservesPublishOrderAcrossThreads()
    {
        InMemoryTransport transport = new();

        Task producer1 = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
                transport.Publish("p1", [1, 2, 3]);
        });

        Task producer2 = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
                transport.Publish("p2", [4, 5]);
        });

        Task.WaitAll(producer1, producer2);

        int count = 0;
        int previousSequence = 0;
        while (transport.TryRead(out TransportMessage message))
        {
            count++;
            Assert.That(message.Sequence, Is.GreaterThan(previousSequence));
            previousSequence = message.Sequence;
        }

        Assert.That(count, Is.EqualTo(100));
        Assert.That(transport.PendingCount, Is.EqualTo(0));
    }

    [EcsSystem]
    private sealed class ASystem : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class ZSystem : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class IncrementSystem : IEcsSystem
    {
        private readonly CounterSink _Sink;

        public IncrementSystem(CounterSink sink)
        {
            _Sink = sink;
        }

        public void Execute(EcsWorld world) => _Sink.Count++;
    }

    private sealed class CounterSink
    {
        public int Count { get; set; }
    }

    private sealed class RuntimeObserver : ISystemExecutionObserver
    {
        public int TickStarts { get; private set; }
        public int TickCompletes { get; private set; }
        public int SystemRuns { get; private set; }

        public bool IsEnabled => true;

        public bool ShouldSampleTick(int tick) => true;

        public void OnTickStarted(
            int tick,
            int systemCount,
            int batchCount,
            int maxBatchSize,
            double batchingEfficiency)
            => TickStarts++;

        public void OnSystemExecuted(int tick, Type systemType, long elapsedTicks, long allocatedBytes)
            => SystemRuns++;

        public void OnTickCompleted(int tick, long elapsedTicks, long allocatedBytes)
            => TickCompletes++;
    }
}
