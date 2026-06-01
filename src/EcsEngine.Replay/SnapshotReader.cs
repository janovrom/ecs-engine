using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Reads a binary snapshot produced by <see cref="SnapshotWriter"/> and restores
/// an <see cref="EcsWorld"/> (D-056, D-057).
/// </summary>
public sealed class SnapshotReader
{
    private readonly ComponentSerializerRegistry _Registry;

    public SnapshotReader(ComponentSerializerRegistry registry)
    {
        _Registry = registry;
    }

    /// <summary>
    /// Reads from <paramref name="reader"/> and returns a restored world.
    /// Throws <see cref="InvalidDataException"/> if the stream contains an unknown
    /// component type name or a malformed header.
    /// </summary>
    public EcsWorld Read(BinaryReader reader)
    {
        // Validate header
        uint magic = reader.ReadUInt32();
        if (magic != Magic)
            throw new InvalidDataException($"Invalid snapshot magic 0x{magic:X8}. Expected 0x{Magic:X8}.");

        ushort version = reader.ReadUInt16();
        if (version != Version)
            throw new InvalidDataException($"Unsupported snapshot version {version}. Expected {Version}.");

        reader.ReadByte(); // SnapshotMode — recorded for informational purposes; not enforced on read
        int tick = reader.ReadInt32();

        EcsWorld world = new();
        world.SetTick(tick);

        // Restore entity set
        int entityCount = reader.ReadInt32();
        for (int i = 0; i < entityCount; i++)
        {
            int id = reader.ReadInt32();
            world.CreateEntityWithId(id);
        }

        // Restore components
        int componentTypeCount = reader.ReadInt32();
        for (int t = 0; t < componentTypeCount; t++)
        {
            ushort nameLen = reader.ReadUInt16();
            byte[] nameBytes = reader.ReadBytes(nameLen);
            string typeName = System.Text.Encoding.UTF8.GetString(nameBytes);
            int entityCountForType = reader.ReadInt32();

            if (!_Registry.TryGetSerializer(typeName, out IComponentSerializer? serializer))
                throw new InvalidDataException(
                    $"No serializer registered for component type '{typeName}'. " +
                    "Register one via ComponentSerializerRegistry.Register<T>() before reading.");

            for (int i = 0; i < entityCountForType; i++)
            {
                int entityId = reader.ReadInt32();
                serializer!.ReadAndApply(reader, world, new EntityId(entityId));
            }
        }

        // Commit all queued component additions in one safe-point
        world.ApplySafePoint();

        return world;
    }

    private const uint Magic = 0x45435353u;
    private const ushort Version = 1;
}
