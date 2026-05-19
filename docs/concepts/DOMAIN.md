# DOMAIN

Status: Final

## Confirmed Terms

- 3D Occupancy Grid: The simulation world is represented as occupied/unoccupied cube cells in true 3D (x, y, z), not a 2.5D heightfield.
- Climb Speed: A figure has a separate climbing-speed value used for vertical movement; this value may equal regular movement speed.
- Speed Modifiers: Movement-affecting effects including Sprint and Heavy Terrain, with configurable multiplier values.
- Modifier Stacking Rule: When Sprint and Heavy Terrain are both active, they cancel to net x1 movement multiplier.
- Movement Budget Rounding: Any fractional movement result is always rounded down (floor).
- Diagonal Move Cost: A diagonal step costs 1.5 movement units for both planar diagonals and vertical diagonals.
- Pause Snapshot Mode: Snapshot timing is configurable between deterministic tick-boundary snapshots and immediate mid-tick best-effort snapshots.
- Startup Mode Selection: A single executable selects local-only or server mode via configuration file at startup.
- Source Generator Scope (v1): Generate entity/component registration, system scheduling graph, and config-operation bindings into strongly typed commands without reflection.
- System Dependency Wiring: Dependencies between systems are authored manually in code via fluent syntax.
- Static Scheduling Graph: The system scheduling graph is fully static at compile time; fluent dependency declarations must conform to this static model.
- Deterministic Replay: Running the same configuration operations with the same seed must produce identical simulation outputs across runs.
- Hybrid ECS Storage: Hot-path components are stored in archetype-based (SoA, chunked) arrays for cache/batch performance; rare/optional components use sparse set arrays for efficient random access.
- Cube Area: A play space composed of cube cells where each cell can be selected and elevated.
- Lasso Elevation: Selecting a set of cube cells and increasing their elevation by exactly one level in a single operation.
- Figure: A movable game unit/token that follows DnD-like movement speed constraints.
- Preview Path: A mutable path visualization/edit state before movement is committed.
- Committed Path: The finalized movement path that becomes simulation input.
- AOT-Safe Runtime: Runtime behavior compatible with AOT; no reflection-based execution.
- Pause Snapshot: Ability to pause simulation and inspect current ECS state at that exact point.
- Job Batch: A schedulable group of ECS work units intended for CPU-parallel execution with allocation-free hot paths.
- In-Memory Transport: Configurable networking mode using in-process/in-memory message passing (possibly cross-thread) for current scope.
- Scripted Operations: Engine operations loaded from configuration files (no live user input in first renderer milestone).
- Adaptive Tick Policy: Simulation tick rate can change at runtime based on activity/load. All tick-rate changes are logged for replayability. Debug/test builds may override tick schedule for profiling or testing.

## Proposed Terms (awaiting confirmation)

- None.

## Notes

- Rendering is 3D in concept, but first visual milestone uses 2D SkiaSharp projection/output.
- Additional area-editing tools beyond lasso may be needed later, but are intentionally out of current interview scope.
