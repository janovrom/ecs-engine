namespace EcsEngine.Core.Storage;

internal interface IComponentColumn
{
    void CopyTo(int sourceSlot, IComponentColumn dest, int destSlot);
    void RemoveAt(int slot, int lastSlot);
    void Clear(int slot);
}
