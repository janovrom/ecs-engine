# Local Setup

Get the ECS Engine running locally in seconds.

## Prerequisites

- **.NET SDK 10**: [Download](https://dotnet.microsoft.com/download)
- **Git**: For cloning the repository
- **Supported OS**: Windows, Linux, macOS

## Quick Start

### Clone & Build
```bash
git clone https://github.com/janovrom/ecs-engine.git
cd ecs-engine
dotnet build EcsEngine.slnx
```

### Run Tests
```bash
dotnet test EcsEngine.slnx
```

**That's it!** No additional configuration needed.

## Common Commands

```bash
# Build everything
dotnet build EcsEngine.slnx

# Run all tests
dotnet test EcsEngine.slnx

# Run specific test project
dotnet test tests/EcsEngine.Core.Tests/EcsEngine.Core.Tests.csproj

# Build in Release mode
dotnet build EcsEngine.slnx -c Release

# Clean build artifacts
dotnet clean EcsEngine.slnx
```

## Project Structure

```
ecs-engine/
├── src/
│   ├── EcsEngine.Core/           # Core ECS engine
│   └── EcsEngine.Simulation/     # Example simulation
├── tests/
│   ├── EcsEngine.Core.Tests/     # Unit tests
│   └── EcsEngine.Integration.Tests/
├── docs/
│   ├── build/                    # Build documentation
│   ├── setup/                    # This file
│   ├── concepts/                 # Design decisions & terminology
│   └── reports/                  # Milestone reports
├── artifacts/                    # Build outputs (auto-created)
└── EcsEngine.slnx               # Solution file
```

## Environment Variables

None required. Everything is configured in version control.

## Build Policies

The build enforces:
- No compilation warnings (TreatWarningsAsErrors)
- Vulnerability scanning (NuGetAudit)
- Nullable reference types enabled
- Code style via .editorconfig

If the build fails due to warnings or style violations, see:
- [Build Configuration](../build/CONFIGURATION.md)
- [Code Style Guide](../.copilot-instructions.md)

## Advanced Tasks

For optional developer tasks (SBOM generation, AOT testing, license audits), see:
- [Build Targets & Invocation](../build/BUILD_TARGETS.md)

## Troubleshooting

### "dotnet: command not found"
Install .NET SDK 10: https://dotnet.microsoft.com/download

### Build fails with "error: TreatWarningsAsErrors"
Fix compilation warnings. See the error message for which file and line.

### Tests fail
Ensure you're using .NET SDK 10+:
```bash
dotnet --version
```

### Artifacts or restore issues
Clean and rebuild:
```bash
dotnet clean EcsEngine.slnx
dotnet build EcsEngine.slnx
```

## Next Steps

- Read [Build Targets & Invocation](../build/BUILD_TARGETS.md) for optional tasks
- Check out [Repository Setup Decisions](../concepts/REPOSITORY_SETUP_DECISIONS.md) for design rationale
- Explore [Security & Compliance](../security-and-compliance.md) for policies
