# DECISIONS

Status: Final

## Confirmed

- D-001: v1 target domain is a Dungeons and Dragons tabletop engine.
- D-002: World editing supports lasso selection and +1 elevation operations over cube-based areas.
- D-003: Pathfinding includes speed-aware movement and editable pre-commit path.
- D-004: Runtime constraints include AOT compatibility and no reflection.
- D-005: Engine must support pause-and-inspect snapshots.
- D-006: Scheduling model is CPU-parallel job batches with zero-allocation goals in batching/execution.
- D-007: Networking transport is configurable; current non-goal is real transport implementation beyond in-memory cross-thread mode.
- D-008: Initial rendering uses 2D SkiaSharp and scripted operation playback from configuration files, with no live input.
- D-009: Performance floor is stable 60 FPS for initial scope.

- D-010: Simulation world model is true 3D occupancy (x, y, z cube occupancy), not 2.5D heightfield.
- D-011: Vertical movement cost is governed by a separate climbing-speed value per figure (which may match regular movement speed).
- D-012: Speed modifiers include Sprint and Heavy Terrain, with configurable multiplier values.
- D-013: If Sprint and Heavy Terrain both apply, they cancel out to a net x1 movement multiplier.
- D-014: Fractional movement budget results are always rounded down (floor).
- D-015: Diagonal movement steps cost 1.5 movement units, including both planar and vertical diagonals.
- D-016: Pause snapshot behavior is configurable between hard deterministic tick-boundary and immediate mid-tick best-effort modes.
- D-017: Runtime mode selection uses a single executable with configuration-file-based local-only or server startup.
- D-018: v1 source generators produce entity/component registration, system scheduling graph, and config-operation bindings as strongly typed commands with no reflection.
- D-019: System dependency configuration is authored manually in code through fluent syntax.
- D-020: The scheduling graph is fully static at compile time.
- D-021: Simulation must support deterministic replay across runs from the same config-operation input and seed.
- D-022: Tick model is adaptive (up/down) for load and activity, with all tick-rate changes logged for replay. Debug/test builds may override tick schedule for profiling or testing.
- D-023: ECS storage is hybrid: archetype-based for hot-path, sparse set for rare/optional components.
- D-024: ECS struct layout enforces explicit alignment/padding (e.g., 16/32/64-byte) for SIMD/cache, but this is transparent to users of the structs.
- D-025: Engine always auto-batches compatible jobs for maximum parallelism; no manual batch boundaries in v1.
- D-026: Job execution order is deterministic when dependencies exist; independent jobs of the same type may run in any order. Barriers can be inserted to verify completion of specific jobs.
- D-027: Components are immutable; systems replace entire component structs. For hot-path, ref structs or 'in' parameters with readonly structs are used to avoid defensive copies.
- D-028: ECS state serialization is always binary for speed/size, with a parser available to convert snapshots to text/JSON for debugging or inspection.
- D-029: Entity/component IDs are configurable (incrementing or randomized). Lookup must always be possible; assets may use internal IDs, but a mapping or static linking is required for external references.
- D-030: Entity/component deletion is deferred; when marked for deletion, they cannot be part of new jobs, but may still be processed within the same frame. Processing is preferred over extra validation.
- D-031: ECS events/messages use deferred processing; all events are queued and processed in the next tick, ensuring determinism and parallel-friendliness. UI updates render at higher rates but simulation only sees input on the next tick.
- D-032: Query API: systems declare a single query per class via `IRequire<T>` and `IReject<T>` interfaces. Source generators produce iteration methods and a central QueryRegistry. One query per system enforces single responsibility; code duplication is acceptable over SRP violations. Hierarchies are deferred.
- D-033: System execution is source-generated as a monolithic ExecuteAllSystems(world) method, ordered by the static dependency graph from fluent registration. No runtime polymorphism or public Execute() methods are exposed. Order is verifiable and deterministic.
- D-034: System error handling: No logic control flow via exceptions. Fatal exceptions (e.g., OutOfMemory) always crash. Irregular state logs and halts in debug; in release, logs (without stack trace) and skips the system based on severity.
- D-035: System hot-reload is supported in debug/dev mode only; release builds do not support hot-reload. Engine supports snapshot/restore for replay during hot-reload.
- D-036: Engine provides full metrics (system execution time, job batching efficiency, memory allocations per system/tick) using OpenTelemetry. Metrics and logs are configurable for enable/disable, publish frequency, and severity, including in release builds.
- D-037: System registration supports runtime registration (e.g., plugins/mods), in addition to compile-time generated registration.
- D-038: System dependencies are provided through constructor injection only.
- D-039: Component version migration is out of scope for v1.
- D-040: Archetype/component-set changes are applied only at safe tick points.
- D-041: ECS inspection/tooling support is available in both debug and release; release builds must not rely on runtime reflection.

