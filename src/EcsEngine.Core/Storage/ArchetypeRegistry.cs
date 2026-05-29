namespace EcsEngine.Core.Storage;

internal sealed class ArchetypeRegistry
{
    private readonly Dictionary<ArchetypeKey, Archetype> _Archetypes = [];
    private readonly Dictionary<int, EntityLocation> _EntityLocations = [];

    public event Action<Archetype>? ArchetypeCreated;

    public IReadOnlyDictionary<ArchetypeKey, Archetype> AllArchetypes => _Archetypes;

    public EntityLocation GetLocation(EntityId entity)
        => _EntityLocations.TryGetValue(entity.Value, out EntityLocation loc) ? loc : EntityLocation.None;

    public ArchetypeKey GetCurrentKey(EntityId entity)
    {
        EntityLocation loc = GetLocation(entity);
        return loc.IsValid ? loc.Chunk!.Owner.Key : ArchetypeKey.Empty;
    }

    // Moves entity to newKey, copying shared components from its current location.
    // If newKey is empty, removes entity from archetype storage entirely.
    public EntityLocation MoveEntity(EntityId entity, ArchetypeKey newKey)
    {
        EntityLocation oldLocation = GetLocation(entity);
        ArchetypeKey oldKey = oldLocation.IsValid ? oldLocation.Chunk!.Owner.Key : ArchetypeKey.Empty;

        if (newKey.Equals(oldKey))
            return oldLocation;

        if (newKey.ComponentTypes.Count == 0)
        {
            if (oldLocation.IsValid)
                RemoveFromChunk(oldLocation);
            _EntityLocations.Remove(entity.Value);
            return EntityLocation.None;
        }

        Archetype newArchetype = GetOrCreateArchetype(newKey);
        (ArchetypeChunk newChunk, int newSlot) = newArchetype.AddEntity(entity);

        if (oldLocation.IsValid)
        {
            ArchetypeChunk oldChunk = oldLocation.Chunk!;
            foreach (Type type in newArchetype.Key.ComponentTypes)
            {
                if (oldChunk.HasColumn(type))
                    oldChunk.GetColumn(type).CopyTo(oldLocation.SlotIndex, newChunk.GetColumn(type), newSlot);
            }
            RemoveFromChunk(oldLocation);
        }

        EntityLocation newLocation = new(newChunk, newSlot);
        _EntityLocations[entity.Value] = newLocation;
        return newLocation;
    }

    public void RemoveEntity(EntityId entity)
    {
        if (!_EntityLocations.TryGetValue(entity.Value, out EntityLocation loc))
            return;
        RemoveFromChunk(loc);
        _EntityLocations.Remove(entity.Value);
    }

    private void RemoveFromChunk(EntityLocation location)
    {
        EntityId swapped = location.Chunk!.RemoveAt(location.SlotIndex);
        if (swapped.Value != 0)
            _EntityLocations[swapped.Value] = new EntityLocation(location.Chunk, location.SlotIndex);
    }

    private Archetype GetOrCreateArchetype(ArchetypeKey key)
    {
        if (!_Archetypes.TryGetValue(key, out Archetype? archetype))
        {
            archetype = CreateArchetype(key);
            _Archetypes[key] = archetype;
            ArchetypeCreated?.Invoke(archetype);
        }
        return archetype;
    }

    private static Archetype CreateArchetype(ArchetypeKey key)
    {
        Dictionary<Type, Func<int, IComponentColumn>> factories = [];
        int chunkSizeBytes = int.MaxValue;
        int totalSizePerEntity = 0;

        foreach (Type type in key.ComponentTypes)
        {
            if (ComponentTypeRegistry.TryGet(type, out ComponentTypeInfo info))
            {
                factories[type] = info.ColumnFactory;
                chunkSizeBytes = Math.Min(chunkSizeBytes, info.ChunkSizeBytes);
                totalSizePerEntity += info.SizeBytes;
            }
        }

        if (chunkSizeBytes == int.MaxValue)
            chunkSizeBytes = ArchetypeStorageAttribute.DefaultChunkSizeBytes;

        int chunkCapacity = Math.Max(1, chunkSizeBytes / Math.Max(1, totalSizePerEntity));
        return new Archetype(key, chunkCapacity, factories);
    }
}
