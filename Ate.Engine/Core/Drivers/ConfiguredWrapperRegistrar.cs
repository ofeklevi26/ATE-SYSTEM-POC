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
        var seenDeviceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cfg in configuration.Drivers)
        {
            ValidateDriverConfiguration(cfg);
            var deviceKey = $"{cfg.DeviceType}::{cfg.DeviceName}";
            if (!seenDeviceKeys.Add(deviceKey))
            {
                throw new InvalidOperationException($"Duplicate configured device identifier '{deviceKey}'.");
            }

            var descriptor = ResolveDescriptor(cfg);
            if (descriptor == null)
            {
                _logger.Error($"No wrapper descriptor found for deviceType='{cfg.DeviceType}'.");
                continue;
            }

            try
            {
                var wrapper = ConfiguredWrapperFactory.Create(cfg, descriptor.WrapperType, _serviceProvider);
                var definition = WrapperOperationRuntime.BuildDefinition(wrapper);
                if (!string.IsNullOrWhiteSpace(cfg.DeviceName))
                {
                    definition.DriverDisplayName = cfg.DeviceName;
                }

                _registry.RegisterInstance(wrapper, cfg.DeviceName, definition);
                _logger.Info($"Registered configured wrapper '{descriptor.WrapperType.Name}' for {cfg.DeviceType}::{cfg.DeviceName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create configured wrapper '{descriptor.WrapperType.FullName}' for deviceName='{cfg.DeviceName}'.", ex);

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
        return _descriptors.FirstOrDefault(d => d.DeviceType.Equals(configuration.DeviceType, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateDriverConfiguration(DriverInstanceConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.DeviceType))
        {
            throw new InvalidOperationException($"Configured entry has an empty deviceType for deviceName '{configuration.DeviceName}'.");
        }

        if (string.IsNullOrWhiteSpace(configuration.DeviceName))
        {
            throw new InvalidOperationException($"Configured entry for deviceType '{configuration.DeviceType}' is missing required deviceName.");
        }
    }
}
