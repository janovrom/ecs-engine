using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Writes a binary snapshot of an <see cref="EcsWorld"/> to a <see cref="BinaryWriter"/> (D-056).
/// </summary>
public sealed class SnapshotWriter
{
    private readonly ComponentSerializerRegistry _Registry;

    public SnapshotWriter(ComponentSerializerRegistry registry)
    {
        _Registry = registry;
    }

    /// <summary>
    /// Writes the current state of <paramref name="world"/> to <paramref name="writer"/>.
    /// When <paramref name="mode"/> is <see cref="SnapshotMode.TickBoundary"/>, there must be
    /// no pending component mutations or pending deletions at the time of the call.
    /// </summary>
    public void Write(BinaryWriter writer, EcsWorld world, SnapshotMode mode)
    {
        // Header
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write((byte)mode);
        writer.Write(world.Tick);

        // Entity list sorted ascending for deterministic output
        int[] entityIds = [.. world.AliveEntityIds.Order()];
        writer.Write(entityIds.Length);
        foreach (int id in entityIds)
            writer.Write(id);

        // Component data per registered type
        IReadOnlyList<IComponentSerializer> serializers = _Registry.Serializers;
        writer.Write(serializers.Count);

        foreach (IComponentSerializer s in serializers)
        {
            // Compute the subset of entities that carry this component
            List<int> withComponent = [.. entityIds.Where(id => s.HasComponent(world, new EntityId(id)))];

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(s.TypeName);
            writer.Write((ushort)nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(withComponent.Count);

            foreach (int id in withComponent)
            {
                writer.Write(id);
                s.WriteComponent(writer, world, new EntityId(id));
            }
        }
    }

    private const uint Magic = 0x45435353u; // "ECSS" little-endian
    private const ushort Version = 1;
}
