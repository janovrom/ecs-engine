using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Serializes and deserializes a single component type to/from binary.
/// </summary>
public interface IComponentSerializer
{
    /// <summary>Fully-qualified type name of the component, used as the binary tag.</summary>
    string TypeName { get; }

    /// <summary>Returns true if the entity has this component in the given world.</summary>
    bool HasComponent(EcsWorld world, in EntityId entityId);

    /// <summary>
    /// Writes the component value for <paramref name="entityId"/> to <paramref name="writer"/>.
    /// Called only when <see cref="HasComponent"/> returns true.
    /// </summary>
    void WriteComponent(BinaryWriter writer, EcsWorld world, in EntityId entityId);

    /// <summary>
    /// Reads a component value from <paramref name="reader"/> and queues it for addition
    /// on <paramref name="entityId"/> via <c>QueueAddComponent</c>.
    /// </summary>
    void ReadAndApply(BinaryReader reader, EcsWorld world, in EntityId entityId);
}

internal sealed class ComponentSerializer<T> : IComponentSerializer
    where T : struct, IEcsComponent
{
    private readonly Action<BinaryWriter, T> _Write;
    private readonly Func<BinaryReader, T> _Read;

    public string TypeName { get; } = typeof(T).FullName ?? typeof(T).Name;

    public ComponentSerializer(Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
    {
        _Write = write;
        _Read = read;
    }

    public bool HasComponent(EcsWorld world, in EntityId entityId)
        => world.TryGetComponent<T>(entityId, out _);

    public void WriteComponent(BinaryWriter writer, EcsWorld world, in EntityId entityId)
    {
        if (world.TryGetComponent<T>(entityId, out T component))
            _Write(writer, component);
    }

    public void ReadAndApply(BinaryReader reader, EcsWorld world, in EntityId entityId)
    {
        T component = _Read(reader);
        world.QueueAddComponent(entityId, in component);
    }
}
