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
        return configuration.DeviceType.Equals("DMM", System.StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        var endpoint = ConnectionEndpointResolver.Resolve(configuration);
        var wrapper = new DmmDeviceWrapper(configuration.DriverId, configuration.Ip, configuration.Channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper),
            Description = $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{configuration.Channel}"
        };
    }
}
