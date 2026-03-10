# ATE-SYSTEM-POC

This repository contains a minimal ATE (Automated Test Equipment) proof of concept with:
- shared contracts (`Ate.Contracts`),
- an HTTP-hosted execution engine (`Ate.Engine`),
- and a WPF client (`Ate.Ui`).


Below is a **file-by-file tree** with each file's responsibility (high level only).

```text
ATE-SYSTEM-POC/
├── ATE-SYSTEM-POC.sln                     # Solution entry that groups all projects.
│
├── Ate.Contracts/
│   ├── Ate.Contracts.csproj               # Shared contracts library definition (target framework and build settings).
│   └── Models.cs                          # DTOs and capability models shared between Engine and UI.
│
├── Ate.Engine/
│   ├── Ate.Engine.csproj                  # Engine executable project and package references (OWIN/WebApi/JSON).
│   ├── engine-config.json                 # Configured device wrapper instances (type, id, provider + settings).
│   ├── README.md                          # Engine-local architecture notes.
│   │
│   ├── Host/
│   │   ├── Program.cs                     # Thin entry point that starts/stops EngineRuntime.
│   │   ├── Startup.cs                     # OWIN/WebApi pipeline and route/json configuration.
│   │   ├── EngineRuntime.cs               # Runtime composition/bootstrap (DI setup, wrapper/driver registration, web host lifecycle).
│   │   ├── ServiceProviderDependencyResolver.cs # Web API dependency resolver adapter over IServiceProvider.
│   │   └── Configuration/
│   │       └── EngineConfiguration.cs     # Loads/parses engine-config.json into typed config models.
│   │
│   ├── Api/
│   │   └── Controllers/
│   │       ├── CommandController.cs       # POST endpoint to enqueue device-operation commands.
│   │       ├── StatusController.cs        # GET endpoint to read runtime status (state, queue, current command, errors).
│   │       ├── EngineController.cs        # POST endpoints for control actions (pause/resume/clear/abort).
│   │       └── CapabilitiesController.cs  # GET endpoint exposing available device capabilities/operations.
│   │
│   ├── Core/
│   │   ├── Commands/
│   │   │   ├── IAteCommand.cs             # Command contract used by the execution queue.
│   │   │   ├── CommandInvoker.cs          # Queue worker lifecycle (enqueue, run, pause, resume, stop, abort).
│   │   │   └── OperateDeviceCommand.cs    # Concrete queued command that resolves a driver and executes an operation.
│   │   └── Drivers/
│   │       ├── IDeviceDriver.cs           # Engine-facing wrapper contract (device type/id + ExecuteAsync).
│   │       ├── IConfiguredWrapperProvider.cs # Configured-wrapper provider seam (validate/create/describe).
│   │       ├── IDriverModule.cs            # Driver-family DI module seam (register provider + hardware services).
│   │       ├── DriverRegistry.cs          # Driver registration/lookup and capability-definition storage.
│   │       └── DriverLoader.cs            # Optional plugin loader that discovers/registers drivers from assemblies.
│   │
│   ├── DeviceIntegration/
│   │   ├── Hardware/
│   │   │   ├── IDmmHardwareDriver.cs      # Hardware-level DMM interface used by wrappers.
│   │   │   └── IPsuHardwareDriver.cs      # Hardware-level PSU interface used by wrappers.
│   │   ├── Wrappers/
│   │   │   ├── DmmDeviceWrapper.cs        # DMM engine wrapper translating engine operations to DMM hardware calls.
│   │   │   └── PsuDeviceWrapper.cs        # PSU engine wrapper translating engine operations to PSU hardware calls.
│   │   ├── Modules/
│   │   │   ├── DmmDriverModule.cs        # DMM module (registers DMM provider + hardware factory).
│   │   │   ├── PsuDriverModule.cs        # PSU module (registers PSU provider + hardware factory).
│   │   │   └── README.md                 # Convention notes for adding new driver modules.
│   │   ├── Providers/
│   │   │   ├── DmmConfiguredWrapperProvider.cs # Built-in DMM configured-wrapper provider (instantiation + metadata).
│   │   │   ├── DmmConnectionSettings.cs         # DMM-specific connection/address/endpoint parsing helpers.
│   │   │   ├── PsuConfiguredWrapperProvider.cs # Built-in PSU configured-wrapper provider (instantiation + metadata).
│   │   │   └── PsuConnectionSettings.cs         # PSU-specific connection/address/endpoint parsing helpers.
│   │   └── DemoDrivers/
│   │       ├── DemoDmmHardwareDriver.cs   # Simulated DMM hardware implementation for local/testing use.
│   │       └── DemoPsuHardwareDriver.cs   # Simulated PSU hardware implementation for local/testing use.
│   │
│   └── Common/
│       ├── Infrastructure/
│       │   ├── ILogger.cs                 # Logging abstraction used across engine components.
│       │   └── ConsoleLogger.cs           # Console-based logger implementation.
│       └── Serialization/
│           └── ParameterValueNormalizer.cs# Converts incoming JSON parameter values into runtime-friendly CLR values.
│
└── Ate.Ui/
    ├── Ate.Ui.csproj                      # WPF client project and UI dependencies.
    ├── App.xaml                           # WPF app declaration/resources and startup window reference.
    ├── App.xaml.cs                        # WPF application code-behind entry class.
    ├── MainWindow.xaml                    # Main client UI layout (device/operation/parameters/controls/status).
    ├── MainWindow.xaml.cs                 # Main window code-behind that sets ViewModel as DataContext.
    ├── Services/
    │   └── AteClient.cs                   # HTTP client wrapper for calling engine API endpoints.
    └── ViewModels/
        └── MainViewModel.cs               # UI state/commands: load capabilities, build params, send commands, poll status.
```

## Architectural intent (high-level)

- `Ate.Contracts` stays implementation-agnostic and only carries shared transport models.
- `Ate.Engine` isolates runtime core from device-integration concerns and keeps wrappers separate from hardware implementations.
- `Ate.Engine` now uses constructor injection for API controllers via a service-provider-based dependency resolver, removing global host context state.
- `Ate.Ui` remains a thin client that drives the engine exclusively through HTTP contracts.


## Wiring a new nugget wrapper (minimal-change flow)

1. Implement a provider class that implements `IConfiguredWrapperProvider`.
   - Each provider owns its own connection/endpoint settings and parsing logic (no shared endpoint builder).
   - Capability metadata (`DeviceCommandDefinition`) is auto-generated from wrapper methods marked with `[DriverOperation]`.
2. Implement an `IDriverModule` that registers provider + hardware factory into DI.
3. Place the module/provider assembly in `Ate.Engine/drivers` (auto-discovered) or compile it in-engine.
4. Configure `engine-config.json` for each instance using:
   - `wrapperProviderType` (provider name/type),
   - provider-specific `settings` keys.
5. Start engine; UI fetches capabilities dynamically from `/api/capabilities`, so form fields update without UI code changes.
