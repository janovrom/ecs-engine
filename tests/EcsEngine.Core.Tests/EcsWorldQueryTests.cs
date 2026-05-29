using NUnit.Framework;
using EcsEngine.Core;

namespace EcsEngine.Core.Tests;

[TestFixture]
public class EcsWorldQueryTests
{
    [Test]
    public void QueryEach_SingleArchetypeComponent_VisitsAllMatchingEntities()
    {
        // Arrange
        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        EntityId e2 = world.CreateEntity();
        world.QueueAddComponent(e1, new Position(1f, 0f, 0f));
        world.QueueAddComponent(e2, new Position(2f, 0f, 0f));
        world.ApplySafePoint();

        // Act
        List<float> visited = [];
        world.QueryEach<Position>(
            (EntityId _, in Position p) => visited.Add(p.X));

        // Assert
        Assert.That(visited, Has.Count.EqualTo(2));
        Assert.That(visited, Is.EquivalentTo(new[] { 1f, 2f }));
    }

    [Test]
    public void QueryEach_TwoArchetypeComponents_ReturnsOnlyEntitiesWithBoth()
    {
        // Arrange
        EcsWorld world = new();
        EntityId withBoth = world.CreateEntity();
        EntityId posOnly = world.CreateEntity();

        world.QueueAddComponent(withBoth, new Position(10f, 0f, 0f));
        world.QueueAddComponent(withBoth, new Velocity(5f, 0f, 0f));
        world.QueueAddComponent(posOnly, new Position(99f, 0f, 0f));
        world.ApplySafePoint();

        // Act
        List<(float px, float vx)> results = [];
        world.QueryEach<Position, Velocity>(
            (EntityId _, in Position p, in Velocity v) => results.Add((p.X, v.X)));

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].px, Is.EqualTo(10f));
        Assert.That(results[0].vx, Is.EqualTo(5f));
    }

    [Test]
    public void QueryEach_SparseComponent_VisitsMatchingEntities()
    {
        // Arrange
        EcsWorld world = new();
        EntityId withStatus = world.CreateEntity();
        EntityId without = world.CreateEntity();
        world.QueueAddComponent(withStatus, new Status(7));
        world.ApplySafePoint();

        // Act
        List<int> values = [];
        world.QueryEach<Status>(
            (EntityId _, in Status s) => values.Add(s.Value));

        // Assert
        Assert.That(values, Has.Count.EqualTo(1));
        Assert.That(values[0], Is.EqualTo(7));
    }

    [Test]
    public void QueryEach_MixedStorage_VisitsEntitiesWithBoth()
    {
        // Arrange
        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        EntityId e2 = world.CreateEntity();

        world.QueueAddComponent(e1, new Position(1f, 0f, 0f));
        world.QueueAddComponent(e1, new Status(10));
        world.QueueAddComponent(e2, new Position(2f, 0f, 0f)); // no Status
        world.ApplySafePoint();

        // Act
        List<(float px, int sv)> results = [];
        world.QueryEach<Position, Status>(
            (EntityId _, in Position p, in Status s) => results.Add((p.X, s.Value)));

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].px, Is.EqualTo(1f));
        Assert.That(results[0].sv, Is.EqualTo(10));
    }

    [Test]
    public void QueryEach_EmptyWorld_NeverInvokesCallback()
    {
        // Arrange
        EcsWorld world = new();

        // Act
        int count = 0;
        world.QueryEach<Position>((EntityId _, in Position _) => count++);

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void QueryRegistry_CacheIsRebultAfterNewArchetypeCreated()
    {
        // Arrange
        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        world.QueueAddComponent(e1, new Position(1f, 0f, 0f));
        world.ApplySafePoint();

        // Act — prime cache with single-component archetype, then add second entity with extra component
        List<float> firstQuery = [];
        world.QueryEach<Position>((EntityId _, in Position p) => firstQuery.Add(p.X));

        EntityId e2 = world.CreateEntity();
        world.QueueAddComponent(e2, new Position(2f, 0f, 0f));
        world.QueueAddComponent(e2, new Velocity(1f, 0f, 0f));
        world.ApplySafePoint(); // new archetype {Position, Velocity} created → cache invalidated

        List<float> secondQuery = [];
        world.QueryEach<Position>((EntityId _, in Position p) => secondQuery.Add(p.X));

        // Assert — second query picks up both archetypes
        Assert.That(firstQuery, Has.Count.EqualTo(1));
        Assert.That(secondQuery, Has.Count.EqualTo(2));
    }

    [Test]
    public void PreloadQuery_BuildsCacheBeforeFirstUse()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();
        world.QueueAddComponent(entity, new Position(3f, 0f, 0f));
        world.QueueAddComponent(entity, new Velocity(1f, 0f, 0f));
        world.ApplySafePoint();

        // Act — preload, then query
        world.PreloadQuery<Position, Velocity>();
        List<float> results = [];
        world.QueryEach<Position, Velocity>(
            (EntityId _, in Position p, in Velocity _) => results.Add(p.X));

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.EqualTo(3f));
    }

    [Test]
    public void QueryEach_ThreeArchetypeComponents_ReturnsCorrectData()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();
        world.QueueAddComponent(entity, new Position(1f, 2f, 3f));
        world.QueueAddComponent(entity, new Velocity(4f, 5f, 6f));
        world.QueueAddComponent(entity, new Mass(7f));
        world.ApplySafePoint();

        // Act
        List<(float px, float vx, float m)> results = [];
        world.QueryEach<Position, Velocity, Mass>(
            (EntityId _, in Position p, in Velocity v, in Mass m) =>
                results.Add((p.X, v.X, m.Value)));

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.EqualTo((1f, 4f, 7f)));
    }

    // --- Test component types ---

    [ArchetypeStorage]
    private readonly record struct Position(float X, float Y, float Z) : IEcsComponent;

    [ArchetypeStorage]
    private readonly record struct Velocity(float X, float Y, float Z) : IEcsComponent;

    [ArchetypeStorage]
    private readonly record struct Mass(float Value) : IEcsComponent;

    [SparseStorage]
    private readonly record struct Status(int Value) : IEcsComponent;
}
