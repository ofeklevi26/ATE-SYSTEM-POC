# Full Project Walkthrough: ATE-SYSTEM-POC

This document explains the full repository end-to-end: architecture, runtime flow, every main connection point, and each file’s role (including key classes, methods, and behavior).

## 1) Big-picture architecture

The solution contains 3 projects:

- **Ate.Contracts**: shared DTOs + capability schema consumed by both server and client.
- **Ate.Engine**: self-hosted HTTP engine (OWIN + Web API) that discovers/registers device wrappers, exposes capabilities, and executes queued commands.
- **Ate.Ui**: WPF client that discovers runtime capabilities and dynamically renders operation parameters.

### Main flow

1. Engine boots, builds dependency injection container, discovers driver modules + plugin assemblies.
2. Engine loads `engine-config.json` to instantiate configured wrappers.
3. Wrappers are registered in `DriverRegistry` along with operation metadata.
4. UI fetches metadata from `GET /api/capabilities`.
5. User submits operation via `POST /api/command`.
6. Command enters queue (`CommandInvoker`) and executes against resolved wrapper.
7. UI polls `GET /api/status` for queue state/error visibility.

---

## 2) Solution-level files

### `ATE-SYSTEM-POC.sln`
- Binds three projects into one solution: `Ate.Contracts`, `Ate.Engine`, `Ate.Ui`.
- Provides Debug/Release Any CPU configurations.

### `README.md` (root)
- High-level system overview.
- Startup and API behavior summary.
- Configuration notes and extension path.

### `ADD_NEW_DRIVER.md`
- Prescribes how to add a new device family:
  - add hardware interface + implementation,
  - add wrapper with `[DriverOperation]` methods,
  - add module and wrapper descriptor,
  - register through config,
  - keep contracts in sync for known families.

### `PROJECT_STATE_REVIEW.md`
- Captures current implementation maturity and runtime behavior expectations.

### `STANDALONE_ATECLIENT_GUIDE.md`
- Describes headless/non-WPF usage of engine APIs.
- Shows canonical API workflow and payload expectations.

---

## 3) Ate.Contracts (shared contract model)

### `Ate.Contracts/Ate.Contracts.csproj`
- Targets `netstandard2.0` for maximum compatibility between engine and UI.

### `Ate.Contracts/Models.cs`
Defines all transport and metadata models.

- `DeviceCommandRequest`
  - fields: `DeviceType`, `DriverId`, `Operation`, `Parameters`, `ClientRequestId`.
  - used by `POST /api/command`.
- `DeviceCommandResponse`
  - returned by `POST /api/command`; includes `ServerCommandId` + status `Message`.
- `EngineStatus`
  - returned by `GET /api/status`; includes queue + current command + last error + loaded drivers.
- `ParameterKind` / `NumberFormat`
  - normalized parameter typing for dynamic UIs.
- `CommandParameterDefinition`
  - per-parameter metadata: name, type, required, nullable, default.
- `CommandOperationDefinition`
  - operation metadata + parameter list.
- `DeviceCommandDefinition`
  - device wrapper capability bundle: class metadata + operations.

### `Ate.Contracts/KnownCapabilitiesCatalog.cs`
Contract-first metadata for known families.

- `TryCreateDefinition(deviceType, driverId, out definition)`
  - returns catalog-defined capability metadata for known types (`DMM`, `PSU`).
- `CreateDmmDefinition` / `CreatePsuDefinition`
  - hardcoded operation definitions, including parameter defaults/types.
- `BuildChannelParameter`
  - shared optional channel metadata.

**Why this matters**: known wrappers are validated against this contract at engine startup to catch drift.

---

## 4) Ate.Engine project / runtime core

### `Ate.Engine/Ate.Engine.csproj`
- Console executable targeting `net472`.
- Uses OWIN self-host (`Microsoft.AspNet.WebApi.OwinSelfHost`, `Microsoft.Owin.Host.HttpListener`).
- Uses Newtonsoft JSON and Microsoft DI.
- Copies `engine-config.json` to output.

