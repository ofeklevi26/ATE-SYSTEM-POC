# Ate.Engine

Engine project for command queuing, wrapper execution, capability discovery, and HTTP hosting.


## What this project hosts

- OWIN + Web API HTTP host at `http://localhost:9000/`.
- Command queue worker (`CommandInvoker`) with pause/resume/clear/abort controls.
- Driver registration and lookup (`DriverRegistry`).
- Configured wrapper bootstrapping (`ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory`).
- Runtime operation reflection (`WrapperOperationRuntime`) for execution, with capability metadata sourced from hardcoded contracts for known device types and reflection fallback for unknown/plugin wrappers; known-family wrappers are validated against catalog signatures at startup and fail fast on drift.

## Startup sequence (`EngineRuntime.Start`)

1. Initialize Serilog (console + rolling file logs in `<engine base dir>/logs`).
2. Load plugin assemblies from `<engine base dir>/drivers/*.dll`.
3. Discover `IDriverModule` implementations from built-in and plugin assemblies.
4. Build DI container with logger, registry, invoker, registrar, and API controllers.
5. Load `engine-config.json` into `EngineConfiguration`.
6. Register configured wrappers from config.
7. Load any direct `IDeviceDriver` plugin implementations via `DriverLoader`.
8. Start command invoker worker.
9. Start OWIN host and Web API routes (with global unhandled API exception logging).

## Driver selection responsibility

- At startup, engine registers configured wrappers/driver instances from `engine-config.json` into `DriverRegistry`.
- Client request (`POST /api/command`) specifies `deviceType` and required `deviceName` per command.
- Engine resolves the concrete driver per command by exact `deviceType::deviceName`.
- If a request references an unknown configured name, command validation fails.

## API controllers

- `CommandController` (`api/command`): validates request and enqueues `OperateDeviceCommand` (request `deviceName` must match a configured `engine-config.json` device entry for the given `deviceType`).
- `StatusController` (`api/status`): reports state, queue length, current command, last error, loaded drivers; errors are logged and surfaced as 500 responses.
- `EngineController` (`api/engine/*`): pause/resume/clear/abort-current controls.
- `CapabilitiesController` (`api/capabilities`): returns discovered `DeviceCommandDefinition` data and logs a summary of configured devices and operation counts.

## Configured wrapper model

Required per family:
1. Wrapper implementing `IDeviceDriver` with `[DriverOperation]` methods.
2. Module implementing `IDriverModule` registering hardware DI and one `ConfiguredWrapperDescriptor`.
3. Matching driver entries in `engine-config.json`.

No per-device configured-wrapper provider class is required.

## Current built-in families

- DMM (`DmmDeviceWrapper`, `DmmDriverModule`, `DemoDmmHardwareDriver`)
- PSU (`PsuDeviceWrapper`, `PsuDriverModule`, `DemoPsuHardwareDriver`)

## Related docs

- Root architecture + repo map: `../README.md`
- Driver onboarding guide (configured wrappers): `../ADD_NEW_DRIVER.md`
- Direct plugin DLL guide: `../ADD_DLL_DRIVER.md`
- State review and design notes: `../PROJECT_STATE_REVIEW.md`
- Module-specific conventions: `DeviceIntegration/Modules/README.md`



## Logging

- `ILogger` remains the engine logging abstraction.
- `SerilogBootstrapper` configures Serilog sinks (console + rolling file).
- `SerilogLogger` adapts Serilog into the engine `ILogger` interface so existing components log without direct Serilog dependency.
- `ApiExceptionLogger` captures unhandled Web API pipeline exceptions with request/controller/action context.
- `EngineRuntime.Start` and `Dispose` log startup/shutdown failures (including module discovery/reflection errors) before rethrowing or continuing cleanup.
