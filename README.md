# ATE-SYSTEM-POC

Proof-of-concept Automated Test Equipment (ATE) stack with three projects:
- `Ate.Contracts`: shared DTOs used by both client and engine.
- `Ate.Engine`: self-hosted HTTP execution engine with pluggable driver modules.
- `Ate.Ui`: WPF client that renders command forms from runtime capabilities.

## Repository tree (current)

```text
ATE-SYSTEM-POC/
├── ATE-SYSTEM-POC.sln
├── ADD_NEW_DRIVER.md
├── ADD_DLL_DRIVER.md
├── PROJECT_STATE_REVIEW.md
├── FULL_PROJECT_WALKTHROUGH.md
├── README.md
│
├── Ate.Contracts/
│   ├── Ate.Contracts.csproj
│   ├── KnownCapabilitiesCatalog.cs
│   └── Models.cs
│
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
│       ├── EngineRuntime.cs
│       ├── Program.cs
│       ├── ServiceProviderDependencyResolver.cs
│       ├── Startup.cs
│       └── Configuration/
│           └── EngineConfiguration.cs
│
└── Ate.Ui/
    ├── App.xaml
    ├── App.xaml.cs
    ├── Ate.Ui.csproj
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── Services/
    │   └── AteClient.cs
    └── ViewModels/
        └── MainViewModel.cs
```

## Runtime behavior

1. Engine starts, discovers `IDriverModule` types (built-in + optional plugin assemblies in `drivers/`).
2. Modules register hardware services and `ConfiguredWrapperDescriptor` mappings.
3. Engine loads `engine-config.json` and uses `ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory` to instantiate wrappers.
4. `WrapperOperationRuntime` resolves capability contracts from `Ate.Contracts/KnownCapabilitiesCatalog` for known device types (DMM/PSU), and falls back to `[DriverOperation]` reflection for unknown/plugin wrappers. For known families, startup now validates wrapper signatures against catalog contracts and fails fast on drift.
5. UI calls `GET /api/capabilities` as its runtime capability source of truth (no client-side baked-in capability fallback), renders dynamic operation parameter forms, and sends commands to `POST /api/command`.
6. `CommandInvoker` executes queued commands, while status/control endpoints expose and manage queue state.

## HTTP API summary

- `GET /api/capabilities` → available devices + operations + parameters.
- `POST /api/command` → enqueue command (`deviceName` is required to target the configured device instance).
- `GET /api/status` → engine state, queue depth, current command, last error, loaded drivers.
- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

## Who chooses the driver? (client vs engine)

- **At startup**, engine loads `engine-config.json` and registers available driver instances/wrappers.
- **At command time**, client chooses target intent by sending `deviceType` + `deviceName` in `POST /api/command`.
- **At command time**, engine performs final resolution from the pre-registered drivers in this order:
  1. exact `deviceType::deviceName`.
- Therefore, always target a specific configured instrument by passing its required `deviceName`.

Engine base address is `http://localhost:9000/`.

Engine logging is wired through Serilog (console + rolling file logs under `Ate.Engine/bin/<Configuration>/net472/logs`).
No implicit default device selection is performed for configured devices; requests must provide `deviceName`.

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

Each driver entry uses:
- `deviceName` (**required**) identifier for the configured instrument (also used as `driverDisplayName` in capabilities, e.g., `PSU2`)
- `deviceType` (logical family, e.g., `DMM`)
- Use `deviceName` to distinguish multiple instruments that share the same `deviceType` (for example `PSU` and `PSU2`).
- `settings` (string dictionary used for wrapper constructor binding)

Special constructor binding behavior for configured wrappers:
- `driverId` constructor parameter is populated from config `deviceType`.
- Constructor parameter names are matched against `settings` keys (case-insensitive).
- `endpoint` and `target` constructor parameters support direct keys or `endpointFormat` / `targetFormat` templating.
- Remaining parameters may come from DI or default constructor values.

## Extensibility

To add a new device family, follow `ADD_NEW_DRIVER.md` (configured-wrapper path).

For direct plugin assemblies loaded from `drivers/*.dll`, see `ADD_DLL_DRIVER.md`.



## Standalone client integration

See `STANDALONE_ATECLIENT_GUIDE.md` for using `Ate.Engine` with a custom/non-WPF `AteClient`.
