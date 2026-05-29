using NUnit.Framework;
using EcsEngine.Core;

namespace EcsEngine.Core.Tests;

[TestFixture]
public class EcsWorldStorageTests
{
    [Test]
    public void QueueAddComponent_ArchetypeStorage_IsAccessibleAfterSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act
        world.QueueAddComponent(entity, new HotPosition(1f, 2f, 3f));
        world.ApplySafePoint();

        // Assert
        Assert.That(world.TryGetComponent<HotPosition>(entity, out HotPosition pos), Is.True);
        Assert.That(pos.X, Is.EqualTo(1f));
        Assert.That(pos.Y, Is.EqualTo(2f));
        Assert.That(pos.Z, Is.EqualTo(3f));
    }

    [Test]
    public void QueueAddComponent_SparseStorage_IsAccessibleAfterSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act
        world.QueueAddComponent(entity, new RareStatus(42));
        world.ApplySafePoint();

        // Assert
        Assert.That(world.TryGetComponent<RareStatus>(entity, out RareStatus status), Is.True);
        Assert.That(status.Value, Is.EqualTo(42));
    }

    [Test]
    public void QueueAddComponent_DefaultStorage_BehavesSameAsSparse()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act
        world.QueueAddComponent(entity, new PlainHealth(100));
        bool beforeSafePoint = world.TryGetComponent<PlainHealth>(entity, out _);
        world.ApplySafePoint();

        // Assert
        Assert.That(beforeSafePoint, Is.False);
        Assert.That(world.TryGetComponent<PlainHealth>(entity, out PlainHealth h), Is.True);
        Assert.That(h.Value, Is.EqualTo(100));
    }

    [Test]
    public void TwoArchetypeComponents_EntityMovesToCombinedArchetype()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act
        world.QueueAddComponent(entity, new HotPosition(1f, 0f, 0f));
        world.QueueAddComponent(entity, new HotVelocity(2f, 0f, 0f));
        world.ApplySafePoint();

        // Assert — entity has both components
        Assert.That(world.TryGetComponent<HotPosition>(entity, out HotPosition pos), Is.True);
        Assert.That(world.TryGetComponent<HotVelocity>(entity, out HotVelocity vel), Is.True);
        Assert.That(pos.X, Is.EqualTo(1f));
        Assert.That(vel.X, Is.EqualTo(2f));
    }

    [Test]
    public void QueueRemoveComponent_Archetype_ComponentGoneAfterSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();
        world.QueueAddComponent(entity, new HotPosition(5f, 0f, 0f));
        world.QueueAddComponent(entity, new HotVelocity(1f, 0f, 0f));
        world.ApplySafePoint();

        // Act
        world.QueueRemoveComponent<HotVelocity>(entity);
        world.ApplySafePoint();

        // Assert
        Assert.That(world.TryGetComponent<HotPosition>(entity, out _), Is.True);
        Assert.That(world.TryGetComponent<HotVelocity>(entity, out _), Is.False);
    }

    [Test]
    public void QueueRemoveComponent_Sparse_ComponentGoneAfterSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();
        world.QueueAddComponent(entity, new RareStatus(7));
        world.ApplySafePoint();

        // Act
        world.QueueRemoveComponent<RareStatus>(entity);
        world.ApplySafePoint();

        // Assert
        Assert.That(world.TryGetComponent<RareStatus>(entity, out _), Is.False);
    }

    [Test]
    public void MarkForDeletion_RemovesArchetypeAndSparseComponents()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();
        world.QueueAddComponent(entity, new HotPosition(1f, 0f, 0f));
        world.QueueAddComponent(entity, new RareStatus(3));
        world.ApplySafePoint();

        // Act
        world.MarkForDeletion(entity);
        world.ApplySafePoint();

        // Assert
        Assert.That(world.Exists(entity), Is.False);
    }

    [Test]
    public void MultipleEntities_SwapRemovePreservesOtherEntityData()
    {
        // Arrange
        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        EntityId e2 = world.CreateEntity();
        EntityId e3 = world.CreateEntity();
        world.QueueAddComponent(e1, new HotPosition(1f, 0f, 0f));
        world.QueueAddComponent(e2, new HotPosition(2f, 0f, 0f));
        world.QueueAddComponent(e3, new HotPosition(3f, 0f, 0f));
        world.ApplySafePoint();

        // Act — delete middle entity (triggers swap-remove)
        world.MarkForDeletion(e2);
        world.ApplySafePoint();

        // Assert — remaining entities still have correct data
        Assert.That(world.TryGetComponent<HotPosition>(e1, out HotPosition p1), Is.True);
        Assert.That(world.TryGetComponent<HotPosition>(e3, out HotPosition p3), Is.True);
        Assert.That(p1.X, Is.EqualTo(1f));
        Assert.That(p3.X, Is.EqualTo(3f));
    }

    // --- Test component types ---

    [ArchetypeStorage]
    private readonly record struct HotPosition(float X, float Y, float Z) : IEcsComponent;

    [ArchetypeStorage]
    private readonly record struct HotVelocity(float X, float Y, float Z) : IEcsComponent;

    [SparseStorage]
    private readonly record struct RareStatus(int Value) : IEcsComponent;

    private readonly record struct PlainHealth(int Value) : IEcsComponent;
}
