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
        return configuration.DeviceType.Equals("PSU", System.StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        var endpoint = ConnectionEndpointResolver.Resolve(configuration);
        var wrapper = new PsuDeviceWrapper(configuration.DriverId, configuration.Ip, configuration.Channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper),
            Description = $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{configuration.Channel}"
        };
    }
}
