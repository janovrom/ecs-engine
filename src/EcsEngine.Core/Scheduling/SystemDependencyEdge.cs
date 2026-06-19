namespace EcsEngine.Core.Scheduling;

/// <summary>
/// A directed scheduling dependency edge where <see cref="From"/> executes before <see cref="To"/>.
/// </summary>
public readonly record struct SystemDependencyEdge(Type From, Type To);
