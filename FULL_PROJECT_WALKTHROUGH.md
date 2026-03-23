# Full Project Walkthrough: ATE-SYSTEM-POC

This walkthrough is a code-level map of the repository as it exists today. It explains:

1. end-to-end runtime flow,
2. each project and file,
3. each class/interface/enum,
4. each method/function and what it does.

---

## 1) End-to-end runtime flow

1. `Ate.Engine/Host/Program.cs` starts Serilog and calls `EngineRuntime.Start()`.
2. `EngineRuntime` discovers plugin DLLs, discovers modules, builds DI, loads `engine-config.json`, registers configured wrappers, loads direct plugin drivers, starts the command worker, and starts Web API self-host.
3. `Ate.Ui` (or any external client) calls `GET /api/capabilities` to discover devices/operations.
4. Client sends `POST /api/command` with `deviceType`, `deviceName`, `operation`, `parameters`.
5. `CommandController` validates request + invocation and enqueues `OperateDeviceCommand` into `CommandInvoker`.
6. `CommandInvoker` dequeues and executes commands; `OperateDeviceCommand` resolves the target driver and calls `ExecuteAsync`.
7. Wrapper (`DmmDeviceWrapper` / `PsuDeviceWrapper` / plugin driver) executes operation through `WrapperOperationRuntime`.
8. Client polls `GET /api/status` for queue state/errors and can call engine control endpoints (pause/resume/clear/abort).

---

## 2) Solution-level files

### `ATE-SYSTEM-POC.sln`
- Solution container for `Ate.Contracts`, `Ate.Engine`, and `Ate.Ui`.

### Root markdown documents
- `README.md`: top-level project map and quick start.
- `Ate.Engine/README.md`: engine internals.
- `PROJECT_STATE_REVIEW.md`: concise current-state snapshot.
- `STANDALONE_ATECLIENT_GUIDE.md`: headless client usage.
- `ADD_NEW_DRIVER.md`: configured-wrapper onboarding.
- `ADD_DLL_DRIVER.md`: direct plugin onboarding.

---

## 3) Ate.Contracts project

### `Ate.Contracts/Ate.Contracts.csproj`
- Class library project for shared contracts.

### `Ate.Contracts/Models.cs`

#### `DeviceCommandRequest`
- `DeviceType`: target family key.
- `DeviceName`: configured device instance key.
- `Operation`: operation name.
- `Parameters`: operation argument map.
- `ClientRequestId`: optional client correlation ID.

#### `DeviceCommandResponse`
- `ServerCommandId`: server-generated queue ID.
- `Message`: enqueue status message.

#### `EngineStatus`
- `State`: invoker state string.
- `QueueLength`: pending commands count.
- `CurrentCommand`: in-flight command.
- `LastError`: latest failure text.
- `LoadedDrivers`: registry keys.

#### `ParameterKind` enum
- UI-level categories: `String`, `Integer`, `Number`, `Boolean`.

#### `NumberFormat` enum
- Numeric format metadata: `Decimal`, `Float`, `Double`.

#### `CommandParameterDefinition`
- Metadata for one parameter: name/display/description/type/default/nullability.

#### `CommandOperationDefinition`
- Metadata for one operation.
- `ToString()` returns operation `Name`.

#### `DeviceCommandDefinition`
- Metadata for one device entry + operations.
- `ToString()` returns `DriverDisplayName` if present, else `DeviceType`.

### `Ate.Contracts/KnownCapabilitiesCatalog.cs`

#### `KnownCapabilitiesCatalog` (static)
- Contract-first capability definitions for known families (`DMM`, `PSU`).

Methods:
- `TryCreateDefinition(deviceType, driverId, out definition)`:
  - dispatches to known builders by `deviceType`.
- `CreateDmmDefinition(driverId)`:
  - builds explicit DMM metadata for `Identify` + `MeasureVoltage`.
- `CreatePsuDefinition(driverId)`:
  - builds explicit PSU metadata for `Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`.
- `BuildChannelParameter()`:
  - shared optional `channel` parameter metadata.

---

## 4) Ate.Engine project

### `Ate.Engine/Ate.Engine.csproj`
- .NET Framework executable project (engine host).

### `Ate.Engine/engine-config.json`
- Runtime configured device list.
- Each entry currently has `deviceName`, `deviceType`, `settings`.

---

## 5) Engine host/bootstrap files

### `Ate.Engine/Host/Program.cs`

