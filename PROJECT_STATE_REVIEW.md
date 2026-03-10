# ATE-SYSTEM-POC — Current State Review (Post-Simplification)

This review reflects the **current simplified architecture** where adding a device family is intentionally minimal.

## What changed conceptually

The engine no longer uses per-device configured-wrapper providers (`IConfiguredWrapperProvider`) and per-device connection helper classes.

Configured wrappers are now created by a generic flow:
1. Module registers a `ConfiguredWrapperDescriptor(deviceType, wrapperType)`.
2. `engine-config.json` provides `deviceType`, optional `wrapperType`, `driverId`, and `settings`.
3. `ConfiguredWrapperFactory` reflectively builds wrapper constructor args from config + DI.
4. `WrapperOperationRuntime` auto-discovers `[DriverOperation]` methods for capabilities.

This reduces “new driver overhead” to:
- wrapper,
- module registration,
- config entry,
- optional hardware adapter.

---

## 1) High-level architecture

Projects:
- **Ate.Contracts**: shared DTOs/contracts.
- **Ate.Engine**: command queue, driver registry, API host, wrapper runtime.
- **Ate.Ui**: dynamic client over HTTP contracts.

Runtime flow:
1. UI loads capabilities (`/api/capabilities`) and renders operation forms dynamically.
2. UI submits command (`/api/command`).
3. Engine queue executes command against resolved wrapper.
4. Wrapper method invocation is reflection-driven via `[DriverOperation]`.

---

## 2) Driver integration model (current)

### Required building blocks
1. `IDeviceDriver` wrapper class.
2. `IDriverModule` registration with:
   - hardware dependencies (if needed),
   - `ConfiguredWrapperDescriptor` mapping.
3. Config entry in `engine-config.json`.

### Generic configured wrapper creation

`ConfiguredWrapperFactory` resolves constructor parameters using:
- `driverId` special handling,
- exact `settings` key by constructor parameter name,
- DI service resolution,
- optional default parameter values,
- special formatted keys for `endpoint`/`target` via `endpointFormat`/`targetFormat`.

This makes per-device provider classes unnecessary for most cases.

---

## 3) Startup wiring (EngineRuntime)

At startup:
1. Discover built-in and plugin `IDriverModule` implementations.
2. Build DI container.
3. Load `engine-config.json`.
4. Resolve `ConfiguredWrapperDescriptor` entries from DI.
5. For each configured driver:
   - choose descriptor by `wrapperType` override or `deviceType`,
   - instantiate wrapper via `ConfiguredWrapperFactory`,
   - register wrapper + auto-built capability metadata.
6. Load any raw `IDeviceDriver` plugin implementations.
7. Start command invoker and OWIN host.

---

## 4) Config model

`DriverInstanceConfiguration` now uses:
- `deviceType`
- `driverId`
- `wrapperType` (optional override)
- `settings` (constructor args)

Backward compatibility:
- legacy `wrapperProviderType` JSON is still accepted and mapped into `wrapperType`.

---

## 5) Current built-in device families

- **DMM**
  - Wrapper: `DmmDeviceWrapper`
  - Module: `DmmDriverModule`
  - Hardware demo: `DemoDmmHardwareDriver`

- **PSU**
  - Wrapper: `PsuDeviceWrapper`
  - Module: `PsuDriverModule`
  - Hardware demo: `DemoPsuHardwareDriver`

Both modules now register descriptors directly and do not register provider classes.

---

## 6) Why this is simpler and more modular

- Removes repetitive per-family provider/connection plumbing.
- Keeps extension surface small and predictable.
- Aligns with your requested workflow: **add driver wrapper, register in module, configure address/settings, done**.
- Preserves dynamic UI behavior via wrapper operation discovery.

---

## 7) Practical guidance for adding a new device now

1. Add hardware adapter interface/impl only if wrapper needs SDK abstraction.
2. Add wrapper class implementing `IDeviceDriver`.
3. Add `[DriverOperation]` methods for commands to expose in UI.
4. Add module registration:
   - hardware services,
   - `ConfiguredWrapperDescriptor("NEWTYPE", typeof(NewWrapper))`.
5. Add `engine-config.json` entry with settings matching wrapper constructor parameter names.

No provider class required.


---

## 8) Recommended next revisions (to reduce long-term maintenance cost)

1. **Introduce a tiny test project for wrapper bootstrapping**
   - Add focused tests for `ConfiguredWrapperFactory` constructor binding and type conversion.
   - Add one integration test for `EngineRuntime` config-to-registration flow.

2. **Split runtime composition into dedicated services**
   - Extract configured-wrapper registration from `EngineRuntime` into a dedicated `ConfiguredWrapperRegistrar` service.
   - Keep `EngineRuntime` as thin orchestration only.

3. **Standardize wrapper constructor conventions**
   - Document recommended constructor parameter names (`driverId`, `address`, `channel`, `endpoint`).
   - Avoid multiple public constructors in wrappers to prevent ambiguity.

4. **Optional plugin template package**
   - Provide a small reference driver module template in a separate project/repo.
   - New family then becomes: copy template, implement wrapper methods, update config.

5. **Keep config-only responsibilities in config**
   - Continue avoiding per-family provider classes unless a device truly needs highly custom boot logic.
   - For unusual cases, support optional custom binder extension rather than reintroducing full provider complexity.
