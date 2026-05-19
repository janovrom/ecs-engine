# Repository Setup & .NET Configuration Decisions

**Date**: May 20, 2026  
**Context**: Design interview for clean, centralized build processes with strong security and compliance policies.

## Decision Summary

### D-101: Centralized Package Management
**Decision**: Use `Directory.Packages.props` as the single source of truth for all package versions.

**Rationale**: 
- Ensures deterministic builds across team and CI/CD
- Single point of control for dependency security updates
- Simplifies package upgrade workflow

**Status**: ✅ Already implemented

---

### D-102: NuGetAudit for Vulnerability Detection
**Decision**: Enable NuGetAudit with `mode=all` and `level=low` in all project files.

**Rationale**:
- Scans all dependencies (direct and transitive) for known vulnerabilities
- Strict level (low) catches all reported issues
- Protects against supply-chain attacks

**Status**: ✅ Already implemented

---

### D-103: License Compliance via Audit Step
**Decision**: Document all direct and transitive dependencies' licenses in a separate audit step.

**Approach**:
- Generated during audit, not blocking builds
- Reports on licenses for visibility
- Identifies unwanted dependencies
- Audience: Project maintainer, for learning and oversight

**Implementation**: Separate audit task (documented in docs/build)

**Status**: 🔄 To implement

---

### D-104: Deterministic Builds (CI/CD Scope)
**Decision**: Ensure reproducible builds for CI/CD pipelines; accept risk for 6+ month reproduction.

**Details**:
- Lock file strategy: Rely on `Directory.Packages.props`, no per-project `packages.lock.json`
- Reproducibility scope: Guaranteed across team/CI pipelines with pinned versions
- Risk acceptance: NuGet packages may be delisted; acknowledge inability to guarantee rebuild after months/years
- Transitive dependency policy: Pinned via `Directory.Packages.props`, no auto-upgrades

**Caveat**: Document in docs/security-and-compliance.md

**Status**: ✅ Partial (needs documentation caveat)

---

### D-105: SBOM Generation & Distribution
**Decision**: Generate SPDX-format Software Bill of Materials and attach to GitHub releases.

**Details**:
- **Tool**: `dotnet-cyclonedx`
- **When**: During CI/CD release builds
- **Scope**: Direct and transitive dependencies
- **Content**: Include version hashes for integrity
- **Audience**: Maintainer (learning, visibility of unwanted dependencies)
- **Distribution**: Attached to GitHub releases

**Status**: 🔄 To implement

---

### D-106: Build Artifact Organization
**Decision**: Centralize outputs to `./artifacts/` at repository root via `UseArtifactsOutput=true`.

**Details**:
- Structure: `./artifacts/bin/`, `./artifacts/obj/`, etc. (auto-managed by SDK)
- SBOM outputs: Placed in `./artifacts/` or subfolder as needed
- No scattering of intermediate files across project directories

**Status**: ✅ Already implemented

---

### D-107: Developer vs. Publish Task Separation
**Decision**: Implement task responsibilities as follows:

| Task | Responsibility | How |
|------|-----------------|-----|
| Build | Developer | MSBuild target, developers run via `dotnet build` |
| Test | Developer | MSBuild target, developers run via `dotnet test` |
| NuGetAudit | Developer (opt-in) | MSBuild target, runnable locally |
| SBOM Generation | Developer (opt-in) | MSBuild target, runnable locally |
| AOT Publish Test | Developer (opt-in) | MSBuild target, runnable locally |
| SBOM Attachment to Releases | Publish Only | GitHub Actions script |
| License Report Publication | Publish Only | GitHub Actions script |

**Rationale**: Developers can verify/debug all outputs locally; CI/CD owns release distribution.

**Status**: 🔄 To implement

---

### D-108: External Tooling Installation
**Decision**: Install external build tools in CI/CD only; document in README.

**Tools**:
- `dotnet-cyclonedx` (SBOM generation)
- NuGetAudit (built-in, no install needed)

**Approach**:
- Local tool manifest (`.config/dotnet-tools.json`) in CI/CD runner
- Developers do not install globally; tasks fail gracefully with instructions
- README documents tool requirements and versions

**Status**: 🔄 To implement

---

### D-109: Documentation Structure
**Decision**: Organize docs as follows:

| Document | Purpose | Location |
|----------|---------|----------|
| README.md | Project overview, quick-start, environments | Root |
| docs/setup/ | Zero-friction local setup instructions | docs/setup/README.md |
| docs/build/BUILD_TARGETS.md | Available MSBuild targets, invocation examples | docs/build/ |
| docs/build/CONFIGURATION.md | Build configuration files (props, editorconfig) | docs/build/ |
| docs/security-and-compliance.md | NuGetAudit policy, license compliance, reproducibility caveat | docs/ |
| docs/concepts/REPOSITORY_SETUP_DECISIONS.md | This file: design decisions and rationale | docs/concepts/ |
| docs/concepts/BUILD_TERMINOLOGY.md | Glossary: deterministic builds, SBOM, NuGetAudit, etc. | docs/concepts/ |

**Status**: 🔄 To implement

---

## Next Steps

1. ✅ Formalize terminology in docs/concepts/BUILD_TERMINOLOGY.md
2. ⏳ Create docs/security-and-compliance.md with reproducibility caveat
3. ⏳ Create docs/build/BUILD_TARGETS.md with available targets
4. ⏳ Create docs/build/CONFIGURATION.md explaining props files
5. ⏳ Update CI/CD workflows to generate SBOM
6. ⏳ Update README with tool requirements
