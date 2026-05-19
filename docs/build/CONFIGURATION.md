# Build Configuration

Central configuration files that control build behavior, dependencies, and code style.

## Directory.Packages.props
**Location**: Repository root  
**Scope**: All projects in the solution

**Purpose**: Centralized package version management. All NuGet package versions (direct and transitive) are pinned here, ensuring deterministic builds and simplified dependency updates.

**Example Structure**:
```xml
<Project>
  <ItemGroup>
    <!-- Direct dependencies -->
    <PackageVersion Include="NUnit" Version="4.4.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <!-- Transitive dependencies (if not auto-managed) -->
    <PackageVersion Include="SomeTransitive" Version="1.0.0" />
  </ItemGroup>
  
  <!-- Build policies -->
  <PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
</Project>
```

**Key Features**:
- **Deterministic versioning**: No floating versions (e.g., `1.0.*`)
- **Transitive pinning**: All dependencies explicitly versioned
- **NuGetAudit enabled**: Vulnerability scanning on every build

**Updating Packages**:
1. Edit desired package version in this file
2. Run `dotnet build` to trigger NuGetAudit
3. Resolve vulnerabilities if any
4. Commit changes

---

## Directory.Build.props
**Location**: Repository root  
**Scope**: All projects in the solution

**Purpose**: Common MSBuild properties inherited by all projects (compiler settings, build policies, output configuration).

**Example Structure**:
```xml
<Project>
  <PropertyGroup>
    <!-- Artifact output -->
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>./artifacts</ArtifactsPath>
    
    <!-- Compiler strictness -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    
    <!-- Code analysis -->
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

**Key Features**:
- **Centralized artifacts**: All outputs (binaries, intermediates) go to `./artifacts/`
- **Warnings as errors**: No compilation warnings allowed
- **Nullable reference types**: Enabled for null-safety
- **Latest language features**: Enables modern C# syntax

**When to Update**:
- Require new analyzer rules
- Change artifact layout
- Update compiler strictness policies
- Modify .NET language version requirements

---

## .editorconfig
**Location**: Repository root  
**Scope**: All files (with language-specific sections)

**Purpose**: Enforce consistent code style and formatting across the codebase.

**Example Structure**:
```ini
root = true

# All files
[*]
charset = utf-8
end_of_line = lf
trim_trailing_whitespace = true
insert_final_newline = true

# C# files
[*.cs]
indent_style = space
indent_size = 4

# Naming rules
dotnet_naming_style.readonly_pascal_case.capitalization = pascal_case
dotnet_naming_style.readonly_pascal_case.required_prefix = _
```

**Key Sections**:
- **Indentation & whitespace**: Consistent formatting
- **Naming conventions**: Fields, properties, classes, interfaces
- **New expression style**: Simplified `new()` syntax
- **Collection initializers**: Prefer `[]` over `new List<T>()`
- **Type explicitness**: Avoid `var` in favor of explicit types
- **Namespace declarations**: File-scoped namespaces only

**Enforcement**:
- IDE (Visual Studio, VS Code) honors rules for formatting
- Roslyn analyzers enforce naming conventions
- TreatWarningsAsErrors in Directory.Build.props catches violations

**See Also**: [Code Style Guide](.copilot-instructions.md)

---

## Project-Level Configuration

### .csproj (EcsEngine.Core)
**Example**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Key Settings**:
- **TargetFramework**: .NET 10 (AOT-compatible)
- **Nullable**: Inherits from Directory.Build.props (enable)
- **Package references**: Inherit versions from Directory.Packages.props

### Test Projects (.csproj)
**Key Differences**:
- **Framework**: `net10.0` (same as main projects)
- **IsTestProject**: `true` (allows test-specific settings)
- **Package references**: NUnit, NSubstitute (versions from Directory.Packages.props)

---

## Build Outputs

### Directory Structure
```
./artifacts/
├── bin/
│   ├── EcsEngine.Core/
│   │   └── debug/
│   ├── EcsEngine.Simulation/
│   └── ...
├── obj/
│   ├── EcsEngine.Core/
│   └── ...
└── (SBOM, licenses, etc. for release builds)
```

- **bin/**: Compiled binaries (.dll, .exe)
- **obj/**: Intermediate build artifacts
- **Root level**: Release artifacts (SBOM, license reports)

---

## Policy Enforcement

### Build-Time Checks
1. **Compilation**: C# syntax validation, analyzer rules
2. **NuGetAudit**: Vulnerability scan against NVD database
3. **Warnings as Errors**: No compilation warnings allowed
4. **Code style**: .editorconfig naming and formatting rules
5. **AOT validation** (for simulation): Reflection usage checks

### CI/CD Checks
- All build-time checks + additional analysis
- SBOM generation and attachment
- License report generation
- Cross-platform validation (Windows, Linux)

---

## Related Documentation

- [Build Targets & Invocation](BUILD_TARGETS.md)
- [Security & Compliance](../security-and-compliance.md)
- [Code Style Guide](../.copilot-instructions.md)
