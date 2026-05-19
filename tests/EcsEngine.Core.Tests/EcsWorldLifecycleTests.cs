using NSubstitute;
using NUnit.Framework;

namespace EcsEngine.Core.Tests;

[TestFixture]
public class EcsWorldLifecycleTests
{
    [Test]
    public void CreateEntity_AssignsDeterministicIncrementingIds()
    {
        // Arrange
        EcsWorld world = new();

        // Act
        EntityId entity1 = world.CreateEntity();
        EntityId entity2 = world.CreateEntity();

        // Assert
        Assert.That(entity1.Value, Is.EqualTo(1));
        Assert.That(entity2.Value, Is.EqualTo(2));
    }

    [Test]
    public void QueueAddComponent_AppliesOnlyOnSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act (before safe point)
        world.QueueAddComponent(entity, new PositionComponent(1, 2, 3));
        bool beforeSafePoint = world.TryGetComponent<PositionComponent>(entity, out _);

        // Assert (before safe point)
        Assert.That(beforeSafePoint, Is.False);

        // Act (after safe point)
        world.ApplySafePoint();

        // Assert (after safe point)
        Assert.That(world.TryGetComponent<PositionComponent>(entity, out PositionComponent position), Is.True);
        Assert.That(position.X, Is.EqualTo(1));
        Assert.That(position.Y, Is.EqualTo(2));
        Assert.That(position.Z, Is.EqualTo(3));
    }

    [Test]
    public void MarkForDeletion_DisablesSchedulingImmediately_AndDeletesAtSafePoint()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act
        world.MarkForDeletion(entity);

        // Assert (before safe point)
        Assert.That(world.IsMarkedForDeletion(entity), Is.True);
        Assert.That(world.CanSchedule(entity), Is.False);
        Assert.That(world.Exists(entity), Is.True);
        Assert.Throws<InvalidOperationException>(() => world.QueueRemoveComponent<PositionComponent>(entity));

        // Act
        world.ApplySafePoint();

        // Assert (after safe point)
        Assert.That(world.Exists(entity), Is.False);
    }

    [Test]
    public void QueueEvent_IsDeferredUntilNextTick()
    {
        // Arrange
        EcsWorld world = new();

        // Act
        world.QueueEvent(new TickEvent("queued"));

        // Assert (before advance)
        Assert.That(world.GetCurrentTickEvents<TickEvent>(), Is.Empty);

        // Act
        world.AdvanceTick();

        // Assert (after first advance)
        IReadOnlyList<TickEvent> events = world.GetCurrentTickEvents<TickEvent>();
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Name, Is.EqualTo("queued"));

        // Act
        world.AdvanceTick();

        // Assert (after second advance, event cleared)
        Assert.That(world.GetCurrentTickEvents<TickEvent>(), Is.Empty);
    }

    [Test]
    public void UnitTests_CanUseNSubstitute()
    {
        // Arrange
        ITestDependency dependency = Substitute.For<ITestDependency>();
        dependency.GetValue().Returns(42);

        // Act
        int result = dependency.GetValue();

        // Assert
        Assert.That(result, Is.EqualTo(42));
        dependency.Received(1).GetValue();
    }

    public interface ITestDependency
    {
        int GetValue();
    }

    private readonly record struct PositionComponent(int X, int Y, int Z) : IEcsComponent;

    private readonly record struct TickEvent(string Name) : IEcsEvent;
}
