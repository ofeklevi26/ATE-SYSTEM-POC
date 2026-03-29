# ATE-SYSTEM-POC

Proof-of-concept Automated Test Equipment (ATE) stack with three projects:

- `Ate.Contracts`: shared DTOs and runtime capability metadata models.
- `Ate.Engine`: self-hosted OWIN/Web API command execution engine with configurable wrapper registration.
- `Ate.Ui`: WPF MVVM client that builds operation forms from `/api/capabilities`.

## Repository tree (current)

```text
ATE-SYSTEM-POC/
├── ATE-SYSTEM-POC.sln
├── README.md
├── FULL_PROJECT_WALKTHROUGH.md
├── PROJECT_STATE_REVIEW.md
├── STANDALONE_ATECLIENT_GUIDE.md
├── ADD_NEW_DRIVER.md
├── ADD_DLL_DRIVER.md
│
├── Ate.Contracts/
│   ├── Ate.Contracts.csproj
│   ├── Models.cs
│   └── KnownCapabilitiesCatalog.cs
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
│   │   │   ├── IAteCommand.cs
│   │   │   ├── OperateDeviceCommand.cs
│   │   │   └── CommandInvoker.cs
│   │   └── Drivers/
│   │       ├── IDeviceDriver.cs
│   │       ├── IDriverModule.cs
│   │       ├── DriverRegistry.cs
│   │       ├── DriverLoader.cs
│   │       ├── DriverOperationAttribute.cs
│   │       ├── WrapperOperationRuntime.cs
│   │       ├── ConfiguredWrapperDescriptor.cs
│   │       ├── ConfiguredWrapperFactory.cs
│   │       ├── ConfiguredWrapperRegistrar.cs
│   │       └── ParameterTypeMismatchException.cs
│   ├── DeviceIntegration/
│   │   ├── Hardware/
│   │   │   ├── INiDaqMxHardwareDriver.cs
│   │   │   └── IPsuHardwareDriver.cs
│   │   ├── DemoDrivers/
│   │   │   ├── NiDaqMxHardwareDriver.cs
│   │   │   └── DemoPsuHardwareDriver.cs
│   │   ├── Wrappers/
│   │   │   ├── NiDaqMxDeviceWrapper.cs
│   │   │   └── PsuDeviceWrapper.cs
│   │   └── Modules/
│   │       ├── NiDaqMxDriverModule.cs
│   │       ├── PsuDriverModule.cs
│   │       └── README.md
│   └── Host/
│       ├── Program.cs
│       ├── EngineRuntime.cs
│       ├── Startup.cs
│       ├── ServiceProviderDependencyResolver.cs
│       └── Configuration/
│           └── EngineConfiguration.cs
│
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

## Runtime behavior (current)

1. `Program.Main` initializes Serilog and starts `EngineRuntime`.
2. Engine loads plugin assemblies from `<base>/drivers/*.dll` (best effort).
3. Engine discovers all `IDriverModule` implementations (built-in + plugins) and lets each module register DI services and wrapper descriptors.
4. Engine loads `engine-config.json` and materializes configured wrappers through `ConfiguredWrapperRegistrar` + `ConfiguredWrapperFactory`.
5. Engine builds per-device command definitions through `WrapperOperationRuntime`:
   - known families (`NiDaqMx`, `PSU`) are generated from `KnownCapabilitiesCatalog` and validated against reflected wrapper signatures,
   - unknown families use reflection over `[DriverOperation]` methods.
6. Engine optionally loads direct plugin `IDeviceDriver` implementations via `DriverLoader`.
7. Engine starts `CommandInvoker` and HTTP API at `http://localhost:9000/`.

## HTTP API summary

- `GET /api/capabilities`
  - Returns device definitions registered with metadata.
  - Includes configured wrappers (for example: `NiDaqMx`, `PSU`, `PSU2`).
- `POST /api/command`
  - Enqueues a command.
  - Required request fields: `deviceType`, `deviceName`, `operation`.
- `GET /api/status`
  - Returns queue state (`State`, `QueueLength`, `CurrentCommand`, `LastError`) and loaded keys from `DriverRegistry`.
- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

## Driver selection model

- Engine registers concrete targets at startup using keys shaped as `deviceType::deviceName`.
- Client must specify `deviceType` + `deviceName` per `POST /api/command`.
- Resolution is exact match only (`TryResolve(deviceType, deviceName)`); there is no implicit default fallback for configured wrappers.

## Current default configuration (`Ate.Engine/engine-config.json`)

Configured devices in repo:

- `NiDaqMx::NiDaqMx`
- `PSU::PSU`
- `PSU::PSU2`

Each entry provides:

- `deviceName` (required; command-time selector)
- `deviceType` (wrapper family selector)
- `settings` (constructor-binding string dictionary)

## Constructor binding behavior for configured wrappers

`ConfiguredWrapperFactory` resolves constructor parameters in this order:

1. parameter named `driverId` => config `deviceType`
2. matching `settings[parameterName]`
3. `endpoint` / `target` value (direct key or formatted template)
4. DI service by parameter type
5. parameter default value

If no constructor is resolvable, or constructor resolution is ambiguous, startup registration fails for that wrapper.

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

## Related docs

- Engine internals: `Ate.Engine/README.md`
- Driver family onboarding (configured wrappers): `ADD_NEW_DRIVER.md`
- Direct DLL plugin onboarding: `ADD_DLL_DRIVER.md`
- Headless client guide: `STANDALONE_ATECLIENT_GUIDE.md`
- Detailed walkthrough: `FULL_PROJECT_WALKTHROUGH.md`
- Current-state review: `PROJECT_STATE_REVIEW.md`
