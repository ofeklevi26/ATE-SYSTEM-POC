using System.Collections.Generic;
using Ate.Contracts;
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
            Definition = BuildDefinition(configuration),
            Description = $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{configuration.Channel}"
        };
    }

    private static DeviceCommandDefinition BuildDefinition(DriverInstanceConfiguration cfg)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = cfg.DeviceType,
            DriverId = cfg.DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition
                {
                    Name = "channel",
                    Type = ParameterValueType.Integer,
                    IsRequired = true,
                    DefaultValue = cfg.Channel.ToString()
                }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "MeasureVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "range", Type = ParameterValueType.Decimal, DefaultValue = "10.0" }
                    }
                },
                new CommandOperationDefinition { Name = "Identify" }
            }
        };
    }
}
