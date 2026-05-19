using NUnit.Framework;
using EcsEngine.Core;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class EcsWorldIntegrationTests
{
    [Test]
    public void SafePointMutationAndDeletion_WorkTogether()
    {
        // Arrange
        EcsWorld world = new();
        EntityId entity = world.CreateEntity();

        // Act (add component)
        world.QueueAddComponent(entity, new HealthComponent(100));
        world.ApplySafePoint();

        // Assert (component exists)
        Assert.That(world.TryGetComponent<HealthComponent>(entity, out HealthComponent health), Is.True);
        Assert.That(health.Value, Is.EqualTo(100));

        // Act (delete entity)
        world.MarkForDeletion(entity);
        world.ApplySafePoint();

        // Assert (entity and component deleted)
        Assert.That(world.Exists(entity), Is.False);
        Assert.That(world.TryGetComponent<HealthComponent>(entity, out _), Is.False);
    }

    private readonly record struct HealthComponent(int Value) : IEcsComponent;
}
