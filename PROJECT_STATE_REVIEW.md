# ATE-SYSTEM-POC — Current State Review (Tree + Wiring + Dependencies)

This document explains the **current state** of the repository as if you are new to the codebase.

It is structured to answer four questions:
1. What projects and dependencies exist?
2. How does the app work end-to-end?
3. How is everything wired at runtime?
4. What is the job of each file, and what does it depend on?

---

## 1) High-level architecture

The solution has 3 projects:

- **Ate.Contracts** (`netstandard2.0`): shared transport/data models used by both engine and UI.
- **Ate.Engine** (`net472`): self-hosted HTTP execution engine. Owns command queue, driver registry, wrapper/provider/plugin model, and device integration.
- **Ate.Ui** (`net6.0-windows`, WPF): desktop client that talks to engine APIs.

### System flow (mental model)

1. User picks device/operation/parameters in WPF UI.
2. UI posts a `DeviceCommandRequest` to `POST /api/command`.
3. Engine wraps request into an `OperateDeviceCommand` and enqueues it.
4. `CommandInvoker` worker dequeues and executes command.
5. Command resolves correct `IDeviceDriver` from `DriverRegistry`.
6. Wrapper (`DmmDeviceWrapper` or `PsuDeviceWrapper`) maps operation to hardware call.
7. Demo hardware drivers simulate the real instrument response.
8. UI polls `/api/status` and `/api/capabilities` for live state and forms.

---

## 2) Dependency inventory

## 2.1 Solution/project dependency graph

```text
ATE-SYSTEM-POC.sln
├─ Ate.Contracts (no project refs)
├─ Ate.Engine -> references Ate.Contracts
└─ Ate.Ui -> references Ate.Contracts
```

There is **no direct code dependency** from UI to Engine assemblies; integration is over HTTP + shared contracts.

## 2.2 NuGet dependencies

### Ate.Engine
- `Microsoft.AspNet.WebApi.OwinSelfHost` (Web API self-hosting)
- `Microsoft.Owin.Host.HttpListener` (HTTP listener host)
- `Newtonsoft.Json` (JSON serialization/deserialization and token normalization)

### Ate.Ui
- `CommunityToolkit.Mvvm` (MVVM commands/helpers)

### Ate.Contracts
- No external package references.

## 2.3 Runtime/plugin dependencies

- Engine can load additional wrapper providers and device drivers from `Ate.Engine/drivers/*.dll` at startup.
- Built-in providers are DMM + PSU providers; plugin providers can extend configured-wrapper creation.

---

## 3) End-to-end wiring (startup to command execution)

## 3.1 Engine boot sequence (`Program.cs`)

At startup, engine does the following in order:

1. Creates core singletons: `ConsoleLogger`, `DriverRegistry`, `CommandInvoker`.
2. Stores them in `EngineHostContext` static holder for controller access.
3. Loads `engine-config.json` into `EngineConfiguration`.
4. Discovers configured wrapper providers (built-in + plugin DLLs).
5. For each configured driver instance, asks provider to create wrapper+definition and registers it.
6. Creates `DriverLoader` and scans `drivers/*.dll` for raw `IDeviceDriver` implementations.
7. Starts command worker (`invoker.Start()`).
8. Starts OWIN self-host at `http://localhost:9000/` with `Startup` pipeline.
9. On Enter keypress, stops host and gracefully stops invoker.

## 3.2 HTTP pipeline

`Startup.cs` configures:
- attribute routing for controllers,
- camelCase JSON output,
- null-value ignore.

Controllers rely on `EngineHostContext` static references to invoke runtime services.

## 3.3 Command lifecycle

1. `CommandController.EnqueueCommand` validates request.
2. Generates server command id.
3. Normalizes incoming loose JSON parameter values.
4. Creates `OperateDeviceCommand` with request details.
5. Enqueues command into `CommandInvoker` queue.
6. Worker loop waits on semaphore, respects pause flag, dequeues command.
7. Invokes `ExecuteAsync`, tracking `CurrentCommand`, `LastError`, cancellation token.

## 3.4 Driver resolution strategy

`DriverRegistry.TryResolve` tries in this order:
1. Explicit `{deviceType}::{driverId}` if request included driverId.
2. `{deviceType}::default`.
3. First registration whose key starts with `deviceType::`.

