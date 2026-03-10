# Add a New Driver Family (Current Architecture)

This guide explains the **minimal** integration flow in this repository.

## Goal
Add a new device family with dynamic UI operations, without per-device provider classes.

You only need:
1. a wrapper (`IDeviceDriver`) with `[DriverOperation]` methods,
2. a module (`IDriverModule`) that registers hardware + descriptor,
3. a config entry in `Ate.Engine/engine-config.json`.

---

## 1) Create your wrapper

Create a wrapper class under `Ate.Engine/DeviceIntegration/Wrappers` (or plugin assembly) implementing `IDeviceDriver`.

Key rules:
- `DeviceType` should match your family key (for example `"LOAD"`).
- Expose commands via methods marked `[DriverOperation]`.
- Use constructor parameters you want sourced from `settings` (same names).

Example skeleton:

```csharp
public sealed class LoadDeviceWrapper : IDeviceDriver
{
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

    [DriverOperation]
    public object Identify() => _hardware.Identify(Address, Channel);
}
```

---

## 2) Register your module

Create/update an `IDriverModule` and register:
- hardware services your wrapper constructor needs,
- one `ConfiguredWrapperDescriptor("<DEVICE_TYPE>", typeof(<WRAPPER_TYPE>))`.

Example:

```csharp
public sealed class LoadDriverModule : IDriverModule
{
    public string Name => "LOAD";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<ILoadHardwareDriverFactory, DemoLoadHardwareDriverFactory>();
        services.AddTransient<ILoadHardwareDriver>(sp => sp.GetRequiredService<ILoadHardwareDriverFactory>().Create());
        services.AddSingleton(new ConfiguredWrapperDescriptor("LOAD", typeof(LoadDeviceWrapper)));
    }
}
```

---

## 3) Add config in `engine-config.json`

Add a driver entry:

```json
{
  "deviceType": "LOAD",
  "driverId": "default",
  "wrapperType": "LOAD",
  "settings": {
    "address": "192.168.0.50",
    "channel": "1",
    "endpointFormat": "tcp://{address}:5025/ch/{channel}"
  }
}
```

### How constructor binding works
`ConfiguredWrapperFactory` resolves wrapper constructor args in this order:
1. `driverId` parameter from config `driverId`.
2. exact `settings[parameterName]`.
3. special `endpoint` or `target` via `endpoint`/`target` or `endpointFormat`/`targetFormat`.
4. DI service by parameter type.
5. default parameter value.

If no constructor can be resolved or multiple are ambiguous, startup fails fast with a clear error.

---

## 4) Run and verify

1. Start engine.
2. Call `GET /api/capabilities` and confirm your `DeviceType` and operations appear.
3. Execute operations via `POST /api/command`.

UI operation forms are generated dynamically from wrapper `[DriverOperation]` methods.

---

## Notes
- Legacy config key `wrapperProviderType` is still accepted and mapped to `wrapperType`.
- Prefer one public constructor in wrappers to keep binding deterministic.
- Keep constructor parameter names stable (`driverId`, `address`, `channel`, `endpoint`) for predictable config wiring.
