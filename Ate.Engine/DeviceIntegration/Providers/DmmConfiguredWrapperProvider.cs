using System;
using Ate.Engine.Configuration;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;
using Ate.Engine.Wrappers;

namespace Ate.Engine.DeviceIntegration.Providers;

public sealed class DmmConfiguredWrapperProvider : IConfiguredWrapperProvider
{
    private readonly IDmmHardwareDriverFactory _hardwareDriverFactory;

    public DmmConfiguredWrapperProvider(IDmmHardwareDriverFactory hardwareDriverFactory)
    {
        _hardwareDriverFactory = hardwareDriverFactory;
    }

    public string Name => "DMM";

    public bool CanCreate(DriverInstanceConfiguration configuration)
    {
        return configuration.DeviceType.Equals("DMM", StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperValidationResult Validate(DriverInstanceConfiguration configuration)
    {
        if (!DmmConnectionSettings.TryResolveAddress(configuration, out _))
        {
            return ConfiguredWrapperValidationResult.Fail("Missing address. Set settings.address or settings.resourceName.");
        }

        return ConfiguredWrapperValidationResult.Success();
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        if (!DmmConnectionSettings.TryResolveAddress(configuration, out var address))
        {
            throw new InvalidOperationException("DMM provider requires settings.address or settings.resourceName.");
        }

        var channel = DmmConnectionSettings.ResolveChannel(configuration);
        var endpoint = DmmConnectionSettings.BuildEndpoint(configuration, address, channel);

        var wrapper = new DmmDeviceWrapper(configuration.DriverId, address, channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper)
        };
    }

    public string Describe(DriverInstanceConfiguration configuration)
    {
        if (!DmmConnectionSettings.TryResolveAddress(configuration, out var address))
        {
            return $"{Name}::{configuration.DriverId}";
        }

        var channel = DmmConnectionSettings.ResolveChannel(configuration);
        var endpoint = DmmConnectionSettings.BuildEndpoint(configuration, address, channel);
        return $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{channel}";
    }
}
