# Add a New Configured Driver Family

This is the current (config-driven) extension path used by `Ate.Engine`.

## Checklist

1. Add a hardware abstraction (optional but recommended).
2. Add a wrapper implementing `IDeviceDriver`.
3. Expose operations with `[DriverOperation]` methods.
4. Add a module implementing `IDriverModule`.
5. Register `ConfiguredWrapperDescriptor` in that module.
6. Add an `engine-config.json` entry (`deviceName`, `deviceType`, `settings`).
7. Build and verify via `/api/capabilities`, `/api/command`, `/api/status`.

---

## 1) Add hardware interface/implementation (optional)

Put interfaces under `Ate.Engine/DeviceIntegration/Hardware` and implementations under `DemoDrivers` (or your real SDK adapter location).

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

- `DeviceType` is the logical family key.
- Exposed operations must be public and marked `[DriverOperation]`.
- If the family should have strict/stable contracts, add it to `Ate.Contracts/KnownCapabilitiesCatalog.cs`.

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
  "deviceName": "LOAD1",
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

`ConfiguredWrapperFactory` resolves constructor args in this order:

1. `driverId` parameter gets config `deviceType`
2. exact `settings` key by parameter name (case-insensitive)
3. `endpoint`/`target` special handling (`endpoint`, `target`, or `endpointFormat`/`targetFormat`)
4. DI resolution by parameter type
5. default value

If no constructor can be resolved (or multiple are equally resolvable), startup fails for that wrapper.

### Device selection timing

- Configured wrappers are instantiated and registered at startup.
- The client selects the target instance per command using `deviceType` + `deviceName`.
- Command requests require `deviceName`.

---

## 5) Verify

```bash
curl http://localhost:9000/api/capabilities
curl -X POST http://localhost:9000/api/command -H "Content-Type: application/json" -d '{"deviceType":"LOAD","deviceName":"LOAD1","operation":"Identify","parameters":{}}'
curl http://localhost:9000/api/status
```

If the family is in `KnownCapabilitiesCatalog`, startup also validates wrapper signatures against that contract and throws on drift.
