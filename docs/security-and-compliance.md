# Security & Compliance Policy

This document outlines the ECS Engine's approach to package security, license compliance, and build determinism.

## Package Security

### NuGetAudit
The ECS Engine uses **NuGetAudit** to scan all NuGet dependencies (direct and transitive) for known security vulnerabilities.

**Configuration**:
- Enabled in all projects via `Directory.Packages.props`
- `NuGetAudit=true`
- `NuGetAuditMode=all` (scan all projects, not just root)
- `NuGetAuditLevel=low` (report all severity levels)

**Behavior**:
- Runs during build; reports vulnerabilities as warnings/errors
- Developers must resolve or explicitly acknowledge vulnerabilities
- CI/CD enforces no builds with unresolved issues

**Frequency**: On every build (local and CI/CD)

---

## License Compliance

### License Documentation
The ECS Engine documents all dependency licenses for visibility and compliance oversight.

**Approach**:
- Separate audit step, non-blocking
- Generates list of direct and transitive dependency licenses
- Audience: Maintainer (learning, oversight of unwanted licenses)

**Implementation**:
- Executed as optional MSBuild target or CI/CD task
- Output: Human-readable license report and SBOM (SPDX format)

**License Policy**:
- No hard license blocklist currently in place
- Maintainer reviews and documents licenses for each release
- Unwanted licenses can be identified and addressed before release

---

## Deterministic Builds

### Reproducibility at CI/CD Time
The ECS Engine ensures reproducible builds for CI/CD pipelines using pinned package versions in `Directory.Packages.props`.

**Strategy**:
- All dependencies (direct and transitive) pinned to specific versions
- No per-project `packages.lock.json` (centralized control in one file)
- No automatic transitive dependency upgrades
- Single point of control for security updates

**Scope**: Reproducibility guaranteed across:
- Team members' local builds
- CI/CD pipeline runs
- Different OS platforms (Windows, Linux, macOS)

**Benefits**:
- Prevents unexpected transitive dependency changes
- Simplifies vulnerability resolution (one place to pin patches)
- Enables controlled, deliberate dependency updates

### Reproducibility Caveat: Long-Term Limitation

**⚠️ Important**: While builds are deterministic at CI/CD time, **we do not guarantee rebuilding the same commit 6+ months later** will succeed.

**Why**:
- External package repositories (NuGet.org) may delist packages
- Package maintainers may remove old versions
- Registry availability is outside our control
- We intentionally do not mirror or cache external packages

**Acceptance**:
- We accept this risk as a reasonable trade-off against infrastructure complexity
- No package caching or private mirror strategy in place
- Document this limitation for stakeholders

**Mitigation for Production**:
- For production deployments, consider caching `.nupkg` files if long-term reproducibility is critical
- This is outside the scope of the ECS Engine repository setup

---

## Software Bill of Materials (SBOM)

### SBOM Generation
The ECS Engine generates SPDX-format SBOMs for releases.

**Tool**: `dotnet-cyclonedx`

**When**: During release builds (CI/CD only)

**Content**:
- All direct dependencies
- All transitive dependencies
- Version numbers
- Version hashes (for integrity verification)

**Distribution**: Attached to GitHub releases

**Purpose**:
- Provide transparency into dependencies
- Enable security audits by stakeholders
- Support learning and visibility of dependency tree

---

## Configuration Files

### Directory.Packages.props
- **Purpose**: Centralized package version management
- **Location**: Repository root
- **Content**: All NuGet package versions (direct and transitive)
- **Inheritance**: All projects inherit from this file

### Directory.Build.props
- **Purpose**: Common build settings across all projects
- **Location**: Repository root
- **Content**: Global compiler settings, build policies

### .editorconfig
- **Purpose**: Code style and formatting consistency
- **Location**: Repository root
- **Content**: Naming conventions, indentation, whitespace rules

See [Build Configuration](build/CONFIGURATION.md) for details.

---

## Related Documentation

- [Repository Setup Decisions](docs/concepts/REPOSITORY_SETUP_DECISIONS.md)
- [Build Terminology](docs/concepts/BUILD_TERMINOLOGY.md)
- [Build Targets & Invocation](build/BUILD_TARGETS.md)
- [Build Configuration](build/CONFIGURATION.md)
