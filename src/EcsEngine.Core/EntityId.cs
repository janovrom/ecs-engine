namespace EcsEngine.Core;

public readonly record struct EntityId(int Value)
{
    public readonly override string ToString() => $"Entity({Value})";
}
