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
- Providers are registered in host startup and selected via `engine-config.json` entries.
- `DriverInstanceConfiguration.Settings` supports custom per-wrapper arguments and endpoint formatting.
