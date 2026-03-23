# ATE-SYSTEM-POC — Current State Review

This review reflects the repository state as of March 2026.

## 1) Architecture snapshot

Projects:

- **Ate.Contracts**: DTOs and capability schema objects.
- **Ate.Engine**: runtime host, API controllers, command queue, driver registry.
- **Ate.Ui**: WPF client using runtime capabilities.

Execution flow:

1. Client loads `/api/capabilities`.
2. Client submits `/api/command` with `deviceType + deviceName + operation`.
3. `CommandInvoker` dequeues `OperateDeviceCommand`.
4. Wrapper execution routes through `WrapperOperationRuntime`.

## 2) Engine startup composition

`EngineRuntime.Start()` currently:

1. loads plugin assemblies from `drivers/*.dll`,
2. discovers all `IDriverModule` types,
3. builds DI,
4. loads `engine-config.json`,
5. registers configured wrappers,
6. loads direct plugin `IDeviceDriver` types,
7. starts queue worker and OWIN host.

## 3) Driver integration model

Configured-wrapper path (primary model):

- wrapper implements `IDeviceDriver` + `[DriverOperation]` methods,
- module implements `IDriverModule` and registers `ConfiguredWrapperDescriptor`,
- config entry provides `deviceType`, `deviceName`, and `settings`.

Direct plugin path:

- parameterless `IDeviceDriver` classes discovered from plugin assemblies,
- registered under `deviceName = "default"`.

## 4) Constructor binding behavior

`ConfiguredWrapperFactory` parameter resolution order:

1. `driverId` -> config `deviceType`
2. `settings[parameterName]`
3. formatted `endpoint`/`target` values
4. DI by parameter type
5. constructor default value

## 5) Capability generation behavior

`WrapperOperationRuntime.BuildDefinition`:

- known families (`DMM`, `PSU`) use `KnownCapabilitiesCatalog`,
- unknown families use reflection on `[DriverOperation]` methods,
- duplicate operation names are rejected.

Known-family wrappers are validated against catalog signatures at startup.

## 6) Command runtime behavior

`CommandInvoker`:

- uses in-memory queue,
- supports `Pause`, `Resume`, `ClearPending`, `AbortCurrent`,
- tracks `CurrentCommand` and `LastError`.

`CommandController` rejects commands with missing `deviceType`, `deviceName`, or `operation`, and validates invocation signature/types before enqueue.

## 7) Built-in integrations

- **DMM**: `DmmDeviceWrapper`, `DmmDriverModule`, `DemoDmmHardwareDriver`
  - operations: `Identify`, `MeasureVoltage`
- **PSU**: `PsuDeviceWrapper`, `PsuDriverModule`, `DemoPsuHardwareDriver`
  - operations: `Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`

## 8) Current constraints

- queue persistence is not implemented,
- plugin loading is filesystem-based,
- configured driver selection requires exact `deviceType::deviceName`,
- wrapper operation methods are expected to be synchronous (runtime wraps return into `Task<object>`).

## 9) Recommended next steps

1. Add tests for constructor-binding edge cases.
2. Add integration tests for capabilities and command execution.
3. Improve structured logging context around command lifecycle.
4. Add durability/persistence if queue replay is required.
5. Keep catalog and wrapper signatures synchronized for known families.