This gives a practical fallback hierarchy while still allowing explicit targeting.

## 3.5 Wrapper/provider pattern

- `IConfiguredWrapperProvider` converts config entries into concrete `IDeviceDriver` wrapper instances plus UI command metadata (`DeviceCommandDefinition`).
- Built-in providers (`DmmConfiguredWrapperProvider`, `PsuConfiguredWrapperProvider`) use demo hardware drivers and connection endpoint resolution rules.
- `ConnectionEndpointResolver` supports:
  - `settings.endpoint` explicit value,
  - `settings.endpointFormat` with `{ip}` / `{port}` tokens,
  - fallback to `ip:port` or just `ip`.

## 3.6 UI wiring

- `MainWindow` sets `DataContext = new MainViewModel()`.
- `MainViewModel` owns all UI state and async commands.
- `AteClient` is the thin HTTP adapter to engine endpoints.
- On VM init:
  - load capabilities from engine (fallback to built-in catalog if unavailable),
  - start 1-second timer polling status.

---

## 4) File-by-file tree with role + dependencies

```text
ATE-SYSTEM-POC/
├── ATE-SYSTEM-POC.sln
│   Role: Visual Studio/.NET solution container that groups all projects and build configs.
│   Depends on: Ate.Contracts.csproj, Ate.Engine.csproj, Ate.Ui.csproj entries.
│
├── README.md
│   Role: High-level architecture, tree overview, and extension notes.
│   Depends on: Documentation only.
│
├── PROJECT_STATE_REVIEW.md
│   Role: Deep, newcomer-oriented state review (this file).
│   Depends on: Documentation only.
│
├── Ate.Contracts/
│   ├── Ate.Contracts.csproj
│   │   Role: Shared model library build definition (netstandard2.0).
│   │   Depends on: SDK only.
│   │
│   └── Models.cs
│       Role: DTO/contracts shared by engine API and UI client:
│         - DeviceCommandRequest / DeviceCommandResponse
│         - EngineStatus
│         - ParameterValueType
│         - CommandParameterDefinition
│         - CommandOperationDefinition
│         - DeviceCommandDefinition
│       Depends on: Base Class Library collections only.
│
├── Ate.Engine/
│   ├── Ate.Engine.csproj
│   │   Role: Engine executable project config and package references.
│   │   Depends on: Ate.Contracts project + OWIN/WebApi/Newtonsoft packages.
│   │
│   ├── engine-config.json
│   │   Role: Declarative configured device-wrapper instances (DMM/PSU defaults).
│   │   Depends on: Parsed by EngineConfiguration at startup.
│   │
│   ├── README.md
│   │   Role: Engine-local architecture notes.
│   │   Depends on: Documentation only.
│   │
│   ├── Host/
│   │   ├── Program.cs
│   │   │   Role: Process entry point + complete runtime composition root.
│   │   │   Depends on:
│   │   │     - EngineConfiguration (config loading)
│   │   │     - DriverRegistry + DriverLoader (registration/discovery)
│   │   │     - CommandInvoker (queue worker)
│   │   │     - Built-in providers (DMM/PSU)
│   │   │     - OWIN startup class.
│   │   │
│   │   ├── Startup.cs
│   │   │   Role: OWIN/WebApi route + JSON settings.
│   │   │   Depends on: Owin + System.Web.Http + Newtonsoft settings.
│   │   │
│   │   ├── EngineHostContext.cs
│   │   │   Role: Static bridge exposing logger/registry/invoker to controllers.
│   │   │   Depends on: ILogger, DriverRegistry, CommandInvoker.
│   │   │
│   │   └── Configuration/EngineConfiguration.cs
│   │       Role: Config model + JSON load/default logic.
│   │       Depends on: Newtonsoft.Json + file system.
│   │
│   ├── Api/Controllers/
│   │   ├── CommandController.cs
│   │   │   Role: POST `/api/command`; validates request, normalizes params, enqueues command.
│   │   │   Depends on: Ate.Contracts models + OperateDeviceCommand + ParameterValueNormalizer + EngineHostContext.
│   │   │
│   │   ├── StatusController.cs
│   │   │   Role: GET `/api/status`; exposes invoker state and loaded driver keys.
│   │   │   Depends on: EngineHostContext + EngineStatus contract.
│   │   │
│   │   ├── EngineController.cs
│   │   │   Role: POST control endpoints (`pause`, `resume`, `clear`, `abort-current`).
│   │   │   Depends on: EngineHostContext.CommandInvoker.
│   │   │
│   │   └── CapabilitiesController.cs
│   │       Role: GET `/api/capabilities`; returns command metadata used by UI to render forms.
│   │       Depends on: EngineHostContext.DriverRegistry.
│   │
│   ├── Core/Commands/
│   │   ├── IAteCommand.cs
│   │   │   Role: Queue command interface contract.
│   │   │   Depends on: Task/CancellationToken.
│   │   │
│   │   ├── CommandInvoker.cs
│   │   │   Role: Asynchronous queue runner with lifecycle/state management:
│   │   │     - enqueue/start/stop
│   │   │     - pause/resume
│   │   │     - clear pending
│   │   │     - abort current
│   │   │     - state/current/last-error tracking.
│   │   │   Depends on: ConcurrentQueue/SemaphoreSlim + ILogger + IAteCommand.
│   │   │
│   │   └── OperateDeviceCommand.cs
│   │       Role: Concrete command that resolves driver and executes operation.
│   │       Depends on: DriverRegistry + ILogger + request data.
│   │
│   ├── Core/Drivers/
│   │   ├── IDeviceDriver.cs
│   │   │   Role: Engine-facing wrapper/device execution contract.
│   │   │   Depends on: Task/CancellationToken + parameter dictionary.
│   │   │
│   │   ├── IConfiguredWrapperProvider.cs
│   │   │   Role: Config-to-wrapper factory extension point.
│   │   │   Depends on: Engine config models + logger + contracts metadata models.
│   │   │
│   │   ├── DriverRegistry.cs
│   │   │   Role: Stores and resolves driver registrations and capability definitions.
│   │   │   Depends on: ConcurrentDictionary + IDeviceDriver + DeviceCommandDefinition.
│   │   │
│   │   └── DriverLoader.cs
│   │       Role: Reflection-based discovery of IDeviceDriver implementations in DLLs.
│   │       Depends on: file system + reflection + DriverRegistry + ILogger.
│   │
│   ├── DeviceIntegration/
│   │   ├── Hardware/
│   │   │   ├── IDmmHardwareDriver.cs
│   │   │   │   Role: Low-level DMM hardware abstraction interface.
│   │   │   │   Depends on: None beyond BCL primitives.
│   │   │   │
│   │   │   └── IPsuHardwareDriver.cs
│   │   │       Role: Low-level PSU hardware abstraction interface.
│   │   │       Depends on: None beyond BCL primitives.
│   │   │
│   │   ├── DemoDrivers/
│   │   │   ├── DemoDmmHardwareDriver.cs
│   │   │   │   Role: Simulated DMM behavior for testing/local demos.
│   │   │   │   Depends on: IDmmHardwareDriver.
│   │   │   │
│   │   │   └── DemoPsuHardwareDriver.cs
│   │   │       Role: Simulated PSU behavior for testing/local demos.
│   │   │       Depends on: IPsuHardwareDriver.
│   │   │
│   │   ├── Wrappers/
│   │   │   ├── ConnectionEndpointResolver.cs
│   │   │   │   Role: Builds endpoint string from config/settings conventions.
│   │   │   │   Depends on: DriverInstanceConfiguration.
│   │   │   │
│   │   │   ├── DmmDeviceWrapper.cs
│   │   │   │   Role: Translates generic operations to DMM hardware calls (`MeasureVoltage`, `Identify`).
│   │   │   │   Depends on: IDeviceDriver + IDmmHardwareDriver + numeric parsing helpers.
│   │   │   │
│   │   │   └── PsuDeviceWrapper.cs
│   │   │       Role: Translates generic operations to PSU hardware calls
│   │   │             (`Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`).
│   │   │       Depends on: IDeviceDriver + IPsuHardwareDriver + parsing helpers.
│   │   │
│   │   └── Providers/
│   │       ├── DmmConfiguredWrapperProvider.cs
│   │       │   Role: Provider that builds configured DMM wrapper + DMM capability definition.
│   │       │   Depends on: DmmDeviceWrapper + DemoDmmHardwareDriver + endpoint resolver + contracts metadata.
│   │       │
│   │       └── PsuConfiguredWrapperProvider.cs
│   │           Role: Provider that builds configured PSU wrapper + PSU capability definition.
│   │           Depends on: PsuDeviceWrapper + DemoPsuHardwareDriver + endpoint resolver + contracts metadata.
│   │
│   └── Common/
│       ├── Infrastructure/
│       │   ├── ILogger.cs
│       │   │   Role: Minimal logging abstraction.
│       │   │   Depends on: System.Exception.
│       │   │
│       │   └── ConsoleLogger.cs
│       │       Role: Colored console logger implementation.
│       │       Depends on: ILogger + Console.
│       │
│       └── Serialization/ParameterValueNormalizer.cs
│           Role: Converts JSON token-shaped values into runtime CLR-friendly values
│                 (e.g., JValue/JObject/JArray, long->int, double->decimal).
│           Depends on: Newtonsoft.Json.Linq + culture-aware parsing.
│
└── Ate.Ui/
    ├── Ate.Ui.csproj
    │   Role: WPF client project definition + MVVM package.
    │   Depends on: Ate.Contracts + CommunityToolkit.Mvvm.
    │
    ├── App.xaml
    │   Role: WPF application declaration and startup window (`MainWindow.xaml`).
    │   Depends on: WPF runtime.
    │
    ├── App.xaml.cs
    │   Role: App code-behind shell.
    │   Depends on: WPF Application class.
    │
    ├── MainWindow.xaml
    │   Role: UI layout for device/operation selection, dynamic parameter editor,
    │         action buttons, and status panel.
    │   Depends on: MainViewModel-bound properties/commands.
    │
    ├── MainWindow.xaml.cs
    │   Role: Binds `MainViewModel` as DataContext.
    │   Depends on: Ate.Ui.ViewModels.MainViewModel.
    │
    ├── Services/AteClient.cs
    │   Role: HTTP gateway to engine endpoints.
    │   Depends on: HttpClient + Ate.Contracts models + JSON extensions.
    │
    └── ViewModels/MainViewModel.cs
        Role: Main UI orchestrator:
          - loads capabilities,
          - rebuilds operations/parameter inputs,
          - sends commands,
          - polls status,
          - exposes pause/resume/clear/abort commands,
          - contains local fallback capability catalog if engine is down.
        Depends on: AteClient + Ate.Contracts + CommunityToolkit.Mvvm + WPF DispatcherTimer.
```

