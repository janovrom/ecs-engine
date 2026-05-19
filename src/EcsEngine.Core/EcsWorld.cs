using System.Collections;

namespace EcsEngine.Core;

public sealed class EcsWorld
{
    private int _nextEntityId = 1;
    private readonly HashSet<int> _aliveEntityIds = [];
    private readonly HashSet<int> _pendingDeletionEntityIds = [];
    private readonly Dictionary<Type, IComponentStore> _componentStores = [];
    private readonly List<IComponentMutation> _pendingComponentMutations = [];
    private readonly Dictionary<Type, IList> _nextTickEvents = [];
    private readonly Dictionary<Type, IList> _currentTickEvents = [];

    public int Tick { get; private set; }

    public EntityId CreateEntity()
    {
        var entityId = new EntityId(_nextEntityId++);
        _aliveEntityIds.Add(entityId.Value);
        return entityId;
    }

    public bool Exists(EntityId entityId) => _aliveEntityIds.Contains(entityId.Value);

    public bool IsMarkedForDeletion(EntityId entityId) => _pendingDeletionEntityIds.Contains(entityId.Value);

    public bool CanSchedule(EntityId entityId) => Exists(entityId) && !IsMarkedForDeletion(entityId);

    public void MarkForDeletion(EntityId entityId)
    {
        EnsureEntityExists(entityId);
        _pendingDeletionEntityIds.Add(entityId.Value);
    }

    public void QueueAddComponent<T>(EntityId entityId, in T component)
        where T : struct, IEcsComponent
    {
        EnsureEntityCanMutate(entityId);
        _pendingComponentMutations.Add(new AddComponentMutation<T>(entityId, component));
    }

    public void QueueRemoveComponent<T>(EntityId entityId)
        where T : struct, IEcsComponent
    {
        EnsureEntityCanMutate(entityId);
        _pendingComponentMutations.Add(new RemoveComponentMutation<T>(entityId));
    }

    public bool TryGetComponent<T>(EntityId entityId, out T component)
        where T : struct, IEcsComponent
    {
        component = default;

        if (!Exists(entityId) || !_componentStores.TryGetValue(typeof(T), out var store))
        {
            return false;
        }

        return ((ComponentStore<T>)store).TryGet(entityId, out component);
    }

    public void QueueEvent<T>(in T @event)
        where T : struct, IEcsEvent
    {
        var type = typeof(T);
        if (!_nextTickEvents.TryGetValue(type, out var list))
        {
            list = new List<T>();
            _nextTickEvents[type] = list;
        }

        ((List<T>)list).Add(@event);
    }

    public IReadOnlyList<T> GetCurrentTickEvents<T>()
        where T : struct, IEcsEvent
    {
        if (_currentTickEvents.TryGetValue(typeof(T), out var list))
        {
            return (List<T>)list;
        }

        return [];
    }

    public void AdvanceTick()
    {
        Tick++;
        _currentTickEvents.Clear();

        foreach (var (type, eventsForType) in _nextTickEvents)
        {
            _currentTickEvents[type] = eventsForType;
        }

        _nextTickEvents.Clear();
    }

    public void ApplySafePoint()
    {
        foreach (var mutation in _pendingComponentMutations)
        {
            mutation.Apply(this);
        }

        _pendingComponentMutations.Clear();

        foreach (var entityIdValue in _pendingDeletionEntityIds)
        {
            _aliveEntityIds.Remove(entityIdValue);

            foreach (var store in _componentStores.Values)
            {
                store.Remove(new EntityId(entityIdValue));
            }
        }

        _pendingDeletionEntityIds.Clear();
    }

    private ComponentStore<T> GetOrCreateStore<T>()
        where T : struct, IEcsComponent
    {
        var type = typeof(T);
        if (_componentStores.TryGetValue(type, out var store))
        {
            return (ComponentStore<T>)store;
        }

        var typedStore = new ComponentStore<T>();
        _componentStores[type] = typedStore;
        return typedStore;
    }

    private interface IComponentStore
    {
        void Remove(EntityId entityId);
    }

    private sealed class ComponentStore<T> : IComponentStore
        where T : struct, IEcsComponent
    {
        private readonly Dictionary<int, T> _componentsByEntityId = [];

        public void Set(EntityId entityId, in T component) => _componentsByEntityId[entityId.Value] = component;

        public bool TryGet(EntityId entityId, out T component) => _componentsByEntityId.TryGetValue(entityId.Value, out component);

        public void Remove(EntityId entityId) => _componentsByEntityId.Remove(entityId.Value);
    }

    private interface IComponentMutation
    {
        void Apply(EcsWorld world);
    }

    private readonly record struct AddComponentMutation<T>(EntityId EntityId, T Component) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) => world.GetOrCreateStore<T>().Set(EntityId, Component);
    }

    private readonly record struct RemoveComponentMutation<T>(EntityId EntityId) : IComponentMutation
        where T : struct, IEcsComponent
    {
        public void Apply(EcsWorld world) => world.GetOrCreateStore<T>().Remove(EntityId);
    }

    private void EnsureEntityCanMutate(EntityId entityId)
    {
        EnsureEntityExists(entityId);
        if (IsMarkedForDeletion(entityId))
        {
            throw new InvalidOperationException($"{entityId} is marked for deletion and cannot accept new mutations.");
        }
    }

    private void EnsureEntityExists(EntityId entityId)
    {
        if (!Exists(entityId))
        {
            throw new InvalidOperationException($"{entityId} does not exist.");
        }
    }
}