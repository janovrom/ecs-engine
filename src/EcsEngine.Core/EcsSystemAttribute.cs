namespace EcsEngine.Core;

/// <summary>
/// Marks a class as an ECS system. Reserved for Roslyn source generation (M10).
/// For M4, all systems must implement <see cref="IEcsSystem"/> directly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EcsSystemAttribute : Attribute { }
