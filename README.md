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
│   ├── engine-config.json                 # Configured device wrapper instances (type, id, IP, channel).
│   ├── README.md                          # Engine-local architecture notes.
│   │
│   ├── Host/
│   │   ├── Program.cs                     # Process entry point; wires logger/registry/invoker, loads config, starts web host.
│   │   ├── Startup.cs                     # OWIN/WebApi pipeline and route/json configuration.
│   │   ├── EngineHostContext.cs           # Shared host singletons used by API/controllers.
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
- `Ate.Ui` remains a thin client that drives the engine exclusively through HTTP contracts.
