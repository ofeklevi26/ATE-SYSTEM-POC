# Driver Modules

A driver family should be minimal:
1. write a wrapper (`IDeviceDriver`) with `[DriverOperation]` methods,
2. register any hardware services the wrapper needs,
3. register one `ConfiguredWrapperDescriptor(deviceType, wrapperType)`.

At startup, wrappers are instantiated from `engine-config.json` settings + DI services.
The UI capabilities are discovered directly from wrapper methods marked with `[DriverOperation]`.

## Config convention
Each configured driver entry supports:
- `deviceType`
- `driverId`
- `wrapperType` (optional override; defaults to `deviceType` descriptor)
- `settings` (constructor args such as `address`, `channel`, plus `endpoint`/`endpointFormat` or `target`/`targetFormat`)

## Why this is simpler
No per-device configured-wrapper provider classes are required anymore.
Adding a new device family is now wrapper + module (+ hardware adapter if needed).
