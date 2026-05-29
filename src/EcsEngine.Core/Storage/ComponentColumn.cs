namespace EcsEngine.Core.Storage;

internal sealed class ComponentColumn<T> : IComponentColumn
    where T : struct, IEcsComponent
{
    private readonly T[] _Data;

    public ComponentColumn(int capacity)
    {
        _Data = new T[capacity];
    }

    public ref T this[int index] => ref _Data[index];

    public Span<T> GetSpan(int count) => _Data.AsSpan(0, count);

    public void CopyTo(int sourceSlot, IComponentColumn dest, int destSlot)
        => ((ComponentColumn<T>)dest)[destSlot] = _Data[sourceSlot];

    public void RemoveAt(int slot, int lastSlot)
        => _Data[slot] = _Data[lastSlot];

    public void Clear(int slot)
        => _Data[slot] = default;
}
