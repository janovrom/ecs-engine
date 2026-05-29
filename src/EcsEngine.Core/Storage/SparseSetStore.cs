namespace EcsEngine.Core.Storage;

internal sealed class SparseSetStore<T> : ISparseStore
    where T : struct, IEcsComponent
{
    private const int InitialCapacity = 16;

    private int[] _Sparse = new int[InitialCapacity];
    private int[] _DenseEntityIds = new int[InitialCapacity];
    private T[] _DenseData = new T[InitialCapacity];
    private int _Count;

    public SparseSetStore()
    {
        _Sparse.AsSpan().Fill(-1);
    }

    public int Count => _Count;
    public Span<int> DenseEntityIds => _DenseEntityIds.AsSpan(0, _Count);
    public Span<T> DenseData => _DenseData.AsSpan(0, _Count);

    public bool TryGet(EntityId entity, out T component)
    {
        int id = entity.Value;
        if (id >= _Sparse.Length || _Sparse[id] < 0)
        {
            component = default;
            return false;
        }
        component = _DenseData[_Sparse[id]];
        return true;
    }

    public void Set(EntityId entity, in T component)
    {
        int id = entity.Value;
        EnsureSparseCapacity(id + 1);

        if (_Sparse[id] >= 0)
        {
            _DenseData[_Sparse[id]] = component;
            return;
        }

        EnsureDenseCapacity(_Count + 1);
        _Sparse[id] = _Count;
        _DenseEntityIds[_Count] = id;
        _DenseData[_Count] = component;
        _Count++;
    }

    public void Remove(EntityId entity)
    {
        int id = entity.Value;
        if (id >= _Sparse.Length || _Sparse[id] < 0)
            return;

        int denseIndex = _Sparse[id];
        int last = _Count - 1;

        if (denseIndex != last)
        {
            int swappedId = _DenseEntityIds[last];
            _DenseEntityIds[denseIndex] = swappedId;
            _DenseData[denseIndex] = _DenseData[last];
            _Sparse[swappedId] = denseIndex;
        }

        _Sparse[id] = -1;
        _DenseEntityIds[last] = 0;
        _DenseData[last] = default;
        _Count--;
    }

    private void EnsureSparseCapacity(int needed)
    {
        if (needed <= _Sparse.Length) return;
        int oldSize = _Sparse.Length;
        int newSize = Math.Max(needed, oldSize * 2);
        Array.Resize(ref _Sparse, newSize);
        _Sparse.AsSpan(oldSize).Fill(-1);
    }

    private void EnsureDenseCapacity(int needed)
    {
        if (needed <= _DenseData.Length) return;
        int newSize = Math.Max(needed, _DenseData.Length * 2);
        Array.Resize(ref _DenseEntityIds, newSize);
        Array.Resize(ref _DenseData, newSize);
    }
}
