using System.Collections;
using EcsEngine.Core.Query;
using EcsEngine.Core.Storage;

namespace EcsEngine.Core;

public sealed class EcsWorld
{
    private int _nextEntityId = 1;
    private readonly HashSet<int> _AliveEntityIds = [];
    private readonly HashSet<int> _PendingDeletionEntityIds = [];
    private readonly ArchetypeRegistry _ArchetypeRegistry = new();
    private readonly Dictionary<Type, ISparseStore> _SparseStores = [];
    private readonly List<IComponentMutation> _PendingComponentMutations = [];
    private readonly Dictionary<Type, IList> _NextTickEvents = [];
    private readonly Dictionary<Type, IList> _CurrentTickEvents = [];
    private readonly QueryRegistry _QueryRegistry;

    public int Tick { get; private set; }

    public EcsWorld()
    {
        _QueryRegistry = new QueryRegistry(_ArchetypeRegistry);
    }

    public EntityId CreateEntity()
    {
        EntityId entityId = new(_nextEntityId++);
        _AliveEntityIds.Add(entityId.Value);
        return entityId;
    }

    public bool Exists(EntityId entityId) => _AliveEntityIds.Contains(entityId.Value);

    public bool IsMarkedForDeletion(EntityId entityId) => _PendingDeletionEntityIds.Contains(entityId.Value);

    public bool CanSchedule(EntityId entityId) => Exists(entityId) && !IsMarkedForDeletion(entityId);

    public void MarkForDeletion(EntityId entityId)
    {
        EnsureEntityExists(entityId);
        _PendingDeletionEntityIds.Add(entityId.Value);
    }

    public void QueueAddComponent<T>(EntityId entityId, in T component)
        where T : struct, IEcsComponent
    {
        EnsureEntityCanMutate(entityId);
        ComponentTypeInfo info = ComponentTypeRegistry.GetOrRegister<T>();
        if (info.StorageKind == StorageKind.Archetype)
            _PendingComponentMutations.Add(new ArchetypeAddMutation<T>(entityId, component));
        else
            _PendingComponentMutations.Add(new SparseAddMutation<T>(entityId, component));
    }

    public void QueueRemoveComponent<T>(EntityId entityId)
        where T : struct, IEcsComponent
    {
        EnsureEntityCanMutate(entityId);
        ComponentTypeInfo info = ComponentTypeRegistry.GetOrRegister<T>();
        if (info.StorageKind == StorageKind.Archetype)
            _PendingComponentMutations.Add(new ArchetypeRemoveMutation<T>(entityId));
        else
            _PendingComponentMutations.Add(new SparseRemoveMutation<T>(entityId));
    }

    public bool TryGetComponent<T>(EntityId entityId, out T component)
        where T : struct, IEcsComponent
    {
        component = default;
        if (!Exists(entityId)) return false;

        ComponentTypeInfo info = ComponentTypeRegistry.GetOrRegister<T>();
        if (info.StorageKind == StorageKind.Archetype)
        {
            EntityLocation loc = _ArchetypeRegistry.GetLocation(entityId);
            if (!loc.IsValid || !loc.Chunk!.HasColumn(typeof(T))) return false;
            component = loc.Chunk.GetColumn<T>()[loc.SlotIndex];
            return true;
        }

        if (_SparseStores.TryGetValue(typeof(T), out ISparseStore? store))
            return ((SparseSetStore<T>)store).TryGet(entityId, out component);
        return false;
    }

    public void QueueEvent<T>(in T @event)
        where T : struct, IEcsEvent
    {
        Type type = typeof(T);
        if (!_NextTickEvents.TryGetValue(type, out IList? list))
        {
            list = new List<T>();
            _NextTickEvents[type] = list;
        }
        ((List<T>)list).Add(@event);
    }

    public IReadOnlyList<T> GetCurrentTickEvents<T>()
        where T : struct, IEcsEvent
    {
        if (_CurrentTickEvents.TryGetValue(typeof(T), out IList? list))
            return (List<T>)list;
        return [];
    }

    public void AdvanceTick()
    {
        Tick++;
        _CurrentTickEvents.Clear();
        foreach ((Type type, IList eventsForType) in _NextTickEvents)
            _CurrentTickEvents[type] = eventsForType;
        _NextTickEvents.Clear();
    }

    public void ApplySafePoint()
    {
        foreach (IComponentMutation mutation in _PendingComponentMutations)
            mutation.Apply(this);
        _PendingComponentMutations.Clear();

        foreach (int entityIdValue in _PendingDeletionEntityIds)
        {
            EntityId entityId = new(entityIdValue);
            _ArchetypeRegistry.RemoveEntity(entityId);
            foreach (ISparseStore store in _SparseStores.Values)
                store.Remove(entityId);
            _AliveEntityIds.Remove(entityIdValue);
        }
        _PendingDeletionEntityIds.Clear();
    }