#### `Program` (static)
- `Main(string[] args)`:
  - creates startup logger,
  - starts runtime,
  - logs listening URL,
  - waits for Enter,
  - shuts down logger in `finally`.

### `Ate.Engine/Host/EngineRuntime.cs`

#### `EngineRuntime`
- Holds running host references (`_webApp`, `_invoker`) and exposes `BaseAddress`, `Logger`.

Methods:
- private constructor:
  - captures base address/logger/invoker/web host handle.
- `Start(ILogger? bootLoggerOverride = null)`:
  - boot orchestration method.
  - loads plugin assemblies, builds services, loads config, registers wrappers, loads plugin drivers, starts invoker and OWIN host.
  - on fatal error: logs + optionally flushes logger + rethrows.
- `Dispose()`:
  - disposes web host and stops invoker with logging safeguards.
- `BuildServiceCollection(pluginAssemblies, logger)`:
  - registers singleton services + controllers.
  - discovers and executes module registrations.
- `DiscoverDriverAssemblies(driversPath, logger)`:
  - loads DLLs from `drivers` folder (best effort).
- `DiscoverDriverModules(pluginAssemblies, logger)`:
  - scans engine + plugin assemblies for `IDriverModule` implementations.
- `IsDriverModuleType(type)`:
  - type predicate used during module scanning.

### `Ate.Engine/Host/Startup.cs`

#### `Startup`
- Constructor stores dependency resolver + logger.

Methods:
- `Configuration(IAppBuilder app)`:
  - creates `HttpConfiguration`,
  - maps attribute routes,
  - sets DI resolver,
  - sets JSON formatter options,
  - registers `ApiExceptionLogger`,
  - wires Web API middleware.

### `Ate.Engine/Host/ServiceProviderDependencyResolver.cs`

#### `ServiceProviderDependencyResolver`
- Adapter between ASP.NET Web API dependency abstractions and Microsoft DI root provider.

Methods:
- constructor: stores root provider.
- `BeginScope()`:
  - creates per-request scope wrapper.
- `GetService(Type serviceType)`:
  - resolve single service from root.
- `GetServices(Type serviceType)`:
  - resolve enumerable from root.
- `Dispose()`:
  - no-op for root wrapper.

#### `ServiceProviderDependencyScope` (nested class)
- Adapter for scoped resolution.

Methods:
- constructor: stores scope.
- `GetService(Type serviceType)`:
  - resolve single service from scope provider.
- `GetServices(Type serviceType)`:
  - resolve enumerable from scope provider.
- `Dispose()`:
  - disposes scope.

### `Ate.Engine/Host/Configuration/EngineConfiguration.cs`

#### `EngineConfiguration`
- `Drivers`: list of configured device instances.

Methods:
- `Load(string path)`:
  - reads and deserializes JSON config,
  - throws on missing file/invalid JSON/empty config.

#### `DriverInstanceConfiguration`
- `DeviceName`: required command-time selector.
- `DeviceType`: family key.
- `Settings`: case-insensitive string dictionary used for wrapper constructor binding.

---

## 6) Engine infrastructure/common

### `Ate.Engine/Common/Infrastructure/ILogger.cs`

#### `ILogger` interface
Methods:
- `Info(string message)`
- `Error(string message, Exception? ex = null)`

### `Ate.Engine/Common/Infrastructure/SerilogBootstrapper.cs`

#### `SerilogBootstrapper` (static)
Methods:
- `CreateLogger(string baseDirectory)`:
  - configures Serilog console + rolling file sink,
  - returns `ILogger` adapter.
- `Shutdown()`:
  - closes/flushed Serilog pipeline.

### `Ate.Engine/Common/Infrastructure/SerilogLogger.cs`

#### `SerilogLogger`
- Adapter from `Serilog.ILogger` to engine `ILogger`.

Methods:
- constructor: captures underlying Serilog logger.
- `Info(message)`: writes info-level event.
- `Error(message, ex)`: writes error-level event.

### `Ate.Engine/Common/Serialization/ParameterValueNormalizer.cs`

#### `ParameterValueNormalizer` (static)
Methods:
- `Normalize(Dictionary<string, object>? raw)`:
  - normalizes incoming command parameter dictionary,
  - handles null source as empty dictionary.
- `NormalizeValue(object value)`:
  - recursively normalizes values and collections.
- `NormalizeToken(JToken token)`:
  - converts Newtonsoft token types into CLR primitives/collections.

---