### `Ate.Engine/engine-config.json`
- Lists configured driver instances.
- Each entry includes:
  - `deviceType`
  - `driverId`
  - `wrapperType` (optional matcher hint)
  - `settings` (constructor-binding values + templates like `endpointFormat`).

### `Ate.Engine/Host/Program.cs`
- Entry point.
- Calls `EngineRuntime.Start()`, logs base URL, blocks until Enter key.

### `Ate.Engine/Host/EngineRuntime.cs`
The boot orchestrator.

- `Start()` executes startup sequence:
  1. create bootstrap logger,
  2. discover plugin assemblies from `drivers/*.dll`,
  3. build DI (`BuildServiceCollection`),
  4. resolve key services (`DriverRegistry`, `CommandInvoker`, registrar),
  5. load config via `EngineConfiguration.Load`,
  6. register configured wrappers via `ConfiguredWrapperRegistrar.Register`,
  7. load plugin drivers via `DriverLoader`,
  8. start `CommandInvoker`,
  9. start OWIN host at `http://localhost:9000/` with `Startup`.
- `Dispose()` stops host + queue worker.
- `BuildServiceCollection()` registers infra, modules, controllers.
- `DiscoverDriverAssemblies()` safely loads plugin DLLs.
- `DiscoverDriverModules()` scans built-in + plugin assemblies for `IDriverModule` implementations.

### `Ate.Engine/Host/Startup.cs`
- Creates `HttpConfiguration`, enables attribute routing.
- Injects custom dependency resolver.
- Applies camelCase + null-ignore JSON settings.
- Wires Web API middleware.

### `Ate.Engine/Host/ServiceProviderDependencyResolver.cs`
- Bridges ASP.NET Web API dependency resolution to `Microsoft.Extensions.DependencyInjection`.
- Supports root resolution + scoped child resolution.

### `Ate.Engine/Host/Configuration/EngineConfiguration.cs`
- `EngineConfiguration.Load(path)` loads JSON or falls back to default if missing/invalid.
- `CreateDefault()` returns default DMM/PSU configs.
- `DriverInstanceConfiguration` holds per-driver config (`DeviceType`, `DriverId`, `WrapperType`, `Settings`).

---

## 5) Engine infrastructure/common utilities

### `Ate.Engine/Common/Infrastructure/ILogger.cs`
- Minimal abstraction: `Info` and `Error`.

### `Ate.Engine/Common/Infrastructure/ConsoleLogger.cs`
- Colored console logging implementation.
- Info in cyan, error in red.

### `Ate.Engine/Common/Serialization/ParameterValueNormalizer.cs`
Normalizes JSON-bound command parameters.

- `Normalize(raw)` returns safe dictionary and recursively normalizes values.
- Converts Newtonsoft tokens (`JValue/JObject/JArray`) into plain CLR values.
- Converts `long` in int range to `int`.
- Converts `double` to `decimal` (culture invariant).

**Purpose**: consistent type conversion before operation invocation.

---

## 6) Driver model, reflection runtime, and registry

### `Ate.Engine/Core/Drivers/IDeviceDriver.cs`
Driver/wrapper execution contract.
- Properties: `DeviceType`, `DriverId`.
- Method: `ExecuteAsync(operation, parameters, token)`.

### `Ate.Engine/Core/Drivers/IDriverModule.cs`
Device-family registration contract.
- `Name`
- `Register(IServiceCollection)`.

### `Ate.Engine/Core/Drivers/ConfiguredWrapperDescriptor.cs`
- Declares mapping between logical device type and wrapper concrete type.
- Validates wrapper implements `IDeviceDriver`.

### `Ate.Engine/Core/Drivers/DriverOperationAttribute.cs`
- Marks wrapper methods as invokable operations.
- Optional alias name support.

### `Ate.Engine/Core/Drivers/ConfiguredWrapperFactory.cs`
Creates wrapper instances from config + DI.

