using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Ate.Contracts;

namespace Ate.Engine.Drivers;

public sealed class DriverRegistry
{
    private readonly ConcurrentDictionary<string, DriverRegistration> _registrations =
        new ConcurrentDictionary<string, DriverRegistration>(StringComparer.OrdinalIgnoreCase);

    public void Register(string deviceType, string driverId, Func<IDeviceDriver> factory, DeviceCommandDefinition? definition = null)
    {
        var key = BuildKey(deviceType, driverId);
        _registrations[key] = new DriverRegistration(factory, definition);
    }

    public void RegisterInstance(IDeviceDriver driver, DeviceCommandDefinition? definition = null)
    {
        Register(driver.DeviceType, driver.DriverId, () => driver, definition);
    }

    public bool TryResolve(string deviceType, string? driverId, out IDeviceDriver? driver)
    {
        if (!string.IsNullOrWhiteSpace(driverId) &&
            _registrations.TryGetValue(BuildKey(deviceType, driverId), out var explicitRegistration))
        {
            driver = explicitRegistration.Factory();
            return true;
        }

        if (_registrations.TryGetValue(BuildKey(deviceType, "default"), out var defaultRegistration))
        {
            driver = defaultRegistration.Factory();
            return true;
        }

        var fallback = _registrations.FirstOrDefault(kvp =>
            kvp.Key.StartsWith(deviceType + "::", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(fallback.Key))
        {
            driver = fallback.Value.Factory();
            return true;
        }

        driver = null;
        return false;
    }

    public IReadOnlyCollection<string> GetLoadedDrivers()
    {
        return _registrations.Keys.OrderBy(x => x).ToList();
    }

    public IReadOnlyCollection<DeviceCommandDefinition> GetCommandDefinitions()
    {
        return _registrations.Values
            .Select(r => r.Definition)
            .Where(d => d != null)
            .Select(d => d!)
            .OrderBy(d => d.DeviceType)
            .ThenBy(d => d.DriverId)
            .ToList();
    }

    private static string BuildKey(string deviceType, string driverId)
    {
        return $"{deviceType}::{driverId}";
    }

    private sealed class DriverRegistration
    {
        public DriverRegistration(Func<IDeviceDriver> factory, DeviceCommandDefinition? definition)
        {
            Factory = factory;
            Definition = definition;
        }

        public Func<IDeviceDriver> Factory { get; }

        public DeviceCommandDefinition? Definition { get; }
    }
}
