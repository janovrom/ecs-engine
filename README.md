# ECS Engine

A .NET 10 ECS game engine for Dungeons and Dragons tabletop scenarios.

Current repository state:
- Core project skeleton is initialized.
- Concept and decision records are finalized in `docs/concepts`.
- Milestone reporting is tracked in `docs/reports`.

## License

This repository is licensed under WTFPL v2. See `LICENSE`.

## Supported Development Environments

- Linux Fedora 43
- Windows 11

Container tooling (optional):
- Linux: Podman
- Windows: Docker

## Build Output Policy

Builds use a repository-level `artifacts` directory (configured in `Directory.Build.props`).

## Common Commands

```bash
# Restore
 dotnet restore EcsEngine.slnx

# Build
 dotnet build EcsEngine.slnx

# Test
 dotnet test EcsEngine.slnx --no-build

# AOT publish probe (Linux)
 dotnet publish src/EcsEngine.Runtime/EcsEngine.Runtime.csproj -c Release -r linux-x64 -p:PublishAot=true -o ./artifacts/aot/linux-x64
```

## Documentation

- Concepts: `docs/concepts/DOMAIN.md`
- Decisions: `docs/concepts/DECISIONS.md`
- Reports: `docs/reports/`
- Implementation plan: `docs/implementation-plan.md`

## Note
This project is also a playground for testing out ECS, source generators, AOT, and coding agents. That means, don't take it too seriously.