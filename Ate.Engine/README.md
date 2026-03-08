# Ate.Engine layout

This project is organized by responsibility:

- `Host/`
  - App bootstrap, self-host startup, and configuration loading.
- `Api/`
  - HTTP controllers exposed by the engine.
- `Core/`
  - Command processing and driver registration/resolution primitives.
- `DeviceIntegration/`
  - Device-specific integration code:
    - `Wrappers/`: `IDeviceDriver` adapters used by the engine.
    - `Hardware/`: hardware-driver interfaces wrappers depend on.
    - `DemoDrivers/`: simulation/demo implementations of hardware interfaces.
- `Common/`
  - Shared technical utilities (logging, serialization helpers).

The intent is to keep wrappers independent from concrete hardware implementations
so demo drivers can be replaced by NuGet-provided hardware drivers.
