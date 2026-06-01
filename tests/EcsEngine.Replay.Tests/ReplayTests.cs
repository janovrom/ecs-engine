using NUnit.Framework;
using EcsEngine.Core;
using EcsEngine.Replay;

namespace EcsEngine.Replay.Tests;

[TestFixture]
public class ReplayTests
{
    // --- Test event type (local — not a component, no hasher needed) ---

    private readonly record struct DamageEvent(int Amount) : IEcsEvent;

    // --- CreateEntity replay ---

    [Test]
    public void Record_CreateEntity_ReplayedWorldHasSameEntityId()
    {
        OpLog log = new(seed: 1);
        EcsWorld original = new();
        original.AttachOpLog(log);
        EntityId entityId = original.CreateEntity();

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.Exists(entityId), Is.True);
    }

    [Test]
    public void Record_MultipleCreateEntities_ReplayedWorldHasAllEntities()
    {
        OpLog log = new(seed: 2);
        EcsWorld original = new();
        original.AttachOpLog(log);
        EntityId a = original.CreateEntity();
        EntityId b = original.CreateEntity();
        EntityId c = original.CreateEntity();

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.Exists(a), Is.True);
        Assert.That(replayed.Exists(b), Is.True);
        Assert.That(replayed.Exists(c), Is.True);
    }

    // --- QueueAddComponent replay ---

    [Test]
    public void Record_QueueAddComponent_ReplayedWorldHasComponent()
    {
        OpLog log = new(seed: 3);
        EcsWorld original = new();
        original.AttachOpLog(log);
        EntityId entity = original.CreateEntity();
        original.QueueAddComponent(entity, new Health(42));
        original.ApplySafePoint();

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.TryGetComponent<Health>(entity, out Health h), Is.True);
        Assert.That(h.Value, Is.EqualTo(42));
    }

    // --- MarkForDeletion replay ---

    [Test]
    public void Record_MarkForDeletion_ReplayedWorldEntityIsGone()
    {
        OpLog log = new(seed: 4);
        EcsWorld original = new();
        original.AttachOpLog(log);
        EntityId entity = original.CreateEntity();
        original.MarkForDeletion(entity);
        original.ApplySafePoint();

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.Exists(entity), Is.False);
    }

    // --- AdvanceTick replay ---

    [Test]
    public void Record_AdvanceTick_ReplayedWorldHasSameTick()
    {
        OpLog log = new(seed: 5);
        EcsWorld original = new();
        original.AttachOpLog(log);
        original.AdvanceTick();
        original.AdvanceTick();
        original.AdvanceTick();

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(replayed.Tick, Is.EqualTo(original.Tick));
    }

    // --- Hash equivalence (exit criterion) ---

    [Test]
    public void Replay_RunTwice_ProducesIdenticalHash()
    {
        // Build an op-log with a realistic sequence
        OpLog log = new(seed: 42);
        EcsWorld original = new();
        original.AttachOpLog(log);

        EntityId e1 = original.CreateEntity();
        EntityId e2 = original.CreateEntity();
        original.QueueAddComponent(e1, new Health(100));
        original.QueueAddComponent(e2, new Health(50));
        original.ApplySafePoint();
        original.AdvanceTick();
        original.QueueAddComponent(e1, new Position(1f, 2f));
        original.ApplySafePoint();
        original.MarkForDeletion(e2);
        original.ApplySafePoint();

        ComponentHasherRegistry hashers = new();
        hashers.Register<Health>();
        hashers.Register<Position>();

        WorldHasher hasher = new(hashers);

        EcsWorld run1 = new WorldReplayer(log).Run();
        EcsWorld run2 = new WorldReplayer(log).Run();

        ulong hash1 = hasher.Hash(run1);
        ulong hash2 = hasher.Hash(run2);

        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash1, Is.Not.EqualTo(0));
    }

    [Test]
    public void Replay_ProducesIdenticalStateToOriginal()
    {
        OpLog log = new(seed: 99);
        EcsWorld original = new();
        original.AttachOpLog(log);

        EntityId e = original.CreateEntity();
        original.QueueAddComponent(e, new Health(77));
        original.ApplySafePoint();
        original.AdvanceTick();

        ComponentHasherRegistry hashers = new();
        hashers.Register<Health>();
        WorldHasher hasher = new(hashers);

        EcsWorld replayed = new WorldReplayer(log).Run();

        Assert.That(hasher.Hash(original), Is.EqualTo(hasher.Hash(replayed)));
    }

    // --- QueueEvent replay ---

    [Test]
    public void Record_QueueEvent_ReplayedWorldHasEventOnSameTick()
    {
        OpLog log = new(seed: 6);
        EcsWorld original = new();
        original.AttachOpLog(log);
        original.QueueEvent(new DamageEvent(10));
        original.AdvanceTick();

        EcsWorld replayed = new WorldReplayer(log).Run();

        IReadOnlyList<DamageEvent> events = replayed.GetCurrentTickEvents<DamageEvent>();
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Amount, Is.EqualTo(10));
    }
}
