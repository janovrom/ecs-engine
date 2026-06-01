using NUnit.Framework;
using EcsEngine.Core;
using EcsEngine.Replay;

namespace EcsEngine.Replay.Tests;

[TestFixture]
public class TickSchedulerTests
{
    // --- Basic property ---

    [Test]
    public void TickScheduler_InitialInterval_IsSetCorrectly()
    {
        TickScheduler scheduler = new(initialIntervalMs: 33);

        Assert.That(scheduler.IntervalMs, Is.EqualTo(33));
    }

    [Test]
    public void SetInterval_UpdatesIntervalMs()
    {
        TickScheduler scheduler = new(initialIntervalMs: 33);
        scheduler.SetInterval(50);

        Assert.That(scheduler.IntervalMs, Is.EqualTo(50));
    }

    // --- Op-log recording ---

    [Test]
    public void SetInterval_WithOpLogAttached_RecordsOperation()
    {
        TickScheduler scheduler = new(initialIntervalMs: 33);
        OpLog log = new(seed: 0);
        scheduler.AttachOpLog(log);

        scheduler.SetInterval(100);

        Assert.That(log.Operations, Has.Count.EqualTo(1));
    }

    [Test]
    public void SetInterval_WithoutOpLog_DoesNotThrow()
    {
        TickScheduler scheduler = new(initialIntervalMs: 33);

        Assert.DoesNotThrow(() => scheduler.SetInterval(100));
    }

    [Test]
    public void DetachOpLog_StopsRecording()
    {
        TickScheduler scheduler = new(initialIntervalMs: 33);
        OpLog log = new(seed: 0);
        scheduler.AttachOpLog(log);
        scheduler.SetInterval(50);   // recorded
        scheduler.DetachOpLog();
        scheduler.SetInterval(75);   // not recorded

        Assert.That(log.Operations, Has.Count.EqualTo(1));
    }

    // --- Replay (exit criterion) ---

    [Test]
    public void Replay_TickIntervalChanges_ProduceSameFinalInterval()
    {
        // Record: interval changes 33 → 50 → 100 → 25
        OpLog log = new(seed: 7);
        TickScheduler original = new(initialIntervalMs: 33);
        original.AttachOpLog(log);
        original.SetInterval(50);
        original.SetInterval(100);
        original.SetInterval(25);

        // Replay on a fresh scheduler
        TickScheduler replayed = new(initialIntervalMs: 33);
        new WorldReplayer(log).Run(replayed);

        Assert.That(replayed.IntervalMs, Is.EqualTo(original.IntervalMs));
    }

    [Test]
    public void Replay_TickIntervalChanges_ProduceSameOperationCount()
    {
        // The replayed world op-log should NOT double-record tick ops
        OpLog log = new(seed: 8);
        TickScheduler original = new(initialIntervalMs: 33);
        original.AttachOpLog(log);
        original.SetInterval(50);
        original.SetInterval(100);

        Assert.That(log.Operations, Has.Count.EqualTo(2));
    }

    [Test]
    public void Replay_MixedOpsAndIntervalChanges_SchedulerHasCorrectFinalInterval()
    {
        // Mix world mutations and tick interval changes in the same log
        OpLog log = new(seed: 9);
        EcsWorld world = new();
        world.AttachOpLog(log);
        TickScheduler scheduler = new(initialIntervalMs: 16);
        scheduler.AttachOpLog(log);

        EntityId e = world.CreateEntity();
        scheduler.SetInterval(33);
        world.AdvanceTick();
        scheduler.SetInterval(50);
        world.ApplySafePoint();

        // Replay
        TickScheduler replayedScheduler = new(initialIntervalMs: 16);
        EcsWorld replayedWorld = new WorldReplayer(log).Run(replayedScheduler);

        Assert.That(replayedScheduler.IntervalMs, Is.EqualTo(50));
        Assert.That(replayedWorld.Exists(e), Is.True);
        Assert.That(replayedWorld.Tick, Is.EqualTo(world.Tick));
    }
}
