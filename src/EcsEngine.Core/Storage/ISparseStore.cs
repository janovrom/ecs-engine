namespace EcsEngine.Core.Storage;

internal interface ISparseStore
{
    void Remove(in EntityId entityId);
}