- `Create(config, wrapperType, services)`
  - selects best constructor + resolves each parameter.
- `SelectConstructor(...)`
  - one constructor: use it.
  - many constructors: pick resolvable max-arity constructor, reject ambiguity.
- Resolution precedence in `ResolveParameterValue(...)`:
  1. `driverId` param from config `DriverId`.
  2. direct config setting by parameter name.
  3. computed `endpoint` / `target` from direct value or `*Format` template.
  4. DI service by type.
  5. constructor default value.
- `ConvertToType(...)` handles string→primitive/enum conversion.
- `BuildFormattedSetting(...)` performs placeholder replacement (`{address}`, `{port}`, etc).

### `Ate.Engine/Core/Drivers/ConfiguredWrapperRegistrar.cs`
Registers configured wrappers at startup.

- `Register(engineConfiguration)`:
  - resolves wrapper descriptor,
  - builds wrapper instance using factory,
  - builds capability definition using `WrapperOperationRuntime.BuildDefinition`,
  - registers wrapper instance + definition in `DriverRegistry`.
- Contract drift exceptions (from known capabilities validation) are rethrown to fail startup.
- `ResolveDescriptor(...)` matches `wrapperType` by device type, class name, or full type name; otherwise falls back to `deviceType`.

### `Ate.Engine/Core/Drivers/DriverLoader.cs`
Optional plugin direct-driver loader (non-configured wrappers).

- Scans assemblies for non-abstract `IDeviceDriver` with default ctor.
- Instantiates sample to read `DeviceType/DriverId`, then registers factory.
- Logs registration failures but continues.

### `Ate.Engine/Core/Drivers/DriverRegistry.cs`
In-memory key/value registry for drivers and capabilities.

- Key pattern: `deviceType::driverId`.
- `Register(...)` and `RegisterInstance(...)`.
- `TryResolve(deviceType, driverId, out driver)` lookup order:
  1. explicit `deviceType::driverId`,
  2. `deviceType::default`,
  3. any matching `deviceType::*` fallback.
- `GetLoadedDrivers()` returns sorted registry keys.
- `GetCommandDefinitions()` returns capability metadata for API/UI.

### `Ate.Engine/Core/Drivers/WrapperOperationRuntime.cs`
Reflection-based operation metadata + invocation runtime.

- `BuildDefinition(driver, driverParameters?)`
  - known device type: pull from `KnownCapabilitiesCatalog`, then validate wrapper signature consistency.
  - unknown type: reflect `[DriverOperation]` methods and infer parameter metadata.
- `InvokeAsync(wrapper, operation, parameters, token)`
  - resolves target operation from cache,
  - binds/typed-converts parameters,
  - invokes method and unwraps `TargetInvocationException`.
- `ValidateContractConsistency(...)` + `ValidateParameterConsistency(...)`
  - compare reflected wrapper methods against catalog operations for known families.
  - enforces no mismatch on operation existence or parameter shape.
- `BuildOperationDefinition` / `BuildParameterDefinition`
  - infer kinds, number formats, defaults, required/nullable semantics.
- `BindParameters(...)`
  - requires provided value or method default; otherwise throws required-parameter error.
- `ConvertValue(...)`
  - robust cross-type conversion (string/bool/int/decimal + long/double/floats).
- `GetOperationMethods(...)`
  - caches `[DriverOperation]` methods by name and rejects duplicates.

---

## 7) Commanding subsystem

### `Ate.Engine/Core/Commands/IAteCommand.cs`
- Queue item abstraction: `Name` + `ExecuteAsync(token)`.

### `Ate.Engine/Core/Commands/OperateDeviceCommand.cs`
Represents a concrete device operation request.

- Captures IDs, target device, operation, parameters.
- `Name` formatted for logs/status.
- `ExecuteAsync(...)`:
  - resolves driver from registry,
  - logs start,
  - calls `driver.ExecuteAsync(...)`,
  - logs result.

