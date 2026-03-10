# Ate.Engine

For the full repository map, see root `README.md`.

## Extension model (simplified)

Driver integration is intentionally minimal:
1. Implement a wrapper (`IDeviceDriver`) and annotate operations with `[DriverOperation]`.
2. Register hardware dependencies + one `ConfiguredWrapperDescriptor(deviceType, wrapperType)` in an `IDriverModule`.
3. Add a driver entry in `engine-config.json` with `deviceType`, `driverId`, and `settings`.

At startup, the engine:
- discovers `IDriverModule` implementations,
- loads configured drivers from `engine-config.json`,
- builds wrapper constructor arguments from config `settings` and DI,
- auto-discovers wrapper operations for UI/API capability metadata.

No per-device configured-wrapper provider classes are required.
