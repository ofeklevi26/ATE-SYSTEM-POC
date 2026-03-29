# Add a New Driver Family (Configured Wrapper Path)

This is the active end-to-end process for extending the engine with config-driven wrappers.

## Checklist

1. Add hardware abstraction (optional but recommended).
2. Add wrapper implementing `IDeviceDriver`.
3. Add `[DriverOperation]` methods to wrapper.
4. Add module implementing `IDriverModule`.
5. Register `ConfiguredWrapperDescriptor` in module.
6. Add `engine-config.json` entry with `deviceName` + `deviceType`.
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

- `DeviceType` is the family key (`LOAD`, `NiDaqMx`, `PSU`, etc.).
- Invokable operations must be public and marked with `[DriverOperation]`.
- Runtime command binding currently requires request payloads to include every operation parameter name.

---

## 2.5) (Recommended) Add explicit contract metadata

For stable client integration, add your family to `Ate.Contracts/KnownCapabilitiesCatalog.cs`.

At minimum define:

- `DeviceType`, `DriverId`, `DriverDisplayName`, `DriverDescription`
- operation names and parameter names matching wrapper signatures
- parameter `Kind`, optional `NumberFormat`, `Nullable`, and `Default`

If omitted, `/api/capabilities` still works for that family through reflection fallback.

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

---

## 4) Add config entry

In `Ate.Engine/engine-config.json`:

```json
{
  "deviceName": "LOAD_A",
  "deviceType": "LOAD",
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

1. `driverId` special case => config `deviceType`
2. exact `settings` key by parameter name (case-insensitive)
3. `endpoint` / `target` formatted value (`endpointFormat` / `targetFormat`) or direct key value
4. DI resolution by parameter type
5. default constructor value

If no constructor can be resolved (or constructors are ambiguous), wrapper creation fails.

### Device selection timing

- Startup: wrappers are instantiated from `engine-config.json` and registered by key `deviceType::deviceName`.
- Command time: client must send `deviceType` + `deviceName` in `POST /api/command`.
- There is no automatic default selection for configured wrappers.

---

## 5) Verify

1. Run engine.
2. `GET /api/capabilities` and confirm the new device appears.
3. `POST /api/command` for one operation.
4. `GET /api/status` and confirm queue/loaded drivers.

Example:

```bash
curl http://localhost:9000/api/capabilities
curl -X POST http://localhost:9000/api/command \
  -H "Content-Type: application/json" \
  -d '{"deviceType":"LOAD","deviceName":"LOAD_A","operation":"Identify","parameters":{"channel":1}}'
curl http://localhost:9000/api/status
```
