# Driver Modules

A module wires a device family into DI and exposes a wrapper type for config-based instantiation.


## Contract

Implement `IDriverModule`:
- `Name` is informational.
- `Register(IServiceCollection services)` must register:
  1. required hardware services for the wrapper constructor,
  2. one `ConfiguredWrapperDescriptor(deviceType, wrapperType)`.

## Built-in module examples

- `DmmDriverModule`
  - registers `IDmmHardwareDriver -> DemoDmmHardwareDriver`
  - registers descriptor `("DMM", typeof(DmmDeviceWrapper))`

- `PsuDriverModule`
  - registers `IPsuHardwareDriver -> DemoPsuHardwareDriver`
  - registers descriptor `("PSU", typeof(PsuDeviceWrapper))`

## How config resolves wrappers

For each entry in `engine-config.json`:
- resolver matches descriptor by configured `deviceType`.
- `deviceName` is required and identifies the configured instrument within that `deviceType`.

Then `ConfiguredWrapperFactory` constructs the wrapper from:
1. config `deviceType` → constructor parameter `driverId`
2. `settings[parameterName]`
3. formatted `endpoint` / `target` expansion
4. DI services
5. default constructor values

## Conventions for reliable behavior

- Prefer exactly one public wrapper constructor.
- Keep constructor parameter names stable and descriptive (`driverId`, `address`, `channel`, `endpoint`).
- Keep wrapper methods public and mark exposed operations with `[DriverOperation]`.
- Avoid duplicate operation names (including `[DriverOperation(Name=...)]` aliases), because duplicates are rejected.
