# Driver Modules

A module wires a device family into DI and exposes a wrapper type for config-based instantiation.

## Contract

Implement `IDriverModule`:

- `Name` is informational.
- `Register(IServiceCollection services)` must register:
  1. required builder dependencies for the wrapper constructor,
  2. one `ConfiguredWrapperDescriptor(deviceType, wrapperType)`.

## Built-in module examples

- `NiDaqMxDriverModule`
  - registers `INiDaqMxDriverBuilderFactory -> NiDaqMxHardwareDriverBuilderFactory`
  - registers descriptor `("NiDaqMx", typeof(NiDaqMxDeviceWrapper))`

- `PsuDriverModule`
  - registers `IPsuDriverBuilderFactory -> DemoPsuHardwareDriverBuilderFactory`
  - registers descriptor `("PSU", typeof(PsuDeviceWrapper))`

## How config resolves wrappers

For each entry in `engine-config.json`:

- Registrar validates `deviceType` and required `deviceName`.
- Registrar resolves descriptor by config `deviceType`.
- Factory creates wrapper using constructor parameter resolution rules.
- Registry stores wrapper by `deviceType::deviceName`.

`ConfiguredWrapperFactory` constructor argument precedence:

1. parameter `driverId` gets config `deviceType`
2. `settings[parameterName]`
3. `endpoint`/`target` direct value or format expansion
4. DI service by parameter type
5. constructor default value

## Conventions for reliable behavior

- Prefer a single public wrapper constructor.
- Keep constructor parameter names stable and descriptive (`driverId`, `address`, `channel`, `endpoint`, `target`).
- Keep wrapper methods public and annotate invokable operations with `[DriverOperation]`.
- Avoid duplicate operation names (including attribute aliases) because duplicates are rejected.
- Keep wrapper method signatures in sync with `KnownCapabilitiesCatalog` for known families.
