# Build Targets & Invocation

Available build targets for local development and CI/CD.

## Primary Developer Tasks

### Build
Compiles all projects and applies project policies (warnings-as-errors, NuGetAudit, style checks).

```bash
dotnet build EcsEngine.slnx
```

**Output**: Compiled binaries in `./artifacts/bin/`

**Includes**:
- Compilation
- NuGetAudit vulnerability scan
- .editorconfig style validation
- TreatWarningsAsErrors enforcement

---

### Test
Runs all unit tests and integration tests (NUnit framework).

```bash
dotnet test EcsEngine.slnx
```

**Output**: Test results, coverage info (if configured)

**Projects**:
- `EcsEngine.Core.Tests` (unit tests with NSubstitute for mocking)
- `EcsEngine.Integration.Tests` (integration tests without mocking)

---

## Optional Developer Tasks

### NuGetAudit (Standalone)
Explicitly run vulnerability scanning (normally included in build).

```bash
dotnet build EcsEngine.slnx /p:NuGetAudit=true
```

**Output**: Console warnings for any vulnerabilities found

---

### SBOM Generation (Local)
Generate SPDX-format Software Bill of Materials locally.

```bash
dotnet tool install --global CycloneDX.Cli
dotnet CycloneDX EcsEngine.slnx --output ./artifacts/sbom.spdx.json
```

**Output**: `./artifacts/sbom.spdx.json` (SPDX JSON format)

**Notes**:
- Requires `dotnet-cyclonedx` tool (normally in CI/CD only)
- Includes direct and transitive dependencies with hashes

---

### License Audit (Local)
Generate human-readable license report.

```bash
dotnet build EcsEngine.slnx /p:GenerateLicenseReport=true
```

**Output**: License report in `./artifacts/` (format TBD)

**Scope**: Direct and transitive dependencies

---

### AOT Publish Test
Verify Ahead-of-Time compilation compatibility (release builds only).

```bash
dotnet publish src/EcsEngine.Simulation -c Release -r linux-x64 /p:PublishAot=true
```

**Output**: AOT-compiled executable in `./artifacts/publish/`

**Validates**:
- No reflection in hot paths
- AOT-compatible code patterns
- Constructor injection only

---

## CI/CD Publish Tasks

### Generate SBOM (Release Build)
Invoked during GitHub Actions release workflow.

```yaml
# In .github/workflows/release.yml
- name: Generate SBOM
  run: |
    dotnet tool install --global CycloneDX.Cli
    dotnet CycloneDX EcsEngine.slnx --output ./artifacts/sbom.spdx.json
```

**Output**: `sbom.spdx.json` artifact

---

### Attach SBOM to Release
Invoked during GitHub Actions release workflow.

```yaml
# In .github/workflows/release.yml
- name: Attach SBOM to Release
  uses: actions/upload-release-asset@v1
  with:
    asset_path: ./artifacts/sbom.spdx.json
    asset_name: sbom.spdx.json
    asset_content_type: application/json
```

---

### Generate License Report (Release Build)
Invoked during GitHub Actions release workflow.

```yaml
# In .github/workflows/release.yml
- name: Generate License Report
  run: |
    dotnet build EcsEngine.slnx /p:GenerateLicenseReport=true
```

**Output**: License report in `./artifacts/`

---

## Common Workflows

### Local Development Loop
```bash
# Build with audit
dotnet build EcsEngine.slnx

# Run tests
dotnet test EcsEngine.slnx

# Optional: Generate SBOM locally for inspection
dotnet tool install --global CycloneDX.Cli
dotnet CycloneDX EcsEngine.slnx --output ./artifacts/sbom.spdx.json

# Optional: Test AOT compatibility
dotnet publish src/EcsEngine.Simulation -c Release -r linux-x64 /p:PublishAot=true
```

### Adding a New Package
1. Update `Directory.Packages.props` with desired version
2. Run `dotnet build` to trigger NuGetAudit scan
3. Resolve any vulnerabilities before committing
4. SBOM and license audit happen in CI/CD

### Updating Dependencies
1. Identify new versions in `Directory.Packages.props`
2. Run `dotnet build` to validate
3. Run `dotnet test` to ensure compatibility
4. CI/CD will generate updated SBOM and license report on release

---

## Related Documentation

- [Build Configuration](CONFIGURATION.md)
- [Security & Compliance](../security-and-compliance.md)
- [Build Terminology](../concepts/BUILD_TERMINOLOGY.md)
