# Add a DLL Driver Plugin (Direct Driver Path)

This guide explains the direct plugin DLL path for `Ate.Engine`.

Use this when you want to ship a compiled assembly loaded from the engine `drivers/` folder at startup.

> If you want config-driven constructor binding and per-device naming via `engine-config.json`, use `ADD_NEW_DRIVER.md` (configured-wrapper flow).

---

## 1) How DLL discovery works

At startup the engine:

1. Scans `<engine base dir>/drivers/*.dll`.
2. Loads assemblies (best effort; failures are logged).
3. Uses `DriverLoader` to find concrete `IDeviceDriver` types with public parameterless constructors.
4. Registers each discovered type into `DriverRegistry` under `deviceType::default`.

---

## 2) Required contract for direct plugin drivers

Your plugin type should:

- Implement `Ate.Engine.Drivers.IDeviceDriver`.
- Be `public` and concrete.
- Expose a public parameterless constructor.
- Provide stable `DeviceType` and `DriverId` values.
- Implement `ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)`.

Minimal skeleton:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

namespace Vendor.MyDevicePlugin;

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
```

---

## 3) Important behavior differences vs configured wrappers

- Direct plugin drivers are currently registered without capability definitions (`DriverLoader` uses `Register(...)` without metadata).
- Result: direct plugin drivers appear in `GET /api/status` loaded keys, but **do not automatically appear in `GET /api/capabilities`** unless additional registration logic is added.
- Command routing still works if you send `deviceType` + `deviceName` where `deviceName` is `default` for these direct plugin registrations.

---

## 4) Create a plugin project

General setup:

1. Create a .NET class library compatible with engine runtime (`net472` compatible target/framework strategy).
2. Reference assemblies that contain `IDeviceDriver` (`Ate.Engine` output).
3. Implement one or more `IDeviceDriver` types.
4. Build Release output.

Notes:

- Keep third-party SDK DLL dependencies deployable with plugin output.
- Avoid machine-specific hardcoded paths.
- Keep `DeviceType` stable across releases.

---

## 5) Deploy plugin DLL

1. Build plugin.
2. Copy plugin output DLL(s) into:
   - `<engine base dir>/drivers/`
3. Restart engine.

---

## 6) Verify

```bash
# Check loaded registrations
curl http://localhost:9000/api/status

# Attempt plugin command (deviceName defaults to "default" for direct plugin path)
curl -X POST http://localhost:9000/api/command \
  -H "Content-Type: application/json" \
  -d '{"deviceType":"MYDEV","deviceName":"default","operation":"Identify","parameters":{}}'
```

If command fails, inspect engine logs for assembly load, type load, or runtime operation errors.

---

## 7) Troubleshooting checklist

### Plugin not loaded

- DLL not placed under `<engine base dir>/drivers/`.
- Missing dependent DLL prevented assembly/type load.
- Driver type is not public/concrete.
- No public parameterless constructor.

### Command cannot resolve driver

- Request `deviceType` mismatch.
- `deviceName` must match actual registration key (direct plugin path uses `default`).

### Command fails at runtime

- `operation` not handled by plugin.
- Parameter conversion/parsing logic mismatch.
- Vendor SDK not available on target machine.

---

## 8) When to choose each path

Use **direct plugin DLL** when:

- You want a self-contained plugin implementation.
- You are fine handling operation dispatch internally.

Use **configured-wrapper path** (`ADD_NEW_DRIVER.md`) when:

- You need config-driven per-device instances (`deviceName` in `engine-config.json`).
- You want DI-injected hardware services.
- You want reflection/contract capability metadata published through `/api/capabilities`.
