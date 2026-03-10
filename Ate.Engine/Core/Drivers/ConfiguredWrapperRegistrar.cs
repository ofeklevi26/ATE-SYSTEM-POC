using System;
using System.Collections.Generic;
using System.Linq;
using Ate.Engine.Configuration;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Drivers;

public sealed class ConfiguredWrapperRegistrar
{
    private readonly IReadOnlyList<ConfiguredWrapperDescriptor> _descriptors;
    private readonly IServiceProvider _serviceProvider;
    private readonly DriverRegistry _registry;
    private readonly ILogger _logger;

    public ConfiguredWrapperRegistrar(
        IEnumerable<ConfiguredWrapperDescriptor> descriptors,
        IServiceProvider serviceProvider,
        DriverRegistry registry,
        ILogger logger)
    {
        _descriptors = descriptors.ToList();
        _serviceProvider = serviceProvider;
        _registry = registry;
        _logger = logger;
    }

    public void Register(EngineConfiguration configuration)
    {
        foreach (var cfg in configuration.Drivers)
        {
            var descriptor = ResolveDescriptor(cfg);
            if (descriptor == null)
            {
                _logger.Error($"No wrapper descriptor found for deviceType='{cfg.DeviceType}', wrapperType='{cfg.WrapperType ?? "(auto)"}'.");
                continue;
            }

            try
            {
                var wrapper = ConfiguredWrapperFactory.Create(cfg, descriptor.WrapperType, _serviceProvider);
                _registry.RegisterInstance(wrapper, WrapperOperationRuntime.BuildDefinition(wrapper));
                _logger.Info($"Registered configured wrapper '{descriptor.WrapperType.Name}' for {cfg.DeviceType}::{cfg.DriverId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create configured wrapper '{descriptor.WrapperType.FullName}' for driverId='{cfg.DriverId}'.", ex);

                if (IsContractDriftException(ex))
                {
                    throw;
                }
            }
        }
    }

    private static bool IsContractDriftException(Exception ex)
    {
        return ex is InvalidOperationException &&
               (ex.Message ?? string.Empty).IndexOf("KnownCapabilitiesCatalog drift", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private ConfiguredWrapperDescriptor? ResolveDescriptor(DriverInstanceConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.WrapperType))
        {
            return _descriptors.FirstOrDefault(d =>
                d.DeviceType.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase) ||
                d.WrapperType.FullName?.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase) == true ||
                d.WrapperType.Name.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase));
        }

        return _descriptors.FirstOrDefault(d => d.DeviceType.Equals(configuration.DeviceType, StringComparison.OrdinalIgnoreCase));
    }
}