## 7) Engine API layer

### `Ate.Engine/Api/ApiExceptionLogger.cs`

#### `ApiExceptionLogger`
- Global Web API exception logger.

Methods:
- constructor: captures engine logger.
- `Log(ExceptionLoggerContext context)`:
  - extracts request/controller/action context,
  - logs unhandled pipeline exceptions.

### `Ate.Engine/Api/Controllers/CommandController.cs`

#### `CommandController`
- Accepts command requests.

Methods:
- constructor: injects `DriverRegistry`, `ILogger`, `CommandInvoker`.
- `EnqueueCommand(DeviceCommandRequest request)`:
  - validates required fields,
  - normalizes parameters,
  - resolves target driver existence,
  - validates operation invocation/parameter types,
  - creates `OperateDeviceCommand`,
  - enqueues command and returns `DeviceCommandResponse`.

### `Ate.Engine/Api/Controllers/CapabilitiesController.cs`

#### `CapabilitiesController`
Methods:
- constructor: injects registry + logger.
- `GetCapabilities()`:
  - reads definitions from registry,
  - logs summary,
  - returns definitions.

### `Ate.Engine/Api/Controllers/StatusController.cs`

#### `StatusController`
Methods:
- constructor: injects invoker + registry + logger.
- `GetStatus()`:
  - builds `EngineStatus` from invoker + registry state,
  - returns status.

### `Ate.Engine/Api/Controllers/EngineController.cs`

#### `EngineController`
Methods:
- constructor: injects invoker + logger.
- `Pause()`:
  - pauses queue consumption.
- `Resume()`:
  - resumes queue consumption.
- `Clear()`:
  - removes pending queue items.
- `AbortCurrent()`:
  - cancels current command token.

---

## 8) Command queue layer

### `Ate.Engine/Core/Commands/IAteCommand.cs`

#### `IAteCommand` interface
- `Name` property.
- `ExecuteAsync(CancellationToken token)` method.

### `Ate.Engine/Core/Commands/OperateDeviceCommand.cs`

#### `OperateDeviceCommand`
- Immutable command payload wrapper.

Methods:
- constructor:
  - captures command metadata, parameters, registry, logger.
- `ExecuteAsync(token)`:
  - resolves target driver from registry,
  - logs start,
  - invokes driver operation,
  - logs result.

### `Ate.Engine/Core/Commands/CommandInvoker.cs`

#### `CommandInvoker`
- In-memory queue processor with control operations.

Methods:
- constructor: initializes state (`Stopped`) and dependencies.
- `Enqueue(IAteCommand command)`:
  - queues command and signals worker.
- `Start()`:
  - creates worker task/lifetime token and sets state to `Running`.
- `StopAsync()`:
  - requests worker shutdown, aborts current command, awaits worker, sets state `Stopped`.
- `Pause()`:
  - sets pause flag and state `Paused`.
- `Resume()`:
  - clears pause flag, sets state `Running`, re-signals worker.
- `ClearPending()`:
  - dequeues all pending items.
- `AbortCurrent()`:
  - cancels current command CTS.
- `ReportError(string error)`:
  - sets `LastError` and logs message.
- `WorkerLoopAsync(CancellationToken stopToken)` (private):
  - waits for signal,
  - handles paused state,
  - dequeues/executes commands,
  - updates `CurrentCommand`/`LastError`.

---

## 9) Driver core layer

### `Ate.Engine/Core/Drivers/IDeviceDriver.cs`

#### `IDeviceDriver` interface
- `DeviceType` property.
- `DriverId` property.
- `ExecuteAsync(operation, parameters, token)`.

### `Ate.Engine/Core/Drivers/IDriverModule.cs`

#### `IDriverModule` interface
- `Name` property.
- `Register(IServiceCollection services)` method.

### `Ate.Engine/Core/Drivers/DriverOperationAttribute.cs`

#### `DriverOperationAttribute`
- Marks wrapper methods as invokable operations.

Methods:
- constructor `DriverOperationAttribute(string? name = null)`:
  - optional custom operation name override.

### `Ate.Engine/Core/Drivers/ConfiguredWrapperDescriptor.cs`

#### `ConfiguredWrapperDescriptor`
- Pairs `DeviceType` with concrete wrapper type.

Methods:
- constructor validates non-empty `deviceType` and wrapper type compatibility.

### `Ate.Engine/Core/Drivers/ConfiguredWrapperFactory.cs`

