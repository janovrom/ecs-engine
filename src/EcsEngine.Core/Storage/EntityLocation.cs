namespace EcsEngine.Core.Storage;

internal readonly struct EntityLocation
{
    public readonly ArchetypeChunk? Chunk;
    public readonly int SlotIndex;

    public static readonly EntityLocation None = new(null, -1);

    public EntityLocation(ArchetypeChunk? chunk, int slotIndex)
    {
        Chunk = chunk;
        SlotIndex = slotIndex;
    }

    public bool IsValid => Chunk is not null;
}
