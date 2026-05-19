namespace EcsEngine.Core;

public readonly record struct EntityId(int Value)
{
    public override string ToString() => $"Entity({Value})";
}
