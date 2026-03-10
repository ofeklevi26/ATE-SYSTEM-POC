# Ate.Engine

For the complete repository file-by-file tree and responsibilities, see the root `README.md`.

Engine-specific structure is organized by responsibility:
- `Host/` for bootstrapping and hosting,
- `Api/` for HTTP controllers,
- `Core/` for queue/command/driver runtime,
- `DeviceIntegration/` for wrapper + hardware abstraction + demo implementations,
- `Common/` for logging and serialization utilities.


Configured wrapper providers
- `IConfiguredWrapperProvider` is the extension seam for nugget-specific wrapper instantiation.
- Wrapper operations are now discovered from methods marked with `[DriverOperation]` so operation/parameter metadata no longer needs to be hand-authored in providers.
- Providers can be discovered from `drivers/*.dll` at startup.
- `DriverInstanceConfiguration.Settings` supports custom per-wrapper arguments and endpoint formatting.
