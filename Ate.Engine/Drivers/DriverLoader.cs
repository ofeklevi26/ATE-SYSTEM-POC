using System;
using System.IO;
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

    public void LoadBuiltIns(params Type[] driverTypes)
    {
        foreach (var type in driverTypes)
        {
            RegisterType(type);
        }
    }

    public void LoadFromDirectory(string driversPath)
    {
        if (!Directory.Exists(driversPath))
        {
            _logger.Info($"Drivers directory not found, skipping: {driversPath}");
            return;
        }

        foreach (var dll in Directory.GetFiles(driversPath, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var type in asm.GetTypes().Where(IsDriverType))
                {
                    RegisterType(type);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load drivers from '{dll}'.", ex);
            }
        }
    }

    private void RegisterType(Type type)
    {
        try
        {
            var sample = (IDeviceDriver)Activator.CreateInstance(type)!;
            _registry.Register(sample.DeviceType, () => (IDeviceDriver)Activator.CreateInstance(type)!);
            _logger.Info($"Registered driver '{type.FullName}' for device '{sample.DeviceType}'.");
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