#### `ConfiguredWrapperFactory` (internal static)
Methods:
- `Create(configuration, wrapperType, services)`:
  - selects constructor, resolves args, creates wrapper instance.
- `SelectConstructor(configuration, wrapperType, services)`:
  - constructor choice algorithm with ambiguity rejection.
- `CanResolveParameter(config, parameter, services)`:
  - predicate used by constructor selection.
- `ResolveParameterValue(config, parameter, services)`:
  - resolves concrete value by precedence rules.
- `ConvertToType(targetType, raw)`:
  - string conversion helper for primitives/enums.
- `BuildFormattedSetting(settings, valueKey, formatKey, addressKey, channelKey)`:
  - supports direct and templated endpoint/target values.

### `Ate.Engine/Core/Drivers/ConfiguredWrapperRegistrar.cs`

#### `ConfiguredWrapperRegistrar`
Methods:
- constructor: injects descriptors, service provider, registry, logger.
- `Register(EngineConfiguration configuration)`:
  - validates each config entry,
  - prevents duplicate `deviceType::deviceName`,
  - resolves descriptor, builds wrapper, builds capability definition,
  - overrides display name with configured `deviceName`,
  - registers instance in registry,
  - rethrows known capability drift exceptions.
- `IsContractDriftException(Exception ex)` (private):
  - identifies startup-fatal contract drift messages.
- `ResolveDescriptor(configuration)` (private):
  - descriptor lookup by `deviceType`.
- `ValidateDriverConfiguration(configuration)` (private):
  - ensures required `deviceType`/`deviceName`.

### `Ate.Engine/Core/Drivers/DriverRegistry.cs`

#### `DriverRegistry`
- Stores driver factories + optional definitions by key.

Methods:
- `Register(deviceType, deviceName, factory, definition)`:
  - upsert registration under key.
- `RegisterInstance(driver, deviceName, definition)`:
  - convenience wrapper around `Register`.
- `TryResolve(deviceType, deviceName, out driver)`:
  - exact-key lookup.
- `GetLoadedDrivers()`:
  - ordered key list.
- `GetCommandDefinitions()`:
  - ordered non-null definition list.
- `BuildKey(deviceType, deviceName)` (private):
  - key composition helper.

Nested class:
- `DriverRegistration`:
  - stores `DeviceName`, `Factory`, `Definition`.
  - constructor initializes fields.

### `Ate.Engine/Core/Drivers/DriverLoader.cs`

#### `DriverLoader`
Methods:
- constructor: injects registry + logger.
- `LoadFromAssemblies(assemblies)`:
  - iterates distinct assemblies.
- `LoadFromAssembly(assembly)` (private):
  - discovers eligible driver types and registers each.
- `RegisterType(type)` (private):
  - creates sample instance,
  - registers under `deviceType::default`,
  - logs success/failure.
- `IsDriverType(t)` (private):
  - validates concrete `IDeviceDriver` with parameterless ctor.

### `Ate.Engine/Core/Drivers/ParameterTypeMismatchException.cs`

#### `ParameterTypeMismatchException`
- Specialized invalid-operation exception for invocation conversion failures.

Methods:
- constructor:
  - captures operation, parameter name, expected type, received value.
- `BuildMessage(...)` (private):
  - standardized error message formatter.

### `Ate.Engine/Core/Drivers/WrapperOperationRuntime.cs`

#### `WrapperOperationRuntime` (static)
- Central reflection/runtime invocation helper.

Public methods:
- `BuildDefinition(driver, driverParameters = null)`:
  - known-family: returns catalog definition and validates drift.
  - unknown-family: reflects `[DriverOperation]` methods into metadata.
- `InvokeAsync(wrapper, operation, parameters, token)`:
  - resolves method, binds args, invokes method, unwraps `TargetInvocationException`.
- `ValidateInvocation(wrapper, operation, parameters)`:
  - validates operation/parameters without executing hardware side effects.

Private methods:
- `ValidateContractConsistency(wrapperType, contractDefinition)`:
  - compares reflected operations with catalog operations.
- `ValidateParameterConsistency(...)`:
  - compares contract vs reflection parameter name/type/nullability.
- `BuildOperationDefinition(operationName, method)`:
  - builds operation metadata model.
- `BuildParameterDefinition(parameter)`:
  - builds parameter metadata model.
- `GetTypeDisplayName(type)`:
  - user-friendly type name helper (handles nullable/generic).
