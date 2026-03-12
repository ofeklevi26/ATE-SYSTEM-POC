# Add a New Driver Family

This is the current end-to-end process for extending the engine.

## Checklist

1. Add hardware abstraction (optional, but recommended).
2. Add wrapper implementing `IDeviceDriver`.
3. Add `[DriverOperation]` methods on wrapper.
4. Add module implementing `IDriverModule`.
5. Register `ConfiguredWrapperDescriptor` in module.
6. Add `engine-config.json` entry.
7. Build and verify capabilities + command execution.

---

## 1) Add hardware interface/implementation (optional)

Create an interface under `Ate.Engine/DeviceIntegration/Hardware` and a concrete implementation under `DemoDrivers` (or real SDK adapter in plugin code).

Example:

```csharp
public interface ILoadHardwareDriver
{
    string Identify(string address, int channel);
    void Connect(string endpoint);
    void Disconnect();
}
```

---

## 2) Add wrapper implementing `IDeviceDriver`

Create `Ate.Engine/DeviceIntegration/Wrappers/LoadDeviceWrapper.cs`:

```csharp
public sealed class LoadDeviceWrapper : IDeviceDriver
{
    private readonly ILoadHardwareDriver _hardware;

    public LoadDeviceWrapper(string driverId, string address, int channel, string endpoint, ILoadHardwareDriver hardware)
    {
        DriverId = driverId;
        Address = address;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "LOAD";
    public string DriverId { get; }
    public string Address { get; }
    public int Channel { get; }
    public string Endpoint { get; }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _hardware.Connect(Endpoint);
        try
        {
            return WrapperOperationRuntime.InvokeAsync(this, operation, parameters, token);
        }
        finally
        {
            _hardware.Disconnect();
        }
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selected = channel ?? Channel;
        return _hardware.Identify(Address, selected);
    }
}
```

Notes:
- `DeviceType` should represent the family key.
- Exposed operations must be public and marked `[DriverOperation]`.
- For explicit/non-UI-friendly contracts, add a matching entry in `Ate.Contracts/KnownCapabilitiesCatalog.cs`; otherwise the engine will use reflection fallback for this family.


---

## 2.5) (Recommended) Add an explicit contract entry

For stable client integration, add your family to `Ate.Contracts/KnownCapabilitiesCatalog.cs` so operation/parameter schemas are explicit and reusable outside the WPF UI.

At minimum, define:
- `DeviceType`, `DriverId`, `DriverDisplayName`, `DriverDescription`
- all operation names and parameter names exactly as wrapper method signatures
- `Type`, optional `NumberFormat`, `IsRequired`, `Nullable`, and `DefaultValue` values that match runtime behavior

If omitted, `/api/capabilities` still works via reflection fallback, but the schema is not centrally versioned in contracts.

---

## 3) Add module implementing `IDriverModule`

Create `Ate.Engine/DeviceIntegration/Modules/LoadDriverModule.cs`:

```csharp
public sealed class LoadDriverModule : IDriverModule
{
    public string Name => "LOAD";

    public void Register(IServiceCollection services)
    {
        services.AddTransient<ILoadHardwareDriver, DemoLoadHardwareDriver>();
        services.AddSingleton(new ConfiguredWrapperDescriptor("LOAD", typeof(LoadDeviceWrapper)));
    }
}
```

This makes the wrapper discoverable by the configured-wrapper registrar.

---

## 4) Add config entry

In `Ate.Engine/engine-config.json`:

```json
{
  "deviceType": "LOAD",
  "driverId": "default",
  "wrapperType": "LOAD",
  "settings": {
    "address": "192.168.0.50",
    "port": "5025",
    "channel": "1",
    "endpointFormat": "tcp://{address}:{port}/ch/{channel}"
  }
}
```

### Constructor parameter binding order

`ConfiguredWrapperFactory` resolves each constructor argument using:
1. `driverId` special case
2. exact `settings` key by parameter name (case-insensitive)
3. `endpoint` / `target` special formatted values (`endpointFormat` / `targetFormat`)
4. DI resolution by parameter type
5. default value

If no constructor can be resolved (or constructors are ambiguous), wrapper creation fails.

---

## 5) Verify

1. Run engine.
2. `GET /api/capabilities` and confirm new device/operations appear.
3. `POST /api/command` for one operation.
4. `GET /api/status` to confirm queue and loaded drivers.

Example checks:

```bash
curl http://localhost:9000/api/capabilities
curl -X POST http://localhost:9000/api/command -H "Content-Type: application/json" -d '{"deviceType":"LOAD","operation":"Identify","parameters":{}}'
curl http://localhost:9000/api/status
```

---

Use `wrapperType` as the only wrapper selector key in configuration.

