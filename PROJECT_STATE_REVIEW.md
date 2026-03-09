# PROJECT_STATE_REVIEW

## Scope of this update
This review reflects the **current `work` branch state** after the dependency-injection refactor introduced in:
- `Refactor app architecture to use dependency injection`

## Current architecture snapshot

### Solution projects
- `Ate.Contracts`: shared DTO/contracts between engine and UI.
- `Ate.Engine`: self-hosted OWIN/WebApi engine and command execution runtime.
- `Ate.Ui`: WPF client that drives the engine over HTTP.

## Branch-specific changes now in place

### 1) Engine dependency composition is now DI-based
- Replaced ad-hoc/static host context wiring with `Microsoft.Extensions.DependencyInjection` in `Program.cs`.
- Core runtime services (`ILogger`, `DriverRegistry`, `CommandInvoker`) are registered once and resolved from a service provider.

### 2) WebApi controllers are resolved through DI
- Added `ServiceProviderDependencyResolver` adapter to bridge Microsoft DI to WebApi dependency resolution.
- `Startup` now sets `config.DependencyResolver` to this adapter.
- Controllers receive dependencies via constructor injection instead of reading from global static state.

### 3) Removed static global service holder
- `EngineHostContext` was removed from `Ate.Engine/Host`.
- Runtime state/service access moved behind injected dependencies.

### 4) UI startup and view model creation are now DI-driven
- Removed `StartupUri` flow in `App.xaml`; startup window is now resolved in `App.xaml.cs` through a DI container.
- Introduced `IAteClient` abstraction and updated `AteClient` to implement it.
- `MainViewModel` now depends on `IAteClient` via constructor injection.
- `MainWindow` now receives `MainViewModel` through constructor injection.

### 5) Dependency updates
- Added `Microsoft.Extensions.DependencyInjection` package references in:
  - `Ate.Engine/Ate.Engine.csproj`
  - `Ate.Ui/Ate.Ui.csproj`

## Quality/status notes
- The architecture now has a clear composition root in both Engine and UI.
- Controller/view-model dependencies are explicit and easier to test/migrate.
- The environment currently lacks the `dotnet` CLI, so full solution build validation is blocked in this workspace.

## Recommended next follow-ups (optional)
1. Introduce interface abstractions for additional runtime seams (e.g., command queue façade) where unit tests are expected.
2. Add lightweight unit tests for controller constructor-injected behavior.
3. If target framework constraints arise (`net472` + newer DI package), pin/test package versions under CI.
