# Ate.Engine

Engine project for command queuing, wrapper execution, capability discovery, and HTTP hosting.

## What this project hosts

- OWIN + Web API HTTP host at `http://localhost:9000/`.
- Command queue worker (`CommandInvoker`) with pause/resume/clear/abort controls.
- Driver registration and lookup (`DriverRegistry`).
- Configured wrapper bootstrapping (`ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory`).
- Runtime operation reflection (`WrapperOperationRuntime`) for execution, with capability metadata sourced from hardcoded contracts for known device types and reflection fallback for unknown/plugin wrappers; known-family wrappers are validated against catalog signatures at startup and fail fast on drift.

## Startup sequence (`EngineRuntime.Start`)

1. Load plugin assemblies from `<engine base dir>/drivers/*.dll`.
2. Discover `IDriverModule` implementations from built-in and plugin assemblies.
3. Build DI container with logger, registry, invoker, registrar, and API controllers.
4. Load `engine-config.json` into `EngineConfiguration`.
5. Register configured wrappers from config.
6. Load any direct `IDeviceDriver` plugin implementations via `DriverLoader`.
7. Start command invoker worker.
8. Start OWIN host and Web API routes.

## API controllers

- `CommandController` (`api/command`): validates request and enqueues `OperateDeviceCommand`.
- `StatusController` (`api/status`): reports state, queue length, current command, last error, loaded drivers.
- `EngineController` (`api/engine/*`): pause/resume/clear/abort-current controls.
- `CapabilitiesController` (`api/capabilities`): returns discovered `DeviceCommandDefinition` data.

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
- Driver onboarding guide: `../ADD_NEW_DRIVER.md`
- State review and design notes: `../PROJECT_STATE_REVIEW.md`
- Module-specific conventions: `DeviceIntegration/Modules/README.md`