### `Ate.Engine/Core/Commands/CommandInvoker.cs`
Single-worker in-memory command queue runtime.

- Queue: `ConcurrentQueue<IAteCommand>` + `SemaphoreSlim` signal.
- Lifecycle:
  - `Start()` launches worker loop.
  - `StopAsync()` cancels lifetime + current command.
- Controls:
  - `Pause()`, `Resume()`, `ClearPending()`, `AbortCurrent()`.
- State fields:
  - `State`, `CurrentCommand`, `LastError`, `QueueLength`.
- `WorkerLoopAsync(...)`:
  - waits for signal,
  - handles paused mode,
  - dequeues + executes command with linked cancellation token,
  - updates error/state fields on cancellation/exception.

---

## 8) HTTP API controllers

### `Ate.Engine/Api/Controllers/CommandController.cs`
- `POST /api/command`
  - validates request,
  - normalizes parameter values,
  - creates `OperateDeviceCommand`,
  - enqueues into `CommandInvoker`,
  - returns server command ID.

### `Ate.Engine/Api/Controllers/StatusController.cs`
- `GET /api/status`
  - materializes `EngineStatus` using queue + registry state.

### `Ate.Engine/Api/Controllers/EngineController.cs`
- Control endpoints:
  - `POST /api/engine/pause`
  - `POST /api/engine/resume`
  - `POST /api/engine/clear`
  - `POST /api/engine/abort-current`

### `Ate.Engine/Api/Controllers/CapabilitiesController.cs`
- `GET /api/capabilities`
  - returns `DriverRegistry.GetCommandDefinitions()`; this is the UI’s source of truth.

---

## 9) Device integration layers

### Hardware interfaces
- `Ate.Engine/DeviceIntegration/Hardware/IDmmHardwareDriver.cs`
- `Ate.Engine/DeviceIntegration/Hardware/IPsuHardwareDriver.cs`

These define instrument-level operations (connect/identify/measure/set). Wrappers depend on these interfaces.

### Demo hardware drivers
- `DemoDmmHardwareDriver`
  - no-op connect/disconnect, deterministic fake voltage value.
- `DemoPsuHardwareDriver`
  - no-op connect/disconnect, stores simulated output state fields.

### Wrappers (engine-facing drivers)
- `DmmDeviceWrapper` (`IDeviceDriver`)
  - ctor receives config + hardware service (`driverId`, `address`, `channel`, `endpoint`, hardware).
  - `ExecuteAsync`: connect → invoke reflected operation → disconnect.
  - `[DriverOperation]` methods: `MeasureVoltage`, `Identify`.
- `PsuDeviceWrapper` (`IDeviceDriver`)
  - same execution pattern.
  - operations: `Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`.

### Driver modules
- `DmmDriverModule`, `PsuDriverModule` implement `IDriverModule`.
- Register both:
  1. hardware implementation in DI,
  2. `ConfiguredWrapperDescriptor` for configured wrapper creation.

### `DeviceIntegration/Modules/README.md`
- Documents module responsibilities, config resolution behavior, and conventions.

---

## 10) Ate.Ui (WPF client)

### `Ate.Ui/Ate.Ui.csproj`
- WPF app on `net6.0-windows`.
- Uses `CommunityToolkit.Mvvm` for async commands.
- References `Ate.Contracts` for shared models.

### `Ate.Ui/App.xaml` + `App.xaml.cs`
- App bootstrap; startup window = `MainWindow`.

### `Ate.Ui/MainWindow.xaml` + `MainWindow.xaml.cs`
- UI layout:
  - device dropdown,
  - operation dropdown,
  - dynamic parameter list (name/type/value text inputs),
  - command/control buttons,
  - status textbox.
- Code-behind sets `DataContext = new MainViewModel()`.

### `Ate.Ui/Services/AteClient.cs`
Thin HTTP client wrapper around engine endpoints.