- `MapParameterKind(type)`:
  - maps CLR type to `ParameterKind`.
- `MapNumberFormat(type)`:
  - maps numeric CLR types to `NumberFormat`.
- `BindParameters(method, provided)`:
  - binds and converts values for invocation.
- `IsMissingValue(value)`:
  - null/blank detection helper.
- `GetParameterSpecificDefaultValueString(parameter)`:
  - explicit defaults for known parameter names (e.g., `channel`).
- `GetImplicitDefaultValueString(type)`:
  - generic default string helper by type.
- `ConvertValue(operation, parameterName, value, targetType)`:
  - conversion pipeline + mismatch exception wrapping.
- `ResolveOperationMethod(wrapperType, operation)`:
  - operation lookup + unsupported-operation error.
- `GetOperationMethods(wrapperType)`:
  - caches operation methods and enforces duplicate-name rejection.

---

## 10) Device integration files

### Hardware interfaces

#### `Ate.Engine/DeviceIntegration/Hardware/IDmmHardwareDriver.cs`
Methods:
- `Connect(string connectionTarget)`
- `Disconnect()`
- `Identify(string address, int channel)`
- `MeasureVoltage(string address, int channel, decimal range)`

#### `Ate.Engine/DeviceIntegration/Hardware/IPsuHardwareDriver.cs`
Methods:
- `Connect(string connectionTarget)`
- `Disconnect()`
- `Identify(string address, int channel)`
- `SetVoltage(int channel, decimal voltage, decimal currentLimit)`
- `SetCurrentLimit(int channel, decimal currentLimit)`
- `SetOutput(int channel, bool enabled)`

### Demo hardware implementations

#### `Ate.Engine/DeviceIntegration/DemoDrivers/DemoDmmHardwareDriver.cs`
Methods:
- `Connect(connectionTarget)`: stores endpoint state.
- `Disconnect()`: resets connection state.
- `Identify(address, channel)`: returns synthetic identification string.
- `MeasureVoltage(address, channel, range)`: returns deterministic demo numeric value.

#### `Ate.Engine/DeviceIntegration/DemoDrivers/DemoPsuHardwareDriver.cs`
Methods:
- `Connect(connectionTarget)`: stores endpoint state.
- `Disconnect()`: resets connection state.
- `Identify(address, channel)`: returns synthetic identification string.
- `SetVoltage(channel, voltage, currentLimit)`: stores simulated channel state.
- `SetCurrentLimit(channel, currentLimit)`: updates simulated channel current limit.
- `SetOutput(channel, enabled)`: toggles simulated channel output state.

### Wrapper implementations

#### `Ate.Engine/DeviceIntegration/Wrappers/DmmDeviceWrapper.cs`
Methods:
- constructor: captures driver/config/hardware dependency.
- `ExecuteAsync(operation, parameters, token)`:
  - connects hardware endpoint,
  - invokes runtime operation,
  - disconnects in `finally`.
- `[DriverOperation] MeasureVoltage(range = 10.0m, channel = null)`:
  - resolves channel,
  - reads voltage from hardware,
  - returns `{ Value, Unit }` object.
- `[DriverOperation] Identify(channel = null)`:
  - resolves channel,
  - returns hardware identify string.

#### `Ate.Engine/DeviceIntegration/Wrappers/PsuDeviceWrapper.cs`
Methods:
- constructor: captures driver/config/hardware dependency.
- `ExecuteAsync(operation, parameters, token)`:
  - connects endpoint, invokes runtime, disconnects.
- `[DriverOperation] Identify(channel = null)`:
  - returns identify string.
- `[DriverOperation] SetVoltage(voltage, currentLimit = 1.0m, channel = null)`:
  - applies voltage + current limit and returns status text.
- `[DriverOperation] SetCurrentLimit(currentLimit, channel = null)`:
  - applies current limit and returns status text.
- `[DriverOperation] SetOutput(enabled = true, channel = null)`:
  - toggles output and returns status text.
- `[DriverOperation] OutputOff(channel = null)`:
  - convenience method to disable output.

### Module registrations

#### `Ate.Engine/DeviceIntegration/Modules/DmmDriverModule.cs`
- `Name => "DMM"`.
- `Register(IServiceCollection services)`:
  - registers `IDmmHardwareDriver` demo implementation,
  - registers configured wrapper descriptor for DMM wrapper.

