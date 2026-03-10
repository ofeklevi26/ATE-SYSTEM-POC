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

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        var address = DriverConfigurationValueResolver.ResolveAddress(configuration);
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("DMM provider requires a non-empty address. Set settings.address, settings.resourceName, settings.ip, or ip.");
        }

        var channel = DriverConfigurationValueResolver.ResolveChannel(configuration);
        var endpoint = BuildEndpoint(configuration, address, channel);

        var wrapper = new DmmDeviceWrapper(configuration.DriverId, address, channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper),
            Description = $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{channel}"
        };
    }

    private static string BuildEndpoint(DriverInstanceConfiguration configuration, string address, int channel)
    {
        if (DriverConfigurationValueResolver.TryGetSetting(configuration, "endpoint", out var endpoint))
        {
            return endpoint;
        }

        if (DriverConfigurationValueResolver.TryGetSetting(configuration, "endpointFormat", out var endpointFormat))
        {
            var portValue = DriverConfigurationValueResolver.ResolvePort(configuration);
            var tokens = DriverConfigurationValueResolver.BuildDefaultTokens(configuration, address, channel, portValue);
            return DriverConfigurationValueResolver.ApplyTemplate(endpointFormat, tokens);
        }

        var isResourceAddress = DriverConfigurationValueResolver.TryGetSetting(configuration, "resourceName", out _);
        if (!isResourceAddress)
        {
            var port = DriverConfigurationValueResolver.ResolvePort(configuration);
            if (port.HasValue)
            {
                return $"{address}:{port.Value}";
            }
        }

        return address;
    }
}
