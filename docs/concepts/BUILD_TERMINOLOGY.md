# Build & Security Terminology

Formalized terms and concepts used in the ECS Engine repository setup and build process.

## Core Concepts

### Deterministic Builds
**Definition**: Builds that produce identical outputs when given the same inputs (source code, dependencies, configuration) at a specific point in time, reproducible across team members' machines and CI/CD pipelines.

**Scope in ECS Engine**: Guaranteed determinism for CI/CD environments with pinned package versions via `Directory.Packages.props`. Long-term reproducibility (6+ months) is **not guaranteed** due to package availability risk.

**Related**: Reproducibility caveat

---

### Reproducibility Caveat
**Definition**: Acknowledgment that rebuilding a specific commit from 6+ months ago may fail due to external factors (package delisting, registry availability), despite pinned versions.

**ECS Engine Policy**: Accept this risk. Do not invest in package mirroring or caching. Document the limitation for stakeholders.

---

### Software Bill of Materials (SBOM)
**Definition**: A formal, machine-readable inventory of all software dependencies (direct and transitive) included in a project, including version information and checksums.

**Format**: SPDX (Software Package Data Exchange) — standardized, widely recognized format.

**Purpose in ECS Engine**: Provide visibility into dependencies for learning, security auditing, and detecting unwanted or problematic packages.

**Generation**: Via `dotnet-cyclonedx` tool during release builds.

---

### NuGetAudit
**Definition**: Built-in .NET tool that scans all NuGet dependencies (direct and transitive) for known security vulnerabilities.

**Configuration in ECS Engine**:
- `NuGetAudit=true` (enable scanning)
- `mode=all` (scan all projects)
- `level=low` (report all severity levels, including low-risk issues)

**Purpose**: Prevent introduction of vulnerable dependencies into the codebase.

---

### License Audit
**Definition**: Systematic review and documentation of software licenses used by all dependencies.

**ECS Engine Approach**:
- Generate list of all dependency licenses (direct and transitive)
- Document licenses for visibility and compliance
- Separate from build process (non-blocking, informational)

**Audience**: Maintainer, for oversight and learning.

---

### Package Lock Strategy
**Definition**: Approach to managing transitive dependency versions to ensure reproducibility.

**ECS Engine Strategy**: 
- Use `Directory.Packages.props` as single source of truth
- Pin all versions (direct and transitive)
- No per-project `packages.lock.json` unless specific scenarios require it
- No auto-upgrade of transitive dependencies

---

### Build Artifact Organization
**Definition**: Centralized location and structure for build outputs (binaries, intermediates, reports).

**ECS Engine Approach**:
- Root-level `./artifacts/` directory
- Structure managed by SDK (auto-generates `bin/`, `obj/` subdirectories)
- SBOM outputs placed in `./artifacts/` or subdirectories
- Keeps repository root clean; no scattered intermediate files

---

### MSBuild Target
**Definition**: Named build task that can be invoked via `dotnet` CLI (e.g., `dotnet build`, `dotnet test`) or custom targets defined in project files.

**ECS Engine Philosophy**: Developer-facing tasks (build, test, audit, SBOM generation) exposed as MSBuild targets. Publish-only tasks handled in CI/CD scripts.

---

### Developer Task
**Definition**: Build operation that developers need to run locally during development (e.g., compile, unit test, local audit).

**ECS Engine Developer Tasks**:
- Build (primary)
- Test (primary)
- NuGetAudit (opt-in)
- SBOM generation (opt-in)
- AOT publish test (opt-in)

---

### Publish-Only Task
**Definition**: Build operation that runs only during release/publish workflows, not needed for day-to-day development.

**ECS Engine Publish-Only Tasks**:
- SBOM attachment to GitHub releases
- License report publication
- Package distribution

---

## Related Documentation

- [Repository Setup Decisions](REPOSITORY_SETUP_DECISIONS.md)
- [Security & Compliance Policy](../../security-and-compliance.md)
- [Build Targets & Invocation](../../build/BUILD_TARGETS.md)