#### `Ate.Engine/DeviceIntegration/Modules/PsuDriverModule.cs`
- `Name => "PSU"`.
- `Register(IServiceCollection services)`:
  - registers `IPsuHardwareDriver` demo implementation,
  - registers configured wrapper descriptor for PSU wrapper.

---

## 11) Ate.Ui project

### `Ate.Ui/Ate.Ui.csproj`
- WPF desktop application project.

### `Ate.Ui/App.xaml`
- WPF application resources/root definition.

### `Ate.Ui/App.xaml.cs`
- Application entry lifecycle code-behind (default WPF app class).

### `Ate.Ui/MainWindow.xaml`
- Main shell view layout.

### `Ate.Ui/MainWindow.xaml.cs`

#### `MainWindow`
Methods:
- constructor:
  - initializes view,
  - sets `DataContext` to `MainViewModel`.

### `Ate.Ui/Services/AteClient.cs`

#### `AteClient`
Methods:
- constructor `AteClient(baseAddress = "http://localhost:9000/")`:
  - creates configured `HttpClient`.
- `SendCommandAsync(request)`:
  - POSTs command payload and deserializes `DeviceCommandResponse`.
- `GetStatusAsync()`:
  - GETs and deserializes `EngineStatus`.
- `GetCapabilitiesAsync()`:
  - GETs and deserializes `List<DeviceCommandDefinition>`.
- `PauseAsync()`:
  - POST pause endpoint.
- `ResumeAsync()`:
  - POST resume endpoint.
- `ClearAsync()`:
  - POST clear endpoint.
- `AbortCurrentAsync()`:
  - POST abort-current endpoint.

### `Ate.Ui/ViewModels/MainViewModel.cs`

#### `MainViewModel`
Methods:
- constructor:
  - initializes collections,
  - creates commands,
  - starts async initialization + status timer wiring.
- `SendAsync()`:
  - validates selections,
  - builds `DeviceCommandRequest`,
  - sends command,
  - updates status text.
- `RefreshStatusAsync()`:
  - polls engine status and formats status line.
- `InitializeAsync()` (private):
  - loads capabilities then starts polling timer.
- `LoadCapabilitiesAsync()` (private):
  - fetches capabilities and applies or clears UI state on failure.
- `ApplyCapabilities(capabilities)` (private):
  - populates `Devices` and selects first item.
- `ExecuteControlAsync(action, actionName)` (private):
  - wrapper for pause/resume/clear/abort actions.
- `RebuildOperations()` (private):
  - updates operation list for selected device.
- `RebuildParameterInputs()` (private):
  - rebuilds editable parameter list for selected operation.
- `BuildParametersDictionary()` (private):
  - projects input rows into request payload map.
- `ConvertParameterValue(input)` (private static):
  - currently sends trimmed text values.
- `SetField<T>(...)` (private):
  - common property-changed helper.
- `OnPropertyChanged(...)` (private):
  - raises `PropertyChanged`.

#### `ParameterInputViewModel`
Methods/properties:
- constructor:
  - initializes from `CommandParameterDefinition`.
- `TypeLabel`:
  - computed display text for kind/number format.
- `ValueText` setter:
  - updates value and raises `PropertyChanged`.

---

## 12) Current extension points and practical guidance

### A) Configured-wrapper extension path (preferred for first-party families)
- Add hardware interface + implementation.
- Add wrapper with `[DriverOperation]` methods.
- Add module + `ConfiguredWrapperDescriptor`.
- Add config entries (`deviceType`, `deviceName`, `settings`).
- Optional: add explicit family definition to `KnownCapabilitiesCatalog`.

### B) Direct plugin extension path
- Build DLL with concrete `IDeviceDriver` and parameterless ctor.
- Copy to `<engine base dir>/drivers`.
- Driver currently registers as `deviceType::default`.
- By default this path does not contribute metadata to `/api/capabilities`.

---

## 13) Quick call map (request to execution)

`UI/AteClient.SendCommandAsync` -> `CommandController.EnqueueCommand` -> `CommandInvoker.Enqueue` -> `CommandInvoker.WorkerLoopAsync` -> `OperateDeviceCommand.ExecuteAsync` -> `DriverRegistry.TryResolve` -> `Wrapper.ExecuteAsync` -> `WrapperOperationRuntime.InvokeAsync` -> `[DriverOperation]` method.

Status path:

`UI timer` -> `AteClient.GetStatusAsync` -> `StatusController.GetStatus` -> `CommandInvoker`/`DriverRegistry` state.
