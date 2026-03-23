# Driver Modules

A driver module wires a device family into DI and publishes wrapper descriptors for config-based instantiation.

## Contract

Implement `IDriverModule`:

- `Name` is informational.
- `Register(IServiceCollection services)` must register:
  1. hardware/services needed by wrapper constructor,
  2. one `ConfiguredWrapperDescriptor(deviceType, wrapperType)` per wrapper family.

## Built-in examples

- `DmmDriverModule`
  - registers `IDmmHardwareDriver -> DemoDmmHardwareDriver`
  - registers descriptor `("DMM", typeof(DmmDeviceWrapper))`

- `PsuDriverModule`
  - registers `IPsuHardwareDriver -> DemoPsuHardwareDriver`
  - registers descriptor `("PSU", typeof(PsuDeviceWrapper))`

## How config resolves wrappers

For each `engine-config.json` entry:

- descriptor is matched by configured `deviceType`,
- `deviceName` is required and becomes the runtime instance identifier.

`ConfiguredWrapperFactory` constructor resolution order:

1. `driverId` <= `deviceType`
2. `settings[parameterName]`
3. formatted `endpoint` / `target`
4. DI services
5. parameter default values

## Conventions

- Prefer one public wrapper constructor.
- Keep constructor parameter names stable (`driverId`, `address`, `channel`, `endpoint`, etc.).
- Keep exposed operations public and marked `[DriverOperation]`.
- Avoid duplicate operation names/aliases; duplicates are rejected.
