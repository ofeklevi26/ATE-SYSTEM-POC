# Add a DLL Driver Plugin

This guide explains the **direct plugin DLL path** for `Ate.Engine`.

Use this when you want to ship a driver as a compiled assembly that the engine loads from the `drivers/` folder at startup.

> If you want config-driven constructor binding (`engine-config.json`) and DI-wired hardware services, use the configured-wrapper flow in `ADD_NEW_DRIVER.md` instead.

---

## 1) How DLL discovery works

At startup, the engine:

1. Scans `<engine base dir>/drivers/*.dll`.
2. Loads discovered assemblies (best effort).
3. Finds plugin types implementing `IDeviceDriver`.
4. Registers those plugin drivers into `DriverRegistry`.

This is the "direct plugin driver" path, separate from configured wrappers.

---

## 2) Required contract for a direct plugin driver

Your plugin type should:

- Implement `Ate.Engine.DeviceIntegration.Contracts.IDeviceDriver`.
- Be `public` and concrete.
- Expose a **public parameterless constructor**.
- Provide stable `DeviceType` and `DriverId` values.
- Implement `ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)`.

A minimal skeleton:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.DeviceIntegration.Contracts;

namespace Vendor.MyDevicePlugin
{
    public sealed class MyDeviceDriver : IDeviceDriver
    {
        public string DeviceType => "MYDEV";
        public string DriverId => "default";

        public Task<object> ExecuteAsync(
            string operation,
            Dictionary<string, object> parameters,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (string.Equals(operation, "Identify", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>("MYDEV,PLUGIN,1.0");
            }

            throw new InvalidOperationException($"Unsupported operation '{operation}'.");
        }
    }
}
```

---

## 3) Capability visibility for plugin drivers

The UI and clients discover operations from `GET /api/capabilities`.

For plugin/unknown families, the engine can fall back to reflection-based capability discovery. To keep behavior predictable:

- Keep operation names stable.
- Keep parameter names/types stable.
- Return consistent result shapes.

If your family becomes long-lived, consider adding an explicit contract entry in `Ate.Contracts/KnownCapabilitiesCatalog.cs` (same recommendation as configured wrappers).

---

## 4) Create a plugin project

Create a .NET Class Library targeting a runtime compatible with the engine host.

General setup:

1. Create class library project.
2. Add references to the engine contracts assembly containing `IDeviceDriver`.
3. Implement one or more `IDeviceDriver` classes.
4. Build in `Release`.

Important notes:

- Keep third-party SDK dependencies alongside your plugin DLL (or otherwise resolvable at runtime).
- Avoid hardcoding machine-specific paths.
- Prefer deterministic `DeviceType` and `DriverId` constants.

---

## 5) Deploy plugin DLL

1. Build the plugin.
2. Copy plugin output DLL(s) into:
   - `<engine base dir>/drivers/`
3. Restart engine.

If your plugin needs additional vendor SDK DLLs, place those next to your plugin DLL unless your loader strategy provides another resolution path.

---

## 6) Verify end-to-end

After engine starts:

1. Check capabilities:

```bash
curl http://localhost:9000/api/capabilities
```

Confirm your `deviceType` + `driverId` are listed.

2. Execute a command:

```bash
curl -X POST http://localhost:9000/api/command \
  -H "Content-Type: application/json" \
  -d '{"deviceType":"MYDEV","driverId":"default","operation":"Identify","parameters":{}}'
```

3. Check status/logs:

```bash
curl http://localhost:9000/api/status
```

Review engine logs for plugin load or invocation errors.

---

## 7) Troubleshooting checklist

### Plugin does not appear in capabilities

- DLL is not in `<engine base dir>/drivers/`.
- Missing dependency DLL prevents type load.
- Driver class is not `public` or does not implement `IDeviceDriver`.
- No public parameterless constructor.

### Command cannot resolve driver

- Request `deviceType`/`driverId` do not exactly match plugin properties.
- Multiple plugins using same `(deviceType, driverId)` cause ambiguity/override risk.

### Command fails at runtime

- `operation` string not supported by `ExecuteAsync`.
- Parameter conversion mismatch in your plugin parsing logic.
- Vendor SDK connection settings unavailable on target machine.

---

## 8) Direct plugin vs configured-wrapper: when to choose which

Use **direct plugin DLL** when:

- You want minimal integration and self-contained plugin behavior.
- You can handle operation dispatch inside `ExecuteAsync`.

Use **configured-wrapper** (`ADD_NEW_DRIVER.md`) when:

- You need `engine-config.json`-driven constructor binding.
- You want DI-provided hardware services.
- You want `[DriverOperation]`-annotated wrapper methods and standardized wrapper conventions.

---

## 9) Practical recommendation

For production integrations, keep these stable from day 1:

- `DeviceType`
- `DriverId`
- operation names
- parameter names and expected types

That keeps `/api/capabilities`, client command payloads, and regression tests aligned as the plugin evolves.
