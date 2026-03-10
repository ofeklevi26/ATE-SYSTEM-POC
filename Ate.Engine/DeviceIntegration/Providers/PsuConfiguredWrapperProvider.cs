using System;
using Ate.Engine.Configuration;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;
using Ate.Engine.Wrappers;

namespace Ate.Engine.DeviceIntegration.Providers;

public sealed class PsuConfiguredWrapperProvider : IConfiguredWrapperProvider
{
    private readonly IPsuHardwareDriverFactory _hardwareDriverFactory;

    public PsuConfiguredWrapperProvider(IPsuHardwareDriverFactory hardwareDriverFactory)
    {
        _hardwareDriverFactory = hardwareDriverFactory;
    }

    public string Name => "PSU";

    public bool CanCreate(DriverInstanceConfiguration configuration)
    {
        return configuration.DeviceType.Equals("PSU", StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperValidationResult Validate(DriverInstanceConfiguration configuration)
    {
        if (!PsuConnectionSettings.TryResolveAddress(configuration, out _))
        {
            return ConfiguredWrapperValidationResult.Fail("Missing address. Set settings.address or settings.resourceName.");
        }

        return ConfiguredWrapperValidationResult.Success();
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        if (!PsuConnectionSettings.TryResolveAddress(configuration, out var address))
        {
            throw new InvalidOperationException("PSU provider requires settings.address or settings.resourceName.");
        }

        var channel = PsuConnectionSettings.ResolveChannel(configuration);
        var endpoint = PsuConnectionSettings.BuildEndpoint(configuration, address, channel);

        var wrapper = new PsuDeviceWrapper(configuration.DriverId, address, channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper)
        };
    }

    public string Describe(DriverInstanceConfiguration configuration)
    {
        if (!PsuConnectionSettings.TryResolveAddress(configuration, out var address))
        {
            return $"{Name}::{configuration.DriverId}";
        }

        var channel = PsuConnectionSettings.ResolveChannel(configuration);
        var endpoint = PsuConnectionSettings.BuildEndpoint(configuration, address, channel);
        return $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{channel}";
    }
}
