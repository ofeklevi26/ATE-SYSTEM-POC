# Full Project Walkthrough: ATE-SYSTEM-POC

This walkthrough reflects the current repository layout and runtime behavior.

## 1) Architecture

Solution projects:

- **Ate.Contracts** (`netstandard2.0`): shared DTOs and capability contracts.
- **Ate.Engine** (`net472`): OWIN/Web API host, driver runtime, command queue.
- **Ate.Ui** (`net6.0-windows`): WPF MVVM client for runtime capabilities.

High-level flow:

1. Engine starts and discovers modules.
2. Configured wrappers are instantiated from `engine-config.json`.
3. Drivers are registered in `DriverRegistry` as `deviceType::deviceName`.
4. UI/client loads `GET /api/capabilities`.
5. Command requests hit `POST /api/command` and are queued.
6. `CommandInvoker` executes `OperateDeviceCommand` and clients poll `/api/status`.

---

## 2) Contracts project

### `Ate.Contracts/Models.cs`

Defines request/response and metadata models.

Notable runtime shape:

- `DeviceCommandRequest` fields: `DeviceType`, `DeviceName`, `Operation`, `Parameters`, `ClientRequestId`.
- `DeviceCommandDefinition` has `DriverId` and `DriverDisplayName` metadata for capabilities.

### `Ate.Contracts/KnownCapabilitiesCatalog.cs`

Provides explicit capability definitions for `DMM` and `PSU`.

`WrapperOperationRuntime` uses this for known families and validates wrapper-method signature consistency at startup.

---

## 3) Engine host and startup

### `Ate.Engine/Host/EngineRuntime.cs`

Startup sequence:

1. Build logger (`SerilogBootstrapper`).
2. Load plugin assemblies from `drivers/*.dll`.
3. Discover/instantiate `IDriverModule` implementations.
4. Build DI container.
5. Load `engine-config.json`.
6. Register configured wrappers via `ConfiguredWrapperRegistrar`.
7. Register direct plugin `IDeviceDriver` types via `DriverLoader`.
8. Start `CommandInvoker`.
9. Start OWIN host at `http://localhost:9000/`.

### `Ate.Engine/Host/Configuration/EngineConfiguration.cs`

Current config model per driver instance:

- `DeviceName`
- `DeviceType`
- `Settings` (`Dictionary<string, string>`)

No `wrapperType`/`driverId` fields are currently part of configuration.

---

## 4) Driver registration and invocation

### `DriverRegistry`

Stores registrations by exact key: `deviceType::deviceName`.

### `ConfiguredWrapperRegistrar`

- validates required `deviceType` and `deviceName`,
- maps `deviceType` to `ConfiguredWrapperDescriptor`,
- creates wrapper via `ConfiguredWrapperFactory`,
- builds capability definition and overrides `DriverDisplayName` with `deviceName`,
- registers instance into `DriverRegistry`.

### `ConfiguredWrapperFactory`

Constructor parameter resolution order:

1. parameter named `driverId` gets config `deviceType`
2. `settings[parameterName]`
3. `endpoint`/`target` special handling
4. DI resolution
5. default constructor values

### `WrapperOperationRuntime`

- builds capability metadata (catalog-first for known families, reflection fallback otherwise),
- validates command invocation signatures and parameter conversions,
- invokes `[DriverOperation]` methods.

---

## 5) API controllers

- `CapabilitiesController` → `GET /api/capabilities`
- `CommandController` → `POST /api/command`
- `StatusController` → `GET /api/status`
- `EngineController` → pause/resume/clear/abort-current endpoints

`CommandController` requires `deviceType`, `deviceName`, and `operation`.

---

## 6) UI behavior

`Ate.Ui/ViewModels/MainViewModel.cs`:

- loads capabilities on startup,
- allows selecting capability-defined device + operation,
- generates parameter inputs from metadata,
- posts commands through `AteClient`,
- polls status every second,
- exposes pause/resume/clear/abort commands.

---

## 7) Current configured devices

`Ate.Engine/engine-config.json` currently includes:

- `DMM::DMM`
- `PSU::PSU`
- `PSU::PSU2`

---

## 8) Extension docs

- Config-driven wrappers: `ADD_NEW_DRIVER.md`
- Direct plugin DLLs: `ADD_DLL_DRIVER.md`
- Headless client usage: `STANDALONE_ATECLIENT_GUIDE.md`
