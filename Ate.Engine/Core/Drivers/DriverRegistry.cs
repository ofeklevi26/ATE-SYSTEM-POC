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

    public void Register(
        string deviceType,
        string deviceName,
        Func<IDeviceDriver> factory,
        DeviceCommandDefinition? definition = null)
    {
        var key = BuildKey(deviceType, deviceName);
        _registrations[key] = new DriverRegistration(deviceName, factory, definition);
    }

    public void RegisterInstance(IDeviceDriver driver, string deviceName, DeviceCommandDefinition? definition = null)
    {
        Register(driver.DeviceType, deviceName, () => driver, definition);
    }

    public bool TryResolve(string deviceType, string? driverId, string deviceName, out IDeviceDriver? driver)
    {
        if (_registrations.TryGetValue(BuildKey(deviceType, deviceName), out var namedRegistration))
        {
            driver = namedRegistration.Factory();
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
            .ThenBy(d => d.DriverDisplayName)
            .ToList();
    }

    private static string BuildKey(string deviceType, string deviceName)
    {
        return $"{deviceType}::{deviceName}";
    }

    private sealed class DriverRegistration
    {
        public DriverRegistration(string deviceName, Func<IDeviceDriver> factory, DeviceCommandDefinition? definition)
        {
            DeviceName = deviceName;
            Factory = factory;
            Definition = definition;
        }

        public string DeviceName { get; }

        public Func<IDeviceDriver> Factory { get; }

        public DeviceCommandDefinition? Definition { get; }
    }
}
