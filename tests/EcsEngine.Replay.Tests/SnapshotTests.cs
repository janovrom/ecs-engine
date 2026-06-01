using System.IO;
using NUnit.Framework;
using EcsEngine.Core;
using EcsEngine.Replay;

namespace EcsEngine.Replay.Tests;

[TestFixture]
public class SnapshotTests
{
    private static ComponentSerializerRegistry BuildRegistry()
    {
        ComponentSerializerRegistry r = new();
        r.Register<Position>(
            write: (w, p) => { w.Write(p.X); w.Write(p.Y); },
            read: r => new Position(r.ReadSingle(), r.ReadSingle()));
        r.Register<Tag>(
            write: (w, t) => w.Write(t.Id),
            read: r => new Tag(r.ReadInt32()));
        return r;
    }

    private static (SnapshotWriter, SnapshotReader) BuildPair()
    {
        ComponentSerializerRegistry registry = BuildRegistry();
        return (new SnapshotWriter(registry), new SnapshotReader(registry));
    }

    private static (MemoryStream Stream, EcsWorld World) WriteSnapshot(
        EcsWorld world, SnapshotMode mode, SnapshotWriter writer)
    {
        MemoryStream ms = new();
        using BinaryWriter bw = new(ms, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(bw, world, mode);
        bw.Flush();
        ms.Position = 0;
        return (ms, world);
    }

    // --- Roundtrip entity preservation ---

    [Test]
    public void Snapshot_TickBoundary_RoundtripPreservesEntities()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        EntityId a = world.CreateEntity();
        EntityId b = world.CreateEntity();
        world.ApplySafePoint();

        (MemoryStream ms, _) = WriteSnapshot(world, SnapshotMode.TickBoundary, writer);
        using BinaryReader br = new(ms);
        EcsWorld restored = reader.Read(br);

        Assert.That(restored.Exists(a), Is.True);
        Assert.That(restored.Exists(b), Is.True);
    }

    [Test]
    public void Snapshot_RoundtripPreservesComponents()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        EntityId e = world.CreateEntity();
        world.QueueAddComponent(e, new Position(3f, 7f));
        world.QueueAddComponent(e, new Tag(99));
        world.ApplySafePoint();

        (MemoryStream ms, _) = WriteSnapshot(world, SnapshotMode.TickBoundary, writer);
        using BinaryReader br = new(ms);
        EcsWorld restored = reader.Read(br);

        Assert.That(restored.TryGetComponent<Position>(e, out Position pos), Is.True);
        Assert.That(pos.X, Is.EqualTo(3f));
        Assert.That(pos.Y, Is.EqualTo(7f));

        Assert.That(restored.TryGetComponent<Tag>(e, out Tag tag), Is.True);
        Assert.That(tag.Id, Is.EqualTo(99));
    }

    [Test]
    public void Snapshot_RoundtripPreservesTick()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        world.AdvanceTick();
        world.AdvanceTick();
        world.ApplySafePoint();

        (MemoryStream ms, _) = WriteSnapshot(world, SnapshotMode.TickBoundary, writer);
        using BinaryReader br = new(ms);
        EcsWorld restored = reader.Read(br);

        Assert.That(restored.Tick, Is.EqualTo(world.Tick));
    }

    [Test]
    public void Snapshot_RoundtripPreservesEntityCount()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        for (int i = 0; i < 5; i++) world.CreateEntity();
        world.ApplySafePoint();

        (MemoryStream ms, _) = WriteSnapshot(world, SnapshotMode.TickBoundary, writer);
        using BinaryReader br = new(ms);
        EcsWorld restored = reader.Read(br);

        Assert.That(restored.AliveEntityIds, Has.Count.EqualTo(5));
    }

    // --- SnapshotMode.Immediate ---

    [Test]
    public void Snapshot_ImmediateMode_CanBeTakenWithoutApplySafePoint()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        EntityId e = world.CreateEntity();
        world.QueueAddComponent(e, new Tag(1)); // pending — not yet applied

        // Should not throw; Immediate mode is always allowed
        MemoryStream ms = new();
        using BinaryWriter bw = new(ms, System.Text.Encoding.UTF8, leaveOpen: true);
        Assert.DoesNotThrow(() => writer.Write(bw, world, SnapshotMode.Immediate));
    }

    // --- Invalid data ---

    [Test]
    public void SnapshotReader_InvalidMagic_ThrowsInvalidDataException()
    {
        (_, SnapshotReader reader) = BuildPair();
        MemoryStream ms = new();
        using (BinaryWriter bw = new(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            bw.Write(0xDEADBEEFu); // wrong magic

        ms.Position = 0;
        using BinaryReader br = new(ms);
        Assert.Throws<InvalidDataException>(() => reader.Read(br));
    }

    // --- State equivalence after roundtrip ---

    [Test]
    public void Snapshot_RestoredWorldHashMatchesOriginal()
    {
        (SnapshotWriter writer, SnapshotReader reader) = BuildPair();
        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        EntityId e2 = world.CreateEntity();
        world.QueueAddComponent(e1, new Position(1f, 2f));
        world.QueueAddComponent(e2, new Tag(7));
        world.ApplySafePoint();
        world.AdvanceTick();

        (MemoryStream ms, _) = WriteSnapshot(world, SnapshotMode.TickBoundary, writer);
        using BinaryReader br = new(ms);
        EcsWorld restored = reader.Read(br);

        ComponentHasherRegistry hashers = new();
        hashers.Register<Position>();
        hashers.Register<Tag>();
        WorldHasher hasher = new(hashers);

        Assert.That(hasher.Hash(restored), Is.EqualTo(hasher.Hash(world)));
    }
}
