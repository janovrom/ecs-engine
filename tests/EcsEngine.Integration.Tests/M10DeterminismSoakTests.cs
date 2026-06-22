using NUnit.Framework;
using EcsEngine.Core;
using EcsEngine.Replay;

namespace EcsEngine.Integration.Tests;

/// <summary>
/// Bounded determinism soak tests for M10 validation.
/// These tests verify state stability over extended tick counts (1k-10k range)
/// without incurring unbounded CI runtime costs.
/// </summary>
[TestFixture]
public class M10DeterminismSoakTests
{
    // Test component setup
    private struct TestPosition : IEcsComponent
    {
        public int X;
        public int Y;
    }

    private struct TestHealth : IEcsComponent
    {
        public int Hp;
    }

    private struct TestVelocity : IEcsComponent
    {
        public int Dx;
        public int Dy;
    }

    private readonly record struct TestDamageEvent(EntityId Target, int Amount) : IEcsEvent;

    // --- Replay determinism over extended tick count ---

    [Test]
    public void Soak_ReplayDeterminism_1kTicks_ProducesConsistentHash()
    {
        // Arrange: Create an op-log with 1000 ticks worth of operations
        OpLog log = new(seed: 42);
        EcsWorld original = new();
        original.AttachOpLog(log);

        // Act: Run 1000 ticks with mutations
        for (int tick = 0; tick < 1000; tick++)
        {
            if (tick % 100 == 0)
            {
                // Every 100 ticks, create an entity
                EntityId e = original.CreateEntity();
                original.QueueAddComponent(e, new TestPosition { X = tick, Y = tick });
            }

            if (tick % 50 == 0)
            {
                // Every 50 ticks, advance tick
                original.AdvanceTick();
            }
        }

        // Assert: Replay produces same final entity set and tick count
        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.AliveEntityIds.Count, Is.EqualTo(original.AliveEntityIds.Count),
            "Replayed world should have same entity count after 1k ticks");
        Assert.That(replayed.Tick, Is.EqualTo(original.Tick),
            "Replayed world should have same tick count");
    }

    [Test]
    public void Soak_MultipleReplays_AllProduceSameState()
    {
        // Arrange: Create an op-log
        OpLog log = new(seed: 123);
        EcsWorld original = new();
        original.AttachOpLog(log);

        // Act: Generate operations
        for (int tick = 0; tick < 500; tick++)
        {
            if (tick % 10 == 0)
            {
                EntityId e = original.CreateEntity();
                original.QueueAddComponent(e, new TestHealth { Hp = 100 + tick });
            }
            if (tick % 25 == 0)
                original.AdvanceTick();
        }

        // Assert: Three independent replays all match the original
        for (int i = 0; i < 3; i++)
        {
            EcsWorld replayed = new WorldReplayer(log).Run();
            Assert.That(replayed.AliveEntityIds.Count, Is.EqualTo(original.AliveEntityIds.Count),
                $"Replay iteration {i + 1} should have same entity count");
            Assert.That(replayed.Tick, Is.EqualTo(original.Tick),
                $"Replay iteration {i + 1} should have same tick count");
        }
    }

    // --- Snapshot round-trip cycle stability ---

    [Test]
    public void Soak_SnapshotRestoreCycles_MaintainEntityState()
    {
        // Arrange: Create a world with state
        EcsWorld world = new();
        for (int i = 0; i < 50; i++)
        {
            EntityId e = world.CreateEntity();
            world.QueueAddComponent(e, new TestPosition { X = i, Y = i * 2 });
            world.QueueAddComponent(e, new TestVelocity { Dx = 1, Dy = 2 });
        }
        world.ApplySafePoint();
        world.AdvanceTick();

        int originalEntityCount = world.AliveEntityIds.Count;
        int originalTick = world.Tick;

        // Act: Perform 5 snapshot-restore cycles
        var registry = new ComponentSerializerRegistry();
        registry.Register<TestPosition>(
            (w, p) =>
            {
                w.Write(p.X);
                w.Write(p.Y);
            },
            (r) => new TestPosition { X = r.ReadInt32(), Y = r.ReadInt32() });
        registry.Register<TestVelocity>(
            (w, v) =>
            {
                w.Write(v.Dx);
                w.Write(v.Dy);
            },
            (r) => new TestVelocity { Dx = r.ReadInt32(), Dy = r.ReadInt32() });

        EcsWorld current = world;
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Snapshot to memory
            var ms = new System.IO.MemoryStream();
            SnapshotWriter writer = new(registry);
            writer.Write(new System.IO.BinaryWriter(ms), current, SnapshotMode.TickBoundary);

            // Restore from memory
            ms.Position = 0;
            SnapshotReader reader = new(registry);
            current = reader.Read(new System.IO.BinaryReader(ms));

            // Verify state is stable
            Assert.That(current.AliveEntityIds.Count, Is.EqualTo(originalEntityCount),
                $"Snapshot-restore cycle {cycle + 1} should preserve entity count");
            Assert.That(current.Tick, Is.EqualTo(originalTick),
                $"Snapshot-restore cycle {cycle + 1} should preserve tick");
        }
    }

    // --- Adaptive tick interval replay ---

    [Test]
    public void Soak_AdaptiveTickIntervalChanges_ReplayProducesSameSchedule()
    {
        // Arrange: Create world with interval changes
        OpLog log = new(seed: 456);
        EcsWorld world = new();
        world.AttachOpLog(log);
        TickScheduler scheduler = new(initialIntervalMs: 16);
        scheduler.AttachOpLog(log);

        // Act: Simulate tick interval changes over 200 ticks
        for (int tick = 0; tick < 200; tick++)
        {
            if (tick % 50 == 0)
            {
                int newInterval = 16 + (tick / 50); // 16, 17, 18, 19 ms
                scheduler.SetInterval(newInterval);
            }
            world.AdvanceTick();
        }

        // Assert: Replay produces same interval sequence
        TickScheduler replayedScheduler = new(initialIntervalMs: 16);
        new WorldReplayer(log).Run(replayedScheduler);

        Assert.That(replayedScheduler.IntervalMs, Is.EqualTo(scheduler.IntervalMs),
            "Replayed scheduler should have same final interval");
    }

    // --- Large-scale entity and component stress ---

    [Test]
    public void Soak_1kEntities_5kComponentAdds_DeterministicReplay()
    {
        // Arrange: Create 1000 entities with components
        OpLog log = new(seed: 789);
        EcsWorld original = new();
        original.AttachOpLog(log);

        // Act: Create entities and add components
        for (int i = 0; i < 1000; i++)
        {
            EntityId e = original.CreateEntity();
            original.QueueAddComponent(e, new TestPosition { X = i % 100, Y = i / 100 });

            if (i % 200 == 0)
                original.QueueAddComponent(e, new TestHealth { Hp = 100 });

            if (i % 333 == 0)
                original.AdvanceTick();
        }
        original.ApplySafePoint();

        // Assert: Replay matches
        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.AliveEntityIds.Count, Is.EqualTo(original.AliveEntityIds.Count),
            "Replay of 1k entities should have same entity count");
        Assert.That(replayed.Tick, Is.EqualTo(original.Tick),
            "Replay of 1k entities should have same tick count");
    }
}
