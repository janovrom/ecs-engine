# Implementation Plan

Status: Active

This plan reflects finalized decisions in `docs/concepts/DECISIONS.md` and domain terms in `docs/concepts/DOMAIN.md`.

## Development Constraints

1. Version control: repository is hosted in Git; all milestone deliveries are tracked as auditable Git changes.
2. Supported development environments: Linux Fedora 43 and Windows 11 are first-class and must both stay green.
3. Container tooling: Podman is available on Linux and Docker on Windows; use as optional runtime/tooling wrappers where environment parity helps.
4. Milestone reporting: every completed milestone must produce a review report under `docs/reports` (for example `docs/reports/M1.md`).
5. Between-milestone edits: manual changes are allowed; milestone reports include a short delta summary of manual changes since the previous milestone.

## Milestone Completion Protocol

1. Validate milestone exit criteria.
2. Run cross-platform checks for Fedora 43 and Windows 11.
3. Generate or update the milestone report in `docs/reports` with: scope delivered, validation evidence, perf or allocation summary when relevant, and manual-change delta.
4. Mark milestone complete only after report review.

## Milestones

1. M1 - Repository bootstrap and quality gates
- Scope: solution and project layout, CI baseline, release AOT checks.
- Exit criteria: solution builds, baseline tests execute, CI enforces debug and release plus AOT publish checks.

2. M2 - Deterministic ECS core lifecycle
- Scope: world model, entity identity, immutable component contract, safe-point archetype changes, deferred delete and events.
- Exit criteria: lifecycle tests pass, safe-point mutations validated, no mutation outside safe points.

3. M3 - Hybrid storage and query substrate
- Scope: archetype SoA chunks and sparse-set optional storage, alignment and padding enforcement, one-query-per-system semantics, query caching and indexing.
- Exit criteria: storage and query tests green, hot-path allocation budget at zero in benchmarks.

4. M4 - Static scheduling and generated execution
- Scope: fluent dependency declarations, compile-time graph validation, generated ExecuteAllSystems(world), auto-batching and barrier semantics.
- Exit criteria: cycle detection tests pass, deterministic order reproducible across runs, generated executor used as single execution entry point.

5. M5 - Replay, snapshots, adaptive tick determinism
- Scope: binary op-log and seed replay, pause snapshot modes, adaptive tick changes with mandatory logging.
- Exit criteria: repeated replay hash equivalence, snapshot roundtrip tests pass, adaptive tick logs reproduce identically.

6. M6 - Host and runtime extensibility
- Scope: constructor-only DI, runtime system registration window, startup mode config (local/server), in-memory transport.
- Exit criteria: startup mode tests pass, runtime registration deterministic, transport integration tests pass.

7. M7 - Domain vertical slice (D&D gameplay rules)
- Scope: occupancy grid, lasso +1 terrain elevation, movement and climb logic, diagonal costs, modifier stacking and rounding, preview and commit path flow, scripted operations.
- Exit criteria: domain rule tests match decisions, end-to-end scripted scenario passes.

8. M8 - Rendering and developer tooling
- Scope: 3D occupancy to 2D SkiaSharp projection, snapshot inspector (binary to text/json), schedule visualization tooling.
- Exit criteria: rendering integration tests pass, tooling outputs validated, release execution path remains reflection-free.

9. M9 - Observability and performance budgets
- Scope: OpenTelemetry metrics for system timing, batching efficiency, allocations, configurable enablement, publish cadence, severity in release.
- Exit criteria: metrics emitted as configured, performance baselines recorded, 60 FPS floor and zero-allocation hot-path targets validated.

10. M10 - Release hardening and v1 sign-off
- Scope: NativeAOT verification, no-reflection release-path audit, determinism soak tests, release checklist closure.
- Exit criteria: AOT publish and run clean, soak tests stable, D-001 through D-041 implementation checks completed.

## Parallelization Model

1. Parallel track A: M8 can progress once M5 stabilizes state and snapshot contracts.
2. Parallel track B: M9 can begin once M4 exposes stable scheduling and execution hooks.
3. Blocking path: M1 through M7 define the critical architecture chain and should not be reordered.

## Verification Gates

1. Foundation gate: solution builds cleanly, tests run, and release build pipeline is green.
2. Cross-platform gate: each milestone validates on Fedora 43 and Windows 11.
3. Storage gate: allocation profiling confirms zero allocations in hot query and system paths.
4. Scheduling gate: dependency graph validation rejects cycles and produces deterministic execution ordering.
5. Replay gate: repeated seeded runs produce identical state hashes and identical adaptive tick logs.
6. Domain gate: movement, pathfinding, and terrain rules match documented decisions.
7. Observability gate: OpenTelemetry outputs required metrics and remains configurable in release.
8. AOT gate: NativeAOT publish and run passes with no release-path reflection regressions.
9. Performance gate: sustained 60 FPS floor achieved in representative v1 scenario.
10. Reporting gate: milestone report exists in `docs/reports` and includes validation evidence plus manual-change delta.

## Scope Boundaries

Included:
- All confirmed decisions D-001 through D-041.

Excluded:
- O-001 advanced area-edit tools beyond lasso and +1 elevation.
- O-002 component schema and version migration.
- O-003 production release hot-reload of systems.
