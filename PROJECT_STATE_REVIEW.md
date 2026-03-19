# ATE-SYSTEM-POC — Current State Review

This review reflects the repository as it exists now.


## 1) Current architecture snapshot

### Projects
- **Ate.Contracts**: request/response DTOs, status model, and capability contract catalog for known families (`KnownCapabilitiesCatalog`).
- **Ate.Engine**: runtime host, API controllers, command queue, driver registration, contract-first capability resolution with reflection fallback.
- **Ate.Ui**: WPF MVVM client that consumes capabilities and submits commands.

### Execution flow
1. UI loads capability definitions from `GET /api/capabilities`.
2. UI renders operation and parameter inputs from metadata.
3. UI sends commands to `POST /api/command`.
4. `CommandInvoker` dequeues and executes `OperateDeviceCommand`.
5. Selected wrapper executes operation through `WrapperOperationRuntime`.

## 2) Engine composition details

`EngineRuntime.Start()` currently does the following:
1. Discovers plugin assemblies from `drivers` folder.
2. Discovers all `IDriverModule` types (built-in + plugins).
3. Builds DI container with core services and controllers.
4. Loads `engine-config.json`.
5. Registers configured wrappers via `ConfiguredWrapperRegistrar`.
6. Loads direct plugin `IDeviceDriver` implementations through `DriverLoader`.
7. Starts queue worker and OWIN host.

## 3) Driver integration model (current and active)

### Required artifacts for a new family
- wrapper (`IDeviceDriver`) with `[DriverOperation]` methods,
- module (`IDriverModule`) that registers hardware services and descriptor,
- config entry in `engine-config.json`.

### No provider class requirement
Per-family configured-wrapper provider classes are not used in the active model.

### Descriptor selection
If `wrapperType` is set in config, matching can occur against:
- descriptor `DeviceType`,
- wrapper type name,
- wrapper full type name.

Otherwise selection falls back to config `deviceType`.

## 4) Constructor binding behavior

Configured wrapper constructor arguments are resolved in this order:
1. `driverId` parameter from config.
2. `settings[parameterName]`.
3. computed `endpoint` or `target` (from direct value or format template).
4. DI service by parameter type.
5. default parameter value.

Formatting notes:
- `endpointFormat` and `targetFormat` replace `{key}` placeholders with values from `settings`.
- missing placeholders become empty strings.

## 5) Capability generation behavior

`WrapperOperationRuntime.BuildDefinition` now follows a contract-first flow:
- for known built-in families (`DMM`, `PSU`), returns explicit definitions from `Ate.Contracts.KnownCapabilitiesCatalog`,
- for unknown/plugin families, reflects public instance methods with `[DriverOperation]`,
- reflection fallback infers parameter types (`String`, `Integer`, `Decimal`, `Boolean`) and number format metadata.

Duplicate operation names on a wrapper type throw an error.

## 6) Command runtime behavior

`CommandInvoker`:
- uses an in-memory concurrent queue,
- has state values like `Stopped`, `Running`, `Paused`, `Stopping`,
- supports `Pause`, `Resume`, `ClearPending`, and `AbortCurrent`,
- tracks `CurrentCommand` and `LastError`.

Cancellation and command failures are logged and surfaced in `LastError`.

## 6.1) Driver selection lifecycle

- Startup phase: configured wrappers/drivers are loaded and registered in `DriverRegistry`.
- Command phase: each `POST /api/command` resolves a driver using `(deviceType, driverId)` with fallback to `default` and then first device-type match.
- Practical rule: client chooses intent by sending `driverId`; engine chooses final match from preloaded registrations.

## 7) Built-in integrations

- **DMM**
  - wrapper: `DmmDeviceWrapper`
  - module: `DmmDriverModule`
  - hardware: `DemoDmmHardwareDriver`
  - operations: `MeasureVoltage`, `Identify`

- **PSU**
  - wrapper: `PsuDeviceWrapper`
  - module: `PsuDriverModule`
  - hardware: `DemoPsuHardwareDriver`
  - operations: `Identify`, `SetVoltage`, `SetCurrentLimit`, `SetOutput`, `OutputOff`

## 8) UI behavior status

`MainViewModel` currently:
- loads capabilities at startup,
- retrieves capability definitions from `GET /api/capabilities` (engine-driven runtime source),
- rebuilds parameter inputs from selected device + operation,
- converts input strings into typed payloads (`int`, `decimal`, `bool`, `string`),
- polls `/api/status` every second,
- supports pause/resume/clear/abort commands.

## 9) Known constraints

- Queue is in-memory only (no persistence).
- Wrapper operation invocation currently expects synchronous wrapper operation methods.
- Command binding requires input for every operation parameter, and missing values fail fast at invocation time.
- Plugin discovery is filesystem-based (`drivers/*.dll`) with best-effort load.

## 10) Recommended next steps

1. Add automated tests around constructor binding edge cases.
2. Add integration tests for capability generation and command execution.
3. Add structured logging context (device type, driver id, operation, command id).
4. Add optional persistence/replay model for queued commands if durability is required.
5. Keep `KnownCapabilitiesCatalog` synchronized whenever built-in wrapper method signatures change.
6. Cross-reference onboarding docs so driver-extension paths are explicit:
   - use `ADD_NEW_DRIVER.md` for built-in/configured wrappers,
   - use `ADD_DLL_DRIVER.md` for filesystem plugin DLL onboarding.
