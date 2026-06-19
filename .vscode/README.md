# VS Code Configuration Guide

This directory contains VS Code workspace configurations for the ECS Engine project.

## Files

- **launch.json** - Debug launch configurations for running and debugging applications and tests
- **tasks.json** - Build, test, and publish tasks
- **settings.json** - Workspace-specific settings for C# and .NET development
- **extensions.json** - Recommended extensions for this project

## Quick Start

### Debug Configurations (F5 or Ctrl+Shift+D)

1. **.NET Debug Runtime** - Debug the main ECS Engine runtime application
2. **.NET Debug Core.Tests** - Debug core engine unit tests
3. **.NET Debug Replay.Tests** - Debug replay and snapshot unit tests
4. **.NET Debug Integration.Tests** - Debug integration tests
5. **.NET Debug SnapshotInspector** - Debug the snapshot inspection CLI tool
6. **.NET Attach to Process** - Attach debugger to a running .NET process

### Build Tasks (Ctrl+Shift+B)

- **build:debug** - Build solution in Debug configuration (default)
- **build:release** - Build solution in Release configuration

### Test Tasks (Ctrl+Shift+P → "Run Test Task")

- **test:all** - Run all tests in the solution (default test task)
- **test:core** - Run only core engine unit tests
- **test:replay** - Run only replay/snapshot unit tests
- **test:integration** - Run only integration tests

### Publish Tasks

- **publish:aot-linux** - Publish runtime with NativeAOT for Linux x64
- **publish:aot-windows** - Publish runtime with NativeAOT for Windows x64

### Other Tasks

- **clean** - Clean build artifacts

## Workflow Examples

### Running Tests with Debugging

1. Open Command Palette (Ctrl+Shift+P)
2. Select "Run Task" → "test:all" (builds and runs all tests)
3. Set breakpoints in test files and use Debug Console to inspect values

### Debugging a Specific Test Suite

1. Press F5 or go to Debug → Start Debugging
2. Select ".NET Debug Core.Tests" (or other test suite)
3. Tests will run with debugging enabled; breakpoints will pause execution

### Building and Running the Runtime

1. Press Ctrl+Shift+B to build (Debug configuration)
2. Press F5 to start debugging the Runtime
3. Use Debug Console for variable inspection and evaluation

### Publishing for Production

1. Open Command Palette (Ctrl+Shift+P)
2. Select "Run Task" → "publish:aot-linux" (or Windows equivalent)
3. Artifacts will be in `artifacts/publish/`

## Configuration Details

### launch.json

- **preLaunchTask**: Automatically builds before launching debugger
- **console**: "internalConsole" prevents external terminal popup
- **serverReadyAction**: Auto-opens URLs when startup output contains listening address

### tasks.json

- **dependsOn**: Tasks automatically run dependencies (e.g., test tasks depend on build)
- **problemMatcher**: Integrates compiler errors into VS Code's Problems panel
- **presentation**: Controls where task output appears (reveal, panel settings)

### settings.json

- **C# formatting**: Auto-format on save with code actions enabled
- **Roslyn analyzers**: Enable real-time code analysis
- **Reference CodeLens**: Show method references inline
- **EditorConfig support**: Respect `.editorconfig` settings

## Troubleshooting

### Debugger Won't Start

- Ensure artifacts are built: Run "build:debug" task first
- Check that .NET debugging extension is installed: `ms-dotnettools.csharp`
- Verify launch configuration points to correct DLL path

### Tests Don't Show Debug Output

- Use Debug Console (not Debug: Run Without Debugging)
- Check that breakpoints are set before starting debugger
- Verify test explorer shows breakpoints as set

### Build Fails with Missing NuGet Packages

- Run `dotnet restore` in terminal
- Close and reopen VS Code
- Check that package versions match `Directory.Packages.props`

## Additional Resources

- [ECS Engine README](../README.md)
- [Implementation Plan](../docs/implementation-plan.md)
- [VS Code Debugging Docs](https://code.visualstudio.com/docs/editor/debugging)
- [C# Dev Kit Guide](https://code.visualstudio.com/docs/languages/dotnet)