- D-042: Storage selection is declared via `[ArchetypeStorage]` or `[SparseStorage]` attribute on the component struct. Undecorated components default to sparse-set. Source gen abstracts routing; users are oblivious to the storage backend.
- D-043: `[ArchetypeStorage(ChunkSizeBytes = N)]` controls chunk memory budget per component type; default is 16 KB. For a multi-component archetype, chunk capacity = min(ChunkSizeBytes across component types in archetype) / sum(SizeOf(T) for each T in archetype).
- D-044: Query parameter model: `IRequire<T>` maps to `in T` (read-only), `IOutput<T>` maps to `out T` (write replacement), `IReject<T>` is a filter with no parameter. M3 ships a permanent `world.QueryEach<T1,...>(callback)` API that source gen calls internally in M4 but is also directly usable.
- D-045: Query caching is lazy — matched-archetype lists are built on first call and invalidated when a new archetype is created. `world.PreloadQuery<T1,...>()` warms the cache eagerly to avoid first-call latency on the hot path.
- D-046: Benchmarks live in `tests/EcsEngine.Benchmarks` using BenchmarkDotNet. Invoked via `dotnet run -c Release`; not part of `dotnet test`.

- D-047: Systems are classes implementing `IEcsSystem` and optionally annotated with `[EcsSystem]`. The `[EcsSystem]` attribute is reserved for future Roslyn source generation (M10). For M4, all systems must implement `IEcsSystem`.
- D-048: Systems declare read/write access and execution ordering by overriding the static default `IEcsSystem.Configure(ISystemBuilder builder)` method. The default implementation is a no-op.
- D-049: `ISystemBuilder` fluent API — `ReadComponent<T>()`, `WriteComponent<T>()`, `RejectComponent<T>()` declare component access; `After<TSystem>()` and `Before<TSystem>()` declare execution order constraints.
- D-050: Cycle detection runs at `SystemScheduler.Build()` using Kahn's topological sort. A detected cycle throws `SystemSchedulingException` naming the involved system types. Roslyn compile-time detection is deferred to M10.
- D-051: `SystemExecutor.Run(EcsWorld)` is the single execution entry point per D-033. M4 execution is sequential. Parallel batching of independent systems is deferred to M5.
- D-052: Topological sort order is deterministic across runs: when multiple systems have no mutual ordering constraint, they are ordered by `Type.FullName` ascending.

- D-053: Op-log operations implement `IWorldOperation { void Apply(EcsWorld world); void ApplyToScheduler(TickScheduler? scheduler) { } }`. The default `ApplyToScheduler` is a no-op; only `SetTickIntervalOperation` overrides it. Each `EcsWorld` public mutating method records the equivalent operation when an `OpLog` is attached via `AttachOpLog(OpLog)`.
- D-054: `EcsWorld` exposes `public IReadOnlySet<int> AliveEntityIds` for enumeration. Internal helpers `CreateEntityWithId(int)` and `SetTick(int)` enable replay and snapshot restore without breaking the public API. Both are available to `EcsEngine.Replay` and its test project via `[InternalsVisibleTo]`.
- D-055: `TickScheduler` manages current tick interval in milliseconds. `SetInterval(int ms)` records a `SetTickIntervalOperation` to the attached op-log. `SetIntervalDirect(int ms)` is internal and sets the interval without recording (used during replay). `WorldReplayer` calls `op.ApplyToScheduler(scheduler)` for every operation; only `SetTickIntervalOperation` has a non-default implementation.
- D-056: Snapshot binary layout: 4-byte magic (`ECSS`), 2-byte version (1), 1-byte `SnapshotMode`, 4-byte tick, 4-byte entity count, N×4 entity IDs, 4-byte component-type count, then per type: 2-byte UTF-8 type-name length, UTF-8 name bytes, 4-byte entity count, per entity: 4-byte entity ID + component data. `SnapshotMode.TickBoundary` requires no pending mutations at write time; `SnapshotMode.Immediate` is always allowed. On read, unknown type names throw `InvalidDataException`.
- D-057: `ComponentSerializerRegistry` maps `Type → IComponentSerializer` via user-registered typed delegates (`Action<BinaryWriter, T>` write + `Func<BinaryReader, T>` read). Serializers are iterated in deterministic order (sorted by `Type.FullName`). On snapshot read, all component adds are queued then committed with a single `ApplySafePoint`.
- D-058: `WorldHasher` computes a deterministic `ulong` using FNV-1a over: tick, alive entity IDs sorted ascending, then for each registered `ComponentHasherRegistry` entry (sorted by type name): per entity with that component (sorted by entity ID) hashed with a user-supplied `Func<T, uint>`. This is AOT-safe with no reflection on the hash path.

## Deferred Scope

- S-001: Additional area-editing tools beyond lasso are acknowledged and deferred for later design.

## Out Of Scope For v1

- O-001: Advanced area-editing tools beyond lasso selection and +1 elevation operation.
- O-002: Component schema/version migration support.
- O-003: Production/release hot-reload of systems (debug/dev only for v1).

## Pending Confirmation

- None.
