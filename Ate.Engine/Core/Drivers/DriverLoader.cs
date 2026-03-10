using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Drivers;

public sealed class DriverLoader
{
    private readonly DriverRegistry _registry;
    private readonly ILogger _logger;

    public DriverLoader(DriverRegistry registry, ILogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public void LoadFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            LoadFromAssembly(assembly);
        }
    }

    private void LoadFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(IsDriverType))
        {
            RegisterType(type);
        }
    }

    private void RegisterType(Type type)
    {
        try
        {
            var sample = (IDeviceDriver)Activator.CreateInstance(type)!;
            _registry.Register(sample.DeviceType, sample.DriverId, () => (IDeviceDriver)Activator.CreateInstance(type)!);
            _logger.Info($"Registered driver '{type.FullName}' for device '{sample.DeviceType}' with id '{sample.DriverId}'.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to register driver type '{type.FullName}'.", ex);
        }
    }

    private static bool IsDriverType(Type t)
    {
        return !t.IsAbstract && typeof(IDeviceDriver).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null;
    }
}
