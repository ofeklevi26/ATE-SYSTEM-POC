# Ate.Engine

Engine project for command queuing, wrapper execution, capability discovery, and HTTP hosting.

## What this project hosts

- OWIN + Web API host at `http://localhost:9000/`.
- Command queue worker (`CommandInvoker`) with pause/resume/clear/abort controls.
- Driver registration and lookup (`DriverRegistry`).
- Configured wrapper bootstrapping (`ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory`).
- Runtime capability and invocation layer (`WrapperOperationRuntime`) with:
  - contract-first metadata for known device families,
  - reflection fallback for unknown/plugin families,
  - startup drift validation for known families.

## Startup sequence (`EngineRuntime.Start`)

1. Initialize Serilog logger.
2. Load plugin assemblies from `<engine base dir>/drivers/*.dll`.
3. Discover `IDriverModule` implementations from built-in + plugin assemblies.
4. Build DI container.
5. Load `engine-config.json`.
6. Register configured wrappers.
7. Register direct plugin `IDeviceDriver` types.
8. Start command invoker.
9. Start OWIN host + Web API routes.

## Driver selection model

- Startup loads configured devices from `engine-config.json`.
- Commands must specify `deviceType` and `deviceName`.
- Engine resolves only exact `deviceType::deviceName` matches.

## API controllers

- `CapabilitiesController` (`api/capabilities`): runtime capability metadata.
- `CommandController` (`api/command`): validates + enqueues `OperateDeviceCommand`.
- `StatusController` (`api/status`): queue state, current command, last error, loaded drivers.
- `EngineController` (`api/engine/*`): pause/resume/clear/abort-current.

## Configured-wrapper model

Per family, provide:

1. wrapper implementing `IDeviceDriver` with `[DriverOperation]` methods,
2. module implementing `IDriverModule` registering hardware services + one `ConfiguredWrapperDescriptor`,
3. matching driver entries in `engine-config.json`.

## Built-in families

- `DMM` (`DmmDeviceWrapper`, `DmmDriverModule`, `DemoDmmHardwareDriver`)
- `PSU` (`PsuDeviceWrapper`, `PsuDriverModule`, `DemoPsuHardwareDriver`)

## Logging

- `ILogger` remains abstraction used across runtime.
- `SerilogBootstrapper` wires console + rolling file sinks.
- `SerilogLogger` bridges Serilog to `ILogger`.
- `ApiExceptionLogger` captures unhandled API pipeline exceptions.

## Related docs

- Root overview: `../README.md`
- Config-driven extension: `../ADD_NEW_DRIVER.md`
- DLL plugin extension: `../ADD_DLL_DRIVER.md`
- State review: `../PROJECT_STATE_REVIEW.md`
- Module-level conventions: `DeviceIntegration/Modules/README.md`
