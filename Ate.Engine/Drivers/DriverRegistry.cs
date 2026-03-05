using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ate.Engine.Drivers;

public sealed class DriverRegistry
{
    private readonly ConcurrentDictionary<string, Func<IDeviceDriver>> _factories =
        new ConcurrentDictionary<string, Func<IDeviceDriver>>(StringComparer.OrdinalIgnoreCase);

    public void Register(string deviceType, Func<IDeviceDriver> factory)
    {
        _factories[deviceType] = factory;
    }

    public bool TryResolve(string deviceType, out IDeviceDriver? driver)
    {
        if (_factories.TryGetValue(deviceType, out var factory))
        {
            driver = factory();
            return true;
        }

        driver = null;
        return false;
    }

    public IReadOnlyCollection<string> GetLoadedDeviceTypes()
    {
        return _factories.Keys.OrderBy(x => x).ToList();
    }
}