    // --- Query API ---

    public void QueryEach<T1>(QueryCallback<T1> callback)
        where T1 : struct, IEcsComponent
    {
        ComponentTypeInfo info1 = ComponentTypeRegistry.GetOrRegister<T1>();
        if (info1.StorageKind == StorageKind.Archetype)
        {
            QueryKey key = QueryKey.For<T1>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T1> col1 = chunk.GetColumn<T1>();
                    for (int i = 0; i < chunk.Count; i++)
                        callback(entities[i], in col1[i]);
                }
            }
        }
        else
        {
            if (!_SparseStores.TryGetValue(typeof(T1), out ISparseStore? store)) return;
            SparseSetStore<T1> typedStore = (SparseSetStore<T1>)store;
            Span<int> entityIds = typedStore.DenseEntityIds;
            Span<T1> data = typedStore.DenseData;
            for (int i = 0; i < typedStore.Count; i++)
                callback(new EntityId(entityIds[i]), in data[i]);
        }
    }

    public void QueryEach<T1, T2>(QueryCallback<T1, T2> callback)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
    {
        ComponentTypeInfo info1 = ComponentTypeRegistry.GetOrRegister<T1>();
        ComponentTypeInfo info2 = ComponentTypeRegistry.GetOrRegister<T2>();

        if (info1.StorageKind == StorageKind.Archetype && info2.StorageKind == StorageKind.Archetype)
        {
            QueryKey key = QueryKey.For<T1, T2>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T1> col1 = chunk.GetColumn<T1>();
                    ComponentColumn<T2> col2 = chunk.GetColumn<T2>();
                    for (int i = 0; i < chunk.Count; i++)
                        callback(entities[i], in col1[i], in col2[i]);
                }
            }
        }
        else if (info1.StorageKind == StorageKind.Archetype)
        {
            if (!_SparseStores.TryGetValue(typeof(T2), out ISparseStore? store2)) return;
            SparseSetStore<T2> typedStore2 = (SparseSetStore<T2>)store2;
            QueryKey key = QueryKey.For<T1>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T1> col1 = chunk.GetColumn<T1>();
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        if (typedStore2.TryGet(entities[i], out T2 c2))
                            callback(entities[i], in col1[i], in c2);
                    }
                }
            }
        }
        else if (info2.StorageKind == StorageKind.Archetype)
        {
            if (!_SparseStores.TryGetValue(typeof(T1), out ISparseStore? store1)) return;
            SparseSetStore<T1> typedStore1 = (SparseSetStore<T1>)store1;
            QueryKey key = QueryKey.For<T2>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T2> col2 = chunk.GetColumn<T2>();
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        if (typedStore1.TryGet(entities[i], out T1 c1))
                            callback(entities[i], in c1, in col2[i]);
                    }
                }
            }
        }
        else
        {
            if (!_SparseStores.TryGetValue(typeof(T1), out ISparseStore? store1)) return;
            if (!_SparseStores.TryGetValue(typeof(T2), out ISparseStore? store2)) return;
            SparseSetStore<T1> typedStore1 = (SparseSetStore<T1>)store1;
            SparseSetStore<T2> typedStore2 = (SparseSetStore<T2>)store2;
            Span<int> entityIds = typedStore1.DenseEntityIds;
            Span<T1> data1 = typedStore1.DenseData;
            for (int i = 0; i < typedStore1.Count; i++)
            {
                EntityId entity = new(entityIds[i]);
                if (typedStore2.TryGet(entity, out T2 c2))
                    callback(entity, in data1[i], in c2);
            }
        }
    }

    public void QueryEach<T1, T2, T3>(QueryCallback<T1, T2, T3> callback)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
    {
        ComponentTypeInfo info1 = ComponentTypeRegistry.GetOrRegister<T1>();
        ComponentTypeInfo info2 = ComponentTypeRegistry.GetOrRegister<T2>();
        ComponentTypeInfo info3 = ComponentTypeRegistry.GetOrRegister<T3>();

        if (info1.StorageKind == StorageKind.Archetype
            && info2.StorageKind == StorageKind.Archetype
            && info3.StorageKind == StorageKind.Archetype)
        {
            QueryKey key = QueryKey.For<T1, T2, T3>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T1> col1 = chunk.GetColumn<T1>();
                    ComponentColumn<T2> col2 = chunk.GetColumn<T2>();
                    ComponentColumn<T3> col3 = chunk.GetColumn<T3>();
                    for (int i = 0; i < chunk.Count; i++)
                        callback(entities[i], in col1[i], in col2[i], in col3[i]);
                }
            }
        }
        else if (info1.StorageKind == StorageKind.Archetype)
        {
            QueryKey key = QueryKey.For<T1>();
            foreach (Archetype archetype in _QueryRegistry.GetMatchingArchetypes(key))
            {
                foreach (ArchetypeChunk chunk in archetype.Chunks)
                {
                    Span<EntityId> entities = chunk.EntityIds;
                    ComponentColumn<T1> col1 = chunk.GetColumn<T1>();
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        EntityId entity = entities[i];
                        if (TryGetComponent<T2>(entity, out T2 c2) && TryGetComponent<T3>(entity, out T3 c3))
                            callback(entity, in col1[i], in c2, in c3);
                    }
                }
            }
        }
        else
        {
            if (!_SparseStores.TryGetValue(typeof(T1), out ISparseStore? store1)) return;
            SparseSetStore<T1> typedStore1 = (SparseSetStore<T1>)store1;
            Span<int> entityIds = typedStore1.DenseEntityIds;
            Span<T1> data1 = typedStore1.DenseData;
            for (int i = 0; i < typedStore1.Count; i++)
            {
                EntityId entity = new(entityIds[i]);
                if (TryGetComponent<T2>(entity, out T2 c2) && TryGetComponent<T3>(entity, out T3 c3))
                    callback(entity, in data1[i], in c2, in c3);
            }
        }
    }

    public void PreloadQuery<T1>()
        where T1 : struct, IEcsComponent
        => _QueryRegistry.Preload(QueryKey.For<T1>());

    public void PreloadQuery<T1, T2>()
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        => _QueryRegistry.Preload(QueryKey.For<T1, T2>());

    public void PreloadQuery<T1, T2, T3>()
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        => _QueryRegistry.Preload(QueryKey.For<T1, T2, T3>());

    // --- Private mutation application ---

    private void ApplyArchetypeAdd<T>(EntityId entityId, in T component)
        where T : struct, IEcsComponent
    {
        ArchetypeKey currentKey = _ArchetypeRegistry.GetCurrentKey(entityId);
        ArchetypeKey newKey = currentKey.Add(typeof(T));

        if (newKey.Equals(currentKey))
        {
            EntityLocation loc = _ArchetypeRegistry.GetLocation(entityId);
            if (loc.IsValid) loc.Chunk!.GetColumn<T>()[loc.SlotIndex] = component;
            return;
        }

        EntityLocation newLocation = _ArchetypeRegistry.MoveEntity(entityId, newKey);
        newLocation.Chunk!.GetColumn<T>()[newLocation.SlotIndex] = component;
    }

    private void ApplyArchetypeRemove<T>(EntityId entityId)
        where T : struct, IEcsComponent
    {
        ArchetypeKey currentKey = _ArchetypeRegistry.GetCurrentKey(entityId);
        if (!currentKey.Contains(typeof(T))) return;
        ArchetypeKey newKey = currentKey.Remove(typeof(T));
        _ArchetypeRegistry.MoveEntity(entityId, newKey);
    }

    private void ApplySparseAdd<T>(EntityId entityId, in T component)
        where T : struct, IEcsComponent
        => GetOrCreateSparseStore<T>().Set(entityId, component);

    private void ApplySparseRemove<T>(EntityId entityId)
        where T : struct, IEcsComponent
    {
        if (_SparseStores.TryGetValue(typeof(T), out ISparseStore? store))
            ((SparseSetStore<T>)store).Remove(entityId);
    }

    private SparseSetStore<T> GetOrCreateSparseStore<T>()
        where T : struct, IEcsComponent
    {
        Type type = typeof(T);
        if (!_SparseStores.TryGetValue(type, out ISparseStore? store))
        {
            store = new SparseSetStore<T>();
            _SparseStores[type] = store;
        }
        return (SparseSetStore<T>)store;
    }

    // --- Private mutation types ---

    private interface IComponentMutation
    {
        void Apply(EcsWorld world);
    }

    private readonly record struct ArchetypeAddMutation<T>(EntityId EntityId, T Component) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) { T c = Component; world.ApplyArchetypeAdd<T>(EntityId, in c); }
    }

    private readonly record struct ArchetypeRemoveMutation<T>(EntityId EntityId) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) => world.ApplyArchetypeRemove<T>(EntityId);
    }

    private readonly record struct SparseAddMutation<T>(EntityId EntityId, T Component) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) { T c = Component; world.ApplySparseAdd<T>(EntityId, in c); }
    }

    private readonly record struct SparseRemoveMutation<T>(EntityId EntityId) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) => world.ApplySparseRemove<T>(EntityId);
    }

    // --- Guards ---

    private void EnsureEntityCanMutate(EntityId entityId)
    {
        EnsureEntityExists(entityId);
        if (IsMarkedForDeletion(entityId))
            throw new InvalidOperationException($"{entityId} is marked for deletion and cannot accept new mutations.");
    }

    private void EnsureEntityExists(EntityId entityId)
    {
        if (!Exists(entityId))
            throw new InvalidOperationException($"{entityId} does not exist.");
    }
}
