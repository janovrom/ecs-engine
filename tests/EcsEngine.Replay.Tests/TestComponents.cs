using EcsEngine.Core;

namespace EcsEngine.Replay.Tests;

// Shared component types for all tests in this assembly.
// These must be at least internal (not private) so the source generator can
// emit DeterministicHasher<T> implementations and register them via [ModuleInitializer].

[ArchetypeStorage]
internal readonly record struct Position(float X, float Y) : IEcsComponent;

internal readonly record struct Health(int Value) : IEcsComponent;

internal readonly record struct Tag(int Id) : IEcsComponent;
