# Add a DLL Driver Plugin

This guide explains the direct plugin-DLL path for `Ate.Engine`.

Use this when you want to ship drivers as compiled assemblies loaded from `<engine base dir>/drivers`.

> If you want config-driven constructor binding from `engine-config.json`, use `ADD_NEW_DRIVER.md`.

---

## 1) How DLL discovery currently works

At startup, engine:

1. scans `<engine base dir>/drivers/*.dll`,
2. loads assemblies (best effort),
3. discovers:
   - `IDriverModule` implementations (for configured-wrapper registrations),
   - parameterless concrete `IDeviceDriver` implementations (direct registrations).

Direct plugin drivers are currently registered as `deviceType::default` in `DriverRegistry`.

---

## 2) Required contract for direct plugin drivers

Your direct plugin driver type should:

- implement `Ate.Engine.Drivers.IDeviceDriver`,
- be `public`, concrete, and have a **public parameterless constructor**,
- provide stable `DeviceType` and operation names,
- implement `ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)`.

Minimal skeleton:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

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

## 3) Capability visibility for plugin families

`GET /api/capabilities` is the source of truth.

- Known families use `KnownCapabilitiesCatalog`.
- Unknown families use reflection against `[DriverOperation]` methods.

For predictable client behavior, keep operation names and parameter names/types stable.

---

## 4) Create plugin project

1. Create a .NET class library compatible with engine runtime.
2. Reference assemblies containing `IDriverModule` and/or `IDeviceDriver`.
3. Implement plugin module/driver types.
4. Build in `Release`.

---

## 5) Deploy plugin DLL

1. Build plugin.
2. Copy plugin DLLs (and dependencies) to:
   - `<engine base dir>/drivers/`
3. Restart engine.

---

## 6) Verify

```bash
curl http://localhost:9000/api/capabilities
curl -X POST http://localhost:9000/api/command -H "Content-Type: application/json" -d '{"deviceType":"MYDEV","deviceName":"default","operation":"Identify","parameters":{}}'
curl http://localhost:9000/api/status
```

---

## 7) Troubleshooting

### Plugin not visible

- DLL not in `<engine base dir>/drivers`.
- Missing dependency prevents assembly/type load.
- Type is not `public`/concrete.
- Missing parameterless constructor for direct `IDeviceDriver` path.

### Command cannot resolve device

- Request `deviceType`/`deviceName` mismatch.
- For direct plugin drivers, use `deviceName: "default"` unless you provide configured entries.

### Command fails at runtime

- Operation name unsupported.
- Parameter conversion/type mismatch.
- Vendor SDK runtime dependency missing.
