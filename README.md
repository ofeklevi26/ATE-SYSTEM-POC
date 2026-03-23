# ATE-SYSTEM-POC

Proof-of-concept Automated Test Equipment (ATE) stack with three projects:

- `Ate.Contracts`: shared transport models and capability schema types.
- `Ate.Engine`: self-hosted HTTP execution engine with pluggable driver model.
- `Ate.Ui`: WPF client that renders forms from runtime capability metadata.

## Repository structure (current)

```text
ATE-SYSTEM-POC/
├── ATE-SYSTEM-POC.sln
├── README.md
├── FULL_PROJECT_WALKTHROUGH.md
├── PROJECT_STATE_REVIEW.md
├── ADD_NEW_DRIVER.md
├── ADD_DLL_DRIVER.md
├── STANDALONE_ATECLIENT_GUIDE.md
├── Ate.Contracts/
│   ├── Ate.Contracts.csproj
│   ├── Models.cs
│   └── KnownCapabilitiesCatalog.cs
├── Ate.Engine/
│   ├── Ate.Engine.csproj
│   ├── engine-config.json
│   ├── README.md
│   ├── Api/
│   │   ├── ApiExceptionLogger.cs
│   │   └── Controllers/
│   │       ├── CapabilitiesController.cs
│   │       ├── CommandController.cs
│   │       ├── EngineController.cs
│   │       └── StatusController.cs
│   ├── Common/
│   │   ├── Infrastructure/
│   │   │   ├── ILogger.cs
│   │   │   ├── SerilogBootstrapper.cs
│   │   │   └── SerilogLogger.cs
│   │   └── Serialization/
│   │       └── ParameterValueNormalizer.cs
│   ├── Core/
│   │   ├── Commands/
│   │   │   ├── CommandInvoker.cs
│   │   │   ├── IAteCommand.cs
│   │   │   └── OperateDeviceCommand.cs
│   │   └── Drivers/
│   │       ├── ConfiguredWrapperDescriptor.cs
│   │       ├── ConfiguredWrapperFactory.cs
│   │       ├── ConfiguredWrapperRegistrar.cs
│   │       ├── DriverLoader.cs
│   │       ├── DriverOperationAttribute.cs
│   │       ├── DriverRegistry.cs
│   │       ├── IDeviceDriver.cs
│   │       ├── IDriverModule.cs
│   │       ├── ParameterTypeMismatchException.cs
│   │       └── WrapperOperationRuntime.cs
│   ├── DeviceIntegration/
│   │   ├── DemoDrivers/
│   │   │   ├── DemoDmmHardwareDriver.cs
│   │   │   └── DemoPsuHardwareDriver.cs
│   │   ├── Hardware/
│   │   │   ├── IDmmHardwareDriver.cs
│   │   │   └── IPsuHardwareDriver.cs
│   │   ├── Modules/
│   │   │   ├── DmmDriverModule.cs
│   │   │   ├── PsuDriverModule.cs
│   │   │   └── README.md
│   │   └── Wrappers/
│   │       ├── DmmDeviceWrapper.cs
│   │       └── PsuDeviceWrapper.cs
│   └── Host/
│       ├── Configuration/
│       │   └── EngineConfiguration.cs
│       ├── EngineRuntime.cs
│       ├── Program.cs
│       ├── ServiceProviderDependencyResolver.cs
│       └── Startup.cs
└── Ate.Ui/
    ├── Ate.Ui.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── Services/
    │   └── AteClient.cs
    └── ViewModels/
        └── MainViewModel.cs
```

## Runtime behavior

1. Engine starts and discovers built-in + plugin `IDriverModule` types.
2. Modules register services and `ConfiguredWrapperDescriptor` mappings.
3. Engine loads `Ate.Engine/engine-config.json` and creates configured wrappers.
4. Engine registers each configured instance by exact key: `deviceType::deviceName`.
5. Engine also scans plugin assemblies for parameterless `IDeviceDriver` types and registers those as `deviceType::default`.
6. `/api/capabilities` exposes runtime capabilities. For known families (`DMM`, `PSU`) metadata comes from `KnownCapabilitiesCatalog`; other families use reflection on `[DriverOperation]` methods.
7. `POST /api/command` validates and enqueues operations into `CommandInvoker`.

## HTTP API summary

- `GET /api/capabilities` → available device instances, operations, and parameters.
- `POST /api/command` → enqueue command (`deviceType`, `deviceName`, and `operation` are required).
- `GET /api/status` → engine state, queue depth, current command, last error, loaded drivers.
- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

Engine base URL: `http://localhost:9000/`.

## Device selection model

- At startup, configured entries are loaded from `engine-config.json`.
- At command time, client chooses a specific configured instance by sending `deviceType` + `deviceName`.
- The engine resolves only exact matches. There is no fallback to a different configured name.

## Quick start

### 1) Build

```bash
dotnet build ATE-SYSTEM-POC.sln
```

### 2) Run engine

```bash
dotnet run --project Ate.Engine/Ate.Engine.csproj
```

### 3) Run UI (separate shell)

```bash
dotnet run --project Ate.Ui/Ate.Ui.csproj
```

## Configuration notes (`Ate.Engine/engine-config.json`)

Each driver entry currently uses:

- `deviceName` (required, unique inside a `deviceType`)
- `deviceType` (required)
- `settings` (string dictionary used for wrapper constructor binding)

Constructor binding priority in `ConfiguredWrapperFactory`:

1. constructor parameter named `driverId` gets config `deviceType`
2. exact `settings[parameterName]` lookup (case-insensitive)
3. special `endpoint`/`target` handling (`endpoint`, `target`, or `endpointFormat`/`targetFormat`)
4. DI service by type
5. default parameter value

## Extensibility

- Add a config-driven family: `ADD_NEW_DRIVER.md`
- Add a direct plugin DLL: `ADD_DLL_DRIVER.md`
- Use engine from your own client: `STANDALONE_ATECLIENT_GUIDE.md`
