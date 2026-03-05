using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ate.Engine.Drivers;

public sealed class DriverRegistry
{
    private readonly ConcurrentDictionary<string, Func<IDeviceDriver>> _factories =
        new ConcurrentDictionary<string, Func<IDeviceDriver>>(StringComparer.OrdinalIgnoreCase);

    public void Register(string deviceType, string driverId, Func<IDeviceDriver> factory)
    {
        var key = BuildKey(deviceType, driverId);
        _factories[key] = factory;
    }

    public bool TryResolve(string deviceType, string? driverId, out IDeviceDriver? driver)
    {
        if (!string.IsNullOrWhiteSpace(driverId) &&
            _factories.TryGetValue(BuildKey(deviceType, driverId), out var explicitFactory))
        {
            driver = explicitFactory();
            return true;
        }

        if (_factories.TryGetValue(BuildKey(deviceType, "default"), out var defaultFactory))
        {
            driver = defaultFactory();
            return true;
        }

        var fallback = _factories.FirstOrDefault(kvp =>
            kvp.Key.StartsWith(deviceType + "::", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(fallback.Key))
        {
            driver = fallback.Value();
            return true;
        }

        driver = null;
        return false;
    }

    public IReadOnlyCollection<string> GetLoadedDrivers()
    {
        return _factories.Keys.OrderBy(x => x).ToList();
    }

    private static string BuildKey(string deviceType, string driverId)
    {
        return $"{deviceType}::{driverId}";
    }
}
