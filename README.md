# ECS Engine

A .NET 10 ECS game engine for Dungeons and Dragons tabletop scenarios.

**Current Status**: M2 Complete (Deterministic core lifecycle)

## Quick Start

```bash
git clone https://github.com/janovrom/ecs-engine.git
cd ecs-engine
dotnet build EcsEngine.slnx
dotnet test EcsEngine.slnx
```

See [Local Setup](docs/setup/README.md) for detailed instructions.

## Requirements

- **.NET SDK 10.0+** (for building and running)
- **Git** (for cloning the repository)
- **dotnet-cyclonedx** (optional, for SBOM generation — installed in CI/CD)

See [Build Targets & Invocation](docs/build/BUILD_TARGETS.md) for optional developer tasks.

## License

This repository is licensed under WTFPL v2. See `LICENSE`.

## Supported Development Environments

- Linux Fedora 43
- Windows 11

Container tooling (optional):
- Linux: Podman
- Windows: Docker

## Build & Security

- **Deterministic builds**: All dependencies pinned in `Directory.Packages.props`
- **Vulnerability scanning**: NuGetAudit enabled (mode=all, level=low)
- **Code standards**: Enforced via `.editorconfig` and `.copilot-instructions.md`
- **License compliance**: Documented in separate audit step

See [Security & Compliance](docs/security-and-compliance.md) for policies.

## Common Commands

```bash
# Restore dependencies
dotnet restore EcsEngine.slnx

# Build
dotnet build EcsEngine.slnx

# Test
dotnet test EcsEngine.slnx --no-build

# Generate SBOM (local, requires dotnet-cyclonedx)
dotnet tool install --global CycloneDX.Cli
dotnet CycloneDX EcsEngine.slnx --output ./artifacts/sbom.spdx.json

# AOT publish probe (Linux)
dotnet publish src/EcsEngine.Simulation -c Release -r linux-x64 -p:PublishAot=true -o ./artifacts/aot/linux-x64
```

## Documentation

- **Getting Started**: [docs/setup/README.md](docs/setup/README.md)
- **Build Targets**: [docs/build/BUILD_TARGETS.md](docs/build/BUILD_TARGETS.md)
- **Build Configuration**: [docs/build/CONFIGURATION.md](docs/build/CONFIGURATION.md)
- **Security & Compliance**: [docs/security-and-compliance.md](docs/security-and-compliance.md)
- **Design Concepts**: [docs/concepts/](docs/concepts/)
- **Milestone Reports**: [docs/reports/](docs/reports/)
- **Implementation Plan**: [docs/implementation-plan.md](docs/implementation-plan.md)

## Design & Architecture

- **Concepts**: See [docs/concepts/DOMAIN.md](docs/concepts/DOMAIN.md)
- **Decisions**: All decisions (D-001 through D-109) in [docs/concepts/](docs/concepts/)
- **Repository Setup**: Design interview results in [docs/concepts/REPOSITORY_SETUP_DECISIONS.md](docs/concepts/REPOSITORY_SETUP_DECISIONS.md)

## Development

This project is also a playground for testing ECS, source generators, AOT, and coding agents. That means, don't take it too seriously.