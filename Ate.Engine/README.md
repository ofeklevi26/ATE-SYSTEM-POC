# Ate.Engine

Engine project for command queuing, wrapper execution, capability discovery, and HTTP hosting.

## What this project hosts

- OWIN + Web API self-host at `http://localhost:9000/`.
- Command queue worker (`CommandInvoker`) with pause/resume/clear/abort controls.
- Driver registration and lookup (`DriverRegistry`) keyed by `deviceType::deviceName`.
- Configured-wrapper bootstrapping (`ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory`).
- Runtime operation reflection (`WrapperOperationRuntime`) with known-family contract validation and unknown-family reflection fallback.
- Unhandled API exception logging via `ApiExceptionLogger`.

## Startup sequence (`EngineRuntime.Start`)

1. Initialize Serilog (console + rolling file under `<engine base dir>/logs`).
2. Load plugin assemblies from `<engine base dir>/drivers/*.dll`.
3. Discover `IDriverModule` implementations from engine and plugin assemblies.
4. Build DI container (logger, registry, invoker, registrar, controllers, module registrations).
5. Load `engine-config.json`.
6. Register configured wrappers from config.
7. Start command invoker worker.
8. Start OWIN host and Web API routes.

## API controllers

- `CommandController` (`api/command`)
  - Validates `DeviceType`, `DeviceName`, `Operation`.
  - Normalizes payload parameter values.
  - Validates operation invocation against wrapper metadata before enqueue.
- `StatusController` (`api/status`)
  - Returns queue state and loaded driver keys.
- `EngineController` (`api/engine/*`)
  - Pause/resume/clear/abort-current controls.
- `CapabilitiesController` (`api/capabilities`)
  - Returns `DeviceCommandDefinition` entries from registry and logs a summary.

## Configured wrapper model

Required per family:

1. Wrapper implementing `IDeviceDriver` with `[DriverOperation]` methods.
2. Module implementing `IDriverModule` that registers at least one `ConfiguredWrapperDescriptor(deviceType, wrapperType)` and any optional DI services required by wrapper constructor parameters.
3. One or more matching entries in `engine-config.json` with `deviceName`, `deviceType`, and `settings`.

No per-device provider class is required.

## How configured wrapper constructor binding works

For each selected wrapper constructor parameter:

1. `driverId` => config `deviceType`.
2. direct `settings` value by parameter name.
3. computed `endpoint` value from direct key or `endpointFormat`.
4. DI service by parameter type.
5. constructor default value.

If multiple constructors are resolvable with equal highest arity, registration fails as ambiguous.

## Capability definition behavior

`WrapperOperationRuntime.BuildDefinition`:

- Uses `KnownCapabilitiesCatalog` for known device types (`NiDaqMx`, `PSU`).
- Validates known-family reflected signatures against catalog signatures and throws on drift.
- For unknown families, reflects public instance methods decorated with `[DriverOperation]`.
- Rejects duplicate operation names/aliases.

## Driver selection responsibility

- Startup: engine registers configured targets from config.
- Command time: client chooses target intent using `deviceType` + `deviceName`.
- Runtime resolution: exact key match only (`deviceType::deviceName`).

## Built-in families in this repo

- NiDaqMx: `NiDaqMxDeviceWrapper`, `NiDaqMxDriverModule`, `NiDaqMxHardwareDriverBuilder` + `NiDaqMxHardwareDriverAdapter`
- PSU: `PsuDeviceWrapper`, `PsuDriverModule`, `DemoPsuHardwareDriverBuilder` + `DemoPsuHardwareDriverAdapter`

## Logging

- `ILogger` is the engine logging abstraction.
- `SerilogBootstrapper` configures sinks (console + rolling file).
- `SerilogLogger` adapts Serilog to `ILogger`.
- `ApiExceptionLogger` logs unhandled API exceptions with request route context.

## Related docs

- Root overview: `../README.md`
- Configured wrapper onboarding: `../ADD_NEW_DRIVER.md`
- Module conventions: `DeviceIntegration/Modules/README.md`
