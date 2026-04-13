# ATE-SYSTEM-POC — Current State Review

This review reflects the repository as currently implemented.

## 1) Architecture snapshot

### Projects

- **Ate.Contracts**: DTOs and capability schema types (`Models.cs`) plus known-family catalog (`KnownCapabilitiesCatalog`).
- **Ate.Engine**: runtime host, API controllers, driver registration, capability generation, and command queue execution.
- **Ate.Ui**: WPF MVVM client that consumes runtime capabilities and submits commands.

### Execution flow

1. UI/client loads definitions from `GET /api/capabilities`.
2. Client submits command to `POST /api/command` with `deviceType` + `deviceName` + `operation`.
3. `CommandInvoker` dequeues `OperateDeviceCommand`.
4. Command resolves driver through `DriverRegistry.TryResolve(deviceType, deviceName)`.
5. Wrapper executes operation through `WrapperOperationRuntime`.

## 2) Engine startup composition

`EngineRuntime.Start()` currently:

1. creates startup logger,
2. discovers plugin assemblies from `drivers/*.dll`,
3. discovers `IDriverModule` types,
4. builds DI container,
5. loads `engine-config.json`,
6. registers configured wrappers via `ConfiguredWrapperRegistrar`,
7. starts queue worker,
8. starts OWIN host at `http://localhost:9000/`.

## 3) Driver integration model (active)

### Configured wrappers (primary path)

Required artifacts:

- wrapper implementing `IDeviceDriver` with `[DriverOperation]` methods,
- module implementing `IDriverModule` that registers a `ConfiguredWrapperDescriptor`,
- config entries in `engine-config.json` with required `deviceName` and `deviceType`.

Configured wrappers are registered as exact keys: `deviceType::deviceName`.

## 4) Constructor binding behavior

`ConfiguredWrapperFactory` resolves constructor args in this order:

1. `driverId` parameter => config `deviceType`,
2. `settings[parameterName]`,
3. computed `endpoint` or `target` (direct value or format template),
4. DI service by type,
5. default parameter value.

Multiple equal-best resolvable constructors are rejected as ambiguous.

## 5) Capability generation behavior

`WrapperOperationRuntime.BuildDefinition` follows a contract-first flow:

- known families (`NiDaqMx`, `PSU`) use `KnownCapabilitiesCatalog` definitions,
- known-family wrapper signatures are validated against catalog operations/parameters and throw on drift,
- unknown families use reflection fallback over `[DriverOperation]` methods,
- duplicate operation names are rejected.

## 6) Command runtime behavior

`CommandInvoker`:

- uses in-memory `ConcurrentQueue<IAteCommand>`,
- exposes states: `Stopped`, `Running`, `Paused`, `Stopping`,
- supports `Pause`, `Resume`, `ClearPending`, `AbortCurrent`,
- tracks `CurrentCommand` and `LastError`.

Command validation now occurs before enqueue in `CommandController`:

- missing required request fields are rejected,
- unsupported operation / missing parameter / type mismatch are rejected,
- successful requests are enqueued with generated `ServerCommandId`.

## 7) Built-in integrations in repo

- **NiDaqMx** (`NiDaqMxDeviceWrapper`, `NiDaqMxDriverModule`, `NiDaqMxHardwareDriver`)
  - operations: `Identify`, `SetContiniousFrequency`
- **PSU** (`PsuDeviceWrapper`, `PsuDriverModule`, `DemoPsuHardwareDriver`)
  - operations: `Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`

Default `engine-config.json` includes:

- `NiDaqMx::NiDaqMx`
- `PSU::PSU`
- `PSU::PSU2`

## 8) UI behavior status

`MainViewModel` currently:

- loads capabilities at startup,
- rebuilds operation list and parameter inputs from selected capability,
- sends all parameter values as strings in request payload,
- polls `/api/status` every second,
- supports pause/resume/clear/abort controls.

Type coercion is performed server-side by normalizer + runtime conversion.

## 9) Known constraints

- Queue is in-memory only (no persistence).
- Wrapper operation invocation expects synchronous operation methods (invoked via reflection and wrapped in `Task.FromResult`).
- Missing operation parameters fail validation.

## 10) Recommended next steps

1. Add automated tests for constructor binding edge cases and ambiguity detection.
2. Add integration tests for `/api/command` validation behavior.
3. Add tests for `KnownCapabilitiesCatalog` drift checks.
4. Add structured logging context (deviceType/deviceName/operation/commandId).