---

## 5) API contract map

- `POST /api/command` → enqueue command (`DeviceCommandRequest` → `DeviceCommandResponse`)
- `GET /api/status` → engine runtime status (`EngineStatus`)
- `GET /api/capabilities` → list of `DeviceCommandDefinition` (UI form metadata)
- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

These endpoints form the full UI↔Engine integration surface today.

---

## 6) “What to explain to someone else” cheat-sheet

If you need to explain the app quickly:

- It is a **command-driven instrumentation engine** with a **WPF front-end**.
- The UI does not know hardware details; it discovers available operations from `/api/capabilities`.
- Engine receives operations as generic commands, queues them, resolves wrappers, executes, and reports status.
- Wrappers isolate operation semantics; hardware interfaces isolate vendor/device implementation details.
- Providers bridge static JSON config into concrete wrappers + metadata.
- Plugin DLLs can extend providers or register raw drivers without changing core engine code.

---

## 7) Current strengths and notable technical observations

### Strengths
- Clear separation between contracts, runtime engine, and UI client.
- Dynamic capability-driven UI avoids hardcoded forms.
- Provider + wrapper architecture gives good extensibility.
- Queue lifecycle controls (pause/resume/abort/clear) are already in place.
- Parameter normalization reduces brittle JSON type handling.

### Notable observations (current state, not necessarily defects)
- Engine targets `.NET Framework 4.7.2`, while UI targets `.NET 6` (cross-targeting split).
- `EngineHostContext` uses global static state for dependency access.
- UI has a local fallback capability catalog to stay usable when engine is unreachable.
- Demo hardware drivers are in-memory simulations; no real transport implementation is present yet.