- `SendCommandAsync` → `POST api/command`
- `GetStatusAsync` → `GET api/status`
- `GetCapabilitiesAsync` → `GET api/capabilities`
- `PauseAsync` / `ResumeAsync` / `ClearAsync` / `AbortCurrentAsync`

### `Ate.Ui/ViewModels/MainViewModel.cs`
Main MVVM orchestrator.

- State collections:
  - `Devices`, `Operations`, `ParameterInputs`.
- Commands:
  - send, pause, resume, clear, abort.
- Constructor:
  - initializes collections and commands,
  - starts 1-second timer polling `RefreshStatusAsync`,
  - kicks `InitializeAsync`.
- `InitializeAsync`:
  - loads capabilities then starts status timer.
- `LoadCapabilitiesAsync` + `ApplyCapabilities`:
  - fetches from engine; populates device list.
- `SelectedDevice` setter:
  - rebuilds operations and parameter inputs.
- `SelectedOperation` setter:
  - rebuilds parameter inputs.
- `RebuildParameterInputs`:
  - merges driver-level params + operation params (dedup by name).
- `BuildParametersDictionary` + `ConvertParameterValue`:
  - converts text input into typed request payload values.
- `SendAsync`:
  - builds `DeviceCommandRequest` and sends command.
- `RefreshStatusAsync`:
  - polls engine status and formats status text.
- `ExecuteControlAsync`:
  - generic error wrapper for control actions.

### `ParameterInputViewModel` (nested in same file)
- Holds parameter metadata and editable string value.
- Exposes `TypeLabel` (e.g., `Number (Decimal)`).

---

## 11) End-to-end request walkthrough

Example user action: **Set PSU voltage**

1. UI loads capabilities (`GET /api/capabilities`) and shows `PSU` + `SetVoltage` with `voltage/currentLimit/channel` fields.
2. User presses **Send**.
3. UI sends `DeviceCommandRequest` with typed parameters.
4. Engine `CommandController` validates and normalizes payload.
5. Engine creates `OperateDeviceCommand` and queues it.
6. `CommandInvoker` dequeues and executes.
7. `OperateDeviceCommand` resolves driver in `DriverRegistry` (usually `PSU::default`).
8. Driver is `PsuDeviceWrapper`; `ExecuteAsync` connects hardware and dispatches operation via `WrapperOperationRuntime.InvokeAsync`.
9. Reflection binder maps parameter names → method args and applies conversions/defaults.
10. Wrapper method calls underlying `IPsuHardwareDriver` implementation and returns a result string.
11. Result logged; queue status updates; UI sees changes on next `GET /api/status` poll.

---

## 12) Extension and plugin model

Two primary extension paths:

1. **Configured wrapper path (recommended)**
   - implement `IDriverModule`, register hardware + wrapper descriptor.
   - add config entry in `engine-config.json`.
   - wrapper constructor can pull values from config/DI.

2. **Direct plugin driver path**
   - implement `IDeviceDriver` with public parameterless constructor.
   - place plugin DLL in `drivers/`.
   - `DriverLoader` auto-registers based on sample instance properties.

For known built-ins, keep wrapper signature aligned with `KnownCapabilitiesCatalog`; startup fails fast on drift.

---

## 13) Operational constraints / current behavior nuances

- Queue is in-memory only (no persistence).
- Worker processes one command at a time.
- Pause does not remove queued items; it defers processing.
- Abort cancels only the currently executing command.
- Reflection invocation currently targets synchronous wrapper methods (wrapped in `Task.FromResult`).
- UI capability discovery is runtime-driven; no baked fallback schema in viewmodel.

---

## 14) Quick “mental model” summary

- **Contracts** define the wire shapes.
- **Engine Runtime** wires DI, config, modules, and HTTP.
- **Registry** maps logical `(deviceType, driverId)` to wrapper factories/instances + capability metadata.
- **WrapperOperationRuntime** is the reflection brain (metadata + invocation + contract consistency checks).
- **CommandInvoker** is the execution pipeline controller.
- **UI** is a thin capability-driven operator console.

